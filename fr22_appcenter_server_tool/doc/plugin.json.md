```javascript
{
    // Pretty display name of the plugin (application or system plugin) (optional)
    "display_name": "Chromium browser",

    // Description of the plugin (optional)
    "description": "Open source web browser"

    // Features the plugin (application or system plugin) is dependent (optional)
    "depends": {
        // Applicable hardware variant of the device for this plugin
        // Multiple variants can be given separated by '|'
        // (optional)
        "hw-variant": "870-1A|893-1A|893-2A",

        // Applicable modem type of the device for this plugin
        // Multiple modem types can be given separated by '|'
        // "E" for LTE-version
        // "W" for non-LTE-version (WLAN only)
        // (optional)
        "modem": "W|E"
    }
}
```