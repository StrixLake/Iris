
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HttpMethod = System.Net.Http.HttpMethod;
using HttpRequestMessage = System.Net.Http.HttpRequestMessage;

namespace Iris.Core
{
    // the DTO to use for conversion to json
    class JsonMessage
    {
        public JsonMessage(string role, string message)
        {
            this.role = role;
            content.Add(new Dictionary<string, object>
            {
                ["type"] = "text",
                ["text"] = message
            });
        }

        public JsonMessage(Message self)
        {
            // handle the processing of role, content and files
            // in the constructor
            role = self.Role;
            string text;

            if(role == "assistant") text = "<think>" + self.Reasoning + "</think> \n" + self.Content;
            else if(self.files.Count != 0) 
            {
                text = "";
                // append all the files in format <filename> filecontent </filename>
                foreach(Tuple<string, string> file in self.files)
                {
                    text += String.Format("<{0}> \n {1} \n </{0}> \n", file.Item1, file.Item2);
                }
                text += self.Content;
            }
            else text = self.Content;

            content.Add(new Dictionary<string, object>
            {
                ["type"] = "text",
                ["text"] = text
            });
        }

        public string role {get; set;}

        // it can contain either <string, string> for text
        // messages, or <string, dictionary<string, string>> for
        // image urls
        // since the json format for multimodal image llm is
        // content: [
        //  {
        //    type: 'text',
        //    text: "What's in this image?",
        //  },
        //  {
        //    type: 'image_url',
        //    image_url: {
        //      url: base64Image,
        //    },
        //  },
        //]
        public List<Dictionary<string, object>> content {get;set;} = [];
        
    }

    class Payload
    {
        public string? model {get;set;}
        public Dictionary<string, object> provider {get; set;} = [];
        public Dictionary<string, bool> usage{get;set;} = [];
        public string reasoning_effort {get;set;} = "max";
        public string session_id {get;set;} = "iris-session";
        public bool stream {get;set;} = true;
        public int max_tokens {get;set;} = 100_000;

        public List<JsonMessage> messages {get;set;} = [];

        public Payload(string Model, string prefered_provider)
        {
            model = Model;

            if (prefered_provider != "") provider.Add("order", new List<string>{prefered_provider});

            // add the usage include to get provider info
            usage.Add("include", true);

        }
    }

    // the client class just takes a payload string
    // and generates the message
    public class Client
    {
        static HttpClient client;
        static Client()
        {
            string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string apiKey = Path.Combine(documents, ".iris", "apikey.txt");
            using StreamReader reader = new(apiKey);
            string api = reader.ReadLine() ?? "";

            client = new HttpClient();
            client.BaseAddress = new Uri("https://openrouter.ai/api/v1/");
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", api);
            client.DefaultRequestHeaders.Add("X-OpenRouter-Title", "Iris");
            client.DefaultRequestHeaders.Add("HTTP-Referer", "https://github.com/StrixLake/Iris"); // the api needs this header otherwise
                                                                                                   // it won't display the name in usage logs
        }

        public static async Task SendMessage(string json, Message assistantMessage, CancellationToken cancellationToken)
        {
            assistantMessage.messageLog!.Status = "Starting...";

            StringContent payload = new StringContent(json, Encoding.UTF8, "application/json");
            
            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
                                                    {
                                                        Content = payload
                                                    };

            Stopwatch watch = Stopwatch.StartNew();

            using HttpResponseMessage response = await client.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            response.EnsureSuccessStatusCode();

            using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            // we need a steeam reader to handle text conversion
            using StreamReader reader = new(stream);

            assistantMessage.messageLog!.Latency = watch.ElapsedMilliseconds;
            watch.Restart();
            
            assistantMessage.messageLog!.Status = "Streaming...";
            bool was_thinking = true;

            string? line;
            // if it's null, then we've reached the end of stream
            while((line = await reader.ReadLineAsync(cancellationToken)) != null)
            {
                
                // the json responce for a single line is
                // data: [json]
                // {
                //     "id": "gen-1784607249-PXmndw5X5CLJGZ2WhpAz",
                //     "object": "chat.completion.chunk",
                //     "created": 1784607249,
                //     "model": "deepseek/deepseek-v4-flash",
                //     "provider": "DeepSeek",
                //     "system_fingerprint": "fp_8b330d02d0_prod0820_fp8_kvcache_20260402",
                //     "choices": [
                //         {
                //             "index": 0,
                //             "delta": {
                //                 "content": "",
                //                 "role": "assistant",
                //                 "reasoning": "对话",
                //                 "reasoning_details": [
                //                     {
                //                         "type": "reasoning.text",
                //                         "text": "对话",
                //                         "format": "unknown",
                //                         "index": 0
                //                     }
                //                 ]
                //             },
                //             "finish_reason": null,   // this will either be stop or other reason in last response
                //             "native_finish_reason": null
                //         }
                //     ],
                //      "usage": {      // the usage object is only present in the last response
                //           "prompt_tokens": 5,
                //           "completion_tokens": 140,
                //           "total_tokens": 145,
                //           "cost": 0.0000399,
                //           "is_byok": false,
                //           "prompt_tokens_details": {
                //               "cached_tokens": 0,
                //               "cache_write_tokens": 0,
                //               "audio_tokens": 0,
                //               "video_tokens": 0
                //           },
                //           "cost_details": {
                //               "upstream_inference_cost": 0.0000399,
                //               "upstream_inference_prompt_cost": 7e-7,
                //               "upstream_inference_completions_cost": 0.0000392
                //           },
                //           "completion_tokens_details": {
                //               "reasoning_tokens": 81,
                //               "image_tokens": 0,
                //               "audio_tokens": 0
                //            }
                //      },
                // }

                if(line.StartsWith("data:") && !line.EndsWith("[DONE]"))
                {
                    line = line.Substring("data: ".Length);
                    Dictionary<string, JsonElement> api_responce = JsonSerializer.Deserialize<Dictionary<string,JsonElement>>(line) ?? [];

                    JsonElement choice = api_responce["choices"].EnumerateArray().First();
                    Dictionary<string, JsonElement> delta = JsonSerializer.Deserialize<Dictionary<string,JsonElement>>(choice.GetProperty("delta")) ?? [];

                    bool not_finished_reasoning = delta.TryGetValue("reasoning", out JsonElement value);
                    assistantMessage.Reasoning += value.ToString();
                    assistantMessage.Content += delta["content"].ToString();

                    if(not_finished_reasoning && value.ValueKind != JsonValueKind.Null) assistantMessage.messageLog!.Reasoning_Status = "Thinking... " + watch.Elapsed.ToString(@"mm\:ss");
                    else if (was_thinking)
                    {
                        was_thinking = false;
                        assistantMessage.messageLog!.Reasoning_Status = "Thought for " + watch.Elapsed.ToString(@"mm\:ss");
                    }

                    if (api_responce.ContainsKey("usage"))
                    {
                        assistantMessage.messageLog!.Status = "Finished";
                        Dictionary<string, JsonElement> usage = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(api_responce["usage"]) ?? [];
                        assistantMessage.messageLog!.Prompt = usage["prompt_tokens"].GetInt32();
                        assistantMessage.messageLog!.Total = usage["completion_tokens"].GetInt32();
                        assistantMessage.messageLog!.Cost = usage["cost"].GetSingle();
                        assistantMessage.messageLog!.Provider = api_responce["provider"].ToString();
                        assistantMessage.messageLog!.Speed = usage["completion_tokens"].GetInt32() / watch.Elapsed.Seconds;
                        assistantMessage.messageLog!.Cached = usage["prompt_tokens_details"].GetProperty("cached_tokens").GetInt32();
                        assistantMessage.messageLog!.Reasoning = usage["completion_tokens_details"].GetProperty("reasoning_tokens").GetInt32();
                        assistantMessage.messageLog!.Responce = assistantMessage.messageLog!.Total - assistantMessage.messageLog!.Reasoning;
                    }
                }

            }

        }

    }

}