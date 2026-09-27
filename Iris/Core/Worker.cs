using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;


namespace Iris.Core
{
    public partial class Worker : INotifyPropertyChanged
    {
        public Worker()
        {
            ModelChanged();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        
        string model = "xiaomi/mimo-v2.6";
        int context_length = 1_000_000;
        string description = "";
        public string id = "";
        
        public ObservableCollection<string> Inputs {get; set;} = ["text", "image"];
        public ObservableCollection<Message> Context {get; set;} = [];

        public string Model
        {
            get => model;
            set => OnPropertyChanged(ref model, value);
        }

        public int Context_Length
        {
            get => context_length;
            set => OnPropertyChanged(ref context_length, value);
        }

        public string Description
        {
            get => description;
            set => OnPropertyChanged(ref description, value);
        }

        public List<Worker>? splitWorkers;
        public List<Worker>? agents;
        public Worker? preSplitWorker;

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
        }

        void OnPropertyChanged<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}