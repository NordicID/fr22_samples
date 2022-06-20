```javascript
{
    // Components and features the plugin (application or system plugin) is dependent (optional)
    "depends": {
        // Applicable hardware variant of the device for this release (optional)
        // Multiple variants can be given separated by '|'
        // This overrides hw-variant dependency information in ../<PluginName>.json
        "hw-variant": "870-1A|893-1A|893-2A",

        // Applicable modem type of the device for this release (optional)
        // Multiple modem types can be given separated by '|'
        // "E" for LTE-version
        // "W" for non-LTE-version (WLAN only)
        // This overrides modem dependency information in ../<PluginName>.json
        "modem": "W|E",

        // Required firmware version for this release (optional)
        // This overrides firmware dependency information in meta/manifest.json in the plugin package
        "firmware": {
            // Version specifier (mandatory)
            // https://www.python.org/dev/peps/pep-0440/#version-specifiers
            "version": ">=0.9.0"
        },

        // Required system plugins for this release (optional)
        // This overrides system plugin dependency information in meta/manifest.json in the plugin package
        "plugins": [
            {
                // Name of the required system plugin (mandatory)
                "name": "nur3",

                // Version requirement of the system plugin (optional)
                // https://www.python.org/dev/peps/pep-0440/#version-specifiers
                "version": ">=15.2.70"
            }
        ],

        // Required applications for this release (optional)
        // This overrides application dependency information in meta/manifest.json in the plugin package
        "applications": [
            {
                // Name of the required application (mandatory)
                "name": "app-center",

                // Version requirement of the application (optional)
                // https://www.python.org/dev/peps/pep-0440/#version-specifiers
                "version": ">=1.0.5"
            }
        ]
    }
}
```