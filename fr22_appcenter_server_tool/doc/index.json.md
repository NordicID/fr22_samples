```javascript
{
	// List of the available applications in the App Center server (optional)
	"applications": [
		{
			// Name of the application (mandatory)
			"name": "chromium",

			// Pretty display name of the application (optional)
			"display_name": "Chromium browser",

			// Description of the application (optional)
			"description": "Open source web browser",

			// List of the available application releases
			// If missing, removes earlier instances of the application from the App Center client index
			"releases": [
				{
					// Version of the application release (mandatory)
					"version": "89.0.4389.90",

					// URL of the application release package file (mandatory)
					// URL can be either absolute or relational to the location of the index.json file
					"url":	"app/chromium_89.0.4389.90-app.zip",

					// Components and features the application is dependent (optional)
					"depends": {
						// See the specification in "firmware/depends" section
						// (optional)
						"hw-variant": "870-1A"
					}
				}
			]
		}
	],

	// List of the available system plugins in the App Center server (optional)
	"plugins": [
		{
			// Name of the system plugin (mandatory)
			"name": "mono-4.5",

			// Pretty display name of the system plugin (optional)
			"display_name": "Mono 4.5",

			// Description of the system plugin (optional)
			"description": "Open source .NET framework implementation",

			// List of the available system plugin releases
			// If missing, removes earlier instances of the system plugin from the App Center client index
			"releases": [
				{
					// Version of the system plugin release (mandatory)
					"version": "6.12.0.107",

					// URL of the system plugin release package file (mandatory)
					// URL can be either absolute or relational to the location of the index.json file
					"url":	"sys/mono-4.5_6.12.0.107-plugin.zip",

					// Components and features the application is dependent (optional)
					"depends": {
						// See the specification in "firmware/depends" section
					}
				}
			]
		},

		// Another one for an example
		{
			"name": "mono-api-4.5.1",
			"display_name": "Mono API 4.5.1",
			"releases": [
				{
					"version": "6.12.0.107",
					"url":	"sys/mono-api-4.5.1_6.12.0.107-plugin.zip",
					"depends": {
						"plugins": [
							{
								"name": "mono-4.5",
								"version": "==6.12.0.107"
							}
						]
					}
				}
			]
		}
	],

	// List of the available firmware releases in the App Center server (optional)
	"firmware": [
		{
			// Version of the firmware release (mandatory)
			"version": "1.0.0",

			// Description of the firmware release (optional)
			"description": "The first official release",

			// URL of the firmware release package file (mandatory)
			// URL can be either absolute or relational to the location of the index.json file
			"url": "https://applicationserver.nordicid.com/smartg2_repo/fw/update_ext4-1.0.0-12345.zip",

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
	]
}
```