
using ACE.Mods.AntiCheat.Lib;
using ACE.Server.Network.Sequence;

namespace ACE.Mods.AntiCheat;

[HarmonyPatch]
public class PatchClass(BasicMod mod, string settingsName = "Settings.json") : BasicPatch<Settings>(mod, settingsName)
{
    internal static AntiBlink? AntiBlink { get; private set; }
    internal static MultiClientDetector? MultiClientDetector { get; private set; }

    // Kept so Commands.cs (static context) can reach the protected SettingsChanged handler.
    private static PatchClass? _instance;

    public override void Init()
    {
        base.Init();
        _instance = this;
        Settings ??= new Settings();  // ensure non-null before any patches fire
    }

    public override async Task OnWorldOpen()
    {
        Settings = SettingsContainer?.Settings ?? new();
        StartServices();
        StartCleanupTask();
        Commands.Register();

        Mod.Log($"[AntiBlink] OnWorldOpen: EnableAntiBlink={Settings.EnableAntiBlink}, VerboseLogging={Settings.AntiBlinkVerboseLogging}, AntiBlink={(AntiBlink != null ? "initialized" : "null")}", ModManager.LogLevel.Warn);
    }

    /// <summary>Enables or disables the AntiBlink instance (used by Commands).</summary>
    internal static void SetAntiBlink(bool enabled)
    {
        if (enabled)
            AntiBlink ??= new AntiBlink();
        else
            AntiBlink = null;
    }

    /// <summary>
    /// Re-applies whatever value SettingsContainer last loaded from disk and restarts services.
    /// The FileSystemWatcher in BasicPatch keeps SettingsContainer.Settings current, so this
    /// is equivalent to "push the latest on-disk value into the live Settings immediately".
    /// </summary>
    internal static void ReloadSettings()
    {
        _instance?.SettingsChanged(null, EventArgs.Empty);
    }

    private void StartServices()
    {
        if (Settings.EnableAntiBlink)
        {
            AntiBlink ??= new AntiBlink();
        }
        else {
            AntiBlink = null;
        }

        if (Settings.EnableMultiClientDetection)
        {
            MultiClientDetector ??= new MultiClientDetector();
        }
        else {
            MultiClientDetector = null;
        }
    }

    private void StartCleanupTask()
    {
        Task.Run(async () =>
        {
            while (true)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(5)); // Run every 5 minutes
                    MultiClientDetector?.Cleanup();
                }
                catch (Exception ex)
                {
                    Mod.Log($"Error in cleanup task: {ex.Message}", ModManager.LogLevel.Error);
                }
            }
        });
    }

    protected override void SettingsChanged(object? sender, EventArgs e)
    {
        base.SettingsChanged(sender, e);
        Settings = SettingsContainer?.Settings ?? new();
        StartServices();
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Player), nameof(Player.SetRequestedLocation), new Type[] { typeof(Position), typeof(bool) })]
    public static bool PreSetRequestedLocation(Position pos, bool broadcast, Player __instance)
    {
        if (Settings?.AntiBlinkVerboseLogging == true)
            Mod.Log($"[AntiBlink] PreSetRequestedLocation fired for {__instance?.Name}, AntiBlink={(AntiBlink != null ? "active" : "null")}", ModManager.LogLevel.Info);

        try {
            return AntiBlink?.PreSetRequestedLocation(pos, __instance) != false;
        }
        catch (Exception ex) {
            Mod.Log($"[AntiBlink] Exception in hook: {ex}", ModManager.LogLevel.Error);
        }
        
        return true;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Player), nameof(Player.LogOut_Inner))]
    public static void PostLogOut_Inner(bool clientSessionTerminatedAbruptly, Player __instance)
    {
        try {
            MultiClientDetector?.OnPlayerDisconnect(__instance, clientSessionTerminatedAbruptly);
        }
        catch (Exception ex) {
            Mod.Log($"Failed to call multi-client detector: {ex.Message}", ModManager.LogLevel.Error);
        }
    }

}