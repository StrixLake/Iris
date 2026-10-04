using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml.Input;

namespace Iris.Core
{
    public partial class Message : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        string role = "user";
        string reasoning = "";
        string content = "";
        bool streaming = false;
        // base64 representation of images
        public ObservableCollection<string> images = [];
        // <filename, content>
        public ObservableCollection<Tuple<string, string>> files = [];

        // only needed for assistant messages
        public Logs? messageLog {get; set;}

        public string Role
        {
            get => role;
            set => OnPropertyChanged(ref role, value);
        }

        public string Reasoning
        {
            get => reasoning;
            set => OnPropertyChanged(ref reasoning, value);
        }

        public string Content
        {
            get => content;
            set => OnPropertyChanged(ref content, value);
        }

        public bool Streaming
        {
            get => streaming;
            set => OnPropertyChanged(ref streaming, value);
        }

        void OnPropertyChanged<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }

    public partial class Logs : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        string status = "";
        float cost;
        int prompt_tokens;
        int cached_tokens;
        int reasoning_tokens;
        int responce_tokens;
        int total_tokens;
        float latency;
        float speed;
        float clientLatency;
        string reasoning_status = "Thought";
        string provider = "";
        string finish_reason = "";


        public string Status
        {
            get => status;
            set => OnPropertyChanged(ref status, value);
        }

        public float Cost
        {
            get => cost;
            set => OnPropertyChanged(ref cost, value);
        }

        public int Prompt
        {
            get => prompt_tokens;
            set => OnPropertyChanged(ref prompt_tokens, value);
        }

        public int Cached
        {
            get => cached_tokens;
            set => OnPropertyChanged(ref cached_tokens, value);
        }

        public int Reasoning
        {
            get => reasoning_tokens;
            set => OnPropertyChanged(ref reasoning_tokens, value);
        }

        public int Responce
        {
            get => responce_tokens;
            set => OnPropertyChanged(ref responce_tokens, value);
        }

        public int Total
        {
            get => total_tokens;
            set => OnPropertyChanged(ref total_tokens, value);
        }

        public string Finish_Reason
        {
            get => finish_reason;
            set => OnPropertyChanged(ref finish_reason, value);
        }

        public float Latency
        {
            get => latency;
            set => OnPropertyChanged(ref latency, value);
        }

        public float Speed
        {
            get => speed;
            set => OnPropertyChanged(ref speed, value);
        }

        public float ClientLatency
        {
            get => clientLatency;
            set => OnPropertyChanged(ref clientLatency, value);
        }

        public string Reasoning_Status
        {
            get => reasoning_status;
            set => OnPropertyChanged(ref reasoning_status, value);
        }

        public string Provider
        {
            get => provider;
            set => OnPropertyChanged(ref provider, value);
        }

        void OnPropertyChanged<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}