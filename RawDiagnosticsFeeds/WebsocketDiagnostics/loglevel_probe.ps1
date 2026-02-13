param(
    [Parameter(Mandatory = $false)]
    [string]$Ip,

    [Parameter(Mandatory = $false)]
    [string]$DriverDName,

    [Parameter(Mandatory = $false)]
    [int]$TargetLevel = 3,

    [Parameter(Mandatory = $false)]
    [int]$RevertLevel = 0,

    [Parameter(Mandatory = $false)]
    [int]$TimeoutSeconds = 10,

    [Parameter(Mandatory = $false)]
    [int]$InitialSnapshotSeconds = 2,

    [Parameter(Mandatory = $false)]
    [int]$QuerySnapshotSeconds = 4,

    [Parameter(Mandatory = $false)]
    [string]$ResultCsv,

    [switch]$SelfTest,
    [switch]$SkipChange
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$CriticalSystemChannels = @(
    'EVENTS_INPUT',
    'EVENTS_SENSE',
    'EVENTS_DRIVER',
    'DEVICES_EXPANSION',
    'EVENTS_SCHEDULED',
    'DEVICES_RTIPANEL',
    'EVENTS_PERIODIC',
    'USER_GENERAL'
)
$script:DriverNameByDName = @{}

function Get-DriverDisplayName {
    param(
        [Parameter(Mandatory = $true)][string]$DName
    )

    if ($script:DriverNameByDName.ContainsKey($DName)) {
        return [string]$script:DriverNameByDName[$DName]
    }

    return $DName
}

trap {
    Write-Host ("[error] line {0}: {1}" -f $_.InvocationInfo.ScriptLineNumber, $_.Exception.Message)
    if (-not [string]::IsNullOrWhiteSpace($_.ScriptStackTrace)) {
        Write-Host ("[error] stack: {0}" -f $_.ScriptStackTrace)
    }
    exit 1
}

function Write-ShpStreamLine {
    param(
        [Parameter(Mandatory = $true)][string]$RawJson
    )

    $entries = @(Get-LogLevelEntriesFromRawMessage -RawJson $RawJson)
    if ($entries.Count -gt 0) {
        $latest = @{}
        foreach ($entry in $entries) {
            $latest[[string]$entry.DName] = [int]$entry.LogLevel
        }

        foreach ($dName in ($latest.Keys | Sort-Object)) {
            $name = Get-DriverDisplayName -DName $dName
            Write-Host ("[shp-stream] {0} ({1}): Current Value is {2}" -f $dName, $name, $latest[$dName])
        }
        return
    }

    $messages = @(Get-MessageLogTextsFromRawMessage -RawJson $RawJson)
    foreach ($msg in $messages) {
        $parsed = Parse-LogLevelUpdateText -Text ([string]$msg)
        if ($null -ne $parsed) {
            $name = Get-DriverDisplayName -DName $parsed.DName
            Write-Host ("[shp-stream] {0} ({1}) : Value set to Level {2}" -f $parsed.DName, $name, $parsed.Level)
        }
        else {
            Write-Host ("[shp-stream] MessageLog: {0}" -f $msg)
        }
    }
}

function Write-ResponseLine {
    param(
        [Parameter(Mandatory = $true)][string]$Text,
        [Parameter(Mandatory = $true)][bool]$Passed
    )

    if ($Passed) {
        Write-Host $Text -ForegroundColor Green
    }
    else {
        Write-Host $Text -ForegroundColor Red
    }
}

function Assert-Equal {
    param(
        [Parameter(Mandatory = $true)]$Actual,
        [Parameter(Mandatory = $true)]$Expected,
        [Parameter(Mandatory = $true)][string]$Message
    )

    if ($Actual -ne $Expected) {
        throw "Assert failed: $Message. Expected=[$Expected] Actual=[$Actual]"
    }
}

function Invoke-SelfTest {
    $statusJson = '{"name":"XP-8v (Primary Processor)","firmware_version":"24.3.29","ip_address":"192.168.1.143"}'
    $status = ConvertFrom-Json -InputObject $statusJson
    $meta = Get-ProcessorMetadata -StatusJson $status
    Assert-Equal -Actual $meta.DeviceName -Expected 'XP-8v (Primary Processor)' -Message 'Device name should parse from system_status'
    Assert-Equal -Actual $meta.Firmware -Expected '24.3.29' -Message 'Firmware should parse from system_status'

    $driversJson = '[{"id":57,"name":"Lighting Driver"},{"id":58}]'
    $drivers = Parse-DriversJson -JsonText $driversJson
    Assert-Equal -Actual $drivers.Count -Expected 2 -Message 'Driver list count'
    Assert-Equal -Actual $drivers[0].DName -Expected 'DRIVER//57' -Message 'Driver DName format'
    Assert-Equal -Actual $drivers[1].Name -Expected 'DRIVER//58' -Message 'Unnamed driver fallback'

    $levelsJson = '{"messageType":"LogLevels","levels":[{"dName":"DRIVER//57","logLevel":3}]}'
    $lvl = Get-DriverLevelFromMessage -RawJson $levelsJson -DriverDName 'DRIVER//57'
    Assert-Equal -Actual $lvl -Expected 3 -Message 'Driver level extraction'

    $msgParsed = Parse-LogLevelUpdateText -Text 'Diagnostics: Primary Processor - Setting LogLevel on DRIVER (2) to 0'
    Assert-Equal -Actual $msgParsed.DName -Expected 'DRIVER//2' -Message 'MessageLog parser DRIVER id'
    Assert-Equal -Actual $msgParsed.Level -Expected 0 -Message 'MessageLog parser level'

    $wsParsed = Parse-LogLevelUpdateText -Text 'Diagnostics: Primary Processor - OnHTTPServerData() data.websocket = {"type":"Subscribe","resource":"LogLevel","value":{"type":"DEVICES_RTIPANEL","level":"3"}}'
    Assert-Equal -Actual $wsParsed.DName -Expected 'DEVICES_RTIPANEL' -Message 'Websocket payload parser type'
    Assert-Equal -Actual $wsParsed.Level -Expected 3 -Message 'Websocket payload parser level'

    Write-Host 'SelfTest passed.'
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

function Get-ProcessorMetadata {
    param(
        [Parameter(Mandatory = $true)]$StatusJson
    )

    $deviceName = $StatusJson.name
    if ([string]::IsNullOrWhiteSpace($deviceName)) {
        $deviceName = $StatusJson.model
    }
    if ([string]::IsNullOrWhiteSpace($deviceName)) {
        $deviceName = $StatusJson.product
    }
    if ([string]::IsNullOrWhiteSpace($deviceName)) {
        $deviceName = 'UNKNOWN'
    }

    $firmware = $StatusJson.firmware_version
    if ([string]::IsNullOrWhiteSpace($firmware)) {
        $firmware = $StatusJson.version
    }
    if ([string]::IsNullOrWhiteSpace($firmware)) {
        $firmware = 'UNKNOWN'
    }

    $ipAddress = $StatusJson.ip_address

    return [pscustomobject]@{
        DeviceName = [string]$deviceName
        Firmware = [string]$firmware
        IpAddress = [string]$ipAddress
    }
}

function Parse-DriversJson {
    param(
        [Parameter(Mandatory = $true)][string]$JsonText
    )

    $root = ConvertFrom-Json -InputObject $JsonText
    if ($null -eq $root) {
        return @()
    }

    $results = New-Object System.Collections.ArrayList

    function Add-DriverRecord {
        param([Parameter(Mandatory = $true)]$Node)

        $nodeProps = @($Node.PSObject.Properties.Name)
        if (-not ($nodeProps -contains 'id')) {
            return
        }

        $idRaw = $Node.id
        $id = 0
        if (-not [int]::TryParse([string]$idRaw, [ref]$id)) {
            return
        }

        $dName = "DRIVER//$id"
        $name = $dName
        if ($nodeProps -contains 'name' -and -not [string]::IsNullOrWhiteSpace([string]$Node.name)) {
            $name = [string]$Node.name
        }

        [void]$results.Add([pscustomobject]@{
                Id = $id
                DName = $dName
                Name = $name
                HasFriendlyName = ($name -ne $dName)
            })
    }

    function Walk-Node {
        param([Parameter(Mandatory = $true)]$Node)

        if ($null -eq $Node) {
            return
        }

        if ($Node -is [System.Collections.IEnumerable] -and -not ($Node -is [string])) {
            foreach ($child in $Node) {
                Walk-Node -Node $child
            }
            return
        }

        $propNames = @($Node.PSObject.Properties | ForEach-Object { $_.Name })
        if (@($propNames).Length -eq 0) {
            return
        }

        Add-DriverRecord -Node $Node

        foreach ($prop in $Node.PSObject.Properties) {
            $value = $prop.Value
            if ($null -eq $value -or ($value -is [string])) {
                continue
            }

            $valuePropCount = @($value.PSObject.Properties).Length
            if ($value -is [System.Collections.IEnumerable] -or $valuePropCount -gt 0) {
                Walk-Node -Node $value
            }
        }
    }

    Walk-Node -Node $root

    $distinct = @{}
    foreach ($driver in $results) {
        if (-not $distinct.ContainsKey($driver.DName)) {
            $distinct[$driver.DName] = $driver
        }
        elseif (-not $distinct[$driver.DName].HasFriendlyName -and $driver.HasFriendlyName) {
            $distinct[$driver.DName] = $driver
        }
    }

    return @($distinct.Values | Sort-Object Id)
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
    $connectTask = $socket.ConnectAsync($uri, $connectCts.Token)
    $null = $connectTask.GetAwaiter().GetResult()

    return [pscustomobject]@{
        Socket = $socket
        PendingReceiveTask = $null
        PendingBuffer = $null
        MessageStream = (New-Object System.IO.MemoryStream)
    }
}

function Ensure-PendingReceiveTask {
    param(
        [Parameter(Mandatory = $true)]$Session
    )

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
        if ($Session.Socket.State -ne [System.Net.WebSockets.WebSocketState]::Open -and $Session.Socket.State -ne [System.Net.WebSockets.WebSocketState]::CloseReceived) {
            return $null
        }

        Ensure-PendingReceiveTask -Session $Session

        $remainingMs = [Math]::Max(1, [int]([Math]::Ceiling(($deadline - (Get-Date)).TotalMilliseconds)))
        $waitMs = [Math]::Min(200, $remainingMs)
        try {
            if (-not $Session.PendingReceiveTask.Wait($waitMs)) {
                continue
            }
        }
        catch {
            $Session.PendingReceiveTask = $null
            $Session.PendingBuffer = $null
            return $null
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

function Send-JsonMessage {
    param(
        [Parameter(Mandatory = $true)]$Session,
        [Parameter(Mandatory = $true)]$Payload
    )

    $socket = $Session.Socket
    if ($null -eq $socket -or $socket.State -ne [System.Net.WebSockets.WebSocketState]::Open) {
        throw "WebSocket is not open. Current state: $($socket.State)"
    }

    $json = $Payload | ConvertTo-Json -Depth 8 -Compress
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($json)
    $segment = [System.ArraySegment[byte]]::new($bytes)
    $sendTask = $socket.SendAsync($segment, [System.Net.WebSockets.WebSocketMessageType]::Text, $true, [System.Threading.CancellationToken]::None)
    $null = $sendTask.GetAwaiter().GetResult()
}

function Stop-WebSocketSession {
    param(
        [Parameter(Mandatory = $true)]$Session
    )

    if ($null -eq $Session) {
        return
    }

    $socket = $Session.Socket
    if ($null -ne $socket) {
        try {
            if ($null -ne $Session.PendingReceiveTask -and -not $Session.PendingReceiveTask.IsCompleted) {
                $socket.Abort()
            }
            elseif ($socket.State -eq [System.Net.WebSockets.WebSocketState]::Open -or $socket.State -eq [System.Net.WebSockets.WebSocketState]::CloseReceived) {
                $closeCts = [System.Threading.CancellationTokenSource]::new([TimeSpan]::FromSeconds(1))
                try {
                    $closeTask = $socket.CloseAsync([System.Net.WebSockets.WebSocketCloseStatus]::NormalClosure, 'done', $closeCts.Token)
                    $null = $closeTask.GetAwaiter().GetResult()
                }
                catch {
                    $socket.Abort()
                }
                finally {
                    $closeCts.Dispose()
                }
            }
            elseif ($socket.State -ne [System.Net.WebSockets.WebSocketState]::Closed -and $socket.State -ne [System.Net.WebSockets.WebSocketState]::Aborted) {
                $socket.Abort()
            }
        }
        catch {
        }
        finally {
            $Session.PendingReceiveTask = $null
            $Session.PendingBuffer = $null
            $socket.Dispose()
            $Session.Socket = $null
        }
    }

    try {
        if ($null -ne $Session.MessageStream) {
            $Session.MessageStream.Dispose()
        }
    }
    catch {
    }
}

function Get-DriverLevelFromMessage {
    param(
        [Parameter(Mandatory = $true)][string]$RawJson,
        [Parameter(Mandatory = $true)][string]$DriverDName
    )

    $entries = Get-LogLevelEntriesFromRawMessage -RawJson $RawJson
    foreach ($entry in $entries) {
        if ([string]$entry.DName -ieq $DriverDName) {
            return [int]$entry.LogLevel
        }
    }

    return $null
}

function Get-LogLevelEntriesFromObject {
    param(
        [Parameter(Mandatory = $true)]$Object
    )

    $results = New-Object System.Collections.ArrayList
    if ($null -eq $Object) {
        return @()
    }

    if (($Object.PSObject.Properties.Name -contains 'messageType') -and ([string]$Object.messageType -ieq 'LogLevels') -and ($Object.PSObject.Properties.Name -contains 'levels') -and ($null -ne $Object.levels)) {
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

            [void]$results.Add([pscustomobject]@{
                    DName = $dName
                    LogLevel = $intLevel
                })
        }
    }

    if (($Object.PSObject.Properties.Name -contains 'message') -and ($Object.message -is [string]) -and (-not [string]::IsNullOrWhiteSpace([string]$Object.message))) {
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

function Get-LogLevelEntriesFromSnapshotObject {
    param(
        [Parameter(Mandatory = $true)]$Object
    )

    $results = New-Object System.Collections.ArrayList
    if ($null -eq $Object) {
        return @()
    }

    if (($Object.PSObject.Properties.Name -contains 'levels') -and ($null -ne $Object.levels)) {
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

            [void]$results.Add([pscustomobject]@{
                    DName = $dName
                    LogLevel = $intLevel
                })
        }
    }

    return @($results)
}

function Get-LogLevelEntriesFromRawMessage {
    param(
        [Parameter(Mandatory = $true)][string]$RawJson
    )

    try {
        $obj = ConvertFrom-Json -InputObject $RawJson
    }
    catch {
        return @()
    }

    if ($null -eq $obj) {
        return @()
    }

    return @(Get-LogLevelEntriesFromObject -Object $obj)
}

function Try-Parse-LogLevelsSnapshot {
    param(
        [Parameter(Mandatory = $true)][string]$JsonText
    )

    try {
        $obj = ConvertFrom-Json -InputObject $JsonText
    }
    catch {
        return $null
    }

    if ($null -eq $obj) {
        return $null
    }

    $entries = @(Get-LogLevelEntriesFromObject -Object $obj)
    if ($entries.Count -eq 0) {
        $entries = @(Get-LogLevelEntriesFromSnapshotObject -Object $obj)
    }

    if ($entries.Count -eq 0) {
        return $null
    }

    $map = @{}
    foreach ($entry in $entries) {
        $map[[string]$entry.DName] = [int]$entry.LogLevel
    }

    return $map
}

function Get-MessageLogTextsFromObject {
    param(
        [Parameter(Mandatory = $true)]$Object
    )

    $results = New-Object System.Collections.ArrayList
    if ($null -eq $Object) {
        return @()
    }

    if (($Object.PSObject.Properties.Name -contains 'messageType') -and ([string]$Object.messageType -ieq 'MessageLog')) {
        $textValue = [string]$Object.text
        if (-not [string]::IsNullOrWhiteSpace($textValue)) {
            [void]$results.Add($textValue)
        }
    }

    if (($Object.PSObject.Properties.Name -contains 'message') -and ($Object.message -is [string]) -and (-not [string]::IsNullOrWhiteSpace([string]$Object.message))) {
        try {
            $inner = ConvertFrom-Json -InputObject ([string]$Object.message)
            foreach ($innerText in (Get-MessageLogTextsFromObject -Object $inner)) {
                [void]$results.Add($innerText)
            }
        }
        catch {
        }
    }

    return @($results)
}

function Get-MessageLogTextsFromRawMessage {
    param(
        [Parameter(Mandatory = $true)][string]$RawJson
    )

    try {
        $obj = ConvertFrom-Json -InputObject $RawJson
    }
    catch {
        return @()
    }

    if ($null -eq $obj) {
        return @()
    }

    return @(Get-MessageLogTextsFromObject -Object $obj)
}

function Get-DriverIdFromDName {
    param(
        [Parameter(Mandatory = $true)][string]$DriverDName
    )

    $match = [regex]::Match($DriverDName, '^DRIVER//(\d+)$', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
    if (-not $match.Success) {
        return $null
    }

    return [int]$match.Groups[1].Value
}

function Parse-LogLevelUpdateText {
    param(
        [Parameter(Mandatory = $true)][string]$Text
    )

    if ([string]::IsNullOrWhiteSpace($Text)) {
        return $null
    }

    $settingMatch = [regex]::Match($Text, 'Setting LogLevel on (?<target>.+?) to (?<level>\d+)', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
    if ($settingMatch.Success) {
        $target = ([string]$settingMatch.Groups['target'].Value).Trim()
        $level = [int]$settingMatch.Groups['level'].Value

        $driverMatch = [regex]::Match($target, '^DRIVER \((?<id>\d+)\)$', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
        if ($driverMatch.Success) {
            $id = [int]$driverMatch.Groups['id'].Value
            return [pscustomobject]@{
                DName = "DRIVER//$id"
                DriverId = $id
                Level = $level
            }
        }

        return [pscustomobject]@{
            DName = $target
            DriverId = $null
            Level = $level
        }
    }

    $jsonMatch = [regex]::Match($Text, 'data\.websocket\s*=\s*(\{.*\})', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
    if (-not $jsonMatch.Success) {
        return $null
    }

    try {
        $obj = ConvertFrom-Json -InputObject $jsonMatch.Groups[1].Value
    }
    catch {
        return $null
    }

    if ($null -eq $obj -or [string]$obj.resource -ne 'LogLevel' -or $null -eq $obj.value) {
        return $null
    }

    $dName = [string]$obj.value.type
    if ([string]::IsNullOrWhiteSpace($dName)) {
        return $null
    }

    $level = 0
    if (-not [int]::TryParse([string]$obj.value.level, [ref]$level)) {
        return $null
    }

    $id = Get-DriverIdFromDName -DriverDName $dName
    return [pscustomobject]@{
        DName = $dName
        DriverId = $id
        Level = $level
    }
}

function Parse-DriverLevelFromRawText {
    param(
        [Parameter(Mandatory = $true)][string]$RawText
    )

    return Parse-LogLevelUpdateText -Text $RawText
}

function Wait-ForDriverLevelAcks {
    param(
        [Parameter(Mandatory = $true)]$Session,
        [Parameter(Mandatory = $true)][string]$DriverDName,
        [Parameter(Mandatory = $true)][int]$ExpectedLevel,
        [Parameter(Mandatory = $true)][int]$TimeoutSec
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSec)

    $lastLogLevelsRaw = $null
    $logLevelsObserved = $null
    $messageLogObserved = $null
    $logLevelsConfirmed = $false
    $messageLogConfirmed = $false
    $logLevelsRaw = $null
    $messageLogRaw = $null
    $targetDriverId = Get-DriverIdFromDName -DriverDName $DriverDName

    while ((Get-Date) -lt $deadline) {
        $remaining = [Math]::Max(1, [int]([Math]::Ceiling(($deadline - (Get-Date)).TotalSeconds)))
        $raw = Get-NextQueuedMessage -Session $Session -TimeoutSec $remaining
        if ($null -eq $raw) {
            continue
        }

        $entries = @(Get-LogLevelEntriesFromRawMessage -RawJson $raw)
        if ($entries.Count -gt 0) {
            $lastLogLevelsRaw = $raw
            foreach ($entry in $entries) {
                if ([string]$entry.DName -ieq $DriverDName) {
                    $logLevelsObserved = [int]$entry.LogLevel
                    if ([int]$entry.LogLevel -eq $ExpectedLevel) {
                        $logLevelsConfirmed = $true
                        $logLevelsRaw = $raw
                    }
                }
            }
        }

        $messageTexts = @(Get-MessageLogTextsFromRawMessage -RawJson $raw)
        foreach ($text in $messageTexts) {
            $parsed = Parse-LogLevelUpdateText -Text ([string]$text)
            if ($null -eq $parsed) {
                continue
            }

            $msgDName = [string]$parsed.DName
            $msgDriverId = $parsed.DriverId
            $msgLevel = [int]$parsed.Level
            if (($msgDName -ieq $DriverDName) -or ($null -ne $targetDriverId -and $null -ne $msgDriverId -and [int]$msgDriverId -eq [int]$targetDriverId)) {
                $messageLogObserved = $msgLevel
                if ($msgLevel -eq $ExpectedLevel) {
                    $messageLogConfirmed = $true
                    $messageLogRaw = $raw
                }
            }
        }

        $rawParsed = Parse-DriverLevelFromRawText -RawText $raw
        if ($null -ne $rawParsed) {
            $msgDName = [string]$rawParsed.DName
            $msgDriverId = $rawParsed.DriverId
            $msgLevel = [int]$rawParsed.Level
            if (($msgDName -ieq $DriverDName) -or ($null -ne $targetDriverId -and $null -ne $msgDriverId -and [int]$msgDriverId -eq [int]$targetDriverId)) {
                $messageLogObserved = $msgLevel
                if ($msgLevel -eq $ExpectedLevel) {
                    $messageLogConfirmed = $true
                    $messageLogRaw = $raw
                }
            }
        }

        if ($logLevelsConfirmed -and $messageLogConfirmed) {
            break
        }
    }

    return [pscustomobject]@{
        Confirmed = ($logLevelsConfirmed -and $messageLogConfirmed)
        LogLevelsConfirmed = $logLevelsConfirmed
        MessageLogConfirmed = $messageLogConfirmed
        LogLevelsObserved = $logLevelsObserved
        MessageLogObserved = $messageLogObserved
        LogLevelsRaw = if ($null -ne $logLevelsRaw) { $logLevelsRaw } else { $lastLogLevelsRaw }
        MessageLogRaw = $messageLogRaw
        TimedOut = -not ($logLevelsConfirmed -and $messageLogConfirmed)
    }
}

function Collect-ObservedLogLevelEntries {
    param(
        [Parameter(Mandatory = $true)]$Session,
        [Parameter(Mandatory = $true)][int]$TimeoutSec
    )

    $map = @{}
    if ($TimeoutSec -le 0) {
        return $map
    }

    $deadline = (Get-Date).AddSeconds($TimeoutSec)
    while ((Get-Date) -lt $deadline) {
        $remaining = [Math]::Max(1, [int]([Math]::Ceiling(($deadline - (Get-Date)).TotalSeconds)))
        $raw = Get-NextQueuedMessage -Session $Session -TimeoutSec $remaining
        if ($null -eq $raw) {
            continue
        }

        $entries = @(Get-LogLevelEntriesFromRawMessage -RawJson $raw)
        foreach ($entry in $entries) {
            $map[[string]$entry.DName] = [int]$entry.LogLevel
        }
    }

    return $map
}

function Get-LogLevelsSnapshotByReconnect {
    param(
        [Parameter(Mandatory = $true)][string]$Ip,
        [Parameter(Mandatory = $true)][int]$TimeoutSec
    )

    $session = $null
    try {
        $session = New-WebSocketSession -Ip $Ip -ConnectTimeoutSec ([Math]::Max(1, [int]([Math]::Ceiling($TimeoutSec / 2))))
        $deadline = (Get-Date).AddSeconds($TimeoutSec)
        while ((Get-Date) -lt $deadline) {
            $remaining = [Math]::Max(1, [int]([Math]::Ceiling(($deadline - (Get-Date)).TotalSeconds)))
            $raw = Get-NextQueuedMessage -Session $session -TimeoutSec $remaining
            if ($null -eq $raw) {
                continue
            }

            $entries = @(Get-LogLevelEntriesFromRawMessage -RawJson $raw)
            if ($entries.Count -gt 0) {
                $map = @{}
                foreach ($entry in $entries) {
                    $map[[string]$entry.DName] = [int]$entry.LogLevel
                }
                return $map
            }
        }
    }
    finally {
        if ($null -ne $session) {
            Stop-WebSocketSession -Session $session
        }
    }

    return $null
}

function Get-LogLevelsSnapshotByHttp {
    param(
        [Parameter(Mandatory = $true)][string]$Ip,
        [Parameter(Mandatory = $true)][int]$TimeoutSec
    )

    $endpoints = @(
        "http://${Ip}:5000/diagnostics/data/loglevels",
        "http://${Ip}:5000/diagnostics/data/log_levels",
        "http://${Ip}:5000/diagnostics/data/loglevel"
    )

    foreach ($endpoint in $endpoints) {
        try {
            $text = Get-Json -Uri $endpoint -TimeoutSec $TimeoutSec
            $map = Try-Parse-LogLevelsSnapshot -JsonText $text
            if ($null -ne $map) {
                return $map
            }
        }
        catch {
        }
    }

    return $null
}

function Set-DriverLogLevel {
    param(
        [Parameter(Mandatory = $true)]$Session,
        [Parameter(Mandatory = $true)][string]$DriverDName,
        [Parameter(Mandatory = $true)][int]$Level
    )

    $driverId = Get-DriverIdFromDName -DriverDName $DriverDName

    $payloads = New-Object System.Collections.ArrayList
    [void]$payloads.Add([pscustomobject]@{
            type = 'Subscribe'
            resource = 'LogLevel'
            value = [pscustomobject]@{
                type = $DriverDName
                level = [string]$Level
            }
        })

    if ($null -ne $driverId) {
        [void]$payloads.Add([pscustomobject]@{
                type = 'Subscribe'
                resource = 'LogLevel'
                value = [pscustomobject]@{
                    type = 'DRIVER'
                    level = [string]$Level
                    driverId = [string]$driverId
                }
            })
    }

    foreach ($payload in $payloads) {
        Send-JsonMessage -Session $Session -Payload $payload
    }
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

function Append-ResultCsv {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)]$Result
    )

    $row = [pscustomobject]@{
        TimestampUtc = (Get-Date).ToUniversalTime().ToString('o')
        TargetIp = $Result.TargetIp
        DeviceName = $Result.DeviceName
        Firmware = $Result.Firmware
        DriverDName = $Result.DriverDName
        DriverFriendlyName = $Result.DriverFriendlyName
        DriverFriendlyNamePresent = $Result.DriverFriendlyNamePresent
        MissingDriverNameCount = $Result.MissingDriverNameCount
        SetTo3Confirmed = $Result.SetTo3Confirmed
        RevertTo0Confirmed = $Result.RevertTo0Confirmed
        OverallSuccess = $Result.OverallSuccess
    }

    $exists = Test-Path -LiteralPath $Path
    $dir = Split-Path -Parent $Path
    if (-not [string]::IsNullOrWhiteSpace($dir) -and -not (Test-Path -LiteralPath $dir)) {
        New-Item -ItemType Directory -Path $dir | Out-Null
    }

    if ($exists) {
        $row | Export-Csv -Path $Path -Append -NoTypeInformation
    }
    else {
        $row | Export-Csv -Path $Path -NoTypeInformation
    }
}

if ($SelfTest) {
    Invoke-SelfTest
    exit 0
}

if ([string]::IsNullOrWhiteSpace($Ip)) {
    throw 'Parameter -Ip is required unless -SelfTest is used.'
}

if ($TargetLevel -lt 0 -or $TargetLevel -gt 3) {
    throw 'TargetLevel must be 0..3.'
}

if ($RevertLevel -lt 0 -or $RevertLevel -gt 3) {
    throw 'RevertLevel must be 0..3.'
}

if ($InitialSnapshotSeconds -lt 0 -or $InitialSnapshotSeconds -gt 60) {
    throw 'InitialSnapshotSeconds must be 0..60.'
}

if ($QuerySnapshotSeconds -lt 1 -or $QuerySnapshotSeconds -gt 30) {
    throw 'QuerySnapshotSeconds must be 1..30.'
}

$systemStatusUri = "http://${Ip}:5000/diagnostics/data/system_status"
$driversUri = "http://${Ip}:5000/diagnostics/data/drivers"

$statusText = Get-Json -Uri $systemStatusUri
$statusObj = ConvertFrom-Json -InputObject $statusText
$meta = Get-ProcessorMetadata -StatusJson $statusObj

$driversText = Get-Json -Uri $driversUri
$drivers = Parse-DriversJson -JsonText $driversText

if ($drivers.Count -eq 0) {
    throw 'No drivers returned from diagnostics/data/drivers.'
}

$targetDrivers = @()
if (-not [string]::IsNullOrWhiteSpace($DriverDName)) {
    $single = $drivers | Where-Object { $_.DName -ieq $DriverDName } | Select-Object -First 1
    if ($null -eq $single) {
        throw "Requested DriverDName not found: $DriverDName"
    }
    $targetDrivers = @($single)
}
else {
    $targetDrivers = @($drivers | Where-Object { $_.DName -like 'DRIVER//*' } | Sort-Object Id)
    foreach ($channel in $CriticalSystemChannels) {
        if (-not ($targetDrivers | Where-Object { $_.DName -ieq $channel } | Select-Object -First 1)) {
            $targetDrivers += [pscustomobject]@{
                Id = -1
                DName = $channel
                Name = $channel
                HasFriendlyName = $true
            }
        }
    }
}

if ($targetDrivers.Count -eq 0) {
    throw 'Unable to choose target drivers.'
}

$missingFriendly = @($drivers | Where-Object { -not $_.HasFriendlyName })

$script:DriverNameByDName = @{}
foreach ($driver in $drivers) {
    $script:DriverNameByDName[[string]$driver.DName] = [string]$driver.Name
}
foreach ($channel in $CriticalSystemChannels) {
    if (-not $script:DriverNameByDName.ContainsKey($channel)) {
        $script:DriverNameByDName[$channel] = $channel
    }
}

Write-Host ("Target: {0}" -f $Ip)
Write-Host ("Device: {0}" -f $meta.DeviceName)
Write-Host ("Firmware: {0}" -f $meta.Firmware)
Write-Host ("Drivers under test: {0}" -f $targetDrivers.Count)
Write-Host ("Drivers missing friendly names: {0}/{1}" -f $missingFriendly.Count, $drivers.Count)

$overallSuccess = $false
$driverResults = New-Object System.Collections.ArrayList
$observed = @{}
$mutationsExecuted = $false

if (-not $SkipChange) {
    $session = $null
    try {
        Write-Host '[probe] Opening WebSocket session...'
        $session = New-WebSocketSession -Ip $Ip

        Write-Host '[probe] Sending base subscriptions...'
        Send-Subscribe -Session $session -Resource 'MessageLog' -Value 'true'
        Send-Subscribe -Session $session -Resource 'Sysvar' -Value 'true'

        if ([string]::IsNullOrWhiteSpace($DriverDName)) {
            Write-Host ("[probe] Collecting observed LogLevels channels for {0}s..." -f $InitialSnapshotSeconds)
            $observed = Collect-ObservedLogLevelEntries -Session $session -TimeoutSec $InitialSnapshotSeconds
            foreach ($dName in $observed.Keys) {
                if (-not ($targetDrivers | Where-Object { $_.DName -ieq $dName } | Select-Object -First 1)) {
                    $targetDrivers += [pscustomobject]@{
                        Id = -1
                        DName = $dName
                        Name = $dName
                        HasFriendlyName = $false
                    }
                }
            }
            if ($observed.Keys.Count -gt 0) {
                Write-Host ("[probe] Added {0} channels observed from LogLevels feed." -f $observed.Keys.Count)
            }

            Write-Host '[probe] Initial channel values:'
            foreach ($dName in ($targetDrivers | Select-Object -ExpandProperty DName | Sort-Object -Unique)) {
                $name = Get-DriverDisplayName -DName $dName
                if ($observed.ContainsKey($dName)) {
                    Write-Host ("{0} ({1}): Initial Value is {2}" -f $dName, $name, [int]$observed[$dName])
                }
                else {
                    Write-Host ("{0} ({1}): Initial Value is unknown" -f $dName, $name)
                }
            }
        }

        $continueAnswer = Read-Host 'Run set/revert tests now? (Y/N)'
        $shouldRunMutations = $continueAnswer -match '^(y|yes)$'
        if (-not $shouldRunMutations) {
            Write-Host '[probe] User chose not to run set/revert tests. Closing connection.'
        }
        else {
            $mutationsExecuted = $true
            $diagDriver = $targetDrivers | Where-Object { $_.Name -match '^Diagnostics:' } | Select-Object -First 1
            if ($null -ne $diagDriver) {
                Write-Host ("[probe] Setting {0} ({1}) to 3 and leaving it there..." -f $diagDriver.DName, $diagDriver.Name)
                Set-DriverLogLevel -Session $session -DriverDName $diagDriver.DName -Level 3
            }
            foreach ($driver in $targetDrivers) {
                if ($null -ne $diagDriver -and $driver.DName -ieq $diagDriver.DName) {
                    continue
                }
                $initialLevel = $null
                $setConfirmed = $false
                $revertConfirmed = $false
                $querySetLevel = $null
                $queryRevertLevel = $null

                if ($InitialSnapshotSeconds -gt 0 -and [string]::IsNullOrWhiteSpace($DriverDName)) {
                    if ($observed.ContainsKey($driver.DName)) {
                        $initialLevel = [int]$observed[$driver.DName]
                    }
                    else {
                        $initialLevel = $null
                    }
                }

                Write-Host ("[probe] Setting {0} ({1}) to {2}..." -f $driver.DName, $driver.Name, $TargetLevel)
                Set-DriverLogLevel -Session $session -DriverDName $driver.DName -Level $TargetLevel
            $setResult = Wait-ForDriverLevelAcks -Session $session -DriverDName $driver.DName -ExpectedLevel $TargetLevel -TimeoutSec $TimeoutSeconds
            if ($null -ne $setResult.LogLevelsObserved) {
                Write-ResponseLine -Text ("after set -> LogLevels observedLevel={0} confirmed={1}" -f $setResult.LogLevelsObserved, $setResult.LogLevelsConfirmed) -Passed $setResult.LogLevelsConfirmed
            }
            else {
                Write-ResponseLine -Text ("after set -> LogLevels observedLevel=unknown confirmed=False") -Passed $false
            }
            if ($null -ne $setResult.MessageLogObserved) {
                Write-ResponseLine -Text ("after set -> MessageLog observedLevel={0} confirmed={1}" -f $setResult.MessageLogObserved, $setResult.MessageLogConfirmed) -Passed $setResult.MessageLogConfirmed
            }
            else {
                Write-ResponseLine -Text ("after set -> MessageLog observedLevel=unknown confirmed=False") -Passed $false
            }
            if ($setResult.LogLevelsConfirmed -or $setResult.MessageLogConfirmed) {
                $ackLevel = if ($setResult.MessageLogConfirmed) { $setResult.MessageLogObserved } elseif ($setResult.LogLevelsConfirmed) { $setResult.LogLevelsObserved } else { $null }
                if ($null -ne $ackLevel) {
                    Write-Host ("ack received, app would update status to: {0} ({1}) Level {2}" -f $driver.DName, $driver.Name, $ackLevel)
                }
            }

            $querySnapshot = Get-LogLevelsSnapshotByHttp -Ip $Ip -TimeoutSec $QuerySnapshotSeconds
            $querySource = 'Http'
            if ($null -eq $querySnapshot) {
                $querySnapshot = Get-LogLevelsSnapshotByReconnect -Ip $Ip -TimeoutSec $QuerySnapshotSeconds
                $querySource = 'Reconnect'
            }

            if ($null -ne $querySnapshot -and $querySnapshot.ContainsKey($driver.DName)) {
                $querySetLevel = [int]$querySnapshot[$driver.DName]
                $setConfirmed = ($querySetLevel -eq $TargetLevel)
                Write-ResponseLine -Text ("after set -> QuerySnapshot({0}) observedLevel={1} confirmed={2}" -f $querySource, $querySetLevel, $setConfirmed) -Passed $setConfirmed
            }
            else {
                $setConfirmed = $false
                Write-ResponseLine -Text ("after set -> QuerySnapshot({0}) observedLevel=unknown confirmed=False" -f $querySource) -Passed $false
            }

                Write-Host ("[probe] Reverting {0} ({1}) to {2}..." -f $driver.DName, $driver.Name, $RevertLevel)
                Set-DriverLogLevel -Session $session -DriverDName $driver.DName -Level $RevertLevel
            $revertResult = Wait-ForDriverLevelAcks -Session $session -DriverDName $driver.DName -ExpectedLevel $RevertLevel -TimeoutSec $TimeoutSeconds
            if ($null -ne $revertResult.LogLevelsObserved) {
                Write-ResponseLine -Text ("after revert -> LogLevels observedLevel={0} confirmed={1}" -f $revertResult.LogLevelsObserved, $revertResult.LogLevelsConfirmed) -Passed $revertResult.LogLevelsConfirmed
            }
            else {
                Write-ResponseLine -Text ("after revert -> LogLevels observedLevel=unknown confirmed=False") -Passed $false
            }
            if ($null -ne $revertResult.MessageLogObserved) {
                Write-ResponseLine -Text ("after revert -> MessageLog observedLevel={0} confirmed={1}" -f $revertResult.MessageLogObserved, $revertResult.MessageLogConfirmed) -Passed $revertResult.MessageLogConfirmed
            }
            else {
                Write-ResponseLine -Text ("after revert -> MessageLog observedLevel=unknown confirmed=False") -Passed $false
            }
            if ($revertResult.LogLevelsConfirmed -or $revertResult.MessageLogConfirmed) {
                $ackLevel = if ($revertResult.MessageLogConfirmed) { $revertResult.MessageLogObserved } elseif ($revertResult.LogLevelsConfirmed) { $revertResult.LogLevelsObserved } else { $null }
                if ($null -ne $ackLevel) {
                    Write-Host ("ack received, app would update status to: {0} ({1}) Level {2}" -f $driver.DName, $driver.Name, $ackLevel)
                }
            }

            $querySnapshot = Get-LogLevelsSnapshotByHttp -Ip $Ip -TimeoutSec $QuerySnapshotSeconds
            $querySource = 'Http'
            if ($null -eq $querySnapshot) {
                $querySnapshot = Get-LogLevelsSnapshotByReconnect -Ip $Ip -TimeoutSec $QuerySnapshotSeconds
                $querySource = 'Reconnect'
            }

            if ($null -ne $querySnapshot -and $querySnapshot.ContainsKey($driver.DName)) {
                $queryRevertLevel = [int]$querySnapshot[$driver.DName]
                $revertConfirmed = ($queryRevertLevel -eq $RevertLevel)
                Write-ResponseLine -Text ("after revert -> QuerySnapshot({0}) observedLevel={1} confirmed={2}" -f $querySource, $queryRevertLevel, $revertConfirmed) -Passed $revertConfirmed
            }
            else {
                $revertConfirmed = $false
                Write-ResponseLine -Text ("after revert -> QuerySnapshot({0}) observedLevel=unknown confirmed=False" -f $querySource) -Passed $false
            }

                $driverPass = ($setConfirmed -and $revertConfirmed)
                [void]$driverResults.Add([pscustomobject]@{
                        DriverDName = $driver.DName
                        DriverFriendlyName = $driver.Name
                        DriverFriendlyNamePresent = $driver.HasFriendlyName
                        InitialObservedLevel = $initialLevel
                        SetTo3Confirmed = $setConfirmed
                        SetConfirmLogLevels = $setResult.LogLevelsConfirmed
                        SetConfirmMessageLog = $setResult.MessageLogConfirmed
                        SetConfirmQuery = $setConfirmed
                        SetQueryObservedLevel = $querySetLevel
                        RevertTo0Confirmed = $revertConfirmed
                        RevertConfirmLogLevels = $revertResult.LogLevelsConfirmed
                        RevertConfirmMessageLog = $revertResult.MessageLogConfirmed
                        RevertConfirmQuery = $revertConfirmed
                        RevertQueryObservedLevel = $queryRevertLevel
                        OverallSuccess = $driverPass
                    })
            }

            $overallSuccess = (@($driverResults | Where-Object { -not $_.OverallSuccess }).Count -eq 0)
        }
    }
    finally {
        if ($null -ne $session) {
            Write-Host '[probe] Closing WebSocket session...'
            Stop-WebSocketSession -Session $session
        }
    }
}
else {
    foreach ($driver in $targetDrivers) {
        [void]$driverResults.Add([pscustomobject]@{
                DriverDName = $driver.DName
                DriverFriendlyName = $driver.Name
                DriverFriendlyNamePresent = $driver.HasFriendlyName
                InitialObservedLevel = $null
                SetTo3Confirmed = $false
                SetConfirmLogLevels = $false
                SetConfirmMessageLog = $false
                SetConfirmQuery = $false
                SetQueryObservedLevel = $null
                RevertTo0Confirmed = $false
                RevertConfirmLogLevels = $false
                RevertConfirmMessageLog = $false
                RevertConfirmQuery = $false
                RevertQueryObservedLevel = $null
                OverallSuccess = $false
            })
    }
}

$driverResultsArray = @($driverResults)
$missingDriverNamesSample = @($missingFriendly | Select-Object -First 10 -ExpandProperty DName)

$result = [pscustomobject]@{
    TargetIp = $Ip
    DeviceName = $meta.DeviceName
    Firmware = $meta.Firmware
    DriverCountTested = $targetDrivers.Count
    MissingDriverNameCount = $missingFriendly.Count
    MissingDriverNamesSample = $missingDriverNamesSample
    DriverResults = $driverResultsArray
    DriversSetTo3Confirmed = @($driverResultsArray | Where-Object { $_.SetTo3Confirmed }).Count
    DriversRevertTo0Confirmed = @($driverResultsArray | Where-Object { $_.RevertTo0Confirmed }).Count
    OverallSuccess = $overallSuccess
    Notes = if ($missingFriendly.Count -gt 0) {
        'At least some driver entries had no name in diagnostics/data/drivers, so fallback DRIVER//<id> was used.'
    } else {
        'All returned driver entries included friendly names.'
    }
}

if (-not [string]::IsNullOrWhiteSpace($ResultCsv)) {
    foreach ($driverResult in $driverResultsArray) {
        $row = [pscustomobject]@{
            TargetIp = $result.TargetIp
            DeviceName = $result.DeviceName
            Firmware = $result.Firmware
            DriverDName = $driverResult.DriverDName
            DriverFriendlyName = $driverResult.DriverFriendlyName
            DriverFriendlyNamePresent = $driverResult.DriverFriendlyNamePresent
            MissingDriverNameCount = $result.MissingDriverNameCount
            SetTo3Confirmed = $driverResult.SetTo3Confirmed
            SetConfirmLogLevels = $driverResult.SetConfirmLogLevels
            SetConfirmMessageLog = $driverResult.SetConfirmMessageLog
            SetConfirmQuery = $driverResult.SetConfirmQuery
            SetQueryObservedLevel = $driverResult.SetQueryObservedLevel
            RevertTo0Confirmed = $driverResult.RevertTo0Confirmed
            RevertConfirmLogLevels = $driverResult.RevertConfirmLogLevels
            RevertConfirmMessageLog = $driverResult.RevertConfirmMessageLog
            RevertConfirmQuery = $driverResult.RevertConfirmQuery
            RevertQueryObservedLevel = $driverResult.RevertQueryObservedLevel
            OverallSuccess = $driverResult.OverallSuccess
        }
        Append-ResultCsv -Path $ResultCsv -Result $row
    }
    Write-Host "Result appended to $ResultCsv"
}

if (-not $SkipChange -and $mutationsExecuted -and -not $overallSuccess) {
    exit 2
}

exit 0
