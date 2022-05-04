using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NidRpc;
using NurApiDotNet;

namespace RfidSample
{
    class Application
    {
        class TagEntry
        {
            public string epc;
            public byte antennaId;
            public sbyte rssi;
            public short phaseDiff;
            public uint timesSeen;
        }
        public class ByteArrayComparer : IEqualityComparer<byte[]>
        {
            public bool Equals(byte[] lhs, byte[] rhs)
            {
                if (lhs == null || rhs == null)
                {
                    return lhs == rhs;
                }
                return lhs.SequenceEqual(rhs);
            }
            public int GetHashCode(byte[] key)
            {
                if (key == null)
                    return 0;
                return key.Sum(b => b);
            }
        }

        readonly Plugin _rpc;
        readonly NurApi _nur;
        readonly ManualResetEventSlim _backGroundResetEvent = new ManualResetEventSlim();

        readonly SemaphoreSlim _lock = new SemaphoreSlim(1);
        bool _connected = false;
        string _connectError = null;
        NurApi.ReaderInfo? _readerInfo = null;
        bool _streamEnabled = false;
        DateTime? _lastStreamEvent = null;
        uint _nStreamEvents = 0;
        readonly Dictionary<byte[], TagEntry> _tagsSeen = new Dictionary<byte[], TagEntry>(new ByteArrayComparer());

        async public static Task<Application> CreateInstanceAsync(string appName)
        {
            var rpc = new Plugin("application", appName);
            var application = new Application(rpc);
            await rpc.ConnectAsync();
            return application;
        }

        Application(Plugin rpc)
        {
            try
            {
                _nur = new NurApi();
                _nur.ConnectedEvent += NurConnectedEvent;
                _nur.DisconnectedEvent += NurDisconnectedEvent;
                _nur.InventoryStreamEvent += new EventHandler<NurApi.InventoryStreamEventArgs>(OnInventoryStreamEvent);
            }
            catch (NurApiException ex)
            {
                Console.WriteLine(ex.Message);
                throw ex;
            }

            _rpc = rpc;
            _rpc["/rfid/connected"].CallbackReceived += RfidConnected;
            _rpc["/rfid/connect"].CallbackReceived += RfidConnect;
            _rpc["/rfid/disconnect"].CallbackReceived += RfidDisconnect;
            _rpc["/rfid/readerinfo"].CallbackReceived += RfidReaderInfo;

            _rpc["/tags/startStream"].CallbackReceived += TagsStartStream;
            _rpc["/tags/stopStream"].CallbackReceived += TagsStopStream;

            _rpc["/inventory/get"].CallbackReceived += InventoryGet;
        }

        public void Run()
        {
            BackgroundConnect();
            while (true)
            {
                _backGroundResetEvent.Wait();
            }
        }

        #region RPC_CALLBACKS
        async Task<JObject> RfidConnected(object sender, CallbackEventArgs args)
        {
            await _lock.WaitAsync();
            var connected = _connected ? "true" : "false";
            var connectError = _connectError;
            _lock.Release();

            var ret = JObject.Parse($"{{'connected': {connected}}}");
            if (connectError != null)
            {
                ret["connectError"] = connectError;
            }
            return ret;
        }

        void BackgroundConnect()
        {
            var thread = new Thread(() =>
            {
                try
                {
                    _nur.ConnectSocket("127.0.0.1", 4333);
                    _lock.Wait();
                    _connectError = null;
                    _lock.Release();
                }
                catch (NurApiException ex)
                {
                    _lock.Wait();
                    _connectError = ex.Message;
                    _lock.Release();
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }
        async Task<JObject> RfidConnect(object sender, CallbackEventArgs args)
        {
            BackgroundConnect();
            return await Task.FromResult(JObject.Parse("{}"));
        }

        async Task<JObject> RfidDisconnect(object sender, CallbackEventArgs args)
        {
            var thread = new Thread(() =>
            {
                try
                {
                    _nur.Disconnect();
                }
                catch (NurApiException ex)
                {
                    _lock.Wait();
                    _connectError = ex.Message;
                    _lock.Release();
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            return await Task.FromResult(JObject.Parse("{}"));
        }

        private async Task<JObject> RfidReaderInfo(object sender, CallbackEventArgs args)
        {
            await _lock.WaitAsync();
            var readerInfo = _readerInfo;
            _lock.Release();
            try
            {
                if (readerInfo.HasValue)
                {
                    return JObject.Parse(JsonConvert.SerializeObject(readerInfo));
                }
                else
                {
                    return JObject.Parse("{'error': 'No reader info available'}");
                }
            }
            catch (Exception)
            {
                return JObject.Parse("{'error': 'Error serializing reader info'}");
            }
        }
        private async Task<JObject> TagsStartStream(object sender, CallbackEventArgs args)
        {
            await _lock.WaitAsync();
            try
            {
                _streamEnabled = true;
                _tagsSeen.Clear();
                _nStreamEvents = 0;
            }
            finally
            {
                _lock.Release();
            }
            var thread = new Thread(() =>
            {
                try
                {
                    _nur.ClearTagsEx();
                    StartTagStream();
                }
                catch (NurApiException ex)
                {
                    Console.WriteLine($"Failed to start tag reading {ex.Message}");
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            return await Task.FromResult(JObject.Parse("{}"));
        }
        private async Task<JObject> TagsStopStream(object sender, CallbackEventArgs args)
        {
            await _lock.WaitAsync();
            _streamEnabled = false;
            _lock.Release();
            var thread = new Thread(() =>
            {
                try
                {
                    _nur.StopInventoryStream();
                }
                catch (NurApiException ex)
                {
                    Console.WriteLine($"Failed to stop tag reading {ex.Message}");
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            return await Task.FromResult(JObject.Parse("{}"));
        }

        private async Task<JObject> InventoryGet(object sender, CallbackEventArgs args)
        {
            int count;
            uint nStreamEvents;
            string streamEnabled = _streamEnabled ? "true" : "false";
            var tags = new List<TagEntry>();
            await _lock.WaitAsync();
            try
            {
                count = _tagsSeen.Count;
                nStreamEvents = _nStreamEvents;
                foreach (TagEntry tagEntry in _tagsSeen.Values)
                {
                    tags.Add(tagEntry);
                }
            }
            finally
            {
                _lock.Release();
            }

            var jsonTxt = $"{{'count': {count}, 'nInventories': {nStreamEvents}, 'updateEnabled': {streamEnabled}, 'tags': {JsonConvert.SerializeObject(tags)}}}";
            var ret = await Task.FromResult(JObject.Parse(jsonTxt));
            if (_lastStreamEvent.HasValue)
            {
                ret["timestamp"] = _lastStreamEvent.Value.ToString("yyyy-MM-dd HH\\:mm\\:ss");
            }
            return ret;
        }

        #endregion

        #region NUR_CALLBACKS
        void NurConnectedEvent(object sender, NurApi.NurEventArgs e)
        {
            _lock.Wait();
            _connected = true;
            _lock.Release();

            var thread = new Thread(() =>
            {
                NurApi.ReaderInfo? readerInfo = null;
                try
                {
                    readerInfo = _nur.GetReaderInfo();
                }
                catch (NurApiException ex)
                {
                    Console.WriteLine($"Failed to get reader info {ex.Message}");
                }
                _lock.Wait();
                _readerInfo = readerInfo;
                _lock.Release();
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }
        void NurDisconnectedEvent(object sender, NurApi.NurEventArgs e)
        {
            _lock.Wait();
            try
            {
                _connected = false;
                _readerInfo = null;
                _streamEnabled = false;
                _tagsSeen.Clear();
                _nStreamEvents = 0;
            }
            finally
            {
                _lock.Release();
            }
        }

        void OnInventoryStreamEvent(object sender, NurApi.InventoryStreamEventArgs ev)
        {
            NurApi.TagStorage nurStorage = _nur.GetTagStorage();
            _lock.Wait();
            try
            {
                if (!_streamEnabled)
                {
                    return;
                }
                // need to lock access to the tag storage object to
                // prevent NurApi from updating it in the background
                lock (nurStorage)
                {
                    foreach (NurApi.Tag tag in nurStorage)
                    {
                        if (_tagsSeen.TryGetValue(tag.epc, out TagEntry value))
                        {
                            value.antennaId = tag.antennaId;
                            value.rssi = tag.rssi;
                            value.phaseDiff = (short)tag.timestamp;
                            value.timesSeen += 1;
                        }
                        else
                        {
                            _tagsSeen[tag.epc] = new TagEntry() {
                                epc = BitConverter.ToString(tag.epc).Replace("-", ""),
                                antennaId = tag.antennaId,
                                rssi = tag.rssi,
                                phaseDiff = (short)tag.timestamp,
                                timesSeen = 0
                            };
                        }
                    }
                    // Clear NurApi internal tag storage so that we only get new tags next next time
                    nurStorage.Clear();
                }
                // NurApi may disable the stream to prevent unnecessarily powering the radio
                // (in case the application has stopped); start it again if that is the case
                if (_streamEnabled && ev.data.stopped)
                {
                    StartTagStream();
                }
                _lastStreamEvent = DateTime.Now;
                _nStreamEvents++;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                _lock.Release();
            }
        }

        #endregion
        void StartTagStream()
        {
            // TODO: fix when NUR_OPFLAGS_EN_PHASE_DIFF/NUR_DC_PHASEDIFF has been added to NurApiDotNet
            // tag phase diff support (NUR_OPFLAGS_EN_PHASE_DIFF = (1 << 17)) isn't yet available in
            // NurApiDotNet; just assume it is supported in the NUR module and turn it on
            _nur.OpFlags |= (1 << 17);
            _nur.StartInventoryStream();
        }
    }

    class Program
    {
        static async Task Main()
        {
            var application = await Application.CreateInstanceAsync("RfidSample");
            application.Run();
        }
    }
}