using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NidRpc;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace MultipartDemo
{
    class Program
    {
        public static string diskFilepath = Path.Combine(Environment.GetEnvironmentVariable("HOME"), "data/file");
        public static string runtimeSymlink = Path.Combine(Environment.GetEnvironmentVariable("RUNTIME_DIRECTORY"), "file");
        const string symlink = "./frontend/file";

        static async Task Main(string[] args)
        {
            try
            {
                File.CreateSymbolicLink(symlink, runtimeSymlink);
            }
            catch (Exception e)
            {
                Console.WriteLine($"{e.Message}");
            }
            var plugin = new Plugin("application", "MultipartDemo");
            new MultipartPlugin(plugin, "temp");
            // writing to disk will always append the file, file can be removed by creating multipart with same path
            new MultipartPlugin(plugin, "disk", diskFilepath);
            await plugin.ConnectAsync();
            await Task.Delay(-1);
        }

        public class MultipartPlugin : MultipartPostHandler
        {
            string filename = "";

            public MultipartPlugin(Plugin plugin, string path = "", string filename = "", bool enable = true)
              : base(plugin, path, filename, enable)
            {
                plugin[$"/{path}/export"].CallbackReceived += Export;
                plugin[$"/{path}/delete"].CallbackReceived += Delete;
            }
            async Task<JObject> Export(object sender, CallbackEventArgs args)
            {
                if (State != MultipartPostHandler.PostState.Idle)
                {
                    return await Task.FromResult(JObject.Parse($"{{ 'error': 'Busy' }}"));
                }
                try
                {
                    if (File.Exists(runtimeSymlink))
                    {
                        File.Delete(runtimeSymlink);
                    }
                    File.CreateSymbolicLink(runtimeSymlink, filename);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"{e.Message}");
                }
                return await Task.FromResult(new JObject());
            }
            async Task<JObject> Delete(object sender, CallbackEventArgs args)
            {
                try
                {
                    if (File.Exists(filename))
                    {
                        File.Delete(filename);
                    }
                    else
                    {
                        return await Task.FromResult(JObject.Parse($"{{ 'error': 'File does not exist' }}"));
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"{e.Message}");
                }
                return await Task.FromResult(new JObject());
            }
            protected override async Task<JObject> MultipartState(object sender, CallbackEventArgs args)
            {
                return await base.MultipartState(sender, args);
            }
            protected override async Task<JObject> MultipartBegin(object sender, CallbackEventArgs args)
            {
                return await base.MultipartBegin(sender, args);
            }
            protected override async Task<JObject> MultipartWriteChunk(object sender, CallbackEventArgs args)
            {
                var r = await base.MultipartWriteChunk(sender, args);
                if (r.ContainsKey("error"))
                {
                    ChangePostState(MultipartPostHandler.PostState.Idle);
                }
                return r;
            }
            protected override async Task<JObject> MultipartEnd(object sender, CallbackEventArgs args)
            {
                var r = await base.MultipartEnd(sender, args);
                filename = MultipartFile.Name;
                Console.WriteLine($"Writing to file done: {filename}");
                ChangePostState(MultipartPostHandler.PostState.Idle);
                return r; 
            }
        }
    }
}
