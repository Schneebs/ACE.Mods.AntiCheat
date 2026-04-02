
using ACE.Mods.AntiCheat.Lib;
using ACE.Server.Network;
using ACE.Server.Network.Sequence;

namespace ACE.Mods.AntiCheat
{
    internal class AntiBlink
    {
        private Settings Settings => PatchClass.Settings;
        private DateTime _serverStart = DateTime.UtcNow - TimeSpan.FromSeconds(5);

        public AntiBlink()
        {
            Mod.Log($"Enabling AntiBlink: AntiBlinkMonsterDoors:{Settings.AntiBlinkMonsterDoors}", ModManager.LogLevel.Info);
        }


        internal bool PreSetRequestedLocation(Position newPosition, Player __instance)
        {
            bool verbose = Settings.AntiBlinkVerboseLogging;

            // bail early if the player is teleporting, or an admin, or cloaked
            if (__instance.Teleporting) {
                if (verbose) Mod.Log($"[AntiBlink] {__instance.Name}: skipped - teleporting", ModManager.LogLevel.Info);
                return true;
            }
            if (Settings.AdminsAreImmune && __instance.IsAdmin) {
                if (verbose) Mod.Log($"[AntiBlink] {__instance.Name}: skipped - admin immune", ModManager.LogLevel.Info);
                return true;
            }
            if (Settings.CloakedPlayersAreImmune && __instance.Cloaked == true) {
                if (verbose) Mod.Log($"[AntiBlink] {__instance.Name}: skipped - cloaked immune", ModManager.LogLevel.Info);
                return true;
            }

            var now = DateTime.UtcNow;
            var currentPosition = __instance.Location;

            var visibleObjects = __instance.PhysicsObj.ObjMaint.GetVisibleObjects(__instance.PhysicsObj.CurCell);

            // Verbose header: only log when there are candidates to examine (not every footstep).
            int doorsChecked = 0;

            foreach (var obj in visibleObjects)
            {
                if (obj.Position.Landblock != currentPosition.Landblock)
                    continue;

                float zDiff = Math.Abs(obj.Position.Frame.Origin.Z - currentPosition.PositionZ);
                if (zDiff > Settings.AntiBlinkZHeightLimit)
                    continue;

                var wo = obj.WeenieObj?.WorldObject;
                if (wo == null)
                    continue;

                Vector3? collisionPoint = null;

                bool isNonEtherealDoor = IsNonEtherealDoor(obj);
                bool isMonsterDoor = !isNonEtherealDoor && Settings.AntiBlinkMonsterDoors && IsMonsterDoor(obj);

                if (!isNonEtherealDoor && !isMonsterDoor)
                    continue;

                // Log the header once, just before the first door candidate this move.
                if (verbose && doorsChecked == 0)
                    Mod.Log($"[AntiBlink] {__instance.Name}: move {currentPosition} → {newPosition} ({visibleObjects.Count} vis, cell={__instance.PhysicsObj.CurCell?.ID:X8})", ModManager.LogLevel.Info);

                doorsChecked++;

                if (isNonEtherealDoor)
                    collisionPoint = CollisionHelpers.GetDoorCollisionPoint(currentPosition, newPosition, wo);
                else if (isMonsterDoor)
                    collisionPoint = CollisionHelpers.GetDoorCollisionPoint(currentPosition, newPosition, wo);

                if (verbose)
                    Mod.Log($"[AntiBlink]   0x{wo.Guid.Full:X8} '{wo.Name}' ({(isNonEtherealDoor ? "door" : "monsterdoor")}, state={obj.State}): {(collisionPoint.HasValue ? $"COLLISION at {collisionPoint.Value}" : "no intersection")}", ModManager.LogLevel.Info);

                if (collisionPoint.HasValue)
                {
                    var lastBlink = __instance.GetProperty(PropertyFloat.AbuseLoggingTimestamp) ?? 0;
                    if (Math.Abs(lastBlink - (_serverStart - now).TotalMilliseconds) > Settings.AntiBlinkLogIntervalMilliseconds)
                    {
                        __instance.SetProperty(PropertyFloat.AbuseLoggingTimestamp, (_serverStart - now).TotalMilliseconds);
                        Mod.Log($"[AntiBlink] BLOCKED {__instance.Name} through 0x{wo.Guid.Full:X8} '{wo.Name}' at {obj.Position}", ModManager.LogLevel.Warn);

                        if (Settings.AntiBlinkJailOnDetection)
                            TrySendToJail(__instance);
                    }
                    __instance.Sequences.GetNextSequence(SequenceType.ObjectForcePosition);
                    __instance.SendUpdatePosition();

                    return false;
                }
            }

            if (verbose && doorsChecked > 0)
                Mod.Log($"[AntiBlink] {__instance.Name}: {doorsChecked} door(s) checked, no blink detected", ModManager.LogLevel.Info);

            return true;
        }

        // Cached once per AntiBlink lifetime; null means the server doesn't have the jail feature yet.
        private static readonly MethodInfo? _sendToJailMethod =
            typeof(Player).GetMethod("SendToJail", BindingFlags.Public | BindingFlags.Instance);

        private static void TrySendToJail(Player player)
        {
            if (_sendToJailMethod == null)
            {
                Mod.Log("[AntiBlink] SendToJail not available — rebuild the server with Player_Jail.cs to enable jailing.", ModManager.LogLevel.Warn);
                return;
            }
            try
            {
                Mod.Log($"[AntiBlink] Sending {player.Name} to jail.", ModManager.LogLevel.Warn);
                _sendToJailMethod.Invoke(player, null);
            }
            catch (Exception ex)
            {
                Mod.Log($"[AntiBlink] SendToJail threw: {ex.InnerException?.Message ?? ex.Message}", ModManager.LogLevel.Error);
            }
        }

        private bool IsMonsterDoor(PhysicsObj obj)
        {
            return 
                obj.WeenieObj != null
                && obj.WeenieObj.IsMonster
                && obj.WeenieObj.WorldObject.GetProperty(PropertyBool.AiImmobile) == true
                && obj.WeenieObj.WorldObject.GetProperty(PropertyInt.CreatureType) == (int)CreatureType.Wall;
        }

        private bool IsNonEtherealDoor(PhysicsObj obj)
        {
            return 
                obj.WeenieObj != null
                && obj.WeenieObj.WorldObject is Door door
                && obj.State.HasFlag(PhysicsState.Ethereal) != true;
        }

    }
}