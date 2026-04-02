# Multi-Client Detection System

## Overview

The Multi-Client Detection system is designed to identify suspicious patterns of multiple clients disconnecting simultaneously from the same IP address. This helps detect potential multi-boxing or automated client management that could give players unfair advantages.

## Features

### Detection Patterns

1. **VPN Mixed Pattern**: Detects when both VPN and non-VPN clients disconnect within the same time window
2. **Non-Marketplace Pattern**: Flags disconnections that occur outside the marketplace area
3. **Total Threshold Pattern**: Triggers when a high number of clients disconnect regardless of other criteria

### VPN Detection

The system includes a sophisticated VPN detection mechanism with multiple methods:

- **Private Network Detection**: Automatically flags private IP ranges (10.x.x.x, 172.16-31.x.x, 192.168.x.x, etc.)
- **Known VPN Ranges**: Configurable list of known VPN provider IP ranges
- **External VPN Detection**: Support for external VPN detection services (configurable)

### Expandable Architecture

The system is designed to be easily extended with:

- Custom detection patterns
- Additional VPN detection methods
- External service integrations
- Custom reporting mechanisms

## Configuration

### Basic Settings

```json
{
  "EnableMultiClientDetection": true,
  "MultiClientDetectionTimeWindowSeconds": 30,
  "MultiClientDetectionMinDisconnections": 3,
  "MultiClientDetectionVPNThreshold": 3,
  "MultiClientDetectionNonVPNThreshold": 2,
  "MultiClientDetectionNonMarketplaceThreshold": 2,
  "MultiClientDetectionTotalThreshold": 5
}
```

### Advanced Settings

```json
{
  "EnableExternalVPNDetection": false,
  "ExternalVPNDetectionAPIKey": "your-api-key-here",
  "ExternalVPNDetectionServiceURL": "https://api.vpndetection.com/check"
}
```

## Detection Logic

### VPN Mixed Pattern
- Triggers when: VPN disconnections ≥ VPN threshold AND non-VPN disconnections ≥ non-VPN threshold
- Default thresholds: 3 VPN + 2 non-VPN disconnections within 30 seconds
- Use case: Detects users running multiple clients through different connection methods

### Non-Marketplace Pattern
- Triggers when: Non-marketplace disconnections ≥ non-marketplace threshold
- Default threshold: 2 non-marketplace disconnections within 30 seconds
- Use case: Detects suspicious disconnections outside safe zones

### Total Threshold Pattern
- Triggers when: Total disconnections ≥ total threshold
- Default threshold: 5 total disconnections within 30 seconds
- Use case: Catches any high-volume disconnection pattern

## Logging

The system provides detailed logging for investigation:

```
[AntiCheat] SUSPICIOUS MULTI-CLIENT ACTIVITY DETECTED - IP: 192.168.1.100, Pattern: VPN_MIXED_PATTERN, Total Disconnections: 5, VPN: 3, Non-VPN: 2, Non-Marketplace: 4, Players: Player1(12345), Player2(12346), Player3(12347)
[AntiCheat]   - Player1 (Account: 12345) disconnected at 14:30:15 from 192.168.1.100 (Location: 0x12345678)
[AntiCheat]   - Player2 (Account: 12346) disconnected at 14:30:18 from 192.168.1.100 (Marketplace)
```

## Extending the System

### Adding Custom Detection Patterns

1. Create a new method in `MultiClientDetector.cs`:

```csharp
private void CheckCustomPattern(string ipAddress, List<DisconnectionEvent> events)
{
    // Your custom detection logic here
    if (/* your condition */)
    {
        ReportSuspiciousActivity(ipAddress, events, "CUSTOM_PATTERN");
    }
}
```

2. Call it from `CheckForSuspiciousPatterns()`:

```csharp
// Add after existing checks
CheckCustomPattern(ipAddress, recentEvents);
```

### Adding External VPN Detection

1. Implement the external API call in `VPNDetector.cs`:

```csharp
public async Task<bool> CheckExternalVPNDetection(string ipAddress)
{
    using var client = new HttpClient();
    var response = await client.GetAsync($"{_settings.ExternalVPNDetectionServiceURL}?ip={ipAddress}&key={_settings.ExternalVPNDetectionAPIKey}");
    var result = await response.Content.ReadAsStringAsync();
    
    // Parse the response and return true if VPN detected
    return ParseVPNResponse(result);
}
```

2. Update the `DetectVPN()` method to use the external service:

```csharp
if (_settings.EnableExternalVPNDetection)
{
    var externalResult = await CheckExternalVPNDetection(ipAddress);
    if (externalResult)
        return true;
}
```

### Adding Custom Reporting

1. Extend the `ReportSuspiciousActivity()` method:

```csharp
private void ReportSuspiciousActivity(string ipAddress, List<DisconnectionEvent> events, string patternType)
{
    // Existing logging
    Mod.Log(message, ModManager.LogLevel.Warn);
    
    // Add custom reporting
    if (patternType == "CUSTOM_PATTERN")
    {
        // Send to external monitoring system
        SendToExternalMonitoring(ipAddress, events);
        
        // Trigger admin notification
        NotifyAdmins(ipAddress, events);
    }
}
```

## Performance Considerations

- The system uses in-memory caching for VPN detection results
- Disconnection history is automatically cleaned up every 5 minutes
- Events older than 5 minutes are automatically removed
- All operations are thread-safe with proper locking

## Troubleshooting

### Common Issues

1. **False Positives**: Adjust thresholds in settings to reduce false positives
2. **High Memory Usage**: The system automatically cleans up old data, but you can reduce the time window
3. **VPN Detection Issues**: Update the VPN ranges or enable external detection service

### Debugging

Enable debug logging to see detailed information:

```csharp
Mod.Log($"Processing disconnection for {player.Name} from {ipAddress}", ModManager.LogLevel.Debug);
```

## Security Considerations

- IP addresses are logged for investigation purposes
- Consider data retention policies for disconnection history
- External API calls should use HTTPS and proper authentication
- VPN detection should not be the sole basis for punitive action

## Future Enhancements

- Machine learning-based pattern detection
- Integration with player behavior analysis
- Real-time dashboard for monitoring
- Automated response actions (warnings, temporary bans)
- Geographic location-based detection
- Time-of-day pattern analysis 