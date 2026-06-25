using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Windows.Foundation.Metadata;

namespace Iris
{
    [JsonSourceGenerationOptions(WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonSerializable(typeof(Message))]
    [JsonSerializable(typeof(List<Message>))]
    [JsonSerializable(typeof(Chat))]
    [JsonSerializable(typeof(Binding_Message))]
    [JsonSerializable(typeof(ObservableCollection<Binding_Message>))]
    [JsonSerializable(typeof(Payload))]
    [JsonSerializable(typeof(Payload.Provider))]
    [JsonSerializable(typeof(List<string>))]
    [JsonSerializable(typeof(Response))]
    [JsonSerializable(typeof(Response.Choices))]
    [JsonSerializable(typeof(List<Response.Choices>))]
    [JsonSerializable(typeof(Response.Choices.Delta))]
    [JsonSerializable(typeof(Response.Usage))]
    internal partial class JsonContext : JsonSerializerContext { }

    public class Message
    {
        public Message(Binding_Message other) 
        {
            backingMessage = other;
        }
        public Binding_Message backingMessage;
        public string role
        {
            get
            { 
                return backingMessage.role;
            }
        }
        public string content
        {
            get
            {
                if(backingMessage.isAssistantMessage) return "<reasoning>" + backingMessage.reasoning + "<reasoning/>\n" + backingMessage.content;
                if (!backingMessage.isNotSystemPrompt) return "<System Prompt>" + Chat.system_prompt + "</System Prompt>";
                return backingMessage.content;
            }
        }

    }

    class Payload
    {
        public string? model { get; set; }

        [JsonInclude]
        public IList<Message> messages = [];
        [JsonInclude]
        public string reasoning_effort = "xhigh";
        [JsonInclude]
        public bool stream = true;
        [JsonInclude]
        public int max_tokens = 90000;
        
        public class Provider
        {
            public List<string>? only {get; set;}
            bool allow_fallbacks {get; set;} = false;
        }
        public Provider? provider {get; set;}
    }

    class Response
    {
        public class Choices
        {
            public string? finish_reason { get; set; }
            public class Delta
            {
                [JsonInclude]
                public string content = "";
                [JsonInclude]
                public string reasoning = "";
            }
            [JsonInclude]
            public Delta delta = new();
        }
        public class Usage
        { 
            public int completion_tokens { get; set; }
            public int prompt_tokens { get; set; }
            public int total_tokens { get; set; }
            public double cost { get; set; }
        }
        [JsonInclude]
        public List<Choices> choices = [];
        public Usage? usage { get; set; }
    }

    public enum ClientStatus
    {
        JSON_Serialiser_Begin, JSON_Serialiser_Failed, JSON_Serialiser_Success, 
        JSON_Deserialiser_Failed, Network_Error, Response_Error, Response_Success,
        Generation_Begin, Generation_End, Generation_Cancelled, Unknown_error
    }
    public class Client
    {
        public delegate void StatusUpdateEventHandler(ClientStatus status, string log);
        public Client()
        {

            client = new();
            client.BaseAddress = new Uri("https://openrouter.ai/api/v1/");
            client.DefaultRequestHeaders.Add("X-OpenRouter-Title", "Iris");
            client.DefaultRequestHeaders.Add("HTTP-Referer", "https://github.com/StrixLake/Iris"); // the api needs this header otherwise
                                                                                       // it won't display the name in usage logs
        }

        HttpClient client;
        public static string apikey = "";
        public event StatusUpdateEventHandler? StatusUpdate;

        public async Task ChatCompletion(Binding_Message response, List<Message> context, string model, CancellationToken cancellationToken)
        {
            Payload payload = new() { model = model, messages = context};

            if(model.StartsWith("deepseek")) payload.provider = new(){only=["DeepSeek"]};
            string json;

            StatusUpdate?.Invoke(ClientStatus.JSON_Serialiser_Begin, "Starting Serialisation");

            try { json = JsonSerializer.Serialize(payload, JsonContext.Default.Payload); }
            catch
            {
                StatusUpdate?.Invoke(ClientStatus.JSON_Serialiser_Failed, "Json Serializer Failed");
                return;
            }

            StatusUpdate?.Invoke(ClientStatus.JSON_Serialiser_Success, "Serialiser Finished");

            StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
            HttpRequestMessage request = new(HttpMethod.Post, "chat/completions") { Content = content};

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apikey);
            HttpResponseMessage incomplete_response;

            try { incomplete_response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken); }
            catch
            {
                StatusUpdate?.Invoke(ClientStatus.Network_Error, "Send Async Failed, check network");
                return;
            }

            try { incomplete_response.EnsureSuccessStatusCode(); }
            catch
            {
                using Stream stream_temp = await incomplete_response.Content.ReadAsStreamAsync();
                using StreamReader reader_temp = new StreamReader(stream_temp);
                string failed_response = await reader_temp.ReadToEndAsync();

                using JsonDocument response_json = JsonDocument.Parse(failed_response);
                JsonElement error = response_json.RootElement.GetProperty("error");
                string message = error.GetProperty("message").ToString();
                int code = error.GetProperty("code").GetInt32();

                StatusUpdate?.Invoke(ClientStatus.Response_Error, String.Format("Code: {0}. {1}", error, message));
                return;
            }

            StatusUpdate?.Invoke(ClientStatus.Response_Success, "Response Succeed");

            using Stream stream  = await incomplete_response.Content.ReadAsStreamAsync();
            using StreamReader reader = new StreamReader(stream);

            response.isAssistantMessage = true;

            StatusUpdate?.Invoke(ClientStatus.Generation_Begin, "Starting Message Streaming");

            string? line;
            try
            {
                while ((line = await reader.ReadLineAsync(cancellationToken)) != null)  // <-- no EndOfStream
                {
                    if (line.StartsWith("data: ") && !line.Contains("[DONE]"))
                    {
                        line = line["data: ".Length..];
                        Response api_response;

                        try { api_response = JsonSerializer.Deserialize<Response>(line, JsonContext.Default.Response) ?? new Response(); }
                        catch 
                        {
                            StatusUpdate?.Invoke(ClientStatus.JSON_Deserialiser_Failed, String.Format("Deserialisation failed. API returned: {0}", line));
                            return;
                        }
                        response.content += api_response.choices[0].delta.content ?? "";
                        response.reasoning += api_response.choices[0].delta.reasoning ?? "";
                    }
                }

                StatusUpdate?.Invoke(ClientStatus.Generation_End, "Message Streaming Finished");

            }
            catch (OperationCanceledException)
            {
                StatusUpdate?.Invoke(ClientStatus.Generation_Cancelled, "Message Streaming Cancelled");
            }
            catch
            {
                StatusUpdate?.Invoke(ClientStatus.Unknown_error, "Unknown Error Occured");
            }

            response.hasFinishedStreaming = true;

        }

    }
}
