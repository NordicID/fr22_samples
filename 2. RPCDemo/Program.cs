using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NidRpc;

namespace RPCDemo
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Client rpc = new Client("RPCDemo");
            await rpc.ConnectAsync();
            JObject rsp = await rpc.Call("api/builtin/sysinfo/versions");
            if (rsp.ContainsKey("error"))
            {
                Console.WriteLine($"Error: {rsp["error"]}");
            }
            else
            {
                if (rsp.ContainsKey("version"))
                {
                    Console.WriteLine($"Firmware version is {rsp["version"]}");
                }
                else
                {
                    Console.WriteLine("Firmware version is missing!");
                }
            }
        }
    }
}
