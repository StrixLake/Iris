using System;
using System.IO;
using Iris.Core;

namespace Iris.Core
{
    static partial class Tool_Providers
    {
        public static Tuple<string, string> Ls(Tools.Tool tool, Worker worker)
        {
            string path = Path.Combine("C:/Users/yasha/Documents/Projects/Iris", tool.filename?.Trim() ?? "");
            string[] files = Directory.GetFileSystemEntries(path);

            string result = "";

            foreach(string file in files)
            {
                result += file + "\n";
            }
            return new($"LS {tool.filename?.Trim()}", result);
        }

        public static Tuple<string, string> Cat(Tools.Tool tool, Worker worker)
        {
            string path = "C:/Users/yasha/Documents/Projects/Iris";

            if (tool.filename == null) return new("", "");

            tool.filename = tool.filename.Trim();

            path = Path.Combine(path, tool.filename);

            string result = File.OpenText(path).ReadToEnd();

            result = $"<{tool.filename}>\n {result} \n</{tool.filename}>";

            return new($"Read {tool.filename}",result);
        }
    }
}