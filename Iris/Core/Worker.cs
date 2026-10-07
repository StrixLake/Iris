using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using Iris.UI;
using Windows.Foundation.Metadata;


namespace Iris.Core
{
    public partial class Worker : INotifyPropertyChanged
    {
        static Worker()
        {
            string folder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string template = Path.Combine(folder, ".iris", "description_template.txt");
            using StreamReader streamReader = new StreamReader(template);
            descriptionTemplate = streamReader.ReadToEnd();

            string system = Path.Combine(folder, ".iris", "system_prompt.txt");
            using StreamReader streamReader2 = new StreamReader(system);
            system_prompt = streamReader2.ReadToEnd();

        }
        public Worker()
        {
            Model = models[0];
            ModelChanged();

            Message system_message = new()
            {
                Role = "system",
                Content = system_prompt
            };

            Context.Add(system_message);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        
        static ObservableCollection<string> models = ["xiaomi/mimo-v2.6-flash","z-ai/glm-5.2","nvidia/nemotron-3-ultra-550b-a55b","deepseek/deepseek-v4-pro","deepseek/deepseek-v4-flash","xiaomi/mimo-v2.5","deepseek/deepseek-v4-pro-0813","z-ai/glm-5.3"];
        readonly static string descriptionTemplate;
        readonly static string system_prompt;

        string model = "";
        string name = "";
        int context_length = 1_000_000;
        string description = "";
        public string Reasoning_Effort { get; set; } = "Low";
        public string id = "";
        public Logs lastLog = new();
        
        public ObservableCollection<string> Inputs {get; set;} = ["text", "image"];
        public ObservableCollection<Message> Context {get; set;} = [];

        public string prefered_provider {get; set;} = "";

        public string Model
        {
            get => model;
            set 
            {
                OnPropertyChanged(ref model, value);
                ModelChanged();
            }
        }

        public string Name
        {
            get => name;
            set => OnPropertyChanged(ref name, value);
        }

        public int Context_Length
        {
            get => context_length;
            set => OnPropertyChanged(ref context_length, value);
        }

        [JsonIgnore]
        public string Description
        {
            get => description;
            set => OnPropertyChanged(ref description, value);
        }

        public ObservableCollection<string> Models
        {
            get => models;
            set => OnPropertyChanged(ref models, value);
        }

        public List<Worker>? splitWorkers {get;set;}
        public List<Worker>? agents {get;set;}
        public Worker? preSplitWorker {get;set;}

        public List<Message> MessageBox = [];
        Mutex MessageBoxMutex = new();
        CancellationTokenSource cancellationToken = new();

        List<Message> GetContext()
        {
            List<Message> preSplitContext = [];
            if(preSplitWorker != null) preSplitContext = preSplitWorker.GetContext();
            preSplitContext.AddRange(Context);
            return preSplitContext;
        }

        static string GeneratePayload(ObservableCollection<Message> context, Worker worker)
        {
            Payload payload = new(worker.Model, worker.prefered_provider, worker.Reasoning_Effort);

            foreach (Message message in context)
            {
                JsonMessage jsonMessage = new(message);
                if(worker.Inputs.Contains("image"))
                {
                    foreach(string image in message.images)
                    {
                        jsonMessage.content.Add(new Dictionary<string, object>
                        {
                            ["type"] = "image_url",
                            ["image_url"] = new Dictionary<string, string>
                            {
                                ["image_url"] = image
                            }
                        });
                    }
                }
                payload.messages.Add(jsonMessage);
            }

            return JsonSerializer.Serialize(payload);
        }

        public async void SendMessage(Message? prompt)
        {
            // calculate the serializer time
            Stopwatch watch = Stopwatch.StartNew();
            if(prompt is not null) Context.Add(prompt);

            string payload = GeneratePayload(Context, this);
            Message assistantResponce = new()
            {
                Role = "assistant",
                messageLog = new()
            };
            lastLog = assistantResponce.messageLog;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(lastLog)));
            Context.Add(assistantResponce);
            lastLog.ClientLatency = watch.ElapsedMilliseconds;

            cancellationToken.Dispose();
            cancellationToken = new();
            assistantResponce.Streaming = true;
            await Client.SendMessage(payload, assistantResponce, cancellationToken.Token);
            assistantResponce.Streaming = false;
        }

        async void ModelChanged()
        {
            using HttpClient client = new();
            client.BaseAddress = new Uri("https://openrouter.ai/api/v1/model/");

            using HttpResponseMessage responce = await client.GetAsync(model);;

            try 
            {
                responce.EnsureSuccessStatusCode(); 
            }
            catch 
            {
                // if we can't get the info, then we assume that
                // only text can be input
                Inputs = ["text"];
                return; 
            }

            string modelDescription = await responce.Content.ReadAsStringAsync();
            using JsonDocument jsonDocument = JsonDocument.Parse(modelDescription);
            responce.Dispose();

            // we want to get the input modalities and context
            JsonElement architecture = jsonDocument.RootElement.GetProperty("data").GetProperty("architecture");
            JsonElement input_modalities = architecture.GetProperty("input_modalities");
            Inputs = input_modalities.Deserialize<ObservableCollection<string>>() ?? ["text"];
            Name = jsonDocument.RootElement.GetProperty("data").GetProperty("name").ToString();
            Context_Length = int.Parse(jsonDocument.RootElement.GetProperty("data").GetProperty("context_length").ToString());
            int created = int.Parse(jsonDocument.RootElement.GetProperty("data").GetProperty("created").ToString());
            string date_created = DateTimeOffset.FromUnixTimeSeconds(created).ToString("d MMMM, yyy");
            // pricing on the api is per token
            float inPrice = float.Parse(jsonDocument.RootElement.GetProperty("data").GetProperty("pricing").GetProperty("prompt").ToString()) * 1_000_000_000;
            inPrice = (float)(int)inPrice / 1_000;
            float outPrice = float.Parse(jsonDocument.RootElement.GetProperty("data").GetProperty("pricing").GetProperty("completion").ToString()) * 1_000_000_000;
            outPrice = (float)(int)outPrice / 1_000;

            // to get the real description, we need to get the html page
            // of the model, and then regex with a string
            // to extract the description

            using HttpClient anotherClient = new();
            HttpRequestMessage htmlRequest = new(HttpMethod.Get, "https://openrouter.ai/" + Model);

            var htmlResponse = await anotherClient.SendAsync(htmlRequest);
            string html = await htmlResponse.Content.ReadAsStringAsync();

            string regexPattern1 = "\"url\".*\"description\":\"(.*?)\"";
            string regexPattern2 = "(?<=description\":\").*(?=\")";

            Description = Regex.Match(Regex.Match(html, regexPattern1).Value, regexPattern2).Value;
            Description = Description.Replace("\\n", "\n");
            Description = String.Format(descriptionTemplate, Name, Context_Length.ToString("N0"), inPrice, outPrice, date_created, Description);
        }

        void OnPropertyChanged<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}