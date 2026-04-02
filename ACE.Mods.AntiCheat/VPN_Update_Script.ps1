# VPN Subnet Update Management Script
# This script helps manage and test the VPN detection system

param(
    [switch]$ForceUpdate,
    [switch]$CheckStatus,
    [switch]$TestDetection,
    [string]$TestIP = "",
    [switch]$ShowStats,
    [switch]$Backup,
    [switch]$Restore,
    [string]$BackupFile = ""
)

# Configuration
$VPNSubnetsFile = "vpn_subnets.json"
$SettingsFile = "Settings.json"
$LogFile = "vpn_update.log"

function Write-Log {
    param([string]$Message, [string]$Level = "INFO")
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $logEntry = "[$timestamp] [$Level] $Message"
    Write-Host $logEntry
    Add-Content -Path $LogFile -Value $logEntry
}

function Test-VPNDetection {
    param([string]$IPAddress)
    
    Write-Log "Testing VPN detection for IP: $IPAddress"
    
    # Check if IP is in VPN subnets
    if (Test-Path $VPNSubnetsFile) {
        try {
            $vpnData = Get-Content $VPNSubnetsFile | ConvertFrom-Json
            $found = $false
            
            foreach ($provider in $vpnData.PSObject.Properties.Name) {
                $ranges = $vpnData.$provider
                foreach ($range in $ranges) {
                    if ($range -match "-") {
                        $parts = $range.Split("-")
                        $startIP = $parts[0].Trim()
                        $endIP = $parts[1].Trim()
                        
                        if ((Test-IPInRange -IP $IPAddress -StartIP $startIP -EndIP $endIP)) {
                            Write-Log "IP $IPAddress detected as VPN from provider: $provider" "SUCCESS"
                            $found = $true
                            break
                        }
                    }
                }
                if ($found) { break }
            }
            
            if (-not $found) {
                Write-Log "IP $IPAddress not found in VPN subnets" "INFO"
            }
        }
        catch {
            Write-Log "Error testing VPN detection: $($_.Exception.Message)" "ERROR"
        }
    } else {
        Write-Log "VPN subnets file not found: $VPNSubnetsFile" "ERROR"
    }
}

function Test-IPInRange {
    param([string]$IP, [string]$StartIP, [string]$EndIP)
    
    try {
        $ipBytes = [System.Net.IPAddress]::Parse($IP).GetAddressBytes()
        $startBytes = [System.Net.IPAddress]::Parse($StartIP).GetAddressBytes()
        $endBytes = [System.Net.IPAddress]::Parse($EndIP).GetAddressBytes()
        
        for ($i = 0; $i -lt 4; $i++) {
            if ($ipBytes[$i] -lt $startBytes[$i] -or $ipBytes[$i] -gt $endBytes[$i]) {
                return $false
            }
        }
        return $true
    }
    catch {
        return $false
    }
}

function Show-VPNStats {
    if (Test-Path $VPNSubnetsFile) {
        try {
            $vpnData = Get-Content $VPNSubnetsFile | ConvertFrom-Json
            $totalProviders = $vpnData.PSObject.Properties.Name.Count
            $totalRanges = 0
            
            Write-Log "=== VPN Subnet Statistics ===" "INFO"
            Write-Log "Total Providers: $totalProviders" "INFO"
            
            foreach ($provider in $vpnData.PSObject.Properties.Name) {
                $ranges = $vpnData.$provider
                $rangeCount = $ranges.Count
                $totalRanges += $rangeCount
                Write-Log "  $provider`: $rangeCount ranges" "INFO"
            }
            
            Write-Log "Total IP Ranges: $totalRanges" "INFO"
            
            # Show file info
            $fileInfo = Get-Item $VPNSubnetsFile
            Write-Log "File Size: $($fileInfo.Length) bytes" "INFO"
            Write-Log "Last Modified: $($fileInfo.LastWriteTime)" "INFO"
            
        } catch {
            Write-Log "Error reading VPN stats: $($_.Exception.Message)" "ERROR"
        }
    } else {
        Write-Log "VPN subnets file not found: $VPNSubnetsFile" "ERROR"
    }
}

function Backup-VPNSubnets {
    if (Test-Path $VPNSubnetsFile) {
        $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
        $backupFile = "$VPNSubnetsFile.backup.$timestamp"
        
        try {
            Copy-Item $VPNSubnetsFile $backupFile
            Write-Log "Backup created: $backupFile" "SUCCESS"
        } catch {
            Write-Log "Backup failed: $($_.Exception.Message)" "ERROR"
        }
    } else {
        Write-Log "VPN subnets file not found: $VPNSubnetsFile" "ERROR"
    }
}

function Restore-VPNSubnets {
    param([string]$BackupFile)
    
    if ([string]::IsNullOrEmpty($BackupFile)) {
        # Find the most recent backup
        $backups = Get-ChildItem "$VPNSubnetsFile.backup.*" | Sort-Object LastWriteTime -Descending
        if ($backups.Count -gt 0) {
            $BackupFile = $backups[0].FullName
        }
    }
    
    if (Test-Path $BackupFile) {
        try {
            Copy-Item $BackupFile $VPNSubnetsFile -Force
            Write-Log "Restored from backup: $BackupFile" "SUCCESS"
        } catch {
            Write-Log "Restore failed: $($_.Exception.Message)" "ERROR"
        }
    } else {
        Write-Log "Backup file not found: $BackupFile" "ERROR"
    }
}

function Check-VPNStatus {
    Write-Log "=== VPN Detection System Status ===" "INFO"
    
    # Check VPN subnets file
    if (Test-Path $VPNSubnetsFile) {
        $fileInfo = Get-Item $VPNSubnetsFile
        Write-Log "VPN Subnets File: EXISTS" "SUCCESS"
        Write-Log "  Size: $($fileInfo.Length) bytes" "INFO"
        Write-Log "  Modified: $($fileInfo.LastWriteTime)" "INFO"
    } else {
        Write-Log "VPN Subnets File: MISSING" "ERROR"
    }
    
    # Check settings file
    if (Test-Path $SettingsFile) {
        try {
            $settings = Get-Content $SettingsFile | ConvertFrom-Json
            Write-Log "Settings File: EXISTS" "SUCCESS"
            Write-Log "  VPN Updates Enabled: $($settings.EnableVPNSubnetUpdates)" "INFO"
            Write-Log "  Update Interval: $($settings.VPNSubnetUpdateIntervalHours) hours" "INFO"
        } catch {
            Write-Log "Settings File: EXISTS (Parse Error)" "WARN"
        }
    } else {
        Write-Log "Settings File: MISSING" "ERROR"
    }
    
    # Check network connectivity to external sources
    $testUrls = @(
        "https://raw.githubusercontent.com/X4BNet/lists_vpn/main/ipv4.txt",
        "https://raw.githubusercontent.com/blackmatrix7/ios_rule_script/master/rule/Clash/GlobalMedia/GlobalMedia_Resolve.list"
    )
    
    Write-Log "=== Network Connectivity Test ===" "INFO"
    foreach ($url in $testUrls) {
        try {
            $response = Invoke-WebRequest -Uri $url -TimeoutSec 10 -UseBasicParsing
            if ($response.StatusCode -eq 200) {
                Write-Log "  $url : SUCCESS" "SUCCESS"
            } else {
                Write-Log "  $url : FAILED (Status: $($response.StatusCode))" "ERROR"
            }
        } catch {
            Write-Log "  $url : FAILED ($($_.Exception.Message))" "ERROR"
        }
    }
}

# Main execution
Write-Log "VPN Subnet Update Management Script Started" "INFO"

if ($CheckStatus) {
    Check-VPNStatus
}
elseif ($ShowStats) {
    Show-VPNStats
}
elseif ($Backup) {
    Backup-VPNSubnets
}
elseif ($Restore) {
    Restore-VPNSubnets -BackupFile $BackupFile
}
elseif ($TestDetection -and -not [string]::IsNullOrEmpty($TestIP)) {
    Test-VPNDetection -IPAddress $TestIP
}
elseif ($ForceUpdate) {
    Write-Log "Manual update trigger - this will be handled by the ACE server" "INFO"
    Write-Log "The server will check for updates on the next VPN detection request" "INFO"
}
else {
    # Show help
    Write-Log "=== VPN Subnet Update Management Script ===" "INFO"
    Write-Log "Usage:" "INFO"
    Write-Log "  -CheckStatus     : Check system status and connectivity" "INFO"
    Write-Log "  -ShowStats      : Display VPN subnet statistics" "INFO"
    Write-Log "  -Backup         : Create backup of current VPN subnets" "INFO"
    Write-Log "  -Restore        : Restore from most recent backup" "INFO"
    Write-Log "  -TestDetection -TestIP <IP> : Test VPN detection for specific IP" "INFO"
    Write-Log "  -ForceUpdate    : Trigger manual update check" "INFO"
    Write-Log "" "INFO"
    Write-Log "Examples:" "INFO"
    Write-Log "  .\VPN_Update_Script.ps1 -CheckStatus" "INFO"
    Write-Log "  .\VPN_Update_Script.ps1 -ShowStats" "INFO"
    Write-Log "  .\VPN_Update_Script.ps1 -TestDetection -TestIP 185.65.18.123" "INFO"
    Write-Log "  .\VPN_Update_Script.ps1 -Backup" "INFO"
}

Write-Log "Script execution completed" "INFO" 