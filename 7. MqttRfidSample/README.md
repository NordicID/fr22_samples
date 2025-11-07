## MqttRfidSample

Console application for FR22/Sampo S3 RFID reader control via MQTT messaging.

This is an installable application package for FR22/Sampo S3 devices. The application uses .NET 8.0 runtime, which must be installed from the FR22/Sampo S3 app center.

The application connects to an RFID reader NUR-module and exposes MQTT endpoints for remote control and integration.

### Features

- RFID reader (Nur module) connection management
- Tag inventory streaming with real-time updates
- MQTT broker configuration with TLS/SSL support via `settings.json`
- Automatic MQTT connection on startup
- Console-based application with no UI

### Requirements

- **MQTT Broker**: MQTT Broker application (can be installed from FR22 / Sampo S3 App Center or external broker)
- **.NET 8.0 Runtime**: Installed automatically from FR22 app center

### Configuration

The application is configured using `settings.json` file, which is created automatically on first run with default values.

#### Default Configuration

```json
{
  "Host": "localhost",
  "Port": 1883,
  "UseTls": false,
  "Username": null,
  "Password": null,
  "AllowUntrustedCertificates": false,
  "TopicPrefix": "MqttRfidSample",
  "RfidReaderUri": "tcp://localhost:4333"
}
```

#### Configuration Parameters

- `Host` - MQTT broker hostname or IP address
- `Port` - MQTT broker port (default: 1883, TLS: 8883)
- `UseTls` - Enable TLS/SSL encryption (true/false)
- `Username` - MQTT authentication username (null or "" for no auth)
- `Password` - MQTT authentication password (null or "" for no auth)
- `AllowUntrustedCertificates` - Allow self-signed certificates when using TLS
- `TopicPrefix` - MQTT topic prefix for all endpoints
- `RfidReaderUri` - RFID reader connection URI

**Note:** The `settings.json` file is NOT overwritten on application restart. Modify the file to change configuration, then restart the application.

### Usage

1. Install application package through device webui application interface
2. Application starts automatically and connects to MQTT broker using `settings.json` configuration
3. Monitor application logs through FR22 interface
4. External MQTT clients can connect and use the MQTT endpoints

To modify configuration:
1. Stop the application
2. Edit `settings.json` file (located in application runtime directory)
3. Restart the application

### MQTT Endpoints

**Topic Prefix Configuration:** The default topic prefix is `MqttRfidSample` but can be configured in `settings.json`. All endpoints below are relative to this configured prefix.

The application exposes the following endpoints (prefix with your configured topic):

#### RFID Connection Management
- `/rfid/connected` - Check RFID reader connection status
  - Response: `{"connected": true/false, "connectionUri": "tcp://...", "connectError": "..."}`
- `/rfid/connect` - Connect to RFID reader
  - Response: `{"success": true/false, "error": "..."}`
- `/rfid/disconnect` - Disconnect from RFID reader
  - Response: `{"success": true/false, "error": "..."}`
- `/rfid/readerinfo` - Get RFID reader information
  - Response: `{"name": "...", "fccId": "...", "swVerMajor": ..., ...}`

#### Tag Operations
- `/tags/startStream` - Start tag inventory streaming
  - Response: `{"success": true/false, "error": "..."}`
- `/tags/stopStream` - Stop tag inventory streaming
  - Response: `{"success": true/false, "error": "..."}`
- `/tags/clearTags` - Clear tag storage
  - Response: `{"success": true/false, "error": "..."}`

#### Inventory Management
- `/inventory/get` - Get current inventory data
  - Response: `{"count": ..., "nInventories": ..., "updateEnabled": true/false, "timestamp": "...", "tags": [...]}`

### MQTT Communication Example

All MQTT requests must include `id` (unique request identifier) and `client` (your client name) fields. Responses are published to `api/application/<client>` topic.

**Note:** Examples below use the default topic prefix `MqttRfidSample`. If you configure a different topic prefix in `settings.json`, replace it accordingly in all examples.

#### Using MQTT Explorer (recommended for Windows users)

1. Download and install [MQTT Explorer](http://mqtt-explorer.com/)
2. Connect to the MQTT broker (e.g., `device_ip_address:port`)
3. Subscribe to response topic: `api/application/mytest`
4. Publish to `MqttRfidSample/rfid/connect` to connect the reader NUR module with payload:
```json
   {"id": "req1", "client": "mytest"}
   ```
5. Verify the connection: `MqttRfidSample/rfid/connected`
6. Get NUR module information: `MqttRfidSample/rfid/readerinfo`
7. View response in subscribed topic

#### Using mosquitto command line tools

Subscribe to responses (in one terminal):
```bash
mosquitto_sub -h <broker-ip> -t "api/application/mytest"
```

Send requests (in another terminal):
```bash
# Connect to RFID reader
mosquitto_pub -h <broker-ip> -t "MqttRfidSample/rfid/connect" -m '{"id":"req1","client":"mytest"}'

# Start tag streaming
mosquitto_pub -h <broker-ip> -t "MqttRfidSample/tags/startStream" -m '{"id":"req2","client":"mytest"}'

# Get inventory
mosquitto_pub -h <broker-ip> -t "MqttRfidSample/inventory/get" -m '{"id":"req3","client":"mytest"}'
```

#### With TLS/Authentication

If you configured TLS and authentication in `settings.json`:

```bash
# Subscribe with TLS and auth
mosquitto_sub -h <broker-ip> -p 8883 --cafile ca.crt -u username -P password -t "api/application/mytest"

# Publish with TLS and auth
mosquitto_pub -h <broker-ip> -p 8883 --cafile ca.crt -u username -P password -t "MqttRfidSample/rfid/connect" -m '{"id":"req1","client":"mytest"}'
```

### Generating FR22 application package

#### Visual Studio 2022
- Open solution with Visual Studio 2022
- After successful build, package 'MqttRfidSample_1.0.0-app.zip' is generated in the solution folder.

#### Command Line
```bash
dotnet build
```
Package will be created in project directory: `MqttRfidSample_x.x.x-app.zip`

### Publish integration
See project Post-build event.

### Troubleshooting

**Application logs:** View application logs through FR22 webui to monitor MQTT connection status and errors.

**Configuration issues:** If the application fails to load `settings.json`, it will use default values but will NOT overwrite your existing configuration file. Check logs for parsing errors.

**MQTT connection fails:** Verify broker IP, port, and credentials in `settings.json`. Check that MQTT broker is running and accessible.
