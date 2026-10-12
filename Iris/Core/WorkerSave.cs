using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Iris.Core
{ 
    
    public partial class Worker
    {

        public static readonly JsonSerializerOptions serializerOptions = new JsonSerializerOptions()
        {
            WriteIndented = true,
            ReferenceHandler = ReferenceHandler.Preserve
        };

        void Save()
        {
            // if saving in a sub worker or a split worker, tell the parent to do the saving
            if(parentWorker != null)
            {
                parentWorker.Save();
                return;
            }
            if(preSplitWorker != null)
            {
                preSplitWorker.Save();
                return;
            }

            if(filename is null) filename = (new Random()).Next().ToString();

            string saveFile = JsonSerializer.Serialize(this, serializerOptions);

            string rootPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            string historyPath = Path.Combine(rootPath, ".iris", "newhistory", $"{filename}.json");
            File.WriteAllText(historyPath, saveFile);

        }
    }
}