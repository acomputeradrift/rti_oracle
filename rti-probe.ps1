# RTI Diagnostics Endpoint Probe (PowerShell Version)

$ip = "192.168.1.143"
$port = 5000

$endpoints = @(
    "dashboard",
    "drivers",
    "rtipanel",
    "zigbee",
    "upnp",
    "variables",
    "flags",
    "systemlog"
)

Write-Host "Querying RTI diagnostics endpoints at $ip...`n"

foreach ($ep in $endpoints) {

    # Proper variable interpolation
    $url = "http://${ip}:${port}/diagnostics/data/${ep}"

    Write-Host "`n===== $($ep.ToUpper()) ====="

    try {
        # UseBasicParsing prevents the script execution warning
        $response = Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 3 -ErrorAction Stop

        $data = $response.Content
        $preview = if ($data.Length -gt 250) { $data.Substring(0,250) + "..." } else { $data }

        Write-Host "Status: $($response.StatusCode)"
        Write-Host "Content-Type: $($response.Headers['Content-Type'])"
        Write-Host "Length: $($data.Length) bytes"
        Write-Host "Preview:`n$preview"
    }
    catch {
        Write-Host "ERROR: $($_.Exception.Message)"
    }
}

