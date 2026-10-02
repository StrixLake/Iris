using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using Windows.Foundation.Metadata;


namespace Iris.Core
{
    public partial class Worker : INotifyPropertyChanged
    {
        public Worker()
        {
            Model = models[0];
            ModelChanged();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        
        static ObservableCollection<string> models = ["xiaomi/mimo-v2.6-flash"];

        string model = "";
        int context_length = 1_000_000;
        string description = "";
        public string id = "";
        
        public ObservableCollection<string> Inputs {get; set;} = ["text", "image"];
        public ObservableCollection<Message> Context {get; set;} = [];

        public string Model
        {
            get => model;
            set 
            {
                OnPropertyChanged(ref model, value);
                ModelChanged();
            }
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

        static string GeneratePayload(List<Message> context, Worker worker)
        {
            Payload payload = new(worker.Model);

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

        async void ModelChanged()
        {
            using HttpClient client = new();
            client.BaseAddress = new Uri("https://openrouter.ai/api/v1/model/");

            HttpResponseMessage responce;

            try 
            {
                responce = await client.GetAsync(model);
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

            // we want to get the input modalities and context
            JsonElement architecture = jsonDocument.RootElement.GetProperty("data").GetProperty("architecture");
            JsonElement input_modalities = architecture.GetProperty("input_modalities");
            Inputs = input_modalities.Deserialize<ObservableCollection<string>>() ?? ["text"];
            Context_Length = int.Parse(jsonDocument.RootElement.GetProperty("data").GetProperty("context_length").ToString());
            Description = jsonDocument.RootElement.GetProperty("data").GetProperty("description").ToString();

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
        }

        void OnPropertyChanged<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}