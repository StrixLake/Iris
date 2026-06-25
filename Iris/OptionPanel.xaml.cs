using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Iris
{

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class OptionPanel : Page, INotifyPropertyChanged
    {
        public OptionPanel()
        {
            InitializeComponent();
            Models = ["google/gemma-4-31b-it"];

            SelectedModel = Models[0];
        }

        public void ActiveChatChanged(Chat newChat)
        {
            activeChat = newChat;
            SelectedModel = activeChat.model;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedModel)));
        }

        async void GetModelDescription(object o, SelectionChangedEventArgs args)
        {
            string model = SelectedModel;
            if(activeChat != null) activeChat.model = model;
            using HttpClient client = new();
            client.BaseAddress = new Uri("https://openrouter.ai/api/v1/model/");

            HttpResponseMessage responce;
            try 
            {
                responce = await client.GetAsync(model);
                responce.EnsureSuccessStatusCode();
            }
            catch { return; }

            string modelDescription = await responce.Content.ReadAsStringAsync();
            using JsonDocument jsonDocument = JsonDocument.Parse(modelDescription);

            JsonElement data = jsonDocument.RootElement.GetProperty("data");
            string name = data.GetProperty("name").ToString();
            string description = data.GetProperty("description").ToString();
            string contextLength = double.Parse(data.GetProperty("context_length").ToString()).ToString("N0");
            string outputPrice = (double.Parse(data.GetProperty("pricing").GetProperty("completion").ToString()) * 1_000_000).ToString();
            string inputPrice = (double.Parse(data.GetProperty("pricing").GetProperty("prompt").ToString()) * 1_000_000).ToString();

            string modality = data.GetProperty("architecture").GetProperty("modality").ToString();

            ModelDescription = String.Format(@"## {0}
Context Length: {1}  
Input: ${2}  
Output: ${3}  
Modality: {4}  
### Description  
{5}", name, contextLength, inputPrice, outputPrice, modality, description); // there has to be 2 spaces at the end of each line
                                                                            // for the newlines to appear in the markdown textblock

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ModelDescription)));
            

        }
        private void AddModel(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            
            Models.Add(add_model.Text);
            SelectedModel = add_model.Text;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedModel)));
            
            // add the model in the local file
            string modelPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), ".iris");
            modelPath = System.IO.Path.Combine(modelPath, "models.txt");
            using StreamWriter modelFile = new StreamWriter(modelPath, append: true);
            
            modelFile.WriteLine(add_model.Text);
            
            add_model.Text = "";
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        public ObservableCollection<string> Models { get; set; }
        
        public string SelectedModel {  get; set; }
        public Chat? activeChat;

        public string ModelDescription { get; set; } = "";

    }
}
