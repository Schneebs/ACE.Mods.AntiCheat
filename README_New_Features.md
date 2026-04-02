# AntiCheat System - New Features Overview

## Introduction

The AntiCheat system has been enhanced with new capabilities for detecting and monitoring potential cheating behaviors, particularly focusing on VPN detection and multi-client usage patterns. This document provides a high-level overview of how these new additions work.

## Core Components

### 1. VPNDetector Class (`Lib/VPNDetector.cs`)

The `VPNDetector` class is responsible for identifying IP addresses that belong to known VPN services or private networks.

#### Key Features:

- **VPN Subnet Detection**: Maintains a database of known VPN provider IP ranges (ExpressVPN, NordVPN, CyberGhost, etc.)
- **Private Network Detection**: Identifies IPs from private network ranges (10.0.0.0/8, 172.16.0.0/12, 192.168.0.0/16, etc.)
- **Dynamic Updates**: Can fetch updated VPN subnet information from community sources
- **CIDR Support**: Handles Classless Inter-Domain Routing notation for IP range calculations

#### How It Works:

1. **IP Classification**: When an IP address is checked, it's first evaluated against private network ranges
2. **VPN Matching**: The IP is then compared against known VPN provider subnet ranges
3. **Range Calculation**: Uses CIDR notation to calculate start and end IP addresses for subnet ranges
4. **Caching**: Maintains an in-memory cache of VPN subnets for performance

#### Example Usage:
```csharp
var vpnDetector = new VPNDetector();
bool isVPN = vpnDetector.IsVPN("185.199.108.5"); // Would return true for ExpressVPN
bool isPrivate = vpnDetector.IsVPN("192.168.1.1"); // Would return true for private network
```

### 2. MultiClientDetector Class (`MultiClientDetector.cs`)

The `MultiClientDetector` class monitors player connections and disconnections to detect potential multi-client usage patterns.

#### Key Features:

- **Connection Tracking**: Monitors when players connect and disconnect from the server
- **IP Address Logging**: Records IP addresses used by each account
- **Location Monitoring**: Tracks player positions during gameplay
- **Marketplace Detection**: Identifies when players are at marketplace locations (currently placeholder)

#### How It Works:

1. **Event Handling**: Listens to player disconnect events from the game server
2. **Data Collection**: Gathers player information including:
   - IP address (defaults to "0.0.0.0" if unavailable)
   - Account ID
   - Player name
   - Current location coordinates
3. **Logging**: Records all collected data for analysis and monitoring
4. **Future Enhancement**: Will include marketplace position checking for suspicious activity patterns

#### Example Data Collected:
```
Player: TestPlayer123
Account ID: 12345
IP Address: 107.206.130.224
Location: X: 123.45, Y: 67.89, Z: 12.34
Disconnect Time: 8/7/2025 2:57:16 AM
```

## Data Sources

### VPN Subnet Database (`vpn_subnets.json`)

The system maintains a comprehensive database of known VPN provider IP ranges:

- **ExpressVPN**: 185.199.108.0-185.199.127.255, 45.67.212.0-45.67.223.255
- **NordVPN**: 89.187.160.0-89.187.191.255
- **CyberGhost**: 31.13.64.0-31.13.255.255
- **Surfshark**: 103.21.244.0-103.21.255.255, 104.16.0.0-104.16.255.255
- **ProtonVPN**: 37.19.200.0-37.19.255.255
- **IPVanish**: 23.19.0.0-23.19.255.255

### Community Sources

The system can automatically fetch updated VPN subnet information from:
- IP2Location LITE database
- Community-maintained lists
- Backup sources

## Recent Technical Improvements

### 1. Nullable Return Types

The `GetIPRangeFromCIDR` method now returns a nullable tuple `(IPAddress start, IPAddress end)?`, allowing it to return `null` when errors occur instead of throwing exceptions.

### 2. Property Name Corrections

Fixed incorrect property references:
- `player.Session.EndPoint` → `player.Session.EndPointC2S`
- `player.Account.Id` → `player.Account.AccountId`

### 3. Error Handling

Enhanced error handling throughout the system:
- Graceful fallbacks when IP addresses are unavailable
- Exception handling in CIDR calculations
- Nullable tuple deconstruction with proper null checks

## Use Cases

### 1. VPN Detection

**Scenario**: A player connects from IP address 185.199.108.5
**Detection**: System identifies this as an ExpressVPN IP range
**Action**: Logs the connection for monitoring purposes

**Scenario**: A player connects from IP address 192.168.1.100
**Detection**: System identifies this as a private network IP
**Action**: Logs the connection (may indicate local testing or legitimate private network usage)

### 2. Multi-Client Monitoring

**Scenario**: Multiple accounts connect from the same IP address
**Detection**: System logs all connections with timestamps and locations
**Action**: Provides data for administrators to investigate potential multi-client usage

**Scenario**: An account connects from different IP addresses in rapid succession
**Detection**: System logs all connection details
**Action**: Helps identify potential account sharing or suspicious login patterns

## Configuration

### Settings (`Settings.cs`)

The system can be configured through:
- `Settings.json` file for VPN detection parameters
- Mod configuration for logging levels and detection sensitivity
- Customizable VPN subnet update intervals

### Logging Levels

- **Debug**: Detailed VPN detection and IP range calculations
- **Info**: Player connection/disconnection events
- **Warning**: Suspicious activity patterns
- **Error**: System errors and configuration issues

## Future Enhancements

### 1. Marketplace Position Detection

Currently implemented as a placeholder, this feature will:
- Query the game database for marketplace portal positions
- Calculate player distance from marketplace
- Flag suspicious activity patterns around marketplace areas

### 2. Advanced Pattern Recognition

Future versions may include:
- Machine learning for detecting unusual connection patterns
- Geographic location analysis
- Time-based pattern recognition
- Integration with other anti-cheat systems

### 3. Real-Time Alerts

Potential features:
- Immediate notifications for suspicious activity
- Automated response actions
- Integration with server administration tools
- Web-based monitoring dashboard

## Performance Considerations

- **Caching**: VPN subnet data is cached in memory for fast lookups
- **Non-blocking Updates**: VPN subnet updates happen in the background
- **Efficient IP Range Checking**: Uses optimized algorithms for IP range validation
- **Minimal Memory Footprint**: Only stores essential connection data

## Security and Privacy

- **Data Retention**: Connection logs are maintained for analysis purposes
- **IP Privacy**: IP addresses are logged but not exposed to other players
- **Configurable Logging**: Administrators can control what data is collected
- **Audit Trail**: All detection activities are logged for transparency

## Conclusion

The new AntiCheat additions provide a robust foundation for detecting and monitoring potential cheating behaviors in the game. By combining VPN detection with multi-client monitoring, the system offers comprehensive coverage of common cheating vectors while maintaining performance and reliability.

The modular design allows for easy extension and customization, making it adaptable to evolving cheating techniques and server-specific requirements. 