using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Reflection.Metadata.Ecma335;
using System.Text.Json;

// a worker class contains the settings
// for an llm like temp and model
// as well as the payload settings
// like whether to include reasoning or not

namespace Iris.Core
{
    public class Worker
    {
        // default model
        string model_ = "xiomi/mimo-v2.5";
        public string model
        {
            get => model_;
            set 
            { 
                model_ = value; 
                ModelChanged();
            }
        }
        public string variant = "";
        public float temperature = 1;
        public float top_p = 1;
        public List<string> input_modality = ["text", "image"];
        public int context_length = 1_000_000;

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
                input_modality = ["text"];
                return; 
            }

            string modelDescription = await responce.Content.ReadAsStringAsync();
            using JsonDocument jsonDocument = JsonDocument.Parse(modelDescription);

            // we want to get the input modalities and context
            JsonElement architecture = jsonDocument.RootElement.GetProperty("data").GetProperty("architecture");
            JsonElement input_modalities = architecture.GetProperty("input_modalities");
            input_modality = input_modalities.Deserialize<List<string>>() ?? ["text"];
            context_length = int.Parse(jsonDocument.RootElement.GetProperty("data").GetProperty("context_length").ToString());
        }
    }
}