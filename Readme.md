## AntiCheat ACE Mod
An anti-cheat mod for ACE.

### Features:
- **AntiBlink**: Detects / prevents moving through closed doors (and monster doors).
- **Multi-Client Detection**: Detects suspicious patterns of multiple clients disconnecting simultaneously.
- Admins / Cloaked characters are immune from AntiCheat

### Install:
- Download from the latest ACE.Mods.AntiCheat.zip from github [releases](https://github.com/trevis/ACE.Mods.AntiCheat/releases)
- Extract to `C:/ACE/Mods/` (or whatever your mod directory is.) (eg `C:/ACE/Mods/ACE.Mods.AntiCheat/ACE.Mods.AntiCheat.dll`)
- Modify Settings.json as needed

### Settings:
- **AdminsAreImmune**: Whether or not admins are immune to the anti-cheat
- **CloakedPlayersAreImmune**: Whether or not cloaked players are immune to the anti-cheat
- **EnableAntiBlink**: Enable anti-blink, which prevents players from teleporting through closed doors
- **AntiBlinkLogIntervalMilliseconds**: The number of milliseconds between anti-blink logging for a specific player
- **AntiBlinkMonsterDoors**: "Monster Doors" like Mana Barrier will be considered during the anti-blink check when enabled.
- **AntiBlinkZHeightLimit**: The height limit to check for doors when anti-blink is enabled

#### Multi-Client Detection Settings:
- **EnableMultiClientDetection**: Enable multi-client detection to detect suspicious patterns
- **MultiClientDetectionTimeWindowSeconds**: Time window in seconds to monitor for suspicious patterns (default: 30)
- **MultiClientDetectionMinDisconnections**: Minimum disconnections required to trigger analysis (default: 3)
- **MultiClientDetectionVPNThreshold**: VPN disconnections required to flag as suspicious (default: 3)
- **MultiClientDetectionNonVPNThreshold**: Non-VPN disconnections required to flag as suspicious (default: 2)
- **MultiClientDetectionNonMarketplaceThreshold**: Non-marketplace disconnections required to flag as suspicious (default: 2)
- **MultiClientDetectionTotalThreshold**: Total disconnections required to flag as suspicious (default: 5)
- **EnableExternalVPNDetection**: Enable external VPN detection service (requires API key)
- **ExternalVPNDetectionAPIKey**: External VPN detection service API key
- **ExternalVPNDetectionServiceURL**: External VPN detection service URL

### Multi-Client Detection Features:
- **VPN Mixed Pattern**: Detects when both VPN and non-VPN clients disconnect within the same time window
- **Non-Marketplace Pattern**: Flags disconnections that occur outside the marketplace area
- **Total Threshold Pattern**: Triggers when a high number of clients disconnect regardless of other criteria
- **Expandable Architecture**: Easy to add custom detection patterns and external service integrations

For detailed documentation on the Multi-Client Detection system, see [README_MultiClientDetection.md](ACE.Mods.AntiCheat/README_MultiClientDetection.md).

### TODO:
- Speed hack / other detection?
- AntiBlink currently doesn't work across landblock boundaries, so there's probably a couple edgecases it fails on.
- AntiBlink should be fast enough, but could use better filtering for doors to check against.
- Configurable way to deal with offenders. Right now it just logs.
- Only apply applicable harmony hooks optionally based on settings.
- Add more sophisticated VPN detection methods
- Implement automated response actions for detected violations
