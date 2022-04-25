using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NidRpc;

namespace RpcWebUI
{
    class Program
    {
        private static Plugin _rpc;
        static async Task Main(string[] args)
        {
            _rpc = new Plugin("application", "RpcWebUI");
            // Bind topic /api/application/RpcWebUI/beep to Beep() function
            _rpc["/beep"].CallbackReceived += Beep;
            await _rpc.ConnectAsync();

            await Task.Delay(-1);
        }

        private static async Task<JObject> Beep(object sender, CallbackEventArgs args)
        {
            int duration;
            if (args.Payload.TryGetValue("time", out JToken value))
            {
                if (value.Type == JTokenType.Integer)
                {
                    duration = (int)value;
                    if (duration < 0)
                    {
                        return JObject.Parse("{'error': 'Duration is negative: '" + duration + "}");
                    }
                    else if (duration < 50)
                    {
                        return JObject.Parse("{'error': 'Duration is too small: '" + duration + "}");
                    }
                    else if (duration > 10000)
                    {
                        return JObject.Parse("{'error': 'Duration is too big: '" + duration + "}");
                    }
                }
                else
                {
                    return JObject.Parse("{'error': 'Parameter type must be an integer'}");
                }
            } else
            {
                return JObject.Parse("{'error': 'Parameter missing: type'}");
            }

            var callArgs = new Dictionary<string, int>
            {
                { "frequency", 4000 },
                { "duration", duration },
            };
            var rsp = await _rpc.Call("api/builtin/beeper/beep", JObject.FromObject(callArgs));
            return rsp;
        }
    }
}
