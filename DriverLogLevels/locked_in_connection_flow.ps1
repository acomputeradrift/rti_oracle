param(
    [Parameter(Mandatory = $true)]
    [string]$Ip,

    [int]$ConnectTimeoutMs = 5000,
    [int]$EchoTimeoutMs = 3000,
    [int]$SavedLogLevelsTimeoutMs = 30000,
    [int]$HttpTimeoutSeconds = 8,
    [int]$AccessLineTimeoutMs = 3000,
    [int]$StartupAckQuietMs = 2000,
    [int]$StartupAckMaxMs = 8000,
    [int]$ProtectedAckTimeoutMs = 7000
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Net.Http

function Get-Stamp {
    return (Get-Date).ToString("HH:mm:ss.fff")
}

function Write-Stage([string]$Title) {
    Write-Output "[$(Get-Stamp)] === $Title ==="
}

function Write-Sent([string]$Text) {
    Write-Output "[$(Get-Stamp)] Sent     -> $Text"
}

function Write-Received([string]$Text) {
    Write-Output "[$(Get-Stamp)] Received -> $Text"
}

function Write-Wait([string]$Text) {
    Write-Output "[$(Get-Stamp)] Waiting  -> $Text"
}

function Halt-Probe([string]$Reason) {
    Write-Output "[$(Get-Stamp)] HALT     -> $Reason"
    try {
        if ($null -ne ${script:SocketRef}) {
            ${script:SocketRef}.Abort()
        }
    }
    catch {
    }
    throw $Reason
}

function Get-JsonMaybe([string]$Text) {
    if ([string]::IsNullOrWhiteSpace($Text)) {
        return $null
    }

    try {
        return $Text | ConvertFrom-Json
    }
    catch {
        $jsonStart = $Text.IndexOf("{")
        if ($jsonStart -lt 0) {
            return $null
        }

        try {
            return $Text.Substring($jsonStart) | ConvertFrom-Json
        }
        catch {
            return $null
        }
    }
}

function Receive-NextWsMessage {
    param(
        [Parameter(Mandatory = $true)]
        [System.Net.WebSockets.ClientWebSocket]$Socket,
        [Parameter(Mandatory = $true)]
        [int]$StepTimeoutMs
    )

    $startedUtc = [DateTime]::UtcNow
    while ($true) {
        if ($null -eq ${script:PendingReceiveTask}) {
            ${script:PendingReceiveTask} = $Socket.ReceiveAsync(${script:ReceiveSegment}, [System.Threading.CancellationToken]::None)
        }

        $elapsedMs = ([DateTime]::UtcNow - $startedUtc).TotalMilliseconds
        $remainingRaw = [int]($StepTimeoutMs - $elapsedMs)
        if ($remainingRaw -le 0) {
            return $null
        }
        $remainingMs = [Math]::Max(1, $remainingRaw)

        $delayTask = [System.Threading.Tasks.Task]::Delay($remainingMs)
        $completed = [System.Threading.Tasks.Task]::WhenAny(${script:PendingReceiveTask}, $delayTask).GetAwaiter().GetResult()
        if (-not [object]::ReferenceEquals($completed, ${script:PendingReceiveTask})) {
            return $null
        }

        $result = ${script:PendingReceiveTask}.GetAwaiter().GetResult()
        ${script:PendingReceiveTask} = $null

        if ($result.MessageType -eq [System.Net.WebSockets.WebSocketMessageType]::Close) {
            ${script:MessageStream}.SetLength(0)
            return @{
                Type = "Close"
                Text = ""
            }
        }

        if ($result.Count -gt 0) {
            ${script:MessageStream}.Write(${script:ReceiveBuffer}, 0, $result.Count)
        }

        if ($result.EndOfMessage) {
            $payloadBytes = ${script:MessageStream}.ToArray()
            ${script:MessageStream}.SetLength(0)
            if ($result.MessageType -eq [System.Net.WebSockets.WebSocketMessageType]::Text) {
                $text = [System.Text.Encoding]::UTF8.GetString($payloadBytes)
                return @{
                    Type = "Text"
                    Text = $text
                }
            }

            return @{
                Type = "Binary"
                Text = ""
            }
        }
    }
}

function Send-WsJson {
    param(
        [Parameter(Mandatory = $true)]
        [System.Net.WebSockets.ClientWebSocket]$Socket,
        [Parameter(Mandatory = $true)]
        [object]$Payload
    )

    $json = $Payload | ConvertTo-Json -Compress -Depth 50
    Write-Sent $json
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($json)
    $segment = [System.ArraySegment[byte]]::new($bytes)
    $Socket.SendAsync(
        $segment,
        [System.Net.WebSockets.WebSocketMessageType]::Text,
        $true,
        [System.Threading.CancellationToken]::None).GetAwaiter().GetResult() | Out-Null
}

function Select-DriversArray {
    param([object]$DriversJson)

    if ($null -eq $DriversJson) {
        return @()
    }

    if ($DriversJson -is [System.Array]) {
        return $DriversJson
    }

    if ($DriversJson.PSObject.Properties.Name -contains "drivers" -and $DriversJson.drivers -is [System.Array]) {
        return $DriversJson.drivers
    }

    $result = New-Object System.Collections.Generic.List[object]
    foreach ($prop in $DriversJson.PSObject.Properties) {
        if ($prop.Value -is [System.Array]) {
            foreach ($item in $prop.Value) {
                $result.Add($item)
            }
        }
        elseif ($null -ne $prop.Value -and $prop.Value.PSObject.Properties.Name -contains "id") {
            $result.Add($prop.Value)
        }
    }
    return $result.ToArray()
}

function Try-GetDiagnosticsDriverId {
    param([object[]]$Drivers)

    foreach ($driver in $Drivers) {
        if ($null -eq $driver) {
            continue
        }

        $name = [string]$driver.name
        if ([string]::IsNullOrWhiteSpace($name)) {
            continue
        }

        if ($name.StartsWith("Diagnostics:", [System.StringComparison]::OrdinalIgnoreCase)) {
            return [int]$driver.id
        }
    }

    return $null
}

$state = [ordered]@{
    WelcomeReceived = $false
    EchoSubscribeMessageLog = $false
    EchoSubscribeSysvar = $false
    SavedLogLevelsReceived = $false
    SystemStatusAccessed = $false
    DriversStatusAccessed = $false
    LastLogLevelAckUtc = $null
    AckDriverId = $null
    AckDriverLevel1 = $false
    AckPrimaryLevel0 = $false
}

${script:PendingReceiveTask} = $null
${script:ReceiveBuffer} = New-Object byte[] 8192
${script:ReceiveSegment} = [System.ArraySegment[byte]]::new(${script:ReceiveBuffer})
${script:MessageStream} = [System.IO.MemoryStream]::new()
${script:SocketRef} = $null
${script:ConnectSentUtc} = $null
${script:SavedLogLevelsDelayMs} = $null

function Process-IncomingWsMessage {
    param(
        [string]$RawText,
        [hashtable]$State
    )

    $parsed = Get-JsonMaybe $RawText
    if ($null -eq $parsed) {
        Write-Received $RawText
        return
    }

    if ($parsed.PSObject.Properties.Name -contains "messageType") {
        $messageType = [string]$parsed.messageType
        if ($messageType -eq "echo") {
            $echoMessage = [string]$parsed.message
            Write-Received $echoMessage
            if ($echoMessage -like "*Welcome to the RTI Diagnostics Websocket server!*") {
                $State.WelcomeReceived = $true
            }
            if ($echoMessage -like "*Subscribe/MessageLog*") {
                $State.EchoSubscribeMessageLog = $true
            }
            if ($echoMessage -like "*Subscribe/Sysvar*") {
                $State.EchoSubscribeSysvar = $true
            }
            return
        }

        if ($messageType -eq "LogLevels") {
            $State.SavedLogLevelsReceived = $true
            if ($null -eq ${script:SavedLogLevelsDelayMs} -and $null -ne ${script:ConnectSentUtc}) {
                ${script:SavedLogLevelsDelayMs} = [Math]::Round((([DateTime]::UtcNow) - ([DateTime]${script:ConnectSentUtc})).TotalMilliseconds)
            }
            $count = 0
            if ($parsed.PSObject.Properties.Name -contains "levels" -and $parsed.levels -is [System.Array]) {
                $count = $parsed.levels.Count
            }
            Write-Received "saved log levels {`"messageType`":`"LogLevels`",`"levels`":[...]} (count=$count)"
            return
        }

        if ($messageType -eq "MessageLog") {
            $line = [string]$parsed.text
            Write-Received $line

            if ($line -like "*System Status Accessed*") {
                $State.SystemStatusAccessed = $true
            }
            if ($line -like "*Drivers Status Accessed*") {
                $State.DriversStatusAccessed = $true
            }
            if ($line -like "*OnHTTPServerData()*" -and $line -like "*`"resource`":`"MessageLog`"*") {
                $State.EchoSubscribeMessageLog = $true
            }
            if ($line -like "*OnHTTPServerData()*" -and $line -like "*`"resource`":`"Sysvar`"*") {
                $State.EchoSubscribeSysvar = $true
            }

            if ($line -match "Setting LogLevel on") {
                $State.LastLogLevelAckUtc = [DateTime]::UtcNow
            }

            if ($line -match "Setting LogLevel on DRIVER \((\d+)\) to 1") {
                $driverId = [int]$Matches[1]
                if ($null -ne $State.AckDriverId -and $driverId -eq [int]$State.AckDriverId) {
                    $State.AckDriverLevel1 = $true
                }
            }

            if ($line -match "Setting LogLevel on Diagnostics: Primary Processor to 0") {
                $State.AckPrimaryLevel0 = $true
            }

            $embedded = Get-JsonMaybe $line
            if ($null -ne $embedded -and ($embedded.PSObject.Properties.Name -contains "resource") -and ([string]$embedded.resource -eq "LogLevel")) {
                $State.LastLogLevelAckUtc = [DateTime]::UtcNow
                if ($embedded.PSObject.Properties.Name -contains "value") {
                    $value = $embedded.value
                    $ackType = [string]$value.type
                    $ackLevel = [string]$value.level
                    if ($ackType -eq "DRIVER" -and $ackLevel -eq "1") {
                        $ackDriverId = [string]$value.driverId
                        if ($null -ne $State.AckDriverId -and $ackDriverId -eq [string]$State.AckDriverId) {
                            $State.AckDriverLevel1 = $true
                        }
                    }
                    if ($ackType -eq "Diagnostics: Primary Processor" -and $ackLevel -eq "0") {
                        $State.AckPrimaryLevel0 = $true
                    }
                }
            }
            return
        }

        if ($messageType -eq "Sysvar") {
            if (($parsed.PSObject.Properties.Name -contains "id") -and ($parsed.PSObject.Properties.Name -contains "val")) {
                Write-Received ("Sysvar id={0} val={1}" -f $parsed.id, $parsed.val)
            }
            else {
                Write-Received ($parsed | ConvertTo-Json -Compress -Depth 20)
            }
            return
        }

        Write-Received ($parsed | ConvertTo-Json -Compress -Depth 20)
        return
    }

    Write-Received ($parsed | ConvertTo-Json -Compress -Depth 20)
}

function Pump-IncomingUntil {
    param(
        [Parameter(Mandatory = $true)]
        [System.Net.WebSockets.ClientWebSocket]$Socket,
        [Parameter(Mandatory = $true)]
        [hashtable]$State,
        [Parameter(Mandatory = $true)]
        [scriptblock]$Condition,
        [Parameter(Mandatory = $true)]
        [int]$TimeoutMs,
        [Parameter(Mandatory = $true)]
        [string]$TimeoutReason
    )

    $start = [DateTime]::UtcNow
    while ($true) {
        if (& $Condition) {
            return
        }

        $elapsedMs = ([DateTime]::UtcNow - $start).TotalMilliseconds
        if ($elapsedMs -ge $TimeoutMs) {
            Halt-Probe $TimeoutReason
        }

        $remainingMs = [Math]::Max(1, [int]($TimeoutMs - $elapsedMs))
        $incoming = Receive-NextWsMessage -Socket $Socket -StepTimeoutMs $remainingMs
        if ($null -eq $incoming) {
            Halt-Probe $TimeoutReason
        }

        if ($incoming.Type -eq "Close") {
            Halt-Probe "WebSocket closed by processor."
        }

        if ($incoming.Type -eq "Text") {
            Process-IncomingWsMessage -RawText $incoming.Text -State $State
        }
    }
}

$socket = [System.Net.WebSockets.ClientWebSocket]::new()
${script:SocketRef} = $socket
$httpClient = [System.Net.Http.HttpClient]::new()
$httpClient.Timeout = [TimeSpan]::FromSeconds($HttpTimeoutSeconds)

try {
    Write-Stage "CONNECT"
    $wsUri = [Uri]("ws://{0}:1234/diagnosticswss" -f $Ip)
    $socket.Options.SetRequestHeader("Origin", "http://$Ip")
    Write-Sent ("WS CONNECT {0}" -f $wsUri.AbsoluteUri)
    ${script:ConnectSentUtc} = [DateTime]::UtcNow

    $connectCts = [System.Threading.CancellationTokenSource]::new($ConnectTimeoutMs)
    try {
        $socket.ConnectAsync($wsUri, $connectCts.Token).GetAwaiter().GetResult() | Out-Null
    }
    catch [System.OperationCanceledException] {
        Halt-Probe ("Timeout: WebSocket connect did not complete within {0}ms" -f $ConnectTimeoutMs)
    }
    finally {
        $connectCts.Dispose()
    }

    Write-Received "WebSocket handshake accepted"
    Write-Wait ("welcome echo (timeout: {0}ms)" -f $EchoTimeoutMs)
    Pump-IncomingUntil -Socket $socket -State $state -Condition { $state.WelcomeReceived } -TimeoutMs $EchoTimeoutMs -TimeoutReason ("Timeout: welcome echo not received within {0}ms" -f $EchoTimeoutMs)

    Write-Stage "SUBSCRIBE TO MESSAGES"
    Send-WsJson -Socket $socket -Payload @{
        type     = "Subscribe"
        resource = "MessageLog"
        value    = "true"
    }
    Write-Wait "subscribe confirmations (non-blocking; lines shown when received)"

    Write-Stage "RECEIVE SAVED DRIVER LOG LEVELS"
    Write-Wait ("LogLevels (timeout: {0}ms)" -f $SavedLogLevelsTimeoutMs)
    Pump-IncomingUntil -Socket $socket -State $state -Condition { $state.SavedLogLevelsReceived } -TimeoutMs $SavedLogLevelsTimeoutMs -TimeoutReason ("Timeout: saved driver log levels not received within {0}ms" -f $SavedLogLevelsTimeoutMs)

    Write-Stage "GET PROCESSOR TIME"
    $systemStatusUrl = "http://${Ip}:5000/diagnostics/data/system_status"
    Write-Sent ("GET {0}" -f $systemStatusUrl)
    $systemStatusRaw = $httpClient.GetStringAsync($systemStatusUrl).GetAwaiter().GetResult()
    $systemStatusJson = Get-JsonMaybe $systemStatusRaw
    if ($null -eq $systemStatusJson) {
        Halt-Probe "Invalid JSON: system_status response could not be parsed."
    }
    $timestamp = $null
    if (($systemStatusJson.PSObject.Properties.Name -contains "memory_history") -and ($systemStatusJson.memory_history -is [System.Array]) -and ($systemStatusJson.memory_history.Count -gt 0)) {
        $timestamp = $systemStatusJson.memory_history[0].timestamp
    }
    if ($null -eq $timestamp) {
        Halt-Probe "Missing timestamp: memory_history[0].timestamp not found in system_status."
    }
    Write-Received ("memory_history with timestamp={0}" -f $timestamp)

    Write-Stage "GET DRIVER NAMES"
    $driversUrl = "http://${Ip}:5000/diagnostics/data/drivers"
    Write-Sent ("GET {0}" -f $driversUrl)
    $driversRaw = $httpClient.GetStringAsync($driversUrl).GetAwaiter().GetResult()
    $driversJson = Get-JsonMaybe $driversRaw
    if ($null -eq $driversJson) {
        Halt-Probe "Invalid JSON: drivers response could not be parsed."
    }

    $drivers = Select-DriversArray -DriversJson $driversJson
    if ($drivers.Count -le 0) {
        Halt-Probe "No drivers found in /diagnostics/data/drivers response."
    }
    Write-Received ("JSON driver list (count={0})" -f $drivers.Count)

    $diagnosticsDriverId = Try-GetDiagnosticsDriverId -Drivers $drivers
    if ($null -eq $diagnosticsDriverId) {
        Halt-Probe "Diagnostics driver not found (name starts with 'Diagnostics:')."
    }
    $state.AckDriverId = $diagnosticsDriverId

    Write-Stage "SET UP DRIVER LOG LEVEL UI STATUS CONFIRMATIONS"
    Write-Wait ("Startup ACK chatter settle (quiet: {0}ms, max: {1}ms)" -f $StartupAckQuietMs, $StartupAckMaxMs)
    $settleStart = [DateTime]::UtcNow
    while ($true) {
        if ($null -eq $state.LastLogLevelAckUtc) {
            break
        }

        $quietForMs = ([DateTime]::UtcNow - [DateTime]$state.LastLogLevelAckUtc).TotalMilliseconds
        if ($quietForMs -ge $StartupAckQuietMs) {
            break
        }

        $settleElapsed = ([DateTime]::UtcNow - $settleStart).TotalMilliseconds
        if ($settleElapsed -ge $StartupAckMaxMs) {
            Halt-Probe ("Timeout: startup ACK chatter did not settle within {0}ms" -f $StartupAckMaxMs)
        }

        $incoming = Receive-NextWsMessage -Socket $socket -StepTimeoutMs 150
        if ($null -eq $incoming) {
            continue
        }
        if ($incoming.Type -eq "Close") {
            Halt-Probe "WebSocket closed by processor during startup ACK settle."
        }
        if ($incoming.Type -eq "Text") {
            Process-IncomingWsMessage -RawText $incoming.Text -State $state
        }
    }

    $driverPayload = @{
        type     = "Subscribe"
        resource = "LogLevel"
        value    = @{
            type     = "DRIVER"
            driverId = [string]$diagnosticsDriverId
            level    = "1"
        }
    }
    Send-WsJson -Socket $socket -Payload $driverPayload
    Send-WsJson -Socket $socket -Payload $driverPayload

    Write-Wait ("driver level 1 ACK (timeout: {0}ms)" -f $ProtectedAckTimeoutMs)
    Pump-IncomingUntil -Socket $socket -State $state -Condition { $state.AckDriverLevel1 } -TimeoutMs $ProtectedAckTimeoutMs -TimeoutReason ("Timeout: driver level 1 ACK not received within {0}ms" -f $ProtectedAckTimeoutMs)

    Send-WsJson -Socket $socket -Payload @{
        type     = "Subscribe"
        resource = "LogLevel"
        value    = @{
            type  = "Diagnostics: Primary Processor"
            level = "0"
        }
    }

    Write-Wait ("Diagnostics: Primary Processor level 0 ACK (timeout: {0}ms)" -f $ProtectedAckTimeoutMs)
    Pump-IncomingUntil -Socket $socket -State $state -Condition { $state.AckPrimaryLevel0 } -TimeoutMs $ProtectedAckTimeoutMs -TimeoutReason ("Timeout: Diagnostics: Primary Processor level 0 ACK not received within {0}ms" -f $ProtectedAckTimeoutMs)

    Write-Output "[$(Get-Stamp)] === READY ==="
    if ($null -ne ${script:SavedLogLevelsDelayMs}) {
        Write-Output "[$(Get-Stamp)] Saved Log Levels Delay -> ${script:SavedLogLevelsDelayMs} ms (connect to first LogLevels)"
    }
}
finally {
    try {
        if ($null -ne $socket) {
            try {
                $socket.Abort()
            }
            catch {
            }
        }
    }
    finally {
        ${script:PendingReceiveTask} = $null
        if ($null -ne $socket) {
            $socket.Dispose()
        }
        ${script:SocketRef} = $null
        if ($null -ne ${script:MessageStream}) {
            ${script:MessageStream}.Dispose()
        }
        if ($null -ne $httpClient) {
            $httpClient.Dispose()
        }
    }
}
