# VPN Detection System

## Overview

The VPN detection system in ACE.Mods.AntiCheat provides comprehensive detection of VPN and proxy connections using multiple detection methods:

1. **Private Network Detection** - Identifies local/private IP ranges
2. **Known VPN Subnet Detection** - Checks against database of known VPN provider IP ranges
3. **External API Detection** - Integrates with third-party VPN detection services (configurable)

## How It Works

### 1. Private Network Detection
Automatically identifies these private IP ranges:
- `10.0.0.0/8` - Class A private network
- `172.16.0.0/12` - Class B private network  
- `192.168.0.0/16` - Class C private network
- `100.64.0.0/10` - Carrier-grade NAT
- `169.254.0.0/16` - Link-local addresses
- `127.0.0.0/8` - Loopback addresses

### 2. Known VPN Subnet Detection
The system maintains a database of known VPN provider IP ranges in `vpn_subnets.json`. This file contains:
- **Provider names** (e.g., "NordVPN", "ExpressVPN")
- **IP ranges** in format `start_ip-end_ip`
- **Automatic updates** every 24 hours (configurable)

### 3. External API Integration
For enhanced detection, the system can integrate with external VPN detection services:
- **ipinfo.io** - Free tier available
- **ipapi.com** - Comprehensive VPN detection
- **MaxMind** - Enterprise-grade IP intelligence
- **IP2Location** - Commercial VPN database

## Configuration

### Settings
```json
{
  "EnableExternalVPNDetection": false,
  "VPNDetectionCacheExpirationMinutes": 30,
  "VPNSubnetUpdateIntervalHours": 24
}
```

### VPN Subnets File
The `vpn_subnets.json` file should be placed in the ACE server directory. Format:

```json
{
  "ProviderName": [
    "start_ip-end_ip",
    "start_ip-end_ip"
  ]
}
```

Example:
```json
{
  "NordVPN": [
    "89.187.160.0-89.187.191.255",
    "185.65.18.0-185.65.18.255"
  ]
}
```

## Maintaining the VPN Database

### Automatic Updates
The system automatically checks for updates every 24 hours. To implement automatic updates:

1. **Set up a scheduled task** to download updated VPN ranges
2. **Use a reliable source** like IP2Location or MaxMind
3. **Validate the data** before replacing the existing file
4. **Backup the current file** before updates

### Manual Updates
To manually update the VPN database:

1. **Download** updated VPN ranges from a reliable source
2. **Convert** to the required JSON format
3. **Replace** the existing `vpn_subnets.json` file
4. **Restart** the ACE server or reload the mod

### Reliable Data Sources

#### Free Sources
- **IP2Location LITE** - Free database with VPN detection
- **MaxMind GeoLite2** - Free IP geolocation database
- **Community-maintained lists** - GitHub repositories with VPN ranges

#### Commercial Sources
- **IP2Location** - Comprehensive VPN/proxy detection
- **MaxMind** - Enterprise IP intelligence
- **IPQualityScore** - Real-time VPN detection API
- **IPHub** - VPN/proxy detection service

## Performance Considerations

### Caching
- **VPN results cached** for 30 minutes (configurable)
- **Maximum cache size** of 10,000 entries
- **Automatic cleanup** of expired entries
- **Memory usage** typically under 10MB

### Detection Speed
- **Private network detection**: < 1ms
- **Known subnet detection**: < 1ms  
- **External API detection**: 100-500ms (depending on service)

## Monitoring and Logging

### Log Levels
- **Info**: VPN subnet loading, updates
- **Debug**: Individual IP detections
- **Warn**: External API failures
- **Error**: File loading errors, API timeouts

### Statistics
The system provides these statistics:
```csharp
var (totalEntries, expiredEntries) = vpnDetector.GetCacheStats();
var (providers, totalRanges) = vpnDetector.GetVPNSubnetStats();
```

## Troubleshooting

### Common Issues

1. **VPN not detected**
   - Check if IP is in `vpn_subnets.json`
   - Verify external API is working
   - Check logs for errors

2. **False positives**
   - Review private network ranges
   - Check VPN subnet accuracy
   - Adjust detection thresholds

3. **Performance issues**
   - Reduce cache expiration time
   - Limit external API calls
   - Optimize subnet ranges

### Debug Mode
Enable debug logging to see detailed detection information:
```csharp
Mod.Log($"IP {ipAddress} detected as VPN from provider {provider.Key}", ModManager.LogLevel.Debug);
```

## Security Considerations

### Data Privacy
- **No IP addresses logged** in production (configurable)
- **Cache expiration** prevents long-term storage
- **Local processing** - no external data transmission

### Rate Limiting
- **External API calls** limited to prevent abuse
- **Cache-based detection** reduces external dependencies
- **Configurable timeouts** prevent hanging requests

## Future Enhancements

### Planned Features
1. **Machine learning detection** - Pattern-based VPN identification
2. **Behavioral analysis** - Connection pattern detection
3. **Real-time updates** - Live VPN database updates
4. **Custom rules** - User-defined detection criteria

### Integration Possibilities
1. **Discord webhooks** - Alert admins of VPN usage
2. **Database logging** - Store detection results for analysis
3. **API endpoints** - Query detection status programmatically
4. **Dashboard integration** - Web-based monitoring interface

## Support and Contributing

### Reporting Issues
- **GitHub Issues** - Bug reports and feature requests
- **Discord** - Community support and discussion
- **Documentation** - Keep this README updated

### Contributing
1. **Fork the repository**
2. **Add new VPN providers** to the subnet database
3. **Improve detection algorithms**
4. **Update documentation**
5. **Submit pull requests**

---

**Note**: This system is designed to detect common VPN usage patterns. Advanced users may still bypass detection using sophisticated methods. The goal is to deter casual VPN usage, not provide 100% detection coverage. 