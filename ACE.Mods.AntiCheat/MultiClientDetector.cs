using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using ACE.Server.Network;
using ACE.Server.WorldObjects;
using ACE.Entity;
using ACE.Mods.AntiCheat.Lib;

namespace ACE.Mods.AntiCheat
{
    internal class MultiClientDetector
    {
        private Settings Settings => PatchClass.Settings;
        private readonly Dictionary<string, List<DisconnectionEvent>> _disconnectionHistory = new();
        private readonly VPNDetector _vpnDetector;
        private readonly object _lock = new object();
        
        // Resource limits to prevent memory exhaustion
        private const int MAX_EVENTS_PER_IP = 50;
        private const int MAX_IPS_TRACKED = 1000;
        private const int CLEANUP_INTERVAL_MINUTES = 5;

        public MultiClientDetector()
        {
            _vpnDetector = new VPNDetector(Settings);
            Mod.Log("Enabling MultiClientDetector", ModManager.LogLevel.Info);
        }

        public void OnPlayerDisconnect(Player player, bool clientSessionTerminatedAbruptly)
        {
            if (player?.Session?.EndPoint?.Address == null)
                return;

            var ipAddress = player.Session.EndPoint.Address.ToString();
            if (string.IsNullOrWhiteSpace(ipAddress))
                ipAddress = "0.0.0.0";
            var accountId = player.Account?.AccountId ?? 0;
            var playerName = player.Name ?? "Unknown";
            var location = player.Location;
            var isVPN = _vpnDetector.IsVPN(ipAddress);
            var timestamp = DateTime.UtcNow;

            var disconnectionEvent = new DisconnectionEvent
            {
                AccountId = accountId,
                PlayerName = playerName,
                IPAddress = ipAddress,
                IsVPN = isVPN,
                Location = location,
                Timestamp = timestamp,
                ClientSessionTerminatedAbruptly = clientSessionTerminatedAbruptly
            };

            lock (_lock)
            {
                // Enforce total IP limit
                if (_disconnectionHistory.Count >= MAX_IPS_TRACKED)
                {
                    // Remove oldest IP entries to make room
                    var oldestIPs = _disconnectionHistory
                        .OrderBy(kvp => kvp.Value.Min(e => e.Timestamp))
                        .Take(_disconnectionHistory.Count - MAX_IPS_TRACKED + 1);
                    
                    foreach (var oldIP in oldestIPs)
                    {
                        _disconnectionHistory.Remove(oldIP.Key);
                    }
                }

                // Get or create the events list for this IP
                if (!_disconnectionHistory.TryGetValue(ipAddress, out var events))
                {
                    events = new List<DisconnectionEvent>();
                    _disconnectionHistory[ipAddress] = events;
                }

                // Enforce per-IP event limit
                if (events.Count >= MAX_EVENTS_PER_IP)
                {
                    events.RemoveAt(0); // Remove oldest event
                }

                events.Add(disconnectionEvent);

                // Clean up old events (older than 5 minutes)
                var cutoffTime = timestamp.AddMinutes(-CLEANUP_INTERVAL_MINUTES);
                events.RemoveAll(e => e.Timestamp <= cutoffTime);

                // Check for suspicious patterns
                CheckForSuspiciousPatterns(ipAddress);
            }
        }



        private void CheckForSuspiciousPatterns(string ipAddress)
        {
            try
            {
                if (!_disconnectionHistory.TryGetValue(ipAddress, out var events))
                    return;

                var recentEvents = events.Where(e => e.Timestamp > DateTime.UtcNow.AddSeconds(-Settings.MultiClientDetectionTimeWindowSeconds)).ToList();

                if (recentEvents.Count < Settings.MultiClientDetectionMinDisconnections)
                    return;

                // Check for VPN pattern
                var vpnDisconnections = recentEvents.Count(e => e.IsVPN);
                var nonVpnDisconnections = recentEvents.Count(e => !e.IsVPN);

                if (vpnDisconnections >= Settings.MultiClientDetectionVPNThreshold && 
                    nonVpnDisconnections >= Settings.MultiClientDetectionNonVPNThreshold)
                {
                    ReportSuspiciousActivity(ipAddress, recentEvents, "VPN_MIXED_PATTERN");
                    return;
                }

                // Check for non-marketplace disconnections
                var nonMarketplaceDisconnections = recentEvents.Where(e => !IsAtMarketplace(e.Location)).ToList();
                if (nonMarketplaceDisconnections.Count >= Settings.MultiClientDetectionNonMarketplaceThreshold)
                {
                    ReportSuspiciousActivity(ipAddress, recentEvents, "NON_MARKETPLACE_PATTERN");
                    return;
                }

                // Check for total disconnection count
                if (recentEvents.Count >= Settings.MultiClientDetectionTotalThreshold)
                {
                    ReportSuspiciousActivity(ipAddress, recentEvents, "TOTAL_THRESHOLD_PATTERN");
                    return;
                }
            }
            catch (Exception ex)
            {
                Mod.Log($"Error checking suspicious patterns for IP {ipAddress}: {ex.Message}", ModManager.LogLevel.Error);
            }
        }

        private bool IsAtMarketplace(Position location)
        {
            try
            {
                // Check if player is at marketplace location
                // For now, we'll use a simplified approach since DatabaseManager.World is not available
                // You can implement this with your actual database access method
                
                // Placeholder: assume not at marketplace to avoid false positives
                // TODO: Implement proper marketplace position checking using your database access
                return false;
                
                // Original code (commented out until DatabaseManager is available):
                // var marketplacePosition = DatabaseManager.World.GetCachedWeenie("portalmarketplace")?.GetPosition(PositionType.Destination);
                // if (marketplacePosition == null)
                // {
                //     return false;
                // }
                // var distance = location.Distance(marketplacePosition);
                // return distance <= 50.0f;
            }
            catch (Exception ex)
            {
                Mod.Log($"Error checking marketplace location: {ex.Message}", ModManager.LogLevel.Error);
                // On error, assume not at marketplace to avoid false positives
                return false;
            }
        }

        private void ReportSuspiciousActivity(string ipAddress, List<DisconnectionEvent> events, string patternType)
        {
            try
            {
                if (events == null || events.Count == 0)
                {
                    Mod.Log($"Invalid events data for suspicious activity report - IP: {ipAddress}", ModManager.LogLevel.Error);
                    return;
                }

                var eventDetails = string.Join(", ", events.Select(e => $"{e.PlayerName}({e.AccountId})"));
                var vpnCount = events.Count(e => e.IsVPN);
                var nonVpnCount = events.Count(e => !e.IsVPN);
                var nonMarketplaceCount = events.Count(e => !IsAtMarketplace(e.Location));

                var message = $"SUSPICIOUS MULTI-CLIENT ACTIVITY DETECTED - IP: {ipAddress}, Pattern: {patternType}, " +
                             $"Total Disconnections: {events.Count}, VPN: {vpnCount}, Non-VPN: {nonVpnCount}, " +
                             $"Non-Marketplace: {nonMarketplaceCount}, Players: {eventDetails}";

                Mod.Log(message, ModManager.LogLevel.Warn);

                // Additional logging for investigation
                foreach (var evt in events)
                {
                    try
                    {
                        var locationInfo = IsAtMarketplace(evt.Location) ? "Marketplace" : $"Location: {evt.Location}";
                        Mod.Log($"  - {evt.PlayerName} (Account: {evt.AccountId}) disconnected at {evt.Timestamp:HH:mm:ss} from {evt.IPAddress} ({locationInfo})", ModManager.LogLevel.Info);
                    }
                    catch (Exception ex)
                    {
                        Mod.Log($"  - Error logging event details: {ex.Message}", ModManager.LogLevel.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                Mod.Log($"Error in ReportSuspiciousActivity for IP {ipAddress}: {ex.Message}", ModManager.LogLevel.Error);
            }
        }

        public void Cleanup()
        {
            lock (_lock)
            {
                try
                {
                    var cutoffTime = DateTime.UtcNow.AddMinutes(-10);
                    var keysToRemove = new List<string>();
                    
                    foreach (var kvp in _disconnectionHistory)
                    {
                        // Remove old events
                        kvp.Value.RemoveAll(e => e.Timestamp <= cutoffTime);
                        
                        // Mark empty lists for removal
                        if (kvp.Value.Count == 0)
                        {
                            keysToRemove.Add(kvp.Key);
                        }
                    }

                    // Remove empty IP entries
                    foreach (var key in keysToRemove)
                    {
                        _disconnectionHistory.Remove(key);
                    }

                    // Log cleanup statistics
                    var totalEvents = _disconnectionHistory.Values.Sum(events => events.Count);
                    Mod.Log($"Cleanup completed: {_disconnectionHistory.Count} IPs tracked, {totalEvents} total events", ModManager.LogLevel.Debug);
                }
                catch (Exception ex)
                {
                    Mod.Log($"Error during cleanup: {ex.Message}", ModManager.LogLevel.Error);
                }
            }
        }

        public (int totalIPs, int totalEvents) GetStats()
        {
            lock (_lock)
            {
                var totalEvents = _disconnectionHistory.Values.Sum(events => events.Count);
                return (_disconnectionHistory.Count, totalEvents);
            }
        }
    }

    internal class DisconnectionEvent
    {
        public uint AccountId { get; set; }
        public string PlayerName { get; set; } = string.Empty;
        public string IPAddress { get; set; } = string.Empty;
        public bool IsVPN { get; set; }
        public Position Location { get; set; }
        public DateTime Timestamp { get; set; }
        public bool ClientSessionTerminatedAbruptly { get; set; }
    }
} 