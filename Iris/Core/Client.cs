
using System;
using System.Collections.Generic;
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
            role = self.role;
            string text;

            if(role == "assistant") text = "<think>" + self.reasoning + "</think> \n" + self.text;
            else if(self.files.Count != 0) 
            {
                text = "";
                // append all the files in format <filename> filecontent </filename>
                foreach(Tuple<string, string> file in self.files)
                {
                    text += String.Format("<{0}> \n {1} \n </{0}> \n", file.Item1, file.Item2);
                }
                text += self.text;
            }
            else text = self.text;

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
        public float temperature {get;set;}
        public float top_p {get;set;}
        public Dictionary<string, string> provider {get; set;} = [];
        public Dictionary<string, bool> usage{get;set;} = [];
        public string reasoning_effort {get;set;} = "max";
        public string session_id {get;set;} = "iris-session";
        public bool stream {get;set;} = true;
        public int max_tokens {get;set;} = 100_000;

        public List<JsonMessage> messages {get;set;} = [];

        public Payload(Worker worker)
        {
            model = worker.model;
            if(worker.variant != "") model += ":" + worker.variant;

            temperature = worker.temperature;
            top_p = worker.top_p;

            // for deepseek modals, i want to use deepseek provider
            // because it is so much cheaper
            if(worker.model.StartsWith("deepseek"))
            {
                provider.Add("only", "DeepSeek");
            }

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
            StringContent payload = new StringContent(json, Encoding.UTF8, "application/json");
            
            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
                                                    {
                                                        Content = payload
                                                    };

            HttpResponseMessage response = await client.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            response.EnsureSuccessStatusCode();

            using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            // we need a steeam reader to handle text conversion
            using StreamReader reader = new(stream);
            
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
                    Dictionary<string, string> delta = JsonSerializer.Deserialize<Dictionary<string,string>>(choice.GetProperty("delta")) ?? [];

                    delta.TryGetValue("reasoning", out string? value);
                    assistantMessage.reasoning += value;
                    assistantMessage.text += delta["content"];

                    if (api_responce.ContainsKey("usage"))
                    {
                        Dictionary<string, JsonElement> usage = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(api_responce["usage"]) ?? [];
                        int prompt_tokens = usage["prompt_tokens"].GetInt32();
                    }
                }

            }

        }

    }

}