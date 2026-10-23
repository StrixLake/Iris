using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Iris.Core
{
    public partial class Worker
    {
        // response is the assistant's responce containing
        // the tool call
        async Task ToolLoop(Message response)
        {
            Stopwatch watch = Stopwatch.StartNew();

            if (response.Content.Trim().EndsWith("<<TOOL LOOP END>>")) return;
            else if (response.Content.Trim().EndsWith("<<TOOL END>>"))
            {
                // parse the tool call(s) and give the result
                List<Tools.Tool> tools = Tools.Parse(response.Content);
                foreach(Tools.Tool tool in tools)
                {
                    Tuple<string, string> result = Tools.Dispatch(tool, this);
                    Message toolResult = new()
                    {
                        Role = "tool",
                        Reasoning = result.Item1,
                        Content = result.Item2
                    };
                    Context.Add(toolResult);
                }

            }
            else
            {
                // invalid tool call
                Message toolResult = new()
                {
                    Role = "tool",
                    Reasoning = "Invalid Tool Call",
                    Content = "Previous message contained malformed tool call or no tool call. End the tool call loop or use a valid tool call."
                };
                Context.Add(toolResult);
            }


            string payload = GeneratePayload(Context, this, Context.Count);
            Message assistantResponce = new()
            {
                Role = "assistant",
                messageLog = new()
            };
            lastLog = assistantResponce.messageLog;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(lastLog)));
            Context.Add(assistantResponce);
            lastLog.ClientLatency = watch.ElapsedMilliseconds;

            cancellationToken.Dispose();
            cancellationToken = new();
            assistantResponce.Streaming = true;
            await Client.SendMessage(payload, assistantResponce, cancellationToken.Token);
            assistantResponce.Streaming = false;

            Save();

            await ToolLoop(assistantResponce);
        }
    }
}