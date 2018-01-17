PhaseShiftTierPlugin is an extension for Phase Shift to allow the detect of career tier transition to allow
the creation of career story line. Since the source code for Phase Shift is not available, this plugin
reverse engineers some of the tier and user data files in order to track career band achievements. When a
tier change is detected, it performs a callback to an external application, with the tier change info, to
allow the creation of tier change content. In this manner changing the actions that occur when a tier
changes does not require any modifications to this tier detection plugin.

Features:

- The plugin supports following the tiers for any of the main instrument types: bass, drums, guitar and keys.
- The plugin automatically detects which career and band is currently being used in Phase Shift.
- Once the current carreer and band is detected, the plugin automatically optimizes the detection process
  by ignoring other careers and bands.
- Once a tier change has been detected the callback to the external application not only provides the
  tier change info but also the band name. This can be used by the external application to personalize
  the resulting experience. 

Known Limitations:

- Tiers are based on instrument types. Since the plugin runs outside of Phase Shift there does not seem to be
  a way to detect which instrument the player has selected and thus there does not seem to be a way to
  automatically select tiers for the player's instrument type. As such, on startup of the plugin, the user
  must indicate the instrument type which will be used to determine tiers.
- Since optimization is automatic once the current career and band have been identified, switching to a
  different career or band requires restart of the plugin.

Syntax:

PhaseShiftTierPlugin instrument

Where instrument is one of the following key words: bass, drums, guitar, keys.