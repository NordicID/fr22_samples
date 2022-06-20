```javascript
{
    // Version of the firmware release (mandatory)
    "version": "1.0.0",

    // Description of the firmware release (optional)
    "description": "The first official release",

    // URL of the firmware release package file (mandatory)
    // URL can be either absolute (starts with https://) or relational to the location of the repo/ directory
    // If the URL is absolute, the firmware package file is not included in the repo.zip package
    "url": "fw/update_ext4-1.0.0-12345.zip",

    // Components and features the firmware upgrade is dependent (optional)
    "depends": {
        // Applicable hardware variant of the device for this release
        // Multiple variants can be given separated by '|'
        // (optional)
        "hw-variant": "870-1A|893-1A|893-2A",

        // Applicable modem type of the device for this release
        // Multiple modem types can be given separated by '|'
        // "E" for LTE-version
        // "W" for non-LTE-version (WLAN only)
        // (optional)
        "modem": "W|E",

        // Required firmware version for this release (optional)
        "firmware": {
            // Version specifier (mandatory)
            // https://www.python.org/dev/peps/pep-0440/#version-specifiers
            "version": ">=0.9.0"
        },

        // Required system plugins for this release (optional)
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
        "applications": [
            {
                // Name of the required application (mandatory)
                "name": "app-center",

                // Version requirement of the application (optional)
                // https://www.python.org/dev/peps/pep-0440/#version-specifiers
                "version": ">=1.0.5"
            }
        ]
    },

    // Force downgrade (optional)
    "force": true
}
```