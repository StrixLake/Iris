using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection.Metadata;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Xml.Schema;

// all workers are grouped in
// a session and provides
// tool calling, history and so on
// it also interfaces with the ui
// to decide which elements to show

namespace Iris.Core
{
    public class Session
    {
        public string source_directory = "";
        
        // each list of messages is associated with a worker
        // that will generate the next message
        public List<Tuple<List<Message>, Worker>> subWorkers = [];

        // there is 1 worker that the user interacts with
        public Tuple<List<Message>, Worker> worker = new([], new());

        public static string system_prompt = "";

        // don't save the session if it's temperary
        public bool temperary = false;

        public CancellationTokenSource cancelToken = new();

        static string GeneratePayload(List<Message> context, Worker worker)
        {
            // for every message in context, we generate a JsonMessage object, put
            // it in a list in payload object, and then serialise it
            // we check if the worker supports image input to decide if
            // we put the image urls inside the context or not

            Payload payload = new(worker);
            // system prompt
            payload.messages.Add(new("system", system_prompt));

            foreach (Message message in context)
            {
                JsonMessage jsonMessage = new(message);
                if(worker.input_modality.Contains("image"))
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

        async void SendMessage(string prompt, List<Tuple<string, string>> files, List<string> images, List<Message> context, Worker worker)
        {
            Message userMessage = new()
            {
                text = prompt
            };

            foreach (var file in files)
            {
                userMessage.files.Add(file);
            }

            foreach(string image in images)
            {
                userMessage.images.Add(image);
            }

            context.Add(userMessage);

            string payload = GeneratePayload(context, worker);

            Message assistantMessage = new(){role = "assistant"};
            // add the assistant message here so it's visible in the ui
            context.Add(assistantMessage);

            cancelToken.Dispose();
            cancelToken = new();
            await Client.SendMessage(payload, assistantMessage, cancelToken.Token);
        }
    }

    public partial class Message : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        public string role = "user";
        string reasoning_ = "";
        public string reasoning 
        {
            get => reasoning_;
            set 
            {
                reasoning_ = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(reasoning)));
            }
        }
        
        string text_ = "";
        public string text 
        {
            get => text_;
            set 
            {
                text_ = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(text)));
            }
        }

        bool streaming_ = false;
        public bool streaming
        {
            get => streaming_;
            set
            {
                streaming_ = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(streaming)));
            }
        }

        // base64 representation of images
        public ObservableCollection<string> images = [];

        // <filename, content>
        public ObservableCollection<Tuple<string, string>> files = [];
    }

}