param(
    [Parameter(Mandatory = $false)]
    [string]$Ip,

    [Parameter(Mandatory = $false)]
    [int]$AckTimeoutSeconds = 7,

    [Parameter(Mandatory = $false)]
    [int]$StartupSettleMaxSeconds = 8,

    [Parameter(Mandatory = $false)]
    [int]$StartupAckQuietSeconds = 2,

    [Parameter(Mandatory = $false)]
    [int]$ProjectPrimeDelayMs = 300,

    [Parameter(Mandatory = $false)]
    [string]$MutationDriverDName,

    [switch]$SelfTest
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

trap {
    Write-Line -Text ("[FAIL] line {0}: {1}" -f $_.InvocationInfo.ScriptLineNumber, $_.Exception.Message) -ForegroundColor Red
    exit 2
}

$SystemDiagnosticsChannel = 'Diagnostics: Primary Processor'
$SystemDiagnosticsLabel = '[System] Diagnostics: Primary Processor'
$ProjectDiagnosticsLabel = '[Project] Diagnostics: Primary Processor'
$BaselineSystemLevel = 0
$BaselineProjectLevel = 1
$TestProjectLevel = 2
$script:ProjectDiagnosticsDName = $null
$script:ProjectDiagnosticsName = $null
$script:MutationDriverDName = $null
$script:MutationDriverName = $null
$script:AckStatusByDName = @{}
$script:DriverNameByDName = @{}
$script:RecentTextBuffer = New-Object System.Collections.Generic.List[string]

function Write-Step {
    param(
        [Parameter(Mandatory = $true)][int]$Number,
        [Parameter(Mandatory = $true)][string]$Text
    )

    Write-Line -Text ("[STEP {0}] {1}" -f $Number, $Text) -ForegroundColor Cyan
}

function Write-Pass {
    param([Parameter(Mandatory = $true)][string]$Text)
    Write-Line -Text ("[PASS] {0}" -f $Text) -ForegroundColor Black -BackgroundColor Green
}

function Write-Line {
    param(
        [Parameter(Mandatory = $true)][string]$Text,
        [string]$ForegroundColor,
        [string]$BackgroundColor
    )

    $prefix = (Get-Date).ToString('HH:mm:ss.fff')
    $line = ("{0} {1}" -f $prefix, $Text)

    $params = @{ Object = $line }
    if (-not [string]::IsNullOrWhiteSpace($ForegroundColor)) {
        $params.ForegroundColor = $ForegroundColor
    }
    if (-not [string]::IsNullOrWhiteSpace($BackgroundColor)) {
        $params.BackgroundColor = $BackgroundColor
    }

    Write-Host @params
}

function Get-DisplayLabel {
    param([Parameter(Mandatory = $true)][string]$DName)

    if ($DName.Equals($SystemDiagnosticsChannel, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $SystemDiagnosticsLabel
    }

    if (-not [string]::IsNullOrWhiteSpace($script:ProjectDiagnosticsDName) -and
        $DName.Equals($script:ProjectDiagnosticsDName, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $ProjectDiagnosticsLabel
    }

    if ($script:DriverNameByDName.ContainsKey($DName)) {
        $friendly = [string]$script:DriverNameByDName[$DName]
        if (-not [string]::IsNullOrWhiteSpace($friendly) -and
            -not $friendly.Equals($DName, [System.StringComparison]::OrdinalIgnoreCase)) {
            return $friendly
        }
    }

    return $DName
}

function Fail-Now {
    param([Parameter(Mandatory = $true)][string]$Text)
    throw $Text
}

function Add-RecentText {
    param([Parameter(Mandatory = $true)][string]$Text)

    if ([string]::IsNullOrWhiteSpace($Text)) {
        return
    }

    $script:RecentTextBuffer.Add($Text)
    while ($script:RecentTextBuffer.Count -gt 12) {
        $script:RecentTextBuffer.RemoveAt(0)
    }
}

function Invoke-SelfTest {
    $sample = 'Diagnostics: Primary Processor - Setting LogLevel on DRIVER (47) to 1'
    $dName0 = ''
    $level0 = -1
    $ok = Try-ParseAckText -Text $sample -OutDName ([ref]$dName0) -OutLevel ([ref]$level0)
    if (-not $ok) {
        throw 'SelfTest failed: DRIVER(id) ack parse'
    }

    $sample2 = 'Diagnostics: Primary Processor - OnHTTPServerData() data.websocket = {"type":"Subscribe","resource":"LogLevel","value":{"type":"DRIVER","level":"1","driverId":"4"}}'
    $dName = ''
    $level = -1
    if (-not (Try-ParseAckText -Text $sample2 -OutDName ([ref]$dName) -OutLevel ([ref]$level))) {
        throw 'SelfTest failed: embedded websocket ack parse'
    }
    if ($dName -ne 'DRIVER//4' -or $level -ne 1) {
        throw "SelfTest failed: embedded websocket ack values [$dName/$level]"
    }

    Write-Line -Text '[PASS] SelfTest passed.' -ForegroundColor Green
}

function Get-Json {
    param(
        [Parameter(Mandatory = $true)][string]$Uri,
        [int]$TimeoutSec = 8
    )

    $response = Invoke-WebRequest -Uri $Uri -Method Get -TimeoutSec $TimeoutSec -UseBasicParsing
    if ([string]::IsNullOrWhiteSpace($response.Content)) {
        throw "Empty response from $Uri"
    }
    return $response.Content
}

function Parse-DriversJson {
    param([Parameter(Mandatory = $true)][string]$JsonText)

    $root = ConvertFrom-Json -InputObject $JsonText
    if ($null -eq $root) {
        return @()
    }

    $results = New-Object System.Collections.ArrayList

    function Try-AddDriverFromNode {
        param(
            [Parameter(Mandatory = $true)]$Node,
            [Parameter(Mandatory = $true)]$ResultList
        )

        if ($null -eq $Node) {
            return
        }

        $props = @($Node.PSObject.Properties.Name)
        if (-not ($props -contains 'id')) {
            return
        }

        $id = 0
        if (-not [int]::TryParse([string]$Node.id, [ref]$id)) {
            return
        }

        $dName = "DRIVER//$id"
        $name = if (($props -contains 'name') -and -not [string]::IsNullOrWhiteSpace([string]$Node.name)) { [string]$Node.name } else { $dName }
        [void]$ResultList.Add([pscustomobject]@{
                Id = $id
                DName = $dName
                Name = $name
            })
    }

    $targetRoot = $root
    if ($targetRoot -isnot [System.Array]) {
        $rootProps = @($targetRoot.PSObject.Properties.Name)
        if (($rootProps -contains 'drivers') -and ($null -ne $targetRoot.drivers)) {
            $targetRoot = $targetRoot.drivers
        }
    }

    if ($targetRoot -is [System.Array]) {
        foreach ($item in $targetRoot) {
            Try-AddDriverFromNode -Node $item -ResultList $results
        }
    }
    else {
        foreach ($property in $targetRoot.PSObject.Properties) {
            $value = $property.Value
            if ($null -eq $value) {
                continue
            }

            if ($value -is [System.Array]) {
                foreach ($item in $value) {
                    Try-AddDriverFromNode -Node $item -ResultList $results
                }
            }
            else {
                Try-AddDriverFromNode -Node $value -ResultList $results
            }
        }
    }

    return @($results)
}

function Try-SelectDiagnosticsDriver {
    param(
        [Parameter(Mandatory = $true)]$Drivers,
        [ref]$OutDriver
    )

    $OutDriver.Value = $null
    if ($null -eq $Drivers) {
        return $false
    }

    foreach ($driver in $Drivers) {
        if ($null -eq $driver) {
            continue
        }

        $name = [string]$driver.Name
        if (-not [string]::IsNullOrWhiteSpace($name) -and $name.StartsWith('Diagnostics:', [System.StringComparison]::OrdinalIgnoreCase)) {
            $OutDriver.Value = $driver
            return $true
        }
    }

    return $false
}

function New-WebSocketSession {
    param(
        [Parameter(Mandatory = $true)][string]$Ip,
        [int]$ConnectTimeoutSec = 8
    )

    $socket = [System.Net.WebSockets.ClientWebSocket]::new()
    $socket.Options.SetRequestHeader('Origin', "http://${Ip}")
    $uri = [Uri]("ws://${Ip}:1234/diagnosticswss")

    $connectCts = [System.Threading.CancellationTokenSource]::new([TimeSpan]::FromSeconds($ConnectTimeoutSec))
    try {
        $null = $socket.ConnectAsync($uri, $connectCts.Token).GetAwaiter().GetResult()
    }
    finally {
        $connectCts.Dispose()
    }

    return [pscustomobject]@{
        Socket = $socket
        PendingReceiveTask = $null
        PendingBuffer = $null
        MessageStream = (New-Object System.IO.MemoryStream)
    }
}

function Normalize-WebSocketSession {
    param([Parameter(Mandatory = $true)]$Session)

    if ($null -eq $Session) {
        return $null
    }

    if ($Session.PSObject.Properties.Name -contains 'Socket') {
        return $Session
    }

    if ($Session -is [System.Net.WebSockets.ClientWebSocket]) {
        return [pscustomobject]@{
            Socket = $Session
            PendingReceiveTask = $null
            PendingBuffer = $null
            MessageStream = (New-Object System.IO.MemoryStream)
        }
    }

    return $Session
}

function Ensure-PendingReceiveTask {
    param([Parameter(Mandatory = $true)]$Session)
    if ($null -ne $Session.PendingReceiveTask) {
        return
    }
    $buffer = New-Object byte[] 8192
    $segment = [System.ArraySegment[byte]]::new($buffer)
    $Session.PendingBuffer = $buffer
    $Session.PendingReceiveTask = $Session.Socket.ReceiveAsync($segment, [System.Threading.CancellationToken]::None)
}

function Get-NextQueuedMessage {
    param(
        [Parameter(Mandatory = $true)]$Session,
        [Parameter(Mandatory = $true)][int]$TimeoutSec
    )

    $deadline = (Get-Date).AddMilliseconds([Math]::Max(1, [int]([Math]::Ceiling($TimeoutSec * 1000))))
    while ((Get-Date) -lt $deadline) {
        if ($Session.Socket.State -ne [System.Net.WebSockets.WebSocketState]::Open -and
            $Session.Socket.State -ne [System.Net.WebSockets.WebSocketState]::CloseReceived) {
            return $null
        }

        Ensure-PendingReceiveTask -Session $Session
        $remainingMs = [Math]::Max(1, [int]([Math]::Ceiling(($deadline - (Get-Date)).TotalMilliseconds)))
        $waitMs = [Math]::Min(200, $remainingMs)
        if (-not $Session.PendingReceiveTask.Wait($waitMs)) {
            continue
        }

        if ($Session.PendingReceiveTask.IsFaulted -or $Session.PendingReceiveTask.IsCanceled) {
            $Session.PendingReceiveTask = $null
            $Session.PendingBuffer = $null
            return $null
        }

        $result = $Session.PendingReceiveTask.GetAwaiter().GetResult()
        $buffer = $Session.PendingBuffer
        $Session.PendingReceiveTask = $null
        $Session.PendingBuffer = $null

        if ($result.MessageType -eq [System.Net.WebSockets.WebSocketMessageType]::Close) {
            return $null
        }

        if ($result.Count -gt 0) {
            $Session.MessageStream.Write($buffer, 0, $result.Count)
        }

        if ($result.EndOfMessage) {
            $text = [System.Text.Encoding]::UTF8.GetString($Session.MessageStream.ToArray())
            $Session.MessageStream.SetLength(0)
            return $text
        }
    }

    return $null
}

function Stop-WebSocketSession {
    param([Parameter(Mandatory = $true)]$Session)

    if ($null -eq $Session) {
        return
    }

    $socket = $null
    if ($Session.PSObject.Properties.Name -contains 'Socket') {
        $socket = $Session.Socket
    }
    elseif ($Session -is [System.Net.WebSockets.ClientWebSocket]) {
        $socket = $Session
    }

    if ($null -eq $socket) {
        return
    }
    if ($null -ne $socket) {
        try {
            if ($null -ne $Session.PendingReceiveTask -and -not $Session.PendingReceiveTask.IsCompleted) {
                $socket.Abort()
            }
            elseif ($socket.State -eq [System.Net.WebSockets.WebSocketState]::Open -or $socket.State -eq [System.Net.WebSockets.WebSocketState]::CloseReceived) {
                $closeCts = [System.Threading.CancellationTokenSource]::new([TimeSpan]::FromSeconds(1))
                try {
                    $socket.CloseAsync([System.Net.WebSockets.WebSocketCloseStatus]::NormalClosure, 'done', $closeCts.Token).GetAwaiter().GetResult()
                }
                catch {
                    $socket.Abort()
                }
                finally {
                    $closeCts.Dispose()
                }
            }
        }
        finally {
            $socket.Dispose()
            $Session.Socket = $null
        }
    }

    if (($Session.PSObject.Properties.Name -contains 'MessageStream') -and $null -ne $Session.MessageStream) {
        $Session.MessageStream.Dispose()
    }
}

function Send-JsonMessage {
    param(
        [Parameter(Mandatory = $true)]$Session,
        [Parameter(Mandatory = $true)]$Payload
    )

    if ($Session.Socket.State -ne [System.Net.WebSockets.WebSocketState]::Open) {
        throw "WebSocket is not open. State: $($Session.Socket.State)"
    }

    $json = $Payload | ConvertTo-Json -Depth 8 -Compress
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($json)
    $segment = [System.ArraySegment[byte]]::new($bytes)
    $null = $Session.Socket.SendAsync($segment, [System.Net.WebSockets.WebSocketMessageType]::Text, $true, [System.Threading.CancellationToken]::None).GetAwaiter().GetResult()
}

function Send-Subscribe {
    param(
        [Parameter(Mandatory = $true)]$Session,
        [Parameter(Mandatory = $true)][string]$Resource,
        [Parameter(Mandatory = $true)][string]$Value
    )

    $payload = [pscustomobject]@{
        type = 'Subscribe'
        resource = $Resource
        value = $Value
    }
    Send-JsonMessage -Session $Session -Payload $payload
}

function Get-LogLevelEntriesFromObject {
    param([Parameter(Mandatory = $true)]$Object)

    $results = New-Object System.Collections.ArrayList
    if ($null -eq $Object) {
        return @()
    }

    if (($Object.PSObject.Properties.Name -contains 'messageType') -and ([string]$Object.messageType -ieq 'LogLevels') -and
        ($Object.PSObject.Properties.Name -contains 'levels') -and ($null -ne $Object.levels)) {
        foreach ($level in $Object.levels) {
            if ($null -eq $level) {
                continue
            }
            $dName = [string]$level.dName
            if ([string]::IsNullOrWhiteSpace($dName)) {
                continue
            }
            $intLevel = 0
            if (-not [int]::TryParse([string]$level.logLevel, [ref]$intLevel)) {
                $intLevel = 0
            }
            [void]$results.Add([pscustomobject]@{ DName = $dName; LogLevel = $intLevel })
        }
    }

    if (($Object.PSObject.Properties.Name -contains 'message') -and ($Object.message -is [string]) -and -not [string]::IsNullOrWhiteSpace([string]$Object.message)) {
        try {
            $inner = ConvertFrom-Json -InputObject ([string]$Object.message)
            foreach ($innerEntry in (Get-LogLevelEntriesFromObject -Object $inner)) {
                [void]$results.Add($innerEntry)
            }
        }
        catch {
        }
    }

    return @($results)
}

function Try-ParseInitialSnapshot {
    param(
        [Parameter(Mandatory = $true)][string]$RawJson,
        [ref]$OutLevels
    )

    $OutLevels.Value = @{}
    try {
        $obj = ConvertFrom-Json -InputObject $RawJson
    }
    catch {
        return $false
    }

    $entries = @(Get-LogLevelEntriesFromObject -Object $obj)
    if ($entries.Count -eq 0) {
        return $false
    }

    $map = @{}
    foreach ($entry in $entries) {
        $map[[string]$entry.DName] = [int]$entry.LogLevel
    }
    $OutLevels.Value = $map
    return $true
}

function Try-ParseAckText {
    param(
        [Parameter(Mandatory = $true)][string]$Text,
        [ref]$OutDName,
        [ref]$OutLevel
    )

    $OutDName.Value = ''
    $OutLevel.Value = 0

    $jsonStart = $Text.IndexOf('{')
    if ($jsonStart -ge 0) {
        $jsonText = $Text.Substring($jsonStart)
        try {
            $obj = ConvertFrom-Json -InputObject $jsonText
            if ($null -ne $obj -and
                ($obj.PSObject.Properties.Name -contains 'resource') -and
                ([string]$obj.resource -ieq 'LogLevel') -and
                ($obj.PSObject.Properties.Name -contains 'value') -and
                ($null -ne $obj.value)) {
                $type = [string]$obj.value.type
                $levelString = [string]$obj.value.level
                $level = 0
                if (-not [int]::TryParse($levelString, [ref]$level)) {
                    return $false
                }

                if ($type -ieq 'DRIVER') {
                    $driverId = [string]$obj.value.driverId
                    $id = 0
                    if (-not [int]::TryParse($driverId, [ref]$id)) {
                        return $false
                    }
                    $OutDName.Value = "DRIVER//$id"
                }
                else {
                    $OutDName.Value = $type
                }
                $OutLevel.Value = $level
                return -not [string]::IsNullOrWhiteSpace($OutDName.Value)
            }
        }
        catch {
        }
    }

    $driverParen = [regex]::Match($Text, 'Setting LogLevel on DRIVER\s*\((\d+)\)\s*to\s*(\d+)', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
    if ($driverParen.Success) {
        $OutDName.Value = "DRIVER//$($driverParen.Groups[1].Value)"
        $OutLevel.Value = [int]$driverParen.Groups[2].Value
        return $true
    }

    $driverDirect = [regex]::Match($Text, 'Setting LogLevel on\s+(DRIVER//\d+)\s*to\s*(\d+)', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
    if ($driverDirect.Success) {
        $OutDName.Value = [string]$driverDirect.Groups[1].Value
        $OutLevel.Value = [int]$driverDirect.Groups[2].Value
        return $true
    }

    $channel = [regex]::Match($Text, 'Setting LogLevel on\s+([A-Z0-9_:\s]+)\s*to\s*(\d+)', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
    if ($channel.Success) {
        $OutDName.Value = [string]$channel.Groups[1].Value.Trim()
        $OutLevel.Value = [int]$channel.Groups[2].Value
        return $true
    }

    return $false
}

function Get-MessageLogTextsFromRawMessage {
    param([Parameter(Mandatory = $true)][string]$RawJson)

    try {
        $obj = ConvertFrom-Json -InputObject $RawJson
    }
    catch {
        return @()
    }

    $results = New-Object System.Collections.ArrayList

    function WalkMessageLog {
        param([Parameter(Mandatory = $true)]$Node)

        if ($null -eq $Node) {
            return
        }

        if ($Node -is [System.Collections.IEnumerable] -and -not ($Node -is [string])) {
            foreach ($child in $Node) {
                WalkMessageLog -Node $child
            }
            return
        }

        $props = @($Node.PSObject.Properties.Name)
        if ($props -contains 'messageType' -and [string]$Node.messageType -ieq 'MessageLog') {
            $text = [string]$Node.text
            if (-not [string]::IsNullOrWhiteSpace($text)) {
                [void]$results.Add($text)
            }
        }

        if ($props -contains 'message' -and $Node.message -is [string] -and -not [string]::IsNullOrWhiteSpace([string]$Node.message)) {
            try {
                $inner = ConvertFrom-Json -InputObject ([string]$Node.message)
                WalkMessageLog -Node $inner
            }
            catch {
            }
        }

        foreach ($prop in $Node.PSObject.Properties) {
            $value = $prop.Value
            if ($null -eq $value -or ($value -is [string])) {
                continue
            }

            $valueProps = @($value.PSObject.Properties).Length
            if ($value -is [System.Collections.IEnumerable] -or $valueProps -gt 0) {
                WalkMessageLog -Node $value
            }
        }
    }

    WalkMessageLog -Node $obj
    return @($results)
}

function Set-DriverLogLevel {
    param(
        [Parameter(Mandatory = $true)]$Session,
        [Parameter(Mandatory = $true)][string]$DriverDName,
        [Parameter(Mandatory = $true)][int]$Level
    )

    $driverIdMatch = [regex]::Match($DriverDName, '^DRIVER//(\d+)$', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
    $payloads = New-Object System.Collections.ArrayList
    [void]$payloads.Add([pscustomobject]@{
            type = 'Subscribe'
            resource = 'LogLevel'
            value = [pscustomobject]@{
                type = $DriverDName
                level = [string]$Level
            }
        })

    if ($driverIdMatch.Success) {
        [void]$payloads.Add([pscustomobject]@{
                type = 'Subscribe'
                resource = 'LogLevel'
                value = [pscustomobject]@{
                    type = 'DRIVER'
                    level = [string]$Level
                    driverId = [string]$driverIdMatch.Groups[1].Value
                }
            })
    }

    foreach ($payload in $payloads) {
        Send-JsonMessage -Session $Session -Payload $payload
    }
}

function Resolve-AckAliasTargets {
    param([Parameter(Mandatory = $true)][string]$TargetDName)

    $targets = New-Object System.Collections.Generic.HashSet[string]([System.StringComparer]::OrdinalIgnoreCase)
    [void]$targets.Add($TargetDName)

    if (-not [string]::IsNullOrWhiteSpace($script:ProjectDiagnosticsDName)) {
        if ($TargetDName.Equals($SystemDiagnosticsChannel, [System.StringComparison]::OrdinalIgnoreCase)) {
            [void]$targets.Add($script:ProjectDiagnosticsDName)
        }
        if ($TargetDName.Equals($script:ProjectDiagnosticsDName, [System.StringComparison]::OrdinalIgnoreCase)) {
            [void]$targets.Add($SystemDiagnosticsChannel)
        }
    }

    return $targets
}

function Wait-ForAckOrFail {
    param(
        [Parameter(Mandatory = $true)]$Session,
        [Parameter(Mandatory = $true)][string]$TargetDName,
        [Parameter(Mandatory = $true)][int]$ExpectedLevel,
        [Parameter(Mandatory = $true)][int]$TimeoutSec
    )

    $candidates = Resolve-AckAliasTargets -TargetDName $TargetDName
    $deadline = (Get-Date).AddSeconds([Math]::Max(1, $TimeoutSec))
    while ((Get-Date) -lt $deadline) {
        $remainingSec = [Math]::Max(1, [int]([Math]::Ceiling(($deadline - (Get-Date)).TotalSeconds)))
        $raw = Get-NextQueuedMessage -Session $Session -TimeoutSec $remainingSec
        if ([string]::IsNullOrWhiteSpace($raw)) {
            continue
        }

        foreach ($line in (Get-MessageLogTextsFromRawMessage -RawJson $raw)) {
            Add-RecentText -Text $line
            $ackDName = ''
            $ackLevel = 0
            if (-not (Try-ParseAckText -Text ([string]$line) -OutDName ([ref]$ackDName) -OutLevel ([ref]$ackLevel))) {
                continue
            }

            $script:AckStatusByDName[$ackDName] = $ackLevel

            if ($ackLevel -eq $ExpectedLevel -and $candidates.Contains($ackDName)) {
                Write-Line -Text ("[ack][target] {0} => {1}" -f $ackDName, $ackLevel)
                Write-Pass ("ACK confirmed for {0} at level {1}" -f $TargetDName, $ExpectedLevel)
                return
            }

            Write-Line -Text ("[ack][other] {0} => {1}" -f $ackDName, $ackLevel)
        }
    }

    $tail = if ($script:RecentTextBuffer.Count -eq 0) { '<none>' } else { [string]::Join(" | ", $script:RecentTextBuffer) }
    Fail-Now ("ACK timeout for target [{0}] expected level [{1}]. Recent MessageLog lines: {2}" -f $TargetDName, $ExpectedLevel, $tail)
}

function Wait-ForInitialSnapshotOrFail {
    param(
        [Parameter(Mandatory = $true)]$Session,
        [int]$TimeoutSec = 8
    )

    $deadline = (Get-Date).AddSeconds([Math]::Max(1, $TimeoutSec))
    while ((Get-Date) -lt $deadline) {
        $remainingSec = [Math]::Max(1, [int]([Math]::Ceiling(($deadline - (Get-Date)).TotalSeconds)))
        $raw = Get-NextQueuedMessage -Session $Session -TimeoutSec $remainingSec
        if ([string]::IsNullOrWhiteSpace($raw)) {
            continue
        }

        $levelsRef = @{}
        if (Try-ParseInitialSnapshot -RawJson $raw -OutLevels ([ref]$levelsRef)) {
            return $levelsRef
        }
    }

    Fail-Now 'Did not receive initial LogLevels snapshot.'
}

function Wait-ForStartupAckSettle {
    param(
        [Parameter(Mandatory = $true)]$Session,
        [Parameter(Mandatory = $true)][int]$MaxSeconds,
        [Parameter(Mandatory = $true)][int]$QuietSeconds
    )

    $safeMax = [Math]::Max(1, $MaxSeconds)
    $safeQuiet = [Math]::Max(1, $QuietSeconds)
    $started = Get-Date
    $deadline = $started.AddSeconds($safeMax)
    $lastAckAt = $started
    $startupAckCount = 0

    while ((Get-Date) -lt $deadline) {
        $raw = Get-NextQueuedMessage -Session $Session -TimeoutSec 1
        if (-not [string]::IsNullOrWhiteSpace($raw)) {
            foreach ($line in (Get-MessageLogTextsFromRawMessage -RawJson $raw)) {
                Add-RecentText -Text $line
                $ackDName = ''
                $ackLevel = 0
                if (Try-ParseAckText -Text ([string]$line) -OutDName ([ref]$ackDName) -OutLevel ([ref]$ackLevel)) {
                    $lastAckAt = Get-Date
                    $startupAckCount++
                    $script:AckStatusByDName[$ackDName] = $ackLevel
                    Write-Line -Text ("[ack][startup] {0} => {1}" -f $ackDName, $ackLevel)
                }
            }
        }

        if (((Get-Date) - $lastAckAt).TotalSeconds -ge $safeQuiet) {
            break
        }
    }

    return [pscustomobject]@{
        StartupAckCount = $startupAckCount
        Settled = (((Get-Date) - $lastAckAt).TotalSeconds -ge $safeQuiet)
    }
}

if ($SelfTest) {
    Invoke-SelfTest
    exit 0
}

if ([string]::IsNullOrWhiteSpace($Ip)) {
    throw 'Parameter -Ip is required unless -SelfTest is used.'
}

Write-Step -Number 1 -Text 'Loading drivers from processor'
$driversText = Get-Json -Uri ("http://${Ip}:5000/diagnostics/data/drivers")
$drivers = Parse-DriversJson -JsonText $driversText
if ($drivers.Count -eq 0) {
    Fail-Now 'No drivers returned from diagnostics/data/drivers.'
}

$script:DriverNameByDName = @{}
foreach ($driver in $drivers) {
    if ($null -eq $driver) {
        continue
    }
    $d = [string]$driver.DName
    if ([string]::IsNullOrWhiteSpace($d)) {
        continue
    }
    $script:DriverNameByDName[$d] = [string]$driver.Name
}

$projectDiagnostics = $null
if (-not (Try-SelectDiagnosticsDriver -Drivers $drivers -OutDriver ([ref]$projectDiagnostics))) {
    Fail-Now 'Could not resolve project diagnostics driver (name prefix "Diagnostics:").'
}

$script:ProjectDiagnosticsDName = [string]$projectDiagnostics.DName
$script:ProjectDiagnosticsName = [string]$projectDiagnostics.Name
Write-Pass ("Resolved {0}: {1} ({2})" -f $ProjectDiagnosticsLabel, $script:ProjectDiagnosticsName, $script:ProjectDiagnosticsDName)

if (-not [string]::IsNullOrWhiteSpace($MutationDriverDName)) {
    $mutationDriver = $drivers |
        Where-Object { [string]$_.DName -ieq $MutationDriverDName } |
        Select-Object -First 1
    if ($null -eq $mutationDriver) {
        Fail-Now ("Requested mutation driver not found: {0}" -f $MutationDriverDName)
    }
}
else {
    $mutationDriver = $drivers |
        Where-Object {
            -not [string]::IsNullOrWhiteSpace([string]$_.DName) -and
            [string]$_.DName -ne $script:ProjectDiagnosticsDName
        } |
        Select-Object -First 1
}

if ($null -eq $mutationDriver) {
    Fail-Now 'No non-diagnostics driver available for mutation testing.'
}

$script:MutationDriverDName = [string]$mutationDriver.DName
$script:MutationDriverName = [string]$mutationDriver.Name
Write-Pass ("Selected mutation driver: {0} ({1})" -f $script:MutationDriverName, $script:MutationDriverDName)

$session = $null
try {
    Write-Step -Number 2 -Text 'Opening WebSocket diagnostics session'
    $session = Normalize-WebSocketSession -Session (New-WebSocketSession -Ip $Ip)
    Send-Subscribe -Session $session -Resource 'MessageLog' -Value 'true'
    Send-Subscribe -Session $session -Resource 'Sysvar' -Value 'true'
    Send-Subscribe -Session $session -Resource 'LogLevels' -Value 'true'
    Write-Pass 'WebSocket connected and subscriptions sent.'

    Write-Step -Number 3 -Text 'Capturing initial driver log level snapshot'
    $initial = Wait-ForInitialSnapshotOrFail -Session $session -TimeoutSec $AckTimeoutSeconds
    $systemInitial = if ($initial.ContainsKey($SystemDiagnosticsChannel)) { [string]$initial[$SystemDiagnosticsChannel] } else { 'unknown' }
    $projectInitial = if ($initial.ContainsKey($script:ProjectDiagnosticsDName)) { [string]$initial[$script:ProjectDiagnosticsDName] } else { 'unknown' }
    Write-Line -Text ("[info] Initial {0} = {1}" -f $SystemDiagnosticsLabel, $systemInitial)
    Write-Line -Text ("[info] Initial {0} ({1}) = {2}" -f $ProjectDiagnosticsLabel, $script:ProjectDiagnosticsDName, $projectInitial)
    Write-Line -Text '[info] Snapshot entries (all drivers/channels):'
    foreach ($dName in ($initial.Keys | Sort-Object)) {
        $label = Get-DisplayLabel -DName ([string]$dName)
        $level = [int]$initial[$dName]
        if ($label.Equals($dName, [System.StringComparison]::OrdinalIgnoreCase)) {
            Write-Line -Text ("  [snapshot] {0} => {1}" -f $dName, $level)
        }
        else {
            Write-Line -Text ("  [snapshot] {0} ({1}) => {2}" -f $label, $dName, $level)
        }
    }
    Write-Pass 'Initial snapshot captured.'

    Write-Step -Number 4 -Text 'Waiting for RTI startup ACK chatter to settle before hard Diagnostics levels'
    $settle = Wait-ForStartupAckSettle -Session $session -MaxSeconds $StartupSettleMaxSeconds -QuietSeconds $StartupAckQuietSeconds
    if ($settle.Settled) {
        Write-Pass ("RTI startup ACK chatter settled (startup ACK lines observed: {0})." -f $settle.StartupAckCount)
    }
    else {
        Write-Line -Text ("[warn] RTI startup ACK chatter did not fully settle within max wait ({0}s). Continuing." -f $StartupSettleMaxSeconds)
    }

    Write-Step -Number 5 -Text 'Applying hard Diagnostics levels (system=0, project=1) with strict ACK validation'
    Write-Line -Text ("[action] Prime {0} ({1}) -> {2} (no ACK required)" -f $ProjectDiagnosticsLabel, $script:ProjectDiagnosticsDName, $BaselineProjectLevel)
    Set-DriverLogLevel -Session $session -DriverDName $script:ProjectDiagnosticsDName -Level $BaselineProjectLevel
    Start-Sleep -Milliseconds ([Math]::Max(0, $ProjectPrimeDelayMs))

    Write-Line -Text ("[action] Confirm {0} ({1}) -> {2} (ACK required)" -f $ProjectDiagnosticsLabel, $script:ProjectDiagnosticsDName, $BaselineProjectLevel)
    Set-DriverLogLevel -Session $session -DriverDName $script:ProjectDiagnosticsDName -Level $BaselineProjectLevel
    Wait-ForAckOrFail -Session $session -TargetDName $script:ProjectDiagnosticsDName -ExpectedLevel $BaselineProjectLevel -TimeoutSec $AckTimeoutSeconds
    Write-Pass ("Gate confirmed: {0} acknowledged at level {1}" -f $ProjectDiagnosticsLabel, $BaselineProjectLevel)

    Write-Line -Text ("[action] Set {0} -> {1}" -f $SystemDiagnosticsLabel, $BaselineSystemLevel)
    Set-DriverLogLevel -Session $session -DriverDName $SystemDiagnosticsChannel -Level $BaselineSystemLevel
    Wait-ForAckOrFail -Session $session -TargetDName $SystemDiagnosticsChannel -ExpectedLevel $BaselineSystemLevel -TimeoutSec $AckTimeoutSeconds
    Write-Pass ("Gate confirmed: {0} acknowledged at level {1}" -f $SystemDiagnosticsLabel, $BaselineSystemLevel)
    Write-Pass 'Hard Diagnostics levels confirmed by ACK logs.'
    Write-Pass 'Both diagnostics baselines are ACK-confirmed. Proceeding to mutation testing.'

    Write-Step -Number 6 -Text 'Testing status maintenance via ACK logs (non-diagnostics mutation driver)'
    Write-Line -Text ("[action] Test set {0} ({1}) -> {2}" -f $script:MutationDriverName, $script:MutationDriverDName, $TestProjectLevel)
    Set-DriverLogLevel -Session $session -DriverDName $script:MutationDriverDName -Level $TestProjectLevel
    Wait-ForAckOrFail -Session $session -TargetDName $script:MutationDriverDName -ExpectedLevel $TestProjectLevel -TimeoutSec $AckTimeoutSeconds

    Write-Line -Text ("[action] Restore {0} ({1}) -> {2}" -f $script:MutationDriverName, $script:MutationDriverDName, $BaselineProjectLevel)
    Set-DriverLogLevel -Session $session -DriverDName $script:MutationDriverDName -Level $BaselineProjectLevel
    Wait-ForAckOrFail -Session $session -TargetDName $script:MutationDriverDName -ExpectedLevel $BaselineProjectLevel -TimeoutSec $AckTimeoutSeconds
    Write-Pass 'Status maintenance confirmed from ACK logs.'

    Write-Step -Number 7 -Text 'Final status summary'
    $systemFinal = if ($script:AckStatusByDName.ContainsKey($SystemDiagnosticsChannel)) { [string]$script:AckStatusByDName[$SystemDiagnosticsChannel] } else { 'unknown' }
    $projectFinal = if ($script:AckStatusByDName.ContainsKey($script:ProjectDiagnosticsDName)) { [string]$script:AckStatusByDName[$script:ProjectDiagnosticsDName] } else { 'unknown' }
    Write-Line -Text ("[info] ACK-derived status {0} = {1}" -f $SystemDiagnosticsLabel, $systemFinal)
    Write-Line -Text ("[info] ACK-derived status {0} ({1}) = {2}" -f $ProjectDiagnosticsLabel, $script:ProjectDiagnosticsDName, $projectFinal)
    Write-Pass 'Probe completed successfully.'
}
finally {
    if ($null -ne $session) {
        Write-Line -Text '[info] Closing WebSocket session...'
        Stop-WebSocketSession -Session $session
    }
}

exit 0
