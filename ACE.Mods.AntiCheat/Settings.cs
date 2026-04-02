
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
    /// When true, players caught blinking through a closed door are sent to jail
    /// via Player.SendToJail(). Jail duration is controlled by the server's
    /// ucm_jail_duration_seconds config. Admins/cloaked players are never jailed
    /// (they are skipped before the blink check runs).
    /// </summary>
    public bool AntiBlinkJailOnDetection { get; set; } = false;

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
}


