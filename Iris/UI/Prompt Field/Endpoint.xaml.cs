using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http;
using System.Text.Json;
using CommunityToolkit.WinUI.UI.Controls.TextToolbarSymbols;
using Microsoft.UI.Xaml.Controls;
using Windows.System.Threading;
using Core = Iris.Core;

namespace Iris.UI
{
    public partial class EndpointPage : Page
    {
        readonly Core.Worker worker;
        public ObservableCollection<Endpoint> endpoints {get; set;} = [];

        public EndpointPage(Core.Worker worker)
        {
            InitializeComponent();
            this.worker = worker;
            worker.PropertyChanged += ModelChanged;
        }

        async void ModelChanged(object? sender, PropertyChangedEventArgs args)
        {
            if(args.PropertyName != nameof(worker.Model)) return;
            
            // get the providers for the new model
            endpoints.Clear();

            using HttpClient client = new();
            client.BaseAddress = new Uri("https://openrouter.ai/api/v1/models/");

            using HttpResponseMessage responseMessage = await client.GetAsync(worker.Model + "/endpoints");;

            try
            {
                responseMessage.EnsureSuccessStatusCode();
            }
            catch
            {
                return;
            }

            string content = await responseMessage.Content.ReadAsStringAsync();
            JsonElement rootElement = JsonDocument.Parse(content).RootElement;
            JsonElement providers = rootElement.GetProperty("data").GetProperty("endpoints");

            List<Endpoint> providerList = JsonSerializer.Deserialize<List<Endpoint>>(providers) ?? [];
            foreach(var provider in providerList)
            {
                endpoints.Add(provider);
            }
        }
    }



    // "endpoints": [
    //         {
    //             "name": "InferenceNet | z-ai/glm-5.3-20260816",
    //             "model_id": "z-ai/glm-5.3",
    //             "model_name": "Z.ai: GLM 5.3",
    //             "context_length": 1048576,
    //             "pricing": {
    //                 "prompt": "0.00000005",
    //                 "completion": "0.000005",
    //                 "input_cache_read": "0.00000004",
    //                 "discount": 0
    //             },
    //             "provider_name": "InferenceNet",
    //             "tag": "inference-net",
    //             "quantization": "unknown",
    //             "max_completion_tokens": 943718,
    //             "max_prompt_tokens": null,
    //             "supported_parameters": [
    //                 "reasoning",
    //                 "include_reasoning",
    //             ],

    public class Endpoint
    {
        public string provider_name {get; set;} = "";
        public int context_length {get; set;}
        public Pricing pricing {get; set;} = new();
        public string quantization {get; set;} = "";

    }

    public class Pricing
    {

        string prompt_ = "";
        string completion_ = "";
        string input_cache_read_ = "";

        public string prompt 
        {
            get => ToMillionTokens(prompt_ );
            set => prompt_ = value;
        }
        public string completion 
        {
            get => ToMillionTokens(completion_ );
            set => completion_ = value;
        }
        public string input_cache_read 
        {
            get => ToMillionTokens(input_cache_read_ );
            set => input_cache_read_ = value;
        }
        
        public float discount {get; set;}

        public string Discount
        {
            get => ((int)(-discount*100)).ToString() + " %";
        }

        static string ToMillionTokens(string price)
        {
            if (price == "") return "";
            float perToken = float.Parse(price);
            float perMillion = (float)((int)(perToken * 1_000_000_000)) / 1_000;
            return perMillion.ToString();
        }
    }
}
