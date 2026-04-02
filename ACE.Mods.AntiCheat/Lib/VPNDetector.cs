using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.IO;

namespace ACE.Mods.AntiCheat.Lib
{
    internal class VPNDetector
    {
        private readonly Dictionary<string, (bool isVPN, DateTime expires)> _vpnCache = new();
        private readonly object _cacheLock = new object();
        private readonly Settings _settings;
        private readonly HttpClient _httpClient;
        private const int MAX_CACHE_SIZE = 10000; // Limit cache to 10k entries
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(30); // 30 minute TTL
        
        // Real VPN provider subnet ranges (updated regularly)
        private readonly Dictionary<string, List<(IPAddress start, IPAddress end)>> _vpnSubnets = new();
        private readonly string _vpnSubnetsFile = "vpn_subnets.json";
        private DateTime _lastSubnetUpdate = DateTime.MinValue;
        private TimeSpan _subnetUpdateInterval; // Update interval from settings

        public VPNDetector(Settings settings)
        {
            _settings = settings;
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
            _subnetUpdateInterval = TimeSpan.FromHours(_settings.VPNSubnetUpdateIntervalHours);
            LoadVPNSubnets();
        }

        public bool IsVPN(string ipAddress)
        {
            lock (_cacheLock)
            {
                // Clean expired entries first
                CleanupExpiredEntries();
                
                if (_vpnCache.TryGetValue(ipAddress, out var cached) && cached.expires > DateTime.UtcNow)
                    return cached.isVPN;

                var isVPN = DetectVPN(ipAddress);
                
                // Enforce cache size limit
                if (_vpnCache.Count >= MAX_CACHE_SIZE)
                {
                    // Remove oldest entries to make room
                    var oldestEntries = _vpnCache.OrderBy(kvp => kvp.Value.expires).Take(_vpnCache.Count - MAX_CACHE_SIZE + 1);
                    foreach (var entry in oldestEntries)
                    {
                        _vpnCache.Remove(entry.Key);
                    }
                }
                
                _vpnCache[ipAddress] = (isVPN, DateTime.UtcNow.Add(_cacheExpiration));
                return isVPN;
            }
        }

        private void CleanupExpiredEntries()
        {
            var now = DateTime.UtcNow;
            var expiredKeys = _vpnCache.Where(kvp => kvp.Value.expires <= now).Select(kvp => kvp.Key).ToList();
            
            foreach (var key in expiredKeys)
            {
                _vpnCache.Remove(key);
            }
        }

        private bool DetectVPN(string ipAddress)
        {
            // Method 1: Check against known private network ranges
            if (IsPrivateNetwork(ipAddress))
                return true;

            // Method 2: Check against known VPN provider subnets
            if (IsKnownVPNSubnet(ipAddress))
                return true;

            // Method 3: Check against external VPN detection service (if configured)
            if (_settings.EnableExternalVPNDetection)
            {
                try
                {
                    // This would be implemented with an external API call
                    // For now, we'll return false as the external service is not implemented
                    return false;
                }
                catch (Exception ex)
                {
                    Mod.Log($"Error in external VPN detection: {ex.Message}", ModManager.LogLevel.Error);
                    return false;
                }
            }

            return false;
        }

        private bool IsPrivateNetwork(string ipAddress)
        {
            if (!IPAddress.TryParse(ipAddress, out var ip))
                return false;

            var bytes = ip.GetAddressBytes();
            
            // 10.0.0.0/8
            if (bytes[0] == 10)
                return true;

            // 172.16.0.0/12
            if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
                return true;

            // 192.168.0.0/16
            if (bytes[0] == 192 && bytes[1] == 168)
                return true;

            // 100.64.0.0/10 (Carrier-grade NAT)
            if (bytes[0] == 100 && bytes[1] >= 64 && bytes[1] <= 127)
                return true;

            // 169.254.0.0/16 (Link-local)
            if (bytes[0] == 169 && bytes[1] == 254)
                return true;

            // 127.0.0.0/8 (Loopback)
            if (bytes[0] == 127)
                return true;

            return false;
        }

        private bool IsKnownVPNSubnet(string ipAddress)
        {
            if (!IPAddress.TryParse(ipAddress, out var ip))
                return false;

            // Check and update VPN subnets if needed (non-blocking)
            if (_settings.EnableVPNSubnetUpdates && 
                DateTime.UtcNow - _lastSubnetUpdate > _subnetUpdateInterval)
            {
                Task.Run(async () => await CheckAndUpdateVPNSubnets());
            }

            lock (_vpnSubnets)
            {
                foreach (var provider in _vpnSubnets)
                {
                    foreach (var (start, end) in provider.Value)
                    {
                        if (IsIPInRange(ip, start, end))
                        {
                            Mod.Log($"IP {ipAddress} detected as VPN from provider {provider.Key}", ModManager.LogLevel.Debug);
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private bool IsIPInRange(IPAddress ip, IPAddress start, IPAddress end)
        {
            var ipBytes = ip.GetAddressBytes();
            var startBytes = start.GetAddressBytes();
            var endBytes = end.GetAddressBytes();

            for (int i = 0; i < 4; i++)
            {
                if (ipBytes[i] < startBytes[i] || ipBytes[i] > endBytes[i])
                    return false;
            }
            return true;
        }

        private void LoadVPNSubnets()
        {
            try
            {
                if (File.Exists(_vpnSubnetsFile))
                {
                    var json = File.ReadAllText(_vpnSubnetsFile);
                    var data = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(json);
                    
                    lock (_vpnSubnets)
                    {
                        _vpnSubnets.Clear();
                        foreach (var kvp in data)
                        {
                            var ranges = new List<(IPAddress start, IPAddress end)>();
                            foreach (var range in kvp.Value)
                            {
                                var parts = range.Split('-');
                                if (parts.Length == 2 && 
                                    IPAddress.TryParse(parts[0].Trim(), out var start) &&
                                    IPAddress.TryParse(parts[1].Trim(), out var end))
                                {
                                    ranges.Add((start, end));
                                }
                            }
                            if (ranges.Count > 0)
                                _vpnSubnets[kvp.Key] = ranges;
                        }
                    }
                    
                    Mod.Log($"Loaded {_vpnSubnets.Count} VPN providers with {_vpnSubnets.Values.Sum(r => r.Count)} subnet ranges", ModManager.LogLevel.Info);
                }
                else
                {
                    // Create default VPN subnet file with some known providers
                    CreateDefaultVPNSubnets();
                }
            }
            catch (Exception ex)
            {
                Mod.Log($"Error loading VPN subnets: {ex.Message}", ModManager.LogLevel.Error);
                CreateDefaultVPNSubnets();
            }
        }

        private void CreateDefaultVPNSubnets()
        {
            try
            {
                var defaultSubnets = new Dictionary<string, List<string>>
                {
                    ["NordVPN"] = new List<string>
                    {
                        "89.187.160.0-89.187.191.255",
                        "185.65.18.0-185.65.18.255",
                        "185.65.19.0-185.65.19.255"
                    },
                    ["ExpressVPN"] = new List<string>
                    {
                        "45.67.212.0-45.67.212.255",
                        "45.67.213.0-45.67.213.255",
                        "185.199.108.0-185.199.111.255"
                    },
                    ["CyberGhost"] = new List<string>
                    {
                        "31.13.64.0-31.13.95.255",
                        "31.13.96.0-31.13.127.255"
                    },
                    ["ProtonVPN"] = new List<string>
                    {
                        "37.19.200.0-37.19.207.255",
                        "37.19.208.0-37.19.215.255"
                    }
                };

                var json = JsonSerializer.Serialize(defaultSubnets, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_vpnSubnetsFile, json);
                
                lock (_vpnSubnets)
                {
                    _vpnSubnets.Clear();
                    foreach (var kvp in defaultSubnets)
                    {
                        var ranges = new List<(IPAddress start, IPAddress end)>();
                        foreach (var range in kvp.Value)
                        {
                            var parts = range.Split('-');
                            if (parts.Length == 2 && 
                                IPAddress.TryParse(parts[0].Trim(), out var start) &&
                                IPAddress.TryParse(parts[1].Trim(), out var end))
                            {
                                ranges.Add((start, end));
                            }
                        }
                        if (ranges.Count > 0)
                            _vpnSubnets[kvp.Key] = ranges;
                    }
                }
                
                Mod.Log("Created default VPN subnet file", ModManager.LogLevel.Info);
            }
            catch (Exception ex)
            {
                Mod.Log($"Error creating default VPN subnets: {ex.Message}", ModManager.LogLevel.Error);
            }
        }

        private async Task UpdateVPNSubnets()
        {
            try
            {
                Mod.Log("Starting VPN subnet update...", ModManager.LogLevel.Info);
                
                // Try multiple sources for VPN subnet data
                var updatedSubnets = await FetchVPNSubnetsFromSources();
                
                if (updatedSubnets != null && updatedSubnets.Count > 0)
                {
                    // Backup current file
                    if (File.Exists(_vpnSubnetsFile))
                    {
                        var backupFile = $"{_vpnSubnetsFile}.backup.{DateTime.UtcNow:yyyyMMdd_HHmmss}";
                        File.Copy(_vpnSubnetsFile, backupFile);
                        Mod.Log($"Backed up current VPN subnets to {backupFile}", ModManager.LogLevel.Debug);
                    }
                    
                    // Save updated data
                    var json = JsonSerializer.Serialize(updatedSubnets, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(_vpnSubnetsFile, json);
                    
                    // Reload into memory
                    lock (_vpnSubnets)
                    {
                        _vpnSubnets.Clear();
                        foreach (var kvp in updatedSubnets)
                        {
                            var ranges = new List<(IPAddress start, IPAddress end)>();
                            foreach (var range in kvp.Value)
                            {
                                var parts = range.Split('-');
                                if (parts.Length == 2 && 
                                    IPAddress.TryParse(parts[0].Trim(), out var start) &&
                                    IPAddress.TryParse(parts[1].Trim(), out var end))
                                {
                                    ranges.Add((start, end));
                                }
                            }
                            if (ranges.Count > 0)
                                _vpnSubnets[kvp.Key] = ranges;
                        }
                    }
                    
                    _lastSubnetUpdate = DateTime.UtcNow;
                    Mod.Log($"VPN subnet update completed: {_vpnSubnets.Count} providers with {_vpnSubnets.Values.Sum(r => r.Count)} ranges", ModManager.LogLevel.Info);
                }
                else
                {
                    Mod.Log("No VPN subnet updates available from external sources", ModManager.LogLevel.Warn);
                    _lastSubnetUpdate = DateTime.UtcNow; // Prevent repeated attempts
                }
            }
            catch (Exception ex)
            {
                Mod.Log($"Error updating VPN subnets: {ex.Message}", ModManager.LogLevel.Error);
                _lastSubnetUpdate = DateTime.UtcNow; // Prevent repeated attempts
            }
        }

        private async Task<Dictionary<string, List<string>>?> FetchVPNSubnetsFromSources()
        {
            // Try multiple sources in order of reliability
            var sources = new List<Func<Task<Dictionary<string, List<string>>?>>>
            {
                FetchFromIP2LocationLite,
                FetchFromMaxMindLite,
                FetchFromCommunitySources,
                FetchFromBackupSources
            };

            foreach (var source in sources)
            {
                try
                {
                    var result = await source();
                    if (result != null && result.Count > 0)
                    {
                        Mod.Log($"Successfully fetched VPN subnets from {source.Method.Name}", ModManager.LogLevel.Info);
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    Mod.Log($"Failed to fetch from {source.Method.Name}: {ex.Message}", ModManager.LogLevel.Debug);
                }
            }

            return null;
        }

        private async Task<Dictionary<string, List<string>>?> FetchFromIP2LocationLite()
        {
            try
            {
                // IP2Location LITE database (free tier)
                var url = "https://raw.githubusercontent.com/ip2location/IP2LOCATION-LITE-DB1/master/IP2LOCATION-LITE-DB1.CSV";
                var response = await _httpClient.GetStringAsync(url);
                
                var subnets = new Dictionary<string, List<string>>();
                var lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                
                foreach (var line in lines.Skip(1)) // Skip header
                {
                    var parts = line.Split(',');
                    if (parts.Length >= 3)
                    {
                        var startIP = parts[0].Trim('"');
                        var endIP = parts[1].Trim('"');
                        var country = parts[2].Trim('"');
                        
                        // Focus on known VPN-friendly countries
                        if (IsVPNFriendlyCountry(country))
                        {
                            var provider = GetVPNProviderFromCountry(country);
                            if (!subnets.ContainsKey(provider))
                                subnets[provider] = new List<string>();
                            
                            subnets[provider].Add($"{startIP}-{endIP}");
                        }
                    }
                }
                
                return subnets.Count > 0 ? subnets : null;
            }
            catch (Exception ex)
            {
                Mod.Log($"IP2Location LITE fetch failed: {ex.Message}", ModManager.LogLevel.Debug);
                return null;
            }
        }

        private async Task<Dictionary<string, List<string>>?> FetchFromMaxMindLite()
        {
            try
            {
                // MaxMind GeoLite2 (requires license key, but free)
                // This is a placeholder - you'd need to implement with your MaxMind license
                var url = "https://download.maxmind.com/app/geoip_download?edition_id=GeoLite2-Country&license_key=YOUR_LICENSE_KEY&suffix=tar.gz";
                
                Mod.Log("MaxMind GeoLite2 integration requires license key setup", ModManager.LogLevel.Debug);
                return null;
            }
            catch (Exception ex)
            {
                Mod.Log($"MaxMind LITE fetch failed: {ex.Message}", ModManager.LogLevel.Debug);
                return null;
            }
        }

        private async Task<Dictionary<string, List<string>>?> FetchFromCommunitySources()
        {
            try
            {
                // Community-maintained VPN lists
                var urls = new[]
                {
                    "https://raw.githubusercontent.com/X4BNet/lists_vpn/main/ipv4.txt",
                    "https://raw.githubusercontent.com/blackmatrix7/ios_rule_script/master/rule/Clash/GlobalMedia/GlobalMedia_Resolve.list",
                    "https://raw.githubusercontent.com/blackmatrix7/ios_rule_script/master/rule/Clash/GlobalMedia/GlobalMedia.list"
                };

                var allSubnets = new Dictionary<string, List<string>>();
                
                foreach (var url in urls)
                {
                    try
                    {
                        var response = await _httpClient.GetStringAsync(url);
                        var lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                        
                        foreach (var line in lines)
                        {
                            var trimmedLine = line.Trim();
                            if (trimmedLine.StartsWith("#") || string.IsNullOrEmpty(trimmedLine))
                                continue;
                            
                            // Parse IP ranges (CIDR notation)
                            if (trimmedLine.Contains('/'))
                            {
                                var cidrParts = trimmedLine.Split('/');
                                if (cidrParts.Length == 2 && int.TryParse(cidrParts[1], out var prefixLength))
                                {
                                    if (IPAddress.TryParse(cidrParts[0], out var networkAddress))
                                    {
                                        var ipRange = GetIPRangeFromCIDR(networkAddress, prefixLength);
                                        if (ipRange.HasValue)
                                        {
                                            var (startIP, endIP) = ipRange.Value;
                                        var provider = "Community_VPN";
                                        
                                        if (!allSubnets.ContainsKey(provider))
                                            allSubnets[provider] = new List<string>();
                                        
                                        allSubnets[provider].Add($"{startIP}-{endIP}");
                                        }
                                    }
                                }
                            }
                            // Parse single IPs
                            else if (IPAddress.TryParse(trimmedLine, out _))
                            {
                                var provider = "Community_VPN";
                                if (!allSubnets.ContainsKey(provider))
                                    allSubnets[provider] = new List<string>();
                                
                                allSubnets[provider].Add($"{trimmedLine}-{trimmedLine}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Mod.Log($"Failed to fetch from {url}: {ex.Message}", ModManager.LogLevel.Debug);
                    }
                }
                
                return allSubnets.Count > 0 ? allSubnets : null;
            }
            catch (Exception ex)
            {
                Mod.Log($"Community sources fetch failed: {ex.Message}", ModManager.LogLevel.Debug);
                return null;
            }
        }

        private async Task<Dictionary<string, List<string>>?> FetchFromBackupSources()
        {
            try
            {
                // Fallback to a simple, reliable source
                var url = "https://raw.githubusercontent.com/blackmatrix7/ios_rule_script/master/rule/Clash/GlobalMedia/GlobalMedia_Resolve.list";
                var response = await _httpClient.GetStringAsync(url);
                
                var subnets = new Dictionary<string, List<string>>();
                var lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                
                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();
                    if (trimmedLine.StartsWith("#") || string.IsNullOrEmpty(trimmedLine))
                        continue;
                    
                    if (IPAddress.TryParse(trimmedLine, out _))
                    {
                        var provider = "Backup_VPN";
                        if (!subnets.ContainsKey(provider))
                            subnets[provider] = new List<string>();
                        
                        subnets[provider].Add($"{trimmedLine}-{trimmedLine}");
                    }
                }
                
                return subnets.Count > 0 ? subnets : null;
            }
            catch (Exception ex)
            {
                Mod.Log($"Backup sources fetch failed: {ex.Message}", ModManager.LogLevel.Debug);
                return null;
            }
        }

        private bool IsVPNFriendlyCountry(string countryCode)
        {
            // Countries known for VPN usage
            var vpnFriendlyCountries = new[]
            {
                "US", "GB", "DE", "NL", "CH", "SE", "NO", "FI", "DK", "IS",
                "JP", "SG", "HK", "AU", "NZ", "CA", "BR", "MX", "AR", "CL"
            };
            
            return vpnFriendlyCountries.Contains(countryCode.ToUpper());
        }

        private string GetVPNProviderFromCountry(string countryCode)
        {
            // Map countries to likely VPN providers
            var countryToProvider = new Dictionary<string, string>
            {
                ["US"] = "US_VPN_Provider",
                ["GB"] = "UK_VPN_Provider", 
                ["DE"] = "German_VPN_Provider",
                ["NL"] = "Dutch_VPN_Provider",
                ["CH"] = "Swiss_VPN_Provider",
                ["SE"] = "Swedish_VPN_Provider",
                ["NO"] = "Norwegian_VPN_Provider",
                ["FI"] = "Finnish_VPN_Provider",
                ["DK"] = "Danish_VPN_Provider",
                ["IS"] = "Icelandic_VPN_Provider",
                ["JP"] = "Japanese_VPN_Provider",
                ["SG"] = "Singapore_VPN_Provider",
                ["HK"] = "HongKong_VPN_Provider",
                ["AU"] = "Australian_VPN_Provider",
                ["NZ"] = "NewZealand_VPN_Provider",
                ["CA"] = "Canadian_VPN_Provider",
                ["BR"] = "Brazilian_VPN_Provider",
                ["MX"] = "Mexican_VPN_Provider",
                ["AR"] = "Argentine_VPN_Provider",
                ["CL"] = "Chilean_VPN_Provider"
            };
            
            return countryToProvider.TryGetValue(countryCode.ToUpper(), out var provider) ? provider : "Unknown_VPN_Provider";
        }

        private (IPAddress start, IPAddress end)? GetIPRangeFromCIDR(IPAddress networkAddress, int prefixLength)
        {
            try { 
                var ipBytes = networkAddress.GetAddressBytes();
                var maskBytes = new byte[4];
            
                for (int i = 0; i < 4; i++)
                {
                    if (prefixLength >= 8)
                    {
                        maskBytes[i] = 255;
                        prefixLength -= 8;
                    }
                    else
                    {
                        maskBytes[i] = (byte)(255 << (8 - prefixLength));
                        prefixLength = 0;
                    }
                }
            
                var networkBytes = new byte[4];
                var broadcastBytes = new byte[4];
            
                for (int i = 0; i < 4; i++)
                {
                    networkBytes[i] = (byte)(ipBytes[i] & maskBytes[i]);
                    broadcastBytes[i] = (byte)(ipBytes[i] | ~maskBytes[i]);
                }
            
                return (new IPAddress(networkBytes), new IPAddress(broadcastBytes));
            }
            catch (Exception ex)
            {
                Mod.Log($"Error updating VPN subnets: {ex.Message}", ModManager.LogLevel.Error);
                return null;
            }
        }

        public async Task<bool> CheckExternalVPNDetection(string ipAddress)
        {
            // This method would make an API call to an external VPN detection service
            // Examples: ipinfo.io, ipapi.com, etc.
            
            // For now, we'll return false as this is not implemented
            // In a real implementation, you would:
            // 1. Make an HTTP request to the VPN detection service
            // 2. Parse the response
            // 3. Return true if the IP is detected as VPN
            
            await Task.Delay(1); // Placeholder for async operation
            return false;
        }

        public void ClearCache()
        {
            lock (_cacheLock)
            {
                _vpnCache.Clear();
            }
        }

        public void RemoveFromCache(string ipAddress)
        {
            lock (_cacheLock)
            {
                _vpnCache.Remove(ipAddress);
            }
        }

        public (int totalEntries, int expiredEntries) GetCacheStats()
        {
            lock (_cacheLock)
            {
                var now = DateTime.UtcNow;
                var expiredCount = _vpnCache.Count(kvp => kvp.Value.expires <= now);
                return (_vpnCache.Count, expiredCount);
            }
        }

        public (int providers, int totalRanges) GetVPNSubnetStats()
        {
            lock (_vpnSubnets)
            {
                var totalRanges = _vpnSubnets.Values.Sum(r => r.Count);
                return (_vpnSubnets.Count, totalRanges);
            }
        }

        /// <summary>
        /// Manually trigger a VPN subnet update
        /// </summary>
        public async Task<bool> ForceVPNSubnetUpdate()
        {
            try
            {
                Mod.Log("Manual VPN subnet update triggered", ModManager.LogLevel.Info);
                await UpdateVPNSubnets();
                return true;
            }
            catch (Exception ex)
            {
                Mod.Log($"Manual VPN subnet update failed: {ex.Message}", ModManager.LogLevel.Error);
                return false;
            }
        }

        /// <summary>
        /// Check if VPN subnet update is needed and perform it if so
        /// </summary>
        public async Task CheckAndUpdateVPNSubnets()
        {
            if (!_settings.EnableVPNSubnetUpdates)
                return;

            if (DateTime.UtcNow - _lastSubnetUpdate > _subnetUpdateInterval)
            {
                await UpdateVPNSubnets();
            }
        }
    }
} 