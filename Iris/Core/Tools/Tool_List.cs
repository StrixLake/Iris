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

            path = Path.Combine(path, tool.filename);

            string result = FileManager.ReadFile(Path.GetFullPath(path));

            result = $"<{tool.filename}>\n {result} \n</{tool.filename}>";

            return new($"Read {tool.filename}",result);
        }

        public static Tuple<string, string> RCat(Tools.Tool tool, Worker worker)
        {
            string path = "C:/Users/yasha/Documents/Projects/Iris";

            if (tool.filename == null) return new("", "");

            path = Path.Combine(path, tool.filename);

            string result = FileManager.ReadRawFile(Path.GetFullPath(path));

            result = $"<{tool.filename}>\n {result} \n</{tool.filename}>";

            return new($"Read {tool.filename}",result);
        }

        public static Tuple<string, string> WriteAnchor(Tools.Tool tool, Worker worker)
        {
            double anchor = tool.anchor;

            string path = "C:/Users/yasha/Documents/Projects/Iris";

            string filename = Path.GetFullPath(Path.Combine(path, tool.filename!));

            bool status = FileManager.WritePage(filename, anchor, tool.edit!);

            if(status)
            {
                return new Tuple<string, string>($"Write at {tool.anchor} on {tool.filename}", "Edit successful");
            }
            else
            {
                return new Tuple<string, string>($"Invalid Tool Call", "Edit failed, the file being editied is not in the file manager or the anchor is not present, please reread the file");
            }
        }

        public static Tuple<string, string> ReadAnchor(Tools.Tool tool, Worker worker)
        {
            double anchor = tool.anchor;

            string path = "C:/Users/yasha/Documents/Projects/Iris";

            string filename = Path.GetFullPath(Path.Combine(path, tool.filename!));

            string page = FileManager.ReadPage(filename, anchor);

            if(page == "[|Error: File or Page doesn't exist|]") return new Tuple<string, string>("Invalid Tool Call", page);

            return new Tuple<string, string>($"Page read at anchor {anchor} on file {filename}", page);
        }

        public static Tuple<string, string> ReadAnchorRange(Tools.Tool tool, Worker worker)
        {
            string path = "C:/Users/yasha/Documents/Projects/Iris";

            string filename = Path.GetFullPath(Path.Combine(path, tool.filename!));

            string page = FileManager.ReadPageRange(filename, tool.StartAnchor, tool.EndAnchor);

            if(page == "[|Error: File or Page doesn't exist|]") return new Tuple<string, string>("Invalid Tool Call", page);

            return new Tuple<string, string>($"Read anchors from {tool.StartAnchor} to {tool.EndAnchor}", page);
        }

        public static Tuple<string, string> InsertAnchor(Tools.Tool tool, Worker worker)
        {
            double anchor = tool.anchor;

            string path = "C:/Users/yasha/Documents/Projects/Iris";

            string filename = Path.GetFullPath(Path.Combine(path, tool.filename!));

            bool status = FileManager.InsertAnchor(filename, anchor, tool.edit!);

            if(status)
            {
                return new Tuple<string, string>($"Anchor inserted in file {filename}", $"Anchor inserted after {anchor}");
            }
            else
            {
                return new Tuple<string, string>("Invalid Tool Call", "Failed to insert anchor, file or anchor not found");
            }
        }
    }
}