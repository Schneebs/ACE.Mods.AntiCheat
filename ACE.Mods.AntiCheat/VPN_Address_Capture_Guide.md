# VPN Address Automatic Capture Guide

## Overview

The VPN detection system now includes automatic capture of VPN addresses from multiple sources. This guide explains how to set up and use the automatic VPN address capture system.

## How It Works

### 1. **Automatic Updates**
- The system automatically checks for VPN subnet updates every 24 hours (configurable)
- Updates are performed in the background without blocking VPN detection
- Failed updates don't affect existing VPN detection capabilities

### 2. **Multiple Data Sources**
The system tries these sources in order of reliability:

#### **IP2Location LITE (Free)**
- **Source**: `https://raw.githubusercontent.com/ip2location/IP2LOCATION-LITE-DB1/master/IP2LOCATION-LITE-DB1.CSV`
- **Format**: CSV with IP ranges and country codes
- **Coverage**: Global IP ranges by country
- **Cost**: Free
- **Limitation**: Country-based, not VPN-specific

#### **Community VPN Lists**
- **Source 1**: `https://raw.githubusercontent.com/X4BNet/lists_vpn/main/ipv4.txt`
- **Source 2**: `https://raw.githubusercontent.com/blackmatrix7/ios_rule_script/master/rule/Clash/GlobalMedia/GlobalMedia_Resolve.list`
- **Source 3**: `https://raw.githubusercontent.com/blackmatrix7/ios_rule_script/master/rule/Clash/GlobalMedia/GlobalMedia.list`
- **Format**: IP addresses and CIDR ranges
- **Coverage**: VPN-specific IP ranges
- **Cost**: Free
- **Quality**: Community-maintained, varies

#### **Backup Sources**
- Fallback to reliable community sources
- Ensures system continues working even if primary sources fail

### 3. **Data Processing**
- **IP Range Conversion**: Converts CIDR notation to start-end IP ranges
- **Country Filtering**: Focuses on VPN-friendly countries
- **Provider Mapping**: Maps countries to likely VPN providers
- **Data Validation**: Ensures IP addresses are valid before storage

## Configuration

### Settings File (`Settings.json`)

```json
{
  "EnableVPNSubnetUpdates": true,
  "VPNSubnetUpdateIntervalHours": 24,
  "VPNDetectionCacheExpirationMinutes": 30,
  "VPNDetectionMaxCacheSize": 10000,
  "EnableCommunityVPNSources": true,
  "EnableIP2LocationLite": true,
  "EnableMaxMindLite": false,
  "MaxMindLicenseKey": ""
}
```

### Setting Descriptions

| Setting | Default | Description |
|---------|---------|-------------|
| `EnableVPNSubnetUpdates` | `true` | Enable/disable automatic updates |
| `VPNSubnetUpdateIntervalHours` | `24` | Hours between update attempts |
| `EnableCommunityVPNSources` | `true` | Use community-maintained VPN lists |
| `EnableIP2LocationLite` | `true` | Use IP2Location LITE database |
| `EnableMaxMindLite` | `false` | Use MaxMind GeoLite2 (requires license) |

## Manual Updates

### Force Update via Code

```csharp
// Get VPNDetector instance
var vpnDetector = new VPNDetector(settings);

// Force immediate update
bool success = await vpnDetector.ForceVPNSubnetUpdate();
if (success)
{
    Console.WriteLine("VPN subnet update completed successfully");
}
else
{
    Console.WriteLine("VPN subnet update failed");
}
```

### Check Update Status

```csharp
// Get statistics
var (providers, totalRanges) = vpnDetector.GetVPNSubnetStats();
Console.WriteLine($"VPN Providers: {providers}, Total Ranges: {totalRanges}");

// Get cache statistics
var (totalEntries, expiredEntries) = vpnDetector.GetCacheStats();
Console.WriteLine($"Cache Entries: {totalEntries}, Expired: {expiredEntries}");
```

## Data Sources Setup

### 1. **IP2Location LITE (Recommended for Start)**

**Advantages:**
- Free and reliable
- Regular updates
- Global coverage

**Setup:**
1. No API key required
2. Automatically enabled by default
3. Updates every 24 hours

**Data Quality:**
- High accuracy for country detection
- Good for identifying VPN-friendly regions
- Regular updates from official source

### 2. **Community VPN Lists**

**Advantages:**
- VPN-specific data
- Real-time updates
- Multiple sources for redundancy

**Setup:**
1. Automatically enabled by default
2. No configuration required
3. Multiple fallback sources

**Data Quality:**
- Varies by source
- Some sources may have false positives
- Community-maintained quality

### 3. **MaxMind GeoLite2 (Optional)**

**Advantages:**
- Enterprise-grade data
- High accuracy
- Professional support

**Setup:**
1. Get free license from [MaxMind](https://www.maxmind.com/en/geolite2/signup)
2. Add license key to settings
3. Enable `EnableMaxMindLite`

**Data Quality:**
- Very high accuracy
- Regular updates
- Professional data validation

## Monitoring and Logging

### Log Levels

- **Info**: Successful updates, data loading
- **Debug**: Individual IP detections, source attempts
- **Warn**: Update failures, no data available
- **Error**: Critical failures, data corruption

### Example Log Output

```
[INFO] Starting VPN subnet update...
[INFO] Successfully fetched VPN subnets from FetchFromCommunitySources
[INFO] VPN subnet update completed: 15 providers with 2,847 ranges
[DEBUG] IP 185.65.18.123 detected as VPN from provider Community_VPN
```

## Troubleshooting

### Common Issues

#### 1. **No VPN Subnets Loaded**
```bash
# Check if vpn_subnets.json exists
ls -la vpn_subnets.json

# Check file permissions
chmod 644 vpn_subnets.json

# Verify JSON format
cat vpn_subnets.json | jq .
```

#### 2. **Update Failures**
```bash
# Check network connectivity
ping raw.githubusercontent.com

# Check firewall settings
# Ensure outbound HTTP/HTTPS is allowed

# Check logs for specific error messages
grep "VPN subnet update" server.log
```

#### 3. **Poor Detection Accuracy**
```bash
# Check current VPN subnet data
cat vpn_subnets.json | jq 'keys'

# Force manual update
# Check if new data is available

# Review community source quality
# Some sources may have outdated data
```

### Performance Optimization

#### 1. **Cache Management**
- Default cache size: 10,000 entries
- Cache expiration: 30 minutes
- Automatic cleanup of expired entries

#### 2. **Update Frequency**
- Default: Every 24 hours
- Can be reduced for high-traffic servers
- Manual updates available for immediate needs

#### 3. **Memory Usage**
- Typical memory usage: < 10MB
- Scales with number of VPN ranges
- Automatic cleanup prevents memory leaks

## Advanced Configuration

### Custom Data Sources

To add custom VPN data sources:

1. **Create custom fetch method:**
```csharp
private async Task<Dictionary<string, List<string>>?> FetchFromCustomSource()
{
    try
    {
        var url = "https://your-custom-vpn-list.com/vpn.txt";
        var response = await _httpClient.GetStringAsync(url);
        
        // Parse your custom format
        var subnets = ParseCustomFormat(response);
        return subnets;
    }
    catch (Exception ex)
    {
        Mod.Log($"Custom source fetch failed: {ex.Message}", ModManager.LogLevel.Debug);
        return null;
    }
}
```

2. **Add to source list:**
```csharp
var sources = new List<Func<Task<Dictionary<string, List<string>>?>>>
{
    FetchFromIP2LocationLite,
    FetchFromMaxMindLite,
    FetchFromCommunitySources,
    FetchFromCustomSource,  // Add your custom source
    FetchFromBackupSources
};
```

### Scheduled Updates

For more control over update timing:

```csharp
// Custom update schedule
public async Task ScheduleVPNUpdates()
{
    var updateTimes = new[]
    {
        new TimeSpan(2, 0, 0),   // 2:00 AM
        new TimeSpan(10, 0, 0),  // 10:00 AM
        new TimeSpan(18, 0, 0)   // 6:00 PM
    };
    
    foreach (var time in updateTimes)
    {
        var delay = CalculateDelayUntil(time);
        _ = Task.Run(async () =>
        {
            await Task.Delay(delay);
            await ForceVPNSubnetUpdate();
        });
    }
}
```

## Best Practices

### 1. **Data Quality**
- Use multiple sources for redundancy
- Validate IP ranges before storage
- Regular backup of VPN subnet data
- Monitor detection accuracy

### 2. **Performance**
- Keep update intervals reasonable (24 hours minimum)
- Monitor memory usage
- Use appropriate cache sizes
- Implement rate limiting for external APIs

### 3. **Security**
- Validate all external data
- Use HTTPS for data fetching
- Implement timeout handling
- Log all update activities

### 4. **Maintenance**
- Regular review of data sources
- Monitor community source quality
- Update MaxMind license keys
- Backup configuration files

## Future Enhancements

### Planned Features
1. **Machine Learning Detection**: Pattern-based VPN identification
2. **Real-time Updates**: Live VPN database updates
3. **Custom Rules**: User-defined detection criteria
4. **API Endpoints**: Query detection status programmatically

### Integration Possibilities
1. **Discord Webhooks**: Alert admins of VPN usage
2. **Database Logging**: Store detection results for analysis
3. **Dashboard Integration**: Web-based monitoring interface
4. **Automated Blocking**: Integrate with server ban systems

---

## Quick Start Checklist

- [ ] Verify `EnableVPNSubnetUpdates` is `true` in settings
- [ ] Ensure `vpn_subnets.json` exists and is readable
- [ ] Check network connectivity to external sources
- [ ] Monitor logs for successful updates
- [ ] Test VPN detection with known VPN IPs
- [ ] Configure update frequency as needed
- [ ] Set up monitoring and alerting

The system is now fully functional and will automatically capture VPN addresses from multiple reliable sources. No manual intervention is required for basic operation. 