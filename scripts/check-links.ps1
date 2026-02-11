param(
    [string]$Root = (Get-Location),
    [int]$TimeoutSeconds = 5,
    [int]$MaxLinks = 0,                 # 0 = unlimited
    [switch]$IncludeRedirects,          # Show 3xx separately
    [string[]]$ExcludeDomains,          # Domains to skip
    [switch]$VerboseFailures,
    [switch]$Csv,
    [string]$OutFile = 'link-report.csv'
)

Write-Host "Scanning markdown files under: $Root" -ForegroundColor Cyan
$mdFiles = Get-ChildItem -Path $Root -Recurse -Filter *.md
$pattern = '(https?://[^)\s]+)'
$results = New-Object System.Collections.Generic.List[object]

# Use a single HttpClient for efficiency
$handler = New-Object System.Net.Http.HttpClientHandler
$client  = New-Object System.Net.Http.HttpClient($handler)
$client.Timeout = [TimeSpan]::FromSeconds($TimeoutSeconds)

$linkCount = 0
foreach ($file in $mdFiles) {
    $content = Get-Content -Path $file.FullName -Raw
    $urlMatches = [regex]::Matches($content, $pattern)
    foreach ($m in $urlMatches) {
        $url = $m.Groups[1].Value.Trim()
        if ($ExcludeDomains -and ($ExcludeDomains | Where-Object { $url -like "*$_*" })) { continue }
        $linkCount++
        if ($MaxLinks -gt 0 -and $linkCount -gt $MaxLinks) { break }

        $statusCode = 'ERROR'
        $ok = $false
        $redirect = $false
        try {
            $request = New-Object System.Net.Http.HttpRequestMessage 'Head', $url
            $response = $client.SendAsync($request).GetAwaiter().GetResult()
            $statusCode = [int]$response.StatusCode
            if ($statusCode -ge 300 -and $statusCode -lt 400) { $redirect = $true }
            if ($statusCode -ge 200 -and $statusCode -lt 300) { $ok = $true }
            # Some servers disallow HEAD; fallback to GET if 405
            if ($statusCode -eq 405) {
                $request.Dispose()
                $request = New-Object System.Net.Http.HttpRequestMessage 'Get', $url
                $response = $client.SendAsync($request).GetAwaiter().GetResult()
                $statusCode = [int]$response.StatusCode
                if ($statusCode -ge 200 -and $statusCode -lt 300) { $ok = $true }
                if ($statusCode -ge 300 -and $statusCode -lt 400) { $redirect = $true }
            }
        } catch {
            if ($VerboseFailures) { Write-Warning "Failed: $url -> $($_.Exception.Message)" }
        }
        $results.Add([pscustomobject]@{ File=$file.FullName; Url=$url; StatusCode=$statusCode; OK=$ok; Redirect=$redirect })
    }
    if ($MaxLinks -gt 0 -and $linkCount -ge $MaxLinks) { break }
}

if ($Csv) { $results | Export-Csv -Path $OutFile -NoTypeInformation }

$display = $results
if (-not $IncludeRedirects) { $display = $display | Where-Object { -not $_.Redirect } }

$display | Sort-Object OK, StatusCode | Format-Table File, StatusCode, OK, Redirect, Url -AutoSize

$bad = $results | Where-Object { -not $_.OK }
if ($bad.Count -gt 0) {
    Write-Host "\nBroken or problematic links:" -ForegroundColor Yellow
    $bad | Select-Object File, Url, StatusCode | Format-Table -AutoSize
    Write-Host "Total: $($results.Count) | Failures: $($bad.Count)" -ForegroundColor Yellow
    exit 1
} else {
    Write-Host "\nAll checked links responded with 2xx (or acceptable 3xx redirects)." -ForegroundColor Green
    Write-Host "Total: $($results.Count)" -ForegroundColor Green
}

$client.Dispose()
