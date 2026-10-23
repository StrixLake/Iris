using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

// i don't fully understand how xml parsing
// and deserialization works here

namespace Iris.Core
{
    [XmlRoot("tools")]
    public class Tools
    {
        public class Tool
        {
            [XmlAttribute]
            public string name {get; set;} = "";
            [XmlElement]
            public string? filename {get; set;}
            [XmlElement]
            public string? anchor {get; set;}
            [XmlElement]
            public string? edit {get; set;}
        }

        [XmlElement("tool")]
        public List<Tool> toolcalls {get; set;} = [];

        // it returns the content between
        // <<TOOL BEGIN>> and <<TOOL END>>
        // as an array of string

        public static List<Tool> Parse(string responce)
        {
            // the format of a tool call is
            // <tool name=""></tool>
            // multiple tool calls may be invoked
            // return a list of each tool call

            string pattern = "(?<=<<TOOL BEGIN>>\\s*)<tool name[\\s\\S]*</tool>(?=\\s*<<TOOL END>>)";
            string calls = Regex.Match(responce, pattern).Value;

            calls = "<tools>\n" + calls + "\n</tools>";

            XmlSerializer serializer = new XmlSerializer(typeof(Tools));
            Tools toollist = (Tools?)serializer.Deserialize(new MemoryStream(Encoding.UTF8.GetBytes(calls))) ?? new Tools();

            return toollist.toolcalls;
        }

        public static Tuple<string, string> Dispatch(Tool tool, Worker worker)
        {
            if(tool.name == "ls") return Tool_Providers.Ls(tool, worker);
            if(tool.name == "cat") return Tool_Providers.Cat(tool, worker);
            else return new("", "");
        }
    }
}