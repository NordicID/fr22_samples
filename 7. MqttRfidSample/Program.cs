using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MQTTnet;
using NurApiDotNet;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MqttRfidSample
{
    class MqttConfig
    {
        public string Host { get; set; } = "localhost";
        public int Port { get; set; } = 1883;
        public bool UseTls { get; set; } = false;
        public string? Username { get; set; } = null;
        public string? Password { get; set; } = null;
        public bool AllowUntrustedCertificates { get; set; } = false;
        public string TopicPrefix { get; set; } = "api/application/MqttRfidSample";
        public string RfidReaderUri { get; set; } = "tcp://localhost:4333";

        public static MqttConfig LoadFromFile(string filePath)
        {
            bool fileExists = File.Exists(filePath);

            if (fileExists)
            {
                // File exists - try to load it
                try
                {
                    var json = File.ReadAllText(filePath);
                    var config = JsonConvert.DeserializeObject<MqttConfig>(json);
                    if (config != null)
                    {
                        Console.WriteLine($"[Config] Loaded MQTT configuration from {filePath}");
                        return config;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Config] Failed to load configuration: {ex.Message}");
                    Console.WriteLine($"[Config] Using default configuration (existing file will NOT be overwritten)");
                }

                // File exists but failed to load - return defaults WITHOUT saving
                return new MqttConfig();
            }
            else
            {
                // File does not exist - create default configuration and save it
                Console.WriteLine("[Config] Configuration file not found, creating default");
                var defaultConfig = new MqttConfig();

                try
                {
                    var directory = Path.GetDirectoryName(filePath);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                        Console.WriteLine($"[Config] Created directory: {directory}");
                    }

                    defaultConfig.SaveToFile(filePath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Config] Failed to save default configuration: {ex.Message}");
                }

                return defaultConfig;
            }
        }

        public void SaveToFile(string filePath)
        {
            try
            {
                var json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(filePath, json);
                Console.WriteLine($"[Config] Saved MQTT configuration to {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Config] Failed to save configuration: {ex.Message}");
                throw;
            }
        }
    }

    class Application
    {
        class TagEntry
        {
            public string? epc;
            public string? data;
            public byte antennaId;
            public sbyte rssi;
            public short phaseDiff;
            public uint timesSeen;
        }

        public class ByteArrayComparer : IEqualityComparer<byte[]>
        {
            public bool Equals(byte[]? lhs, byte[]? rhs)
            {
                if (lhs == null || rhs == null)
                {
                    return lhs == rhs;
                }
                return lhs.SequenceEqual(rhs);
            }
            public int GetHashCode(byte[]? key)
            {
                if (key == null)
                    return 0;
                return ((IStructuralEquatable)key).GetHashCode(EqualityComparer<byte>.Default);
            }
        }

        readonly IMqttClient _mqttClient;
        readonly NurApi _nur;
        readonly ManualResetEventSlim _shutdownEvent = new ManualResetEventSlim();
        readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        // Application settings
        const string APP_NAME = "MqttRfidSample";
        const string CONFIG_FILE_PATH = "settings.json";

        // MQTT configuration and state
        MqttConfig _mqttConfig;
        readonly object _lock = new object();
        bool _mqttConnected = false;
        DateTime? _mqttConnectedAt = null;
        string? _mqttConnectionError = null;
        int _reconnectAttempts = 0;
        bool _isReconnecting = false;
        const int RECONNECT_DELAY_INCREMENT_MS = 10000; // 10 seconds

        // RFID reader state
        bool _connected = false;
        string? _connectError = null;
        NurApi.ReaderInfo? _readerInfo = null;
        bool _streamEnabled = false;
        DateTime? _lastStreamEvent = null;
        uint _nStreamEvents = 0;
        readonly Dictionary<byte[], TagEntry> _tagsSeen = new Dictionary<byte[], TagEntry>(new ByteArrayComparer());

        async public static Task<Application> CreateInstanceAsync()
        {
            // Load MQTT configuration
            var mqttConfig = MqttConfig.LoadFromFile(CONFIG_FILE_PATH);

            // Initialize MQTTnet for external MQTT clients
            var mqttClient = new MqttClientFactory().CreateMqttClient();
            var application = new Application(mqttClient, mqttConfig);

            // Setup MQTT event handlers
            mqttClient.ApplicationMessageReceivedAsync += application.OnMessageReceivedAsync;
            mqttClient.ConnectedAsync += application.OnMqttConnectedAsync;
            mqttClient.DisconnectedAsync += application.OnMqttDisconnectedAsync;

            // Initial connection to MQTT broker
            await application.ConnectMqttAsync();

            // If initial connection failed, start reconnection loop
            bool connected;
            lock (application._lock)
            {
                connected = application._mqttConnected;
                if (!connected)
                {
                    application._isReconnecting = true;
                    application._reconnectAttempts = 0;
                }
            }

            if (!connected)
            {
                Console.WriteLine("[MQTT] Initial connection failed, starting automatic reconnection...");
                _ = Task.Run(async () => await application.ReconnectMqttLoopAsync());
            }

            return application;
        }

        Application(IMqttClient mqttClient, MqttConfig mqttConfig)
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
                throw;
            }

            _mqttClient = mqttClient;
            _mqttConfig = mqttConfig;
        }

        public async Task RunAsync()
        {
            Console.WriteLine($"[{APP_NAME}] Application started");
            Console.WriteLine($"[{APP_NAME}] MQTT Broker: {_mqttConfig.Host}:{_mqttConfig.Port}");
            Console.WriteLine($"[{APP_NAME}] Topic Prefix: {_mqttConfig.TopicPrefix}");
            Console.WriteLine($"[{APP_NAME}] RFID Reader URI: {_mqttConfig.RfidReaderUri}");

            // Wait until shutdown
            _shutdownEvent.Wait();

            Console.WriteLine($"[{APP_NAME}] Shutting down...");

            // Cancel any ongoing reconnection attempts
            _cancellationTokenSource.Cancel();

            // Disconnect MQTT client gracefully
            try
            {
                if (_mqttClient.IsConnected)
                {
                    await _mqttClient.DisconnectAsync();
                    Console.WriteLine("[MQTT] Disconnected gracefully");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MQTT] Error during disconnect: {ex.Message}");
            }

            _mqttClient?.Dispose();
            _cancellationTokenSource?.Dispose();
        }

        #region MQTT_CONNECTION_MANAGEMENT
        private async Task ConnectMqttAsync()
        {
            try
            {
                var optionsBuilder = new MqttClientOptionsBuilder()
                    .WithTcpServer(_mqttConfig.Host, _mqttConfig.Port)
                    .WithClientId($"{APP_NAME}_{Guid.NewGuid()}")
                    .WithCleanSession()
                    .WithTimeout(TimeSpan.FromSeconds(10));

                // Add credentials if provided
                if (!string.IsNullOrEmpty(_mqttConfig.Username))
                {
                    optionsBuilder.WithCredentials(_mqttConfig.Username, _mqttConfig.Password);
                    Console.WriteLine($"[MQTT] Using authentication with username: {_mqttConfig.Username}");
                }

                // Add TLS if enabled
                if (_mqttConfig.UseTls)
                {
                    var tlsOptions = new MqttClientTlsOptionsBuilder()
                        .WithSslProtocols(System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13);

                    if (_mqttConfig.AllowUntrustedCertificates)
                    {
                        tlsOptions.WithCertificateValidationHandler(_ => true);
                        Console.WriteLine("[MQTT] TLS enabled with untrusted certificates allowed");
                    }
                    else
                    {
                        Console.WriteLine("[MQTT] TLS enabled with certificate validation");
                    }

                    optionsBuilder.WithTlsOptions(tlsOptions.Build());
                }

                var options = optionsBuilder.Build();

                Console.WriteLine($"[MQTT] Connecting to {_mqttConfig.Host}:{_mqttConfig.Port}...");
                var result = await _mqttClient.ConnectAsync(options, _cancellationTokenSource.Token);
                Console.WriteLine($"[MQTT] Connection result: {result.ResultCode}");

                // Update state based on result
                lock (_lock)
                {
                    if (result.ResultCode == MqttClientConnectResultCode.Success)
                    {
                        _mqttConnected = true;
                        _mqttConnectionError = null;
                    }
                    else
                    {
                        _mqttConnected = false;
                        _mqttConnectionError = $"Connection failed: {result.ResultCode} - {result.ReasonString}";
                    }
                }
            }
            catch (Exception ex)
            {
                lock (_lock)
                {
                    _mqttConnected = false;
                    _mqttConnectionError = ex.Message;
                }
                Console.WriteLine($"[MQTT] Connection failed: {ex.Message}");
            }
        }

        private async Task ReconnectMqttLoopAsync()
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                bool shouldReconnect;
                int currentAttempt;

                lock (_lock)
                {
                    shouldReconnect = !_mqttConnected && _isReconnecting;
                    currentAttempt = _reconnectAttempts;
                }

                if (!shouldReconnect)
                {
                    break;
                }

                // Calculate delay: attempts * 10 seconds
                int delayMs = currentAttempt * RECONNECT_DELAY_INCREMENT_MS;

                if (delayMs > 0)
                {
                    Console.WriteLine($"[MQTT] Reconnecting... Waiting {delayMs / 1000} seconds before retry...");
                    try
                    {
                        await Task.Delay(delayMs, _cancellationTokenSource.Token);
                    }
                    catch (TaskCanceledException)
                    {
                        // Application is shutting down
                        return;
                    }
                }

                await ConnectMqttAsync();

                lock (_lock)
                {
                    if (_mqttConnected)
                    {
                        _isReconnecting = false;
                        _reconnectAttempts = 0;
                        break;
                    }
                    else
                    {
                        _reconnectAttempts++;
                    }
                }
            }
        }

        private async Task OnMqttConnectedAsync(MqttClientConnectedEventArgs e)
        {
            lock (_lock)
            {
                _mqttConnected = true;
                _mqttConnectedAt = DateTime.Now;
                _mqttConnectionError = null;
                _reconnectAttempts = 0;
                _isReconnecting = false;
            }

            Console.WriteLine($"[MQTT] Connected to broker at {_mqttConfig.Host}:{_mqttConfig.Port}");

            // Subscribe to all endpoints using configured topic prefix
            try
            {
                var subscriptionTopic = $"{_mqttConfig.TopicPrefix}/#";
                await _mqttClient.SubscribeAsync(subscriptionTopic);
                Console.WriteLine($"[MQTT] Subscribed to {subscriptionTopic}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MQTT] Subscription failed: {ex.Message}");
            }

            await Task.CompletedTask;
        }

        private async Task OnMqttDisconnectedAsync(MqttClientDisconnectedEventArgs e)
        {
            bool shouldStartReconnect = false;

            lock (_lock)
            {
                _mqttConnected = false;
                if (e.Exception != null)
                {
                    _mqttConnectionError = e.Exception.Message;
                }

                // Start reconnection if not already reconnecting and not shutting down
                if (!_isReconnecting && !_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    _isReconnecting = true;
                    _reconnectAttempts = 0;
                    shouldStartReconnect = true;
                }
            }

            Console.WriteLine($"[MQTT] Disconnected from broker. Reason: {e.Reason}");
            if (e.Exception != null)
            {
                Console.WriteLine($"[MQTT] Exception: {e.Exception.Message}");
            }

            if (shouldStartReconnect)
            {
                Console.WriteLine("[MQTT] Starting automatic reconnection...");
                _ = Task.Run(async () => await ReconnectMqttLoopAsync());
            }

            await Task.CompletedTask;
        }
        #endregion

        /// <summary>
        /// Validates client identifier to prevent MQTT topic injection attacks.
        /// Only allows alphanumeric characters, hyphens, and underscores.
        /// </summary>
        private bool IsValidClientId(string? client)
        {
            return !string.IsNullOrEmpty(client) &&
                   client.Length <= 64 &&
                   System.Text.RegularExpressions.Regex.IsMatch(client, @"^[a-zA-Z0-9_-]+$");
        }

        private async Task PublishResponseAsync(string client, string requestId, JObject result)
        {
            if (!IsValidClientId(client))
            {
                Console.WriteLine($"[PublishResponse] Invalid client ID: {client}");
                return;
            }

            var fullResponse = new JObject
            {
                ["id"] = requestId,
                ["result"] = result
            };

            var responseTopic = $"api/application/{client}";
            var payload = fullResponse.ToString(Formatting.None);

            var message = new MqttApplicationMessageBuilder()
                .WithTopic(responseTopic)
                .WithPayload(payload)
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();

            await _mqttClient.PublishAsync(message);
            Console.WriteLine($"[PublishResponse] Response to {responseTopic}: {payload}");
        }

        private async Task OnMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs e)
        {
            try
            {
                var topic = e.ApplicationMessage.Topic;
                var payload = e.ApplicationMessage.ConvertPayloadToString();

                Console.WriteLine($"[OnMessage] Topic: {topic}");
                Console.WriteLine($"[OnMessage] Payload: {payload}");

                // Verify topic starts with configured prefix
                if (!topic.StartsWith(_mqttConfig.TopicPrefix))
                {
                    Console.WriteLine($"[OnMessage] Topic does not match configured prefix '{_mqttConfig.TopicPrefix}', ignoring");
                    return;
                }

                JObject? json = null;
                try
                {
                    json = JObject.Parse(payload);
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"[OnMessage] Failed to parse JSON: {ex.Message}");
                    return;
                }

                var requestId = json["id"]?.ToString();
                var client = json["client"]?.ToString();

                // Route to appropriate handler based on topic endpoint
                if (topic.EndsWith("/rfid/connected"))
                {
                    await HandleRfidConnected(requestId, client);
                }
                else if (topic.EndsWith("/rfid/connect"))
                {
                    await HandleRfidConnect(requestId, client);
                }
                else if (topic.EndsWith("/rfid/disconnect"))
                {
                    await HandleRfidDisconnect(requestId, client);
                }
                else if (topic.EndsWith("/rfid/readerinfo"))
                {
                    await HandleRfidReaderInfo(requestId, client);
                }
                else if (topic.EndsWith("/tags/startStream"))
                {
                    await HandleTagsStartStream(requestId, client);
                }
                else if (topic.EndsWith("/tags/stopStream"))
                {
                    await HandleTagsStopStream(requestId, client);
                }
                else if (topic.EndsWith("/tags/clearTags"))
                {
                    await HandleClearTagStorage(requestId, client);
                }
                else if (topic.EndsWith("/inventory/get"))
                {
                    await HandleInventoryGet(requestId, client);
                }
                else
                {
                    Console.WriteLine($"[OnMessage] Unknown topic endpoint: {topic}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OnMessage] Exception: {ex.Message}");
            }

            await Task.CompletedTask;
        }

        #region MQTT_HANDLERS
        private async Task HandleRfidConnected(string? requestId, string? client)
        {
            Console.WriteLine("[RfidConnected] Request");

            bool connected;
            string? connectError;
            lock (_lock)
            {
                connected = _connected;
                connectError = _connectError;
            }

            var result = new JObject
            {
                ["connected"] = connected,
                ["connectionUri"] = _mqttConfig.RfidReaderUri
            };

            if (!string.IsNullOrEmpty(connectError))
                result["connectError"] = connectError;

            if (!string.IsNullOrEmpty(requestId) && !string.IsNullOrEmpty(client))
            {
                await PublishResponseAsync(client, requestId, result);
            }
        }

        private async Task HandleRfidConnect(string? requestId, string? client)
        {
            Console.WriteLine("[RfidConnect] Request");

            JObject result;
            try
            {
                await Task.Run(() =>
                {
                    _nur.Connect(new Uri(_mqttConfig.RfidReaderUri));
                    lock (_lock)
                    {
                        _connectError = null;
                    }
                });
                result = new JObject { ["success"] = true };
            }
            catch (NurApiException ex)
            {
                lock (_lock)
                {
                    _connectError = ex.Message;
                }
                result = new JObject
                {
                    ["success"] = false,
                    ["error"] = ex.Message
                };
            }

            if (!string.IsNullOrEmpty(requestId) && !string.IsNullOrEmpty(client))
            {
                await PublishResponseAsync(client, requestId, result);
            }
        }

        private async Task HandleRfidDisconnect(string? requestId, string? client)
        {
            Console.WriteLine("[RfidDisconnect] Request");

            JObject result;
            try
            {
                await Task.Run(() =>
                {
                    _nur.Disconnect();
                });
                result = new JObject { ["success"] = true };
            }
            catch (NurApiException ex)
            {
                lock (_lock)
                {
                    _connectError = ex.Message;
                }
                result = new JObject
                {
                    ["success"] = false,
                    ["error"] = ex.Message
                };
            }

            if (!string.IsNullOrEmpty(requestId) && !string.IsNullOrEmpty(client))
            {
                await PublishResponseAsync(client, requestId, result);
            }
        }

        private async Task HandleRfidReaderInfo(string? requestId, string? client)
        {
            Console.WriteLine("[RfidReaderInfo] Request");

            NurApi.ReaderInfo? readerInfo;
            lock (_lock)
            {
                readerInfo = _readerInfo;
            }

            JObject result;
            try
            {
                result = readerInfo != null
                    ? JObject.FromObject(readerInfo)
                    : new JObject { ["error"] = "No reader info available" };
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Error serializing reader info: {ex.Message}");
                result = new JObject { ["error"] = "Error serializing reader info" };
            }

            if (!string.IsNullOrEmpty(requestId) && !string.IsNullOrEmpty(client))
            {
                await PublishResponseAsync(client, requestId, result);
            }
        }

        private async Task HandleClearTagStorage(string? requestId, string? client)
        {
            Console.WriteLine("[ClearTagStorage] Request");

            JObject result;
            try
            {
                await Task.Run(() =>
                {
                    _nur.ClearTagsEx();

                    lock (_lock)
                    {
                        _tagsSeen.Clear();
                        _nStreamEvents = 0;
                    }
                });

                Console.WriteLine("[ClearTagStorage] Tag storage cleared.");
                result = new JObject { ["success"] = true };
            }
            catch (NurApiException ex)
            {
                Console.WriteLine($"[ClearTagStorage] Failed: {ex.Message}");
                result = new JObject
                {
                    ["success"] = false,
                    ["error"] = ex.Message
                };
            }

            if (!string.IsNullOrEmpty(requestId) && !string.IsNullOrEmpty(client))
            {
                await PublishResponseAsync(client, requestId, result);
            }
        }

        private async Task HandleTagsStartStream(string? requestId, string? client)
        {
            Console.WriteLine("[TagsStartStream] Request");

            JObject result;
            try
            {
                await Task.Run(() =>
                {
                    _nur.ClearTagsEx();
                    StartTagStream();
                });

                lock (_lock)
                {
                    _streamEnabled = true;
                    _tagsSeen.Clear();
                    _nStreamEvents = 0;
                }

                result = new JObject { ["success"] = true };
            }
            catch (NurApiException ex)
            {
                Console.WriteLine($"Failed to start tag reading {ex.Message}");

                lock (_lock)
                {
                    _streamEnabled = false;
                }

                result = new JObject
                {
                    ["success"] = false,
                    ["error"] = ex.Message
                };
            }

            if (!string.IsNullOrEmpty(requestId) && !string.IsNullOrEmpty(client))
            {
                await PublishResponseAsync(client, requestId, result);
            }
        }

        private async Task HandleTagsStopStream(string? requestId, string? client)
        {
            Console.WriteLine("[TagsStopStream] Request");

            lock (_lock)
            {
                _streamEnabled = false;
            }

            JObject result;
            try
            {
                await Task.Run(() =>
                {
                    _nur.StopInventoryStream();
                });
                result = new JObject { ["success"] = true };
            }
            catch (NurApiException ex)
            {
                Console.WriteLine($"Failed to stop tag reading {ex.Message}");
                result = new JObject
                {
                    ["success"] = false,
                    ["error"] = ex.Message
                };
            }

            if (!string.IsNullOrEmpty(requestId) && !string.IsNullOrEmpty(client))
            {
                await PublishResponseAsync(client, requestId, result);
            }
        }

        private async Task HandleInventoryGet(string? requestId, string? client)
        {
            Console.WriteLine("[InventoryGet] Request");

            int count;
            uint nStreamEvents;
            bool streamEnabled;
            DateTime? lastStreamEvent;
            var tags = new List<TagEntry>();

            lock (_lock)
            {
                count = _tagsSeen.Count;
                nStreamEvents = _nStreamEvents;
                streamEnabled = _streamEnabled;
                lastStreamEvent = _lastStreamEvent;
                tags.AddRange(_tagsSeen.Values);
            }

            var result = new JObject
            {
                ["count"] = count,
                ["nInventories"] = nStreamEvents,
                ["updateEnabled"] = streamEnabled,
                ["tags"] = JArray.FromObject(tags)
            };

            if (lastStreamEvent.HasValue)
                result["timestamp"] = lastStreamEvent.Value.ToString("yyyy-MM-dd HH\\:mm\\:ss");

            if (!string.IsNullOrEmpty(requestId) && !string.IsNullOrEmpty(client))
            {
                await PublishResponseAsync(client, requestId, result);
            }
        }

        #endregion

        #region NUR_CALLBACKS
        void NurConnectedEvent(object? sender, NurApi.NurEventArgs e)
        {
            lock (_lock)
            {
                _connected = true;
            }

            _ = Task.Run(() =>
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
                lock (_lock)
                {
                    _readerInfo = readerInfo;
                }
            });
        }

        void NurDisconnectedEvent(object? sender, NurApi.NurEventArgs e)
        {
            lock (_lock)
            {
                _connected = false;
                _readerInfo = null;
                _streamEnabled = false;
                _tagsSeen.Clear();
                _nStreamEvents = 0;
            }
        }

        void OnInventoryStreamEvent(object? sender, NurApi.InventoryStreamEventArgs ev)
        {
            NurApi.TagStorage nurStorage = _nur.GetTagStorage();

            // Copy tags from nurStorage with minimal lock time
            var tagsCopy = new List<NurApi.Tag>();
            lock (nurStorage)
            {
                foreach (NurApi.Tag tag in nurStorage)
                {
                    tagsCopy.Add(tag);
                }
                nurStorage.Clear();
            }

            // Process tags under application lock
            lock (_lock)
            {
                try
                {
                    if (!_streamEnabled)
                    {
                        return;
                    }

                    foreach (var tag in tagsCopy)
                    {
                        if (_tagsSeen.TryGetValue(tag.epc, out TagEntry? value))
                        {
                            value.antennaId = tag.antennaId;
                            value.rssi = tag.rssi;
                            value.phaseDiff = (short)tag.timestamp;
                            value.timesSeen += 1;
                        }
                        else
                        {
                            _tagsSeen[tag.epc] = new TagEntry()
                            {
                                epc = BitConverter.ToString(tag.epc).Replace("-", ""),
                                data = tag.GetDataString() ?? "",
                                antennaId = tag.antennaId,
                                rssi = tag.rssi,
                                phaseDiff = (short)tag.timestamp,
                                timesSeen = 1
                            };
                        }
                    }

                    // Restart stream if stopped
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
            }
        }

        #endregion

        void StartTagStream()
        {
            // Enable inventory read - read 4 words from TID bank during inventory
            _nur.InventoryRead(true, NurApi.NUR_IR_EPCDATA, NurApi.BANK_TID, 0, 4);

            // Enable tag phase angle difference reporting in tag metadata timestamp field
            _nur.OpFlags |= NurApi.OPFLAGS_EN_PHASE_DIFF;
            _nur.StartInventoryStream();
        }
    }

    class Program
    {
        static async Task Main()
        {
            var application = await Application.CreateInstanceAsync();
            await application.RunAsync();
        }
    }
}
