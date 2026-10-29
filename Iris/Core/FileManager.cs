using System;
using System.Collections.Generic;
using System.IO;
using Iris.Core;

namespace Iris.Core
{
    public static class FileManager
    {
        // <filename, file>
        public static Dictionary<string, AnchoredFile> files = [];

        public static bool WritePage(string file, double page, string content)
        {
            if(files.ContainsKey(file) && files[file].pages.ContainsKey(page))
            {
                files[file].pages[page] = content;
                WriteFile(file);
                return true;
            }
            else return false;
        }

        public static bool InsertAnchor(string file, double page, string content)
        {
            // we need to determine the index of the page
            // then get the anchor number of the next page
            // and take the average of both to insert in
            // the middle of those 2 indices
            // because OrderedDictionary is sorted on key
            // we can just insert the key, value directly instead
            // of calculating it's index

            if(!files.ContainsKey(file)) return false;
            if(!files[file].pages.ContainsKey(page)) return false;

            int index = files[file].pages.IndexOf(page);
            var nextPage = files[file].pages.GetAt(index + 1);

            double insertionAnchor = (files[file].pages.GetAt(index).Key + nextPage.Key) / 2;

            files[file].pages.Add(insertionAnchor, content);
            return true;
        }

        public static string ReadPage(string file, double page)
        {
            if(files.ContainsKey(file) && files[file].pages.ContainsKey(page))
            {
                return files[file].pages[page];
            }
            else return "[|Error: File or Page doesn't exist|]";
        }

        public static string ReadFile(string file)
        {
            if(!files.ContainsKey(file))
            {
                files[file] = new AnchoredFile(file);
            }

            return files[file].ReadAnchored();
        }

        public static string ReadRawFile(string file)
        {
            if(!files.ContainsKey(file))
            {
                files[file] = new AnchoredFile(file);
            }

            return files[file].ReadRaw();
        }

        static async void WriteFile(string file)
        {
            string content = files[file].ReadRaw();
            await File.WriteAllTextAsync(file, content);
        }
    }

    public class AnchoredFile
    {
        public OrderedDictionary<double, string> pages = [];

        public AnchoredFile(string path)
        {
            using StreamReader reader = File.OpenText(path);
            
            double i = 0;
            while(!reader.EndOfStream)
            {
                string content = (reader.ReadLine() ?? "") + "\n";
                content += (reader.ReadLine() ?? "") + "\n";
                content += (reader.ReadLine() ?? "") + "\n";

                pages.Add(i, content);

                ++i;
            }

        }

        public string ReadAnchored()
        {
            string content = "";
            foreach(var page in pages)
            {
                content += $"[|Anchor {page.Key}|]\n{page.Value}";
            }
            return content;
        }

        public string ReadRaw()
        {
            string content = "";
            foreach(var page in pages)
            {
                content += page.Value;
            }
            return content;
        }
    }
}