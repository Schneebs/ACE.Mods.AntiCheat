
namespace ACE.Mods.AntiCheat;

public class Settings
{
    /// <summary>
    /// Whether or not admins are immune to the anti-cheat
    /// </summary>
    public bool AdminsAreImmune { get; set; } = true;

    /// <summary>
    /// Whether or not cloaked players are immune to the anti-cheat
    /// </summary>
    public bool CloakedPlayersAreImmune { get; set; } = true;

    /// <summary>
    /// Enable anti-blink, which prevents players from teleporting through closed doors
    /// </summary>
    public bool EnableAntiBlink { get; set; } = true;

    /// <summary>
    /// Enable verbose anti-blink logging to diagnose why detections may not be firing.
    /// Logs every position update, object scan, and door check. Disable in production.
    /// </summary>
    public bool AntiBlinkVerboseLogging { get; set; } = false;

    /// <summary>
    /// The number of milliseconds between anti-blink logging for a specific player
    /// </summary>
    public double AntiBlinkLogIntervalMilliseconds { get; set; } = 5000;

    /// <summary>
    /// "Monster Doors" like Mana Barrier will be considered during the anti-blink check when enabled.
    /// 
    /// Mana Barrier Example: 0x00E5018F [89.917587 -220.604492 -77.994995] 0.995699 0.000000 0.000000 0.092643
    /// </summary>
    public bool AntiBlinkMonsterDoors { get; set; } = true;

    /// <summary>
    /// The height limit to check for doors when anti-blink is enabled
    /// </summary>
    public float AntiBlinkZHeightLimit { get; set; } = 2.6f;

    // Multi-Client Detection Settings
    /// <summary>
    /// Enable multi-client detection to detect suspicious patterns of multiple clients disconnecting
    /// </summary>
    public bool EnableMultiClientDetection { get; set; } = true;

    /// <summary>
    /// Time window in seconds to monitor for suspicious disconnection patterns
    /// </summary>
    public int MultiClientDetectionTimeWindowSeconds { get; set; } = 30;

    /// <summary>
    /// Minimum number of disconnections required to trigger pattern analysis
    /// </summary>
    public int MultiClientDetectionMinDisconnections { get; set; } = 3;

    /// <summary>
    /// Number of VPN disconnections required to flag as suspicious (when combined with non-VPN)
    /// </summary>
    public int MultiClientDetectionVPNThreshold { get; set; } = 3;

    /// <summary>
    /// Number of non-VPN disconnections required to flag as suspicious (when combined with VPN)
    /// </summary>
    public int MultiClientDetectionNonVPNThreshold { get; set; } = 2;

    /// <summary>
    /// Number of non-marketplace disconnections required to flag as suspicious
    /// </summary>
    public int MultiClientDetectionNonMarketplaceThreshold { get; set; } = 2;

    /// <summary>
    /// Total number of disconnections required to flag as suspicious regardless of other criteria
    /// </summary>
    public int MultiClientDetectionTotalThreshold { get; set; } = 5;

    /// <summary>
    /// Enable external VPN detection service (requires API key configuration)
    /// </summary>
    public bool EnableExternalVPNDetection { get; set; } = false;

    /// <summary>
    /// External VPN detection service API key (if using external service)
    /// </summary>
    public string ExternalVPNDetectionAPIKey { get; set; } = string.Empty;

    /// <summary>
    /// External VPN detection service URL (if using external service)
    /// </summary>
    public string ExternalVPNDetectionServiceURL { get; set; } = string.Empty;

    // VPN Subnet Update Settings
    /// <summary>
    /// Enable automatic VPN subnet updates from external sources
    /// </summary>
    public bool EnableVPNSubnetUpdates { get; set; } = true;

    /// <summary>
    /// Interval in hours between automatic VPN subnet updates
    /// </summary>
    public int VPNSubnetUpdateIntervalHours { get; set; } = 24;

    /// <summary>
    /// VPN detection cache expiration time in minutes
    /// </summary>
    public int VPNDetectionCacheExpirationMinutes { get; set; } = 30;

    /// <summary>
    /// Maximum number of VPN cache entries to store
    /// </summary>
    public int VPNDetectionMaxCacheSize { get; set; } = 10000;

    /// <summary>
    /// Enable community VPN source fetching
    /// </summary>
    public bool EnableCommunityVPNSources { get; set; } = true;

    /// <summary>
    /// Enable IP2Location LITE database fetching
    /// </summary>
    public bool EnableIP2LocationLite { get; set; } = true;

    /// <summary>
    /// Enable MaxMind GeoLite2 database fetching (requires license key)
    /// </summary>
    public bool EnableMaxMindLite { get; set; } = false;

    /// <summary>
    /// MaxMind license key for GeoLite2 database access
    /// </summary>
    public string MaxMindLicenseKey { get; set; } = string.Empty;
}


