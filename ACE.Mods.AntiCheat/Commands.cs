
namespace ACE.Mods.AntiCheat;

internal static class Commands
{
    internal static void Register()
    {
        const string usage = "status | blink <on|off> | verbose <on|off> | adminimmune <on|off> | cloakimmune <on|off> | reload";
        const string desc  = "Configure the AntiCheat mod on the fly. Changes are in-memory only; edit Settings.json for persistence.";
        CommandManager.TryAddCommand(Handle, "anticheat", AccessLevel.Developer, CommandHandlerFlag.None, desc, usage);
        CommandManager.TryAddCommand(Handle, "ac",         AccessLevel.Developer, CommandHandlerFlag.None, desc, $"(alias for /anticheat) {usage}");
    }

    // ─── helpers ─────────────────────────────────────────────────────────────

    private static void Say(Session? session, string msg)
    {
        if (session != null)
            session.Network.EnqueueSend(new GameMessageSystemChat(msg, ChatMessageType.System));
        else
            Mod.Log(msg, ModManager.LogLevel.Info);
    }

    private static bool TryParseBool(string token, out bool value)
    {
        if (token.Equals("on",  StringComparison.OrdinalIgnoreCase) ||
            token.Equals("1",   StringComparison.OrdinalIgnoreCase) ||
            token.Equals("true",StringComparison.OrdinalIgnoreCase))
        { value = true;  return true; }

        if (token.Equals("off",  StringComparison.OrdinalIgnoreCase) ||
            token.Equals("0",    StringComparison.OrdinalIgnoreCase) ||
            token.Equals("false",StringComparison.OrdinalIgnoreCase))
        { value = false; return true; }

        value = false; return false;
    }

    // ─── dispatcher ──────────────────────────────────────────────────────────

    private static void Handle(Session session, params string[] parameters)
    {
        var s = PatchClass.Settings;
        if (s == null)
        {
            Say(session, "[AntiCheat] Settings not initialized yet.");
            return;
        }

        string sub = parameters.Length > 0 ? parameters[0].ToLower() : "status";

        switch (sub)
        {
            case "status":
                Status(session, s);
                break;

            case "blink":
                Toggle(session, parameters, "blink", "EnableAntiBlink",
                    v => {
                        s.EnableAntiBlink = v;
                        PatchClass.SetAntiBlink(v);
                    },
                    s.EnableAntiBlink);
                break;

            case "verbose":
                Toggle(session, parameters, "verbose", "AntiBlinkVerboseLogging",
                    v => s.AntiBlinkVerboseLogging = v,
                    s.AntiBlinkVerboseLogging);
                break;

            case "adminimmune":
                Toggle(session, parameters, "adminimmune", "AdminsAreImmune",
                    v => s.AdminsAreImmune = v,
                    s.AdminsAreImmune);
                break;

            case "cloakimmune":
                Toggle(session, parameters, "cloakimmune", "CloakedPlayersAreImmune",
                    v => s.CloakedPlayersAreImmune = v,
                    s.CloakedPlayersAreImmune);
                break;

            case "reload":
                PatchClass.ReloadSettings();
                Say(session, "[AntiCheat] Settings reloaded from Settings.json.");
                Status(session, PatchClass.Settings!);
                break;

            default:
                Say(session, $"[AntiCheat] Unknown subcommand: '{sub}'");
                Say(session, "Usage: /anticheat status | blink <on|off> | verbose <on|off> | adminimmune <on|off> | cloakimmune <on|off> | reload");
                break;
        }
    }

    // ─── sub-handlers ────────────────────────────────────────────────────────

    private static void Status(Session? session, Settings s)
    {
        Say(session, "[AntiCheat] Current settings (in-memory):");
        Say(session, $"  blink         : {Flag(s.EnableAntiBlink)}  (AntiBlink instance: {(PatchClass.AntiBlink != null ? "active" : "null")})");
        Say(session, $"  verbose       : {Flag(s.AntiBlinkVerboseLogging)}");
        Say(session, $"  adminimmune   : {Flag(s.AdminsAreImmune)}");
        Say(session, $"  cloakimmune   : {Flag(s.CloakedPlayersAreImmune)}");
        Say(session, "  (changes reset on server restart — edit Settings.json to persist)");
    }

    private static void Toggle(Session? session, string[] parameters, string name, string settingName,
        Action<bool> apply, bool current)
    {
        if (parameters.Length < 2)
        {
            // No value supplied — show current state
            Say(session, $"[AntiCheat] {settingName} is currently {Flag(current)}. Use /anticheat {name} <on|off> to change.");
            return;
        }

        if (!TryParseBool(parameters[1], out bool value))
        {
            Say(session, $"[AntiCheat] Invalid value '{parameters[1]}'. Use on/off.");
            return;
        }

        apply(value);
        Say(session, $"[AntiCheat] {settingName} → {Flag(value)}");
    }

    private static string Flag(bool v) => v ? "ON" : "OFF";
}
