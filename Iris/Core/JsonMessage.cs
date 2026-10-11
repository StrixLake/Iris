using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Windows.System;

namespace Iris.Core
{
    // the DTO to use for conversion to json
    class JsonMessage
    {
        JsonMessage() => role="user";

        public string role {get; set;}

        // it can contain either <string, string> for text
        // messages, or <string, dictionary<string, string>> for
        // image urls
        // since the json format for multimodal image llm is
        // content: [
        //  {
        //    type: 'text',
        //    text: "What's in this image?",
        //  },
        //  {
        //    type: 'image_url',
        //    image_url: {
        //      url: base64Image,
        //    },
        //  },
        //]
        public List<Dictionary<string, object>> content {get;set;} = [];

        // we use a factory function to construct the
        // json message instead of relying on the constructor
        public static List<JsonMessage> MessageConstructor(object msg, bool include_image)
        {
            // message can only be Core.Message or another message object in context

            if(msg is Message message)
            {

                if(message.Role == "user" || message.Role == "system") return [UserJsonMessage(message, include_image)];
                else if(message.Role == "assistant") return [AssistantJsonMessage(message)];
                else if(message.Role == "tool") return [ToolJsonMessaage(message)];

                else throw new Exception("Incompatible role found");
            }

            else throw new Exception("Incompatible type provided for json message construction");
        }

        static JsonMessage UserJsonMessage(Message message, bool include_image)
        {
            JsonMessage jsonMessage = new();
            jsonMessage.role = message.Role;
            
            string text = "";
            
            foreach(Tuple<string, string> file in message.files)
            {
                text += $"<{file.Item1}> \n {file.Item2} \n </{file.Item1}> \n";
            }

            text += message.Content;

            jsonMessage.content.Add(new Dictionary<string, object>
            {
                ["type"] = "text",
                ["text"] = text
            });

            if(include_image)
            {
                foreach(string image in message.images)
                {
                    jsonMessage.content.Add(new Dictionary<string, object>
                    {
                        ["type"] = "image",
                        ["image_url"] = new Dictionary<string, string>
                        {
                            ["url"] = image
                        }
                    });
                }
            }

            return jsonMessage;   
        }

        static JsonMessage AssistantJsonMessage(Message message)
        {
            JsonMessage jsonMessage = new();
            jsonMessage.role = "assistant";

            string text = text = "<thinking>" + message.Reasoning + "</thinking> \n" + message.Content;

            jsonMessage.content.Add(new Dictionary<string, object>
            {
                ["type"] = "text",
                ["text"] = text
            });

            return jsonMessage;
        }
        
        static JsonMessage ToolJsonMessaage(Message message)
        {
            JsonMessage jsonMessage = new();
            jsonMessage.role = "user";

            string text = text = $"<Tool_Result tool=\"{message.Reasoning}\">" + message.Content + "</Tool_Result> \n";

            jsonMessage.content.Add(new Dictionary<string, object>
            {
                ["type"] = "text",
                ["text"] = text
            });

            return jsonMessage;
        }
    }
}