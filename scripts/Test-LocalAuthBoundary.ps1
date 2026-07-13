param(
    [string]$BaseUrl = "http://localhost:5117"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Net.Http

$BaseUrl = $BaseUrl.TrimEnd("/")
$failures = [System.Collections.Generic.List[string]]::new()
$httpClient = [System.Net.Http.HttpClient]::new()

function Get-SafeBodyPreview {
    param([string]$Body)

    if ([string]::IsNullOrWhiteSpace($Body)) {
        return "<empty>"
    }

    $preview = $Body -replace "\s+", " "
    $preview = $preview -replace "(?i)eyJ[A-Za-z0-9_-]{10,}\.[A-Za-z0-9_-]{10,}\.[A-Za-z0-9_-]{10,}", "[redacted-token]"
    $preview = $preview -replace "(?i)\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}\b", "[redacted-email]"
    $preview = $preview -replace "(?i)\b[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}\b", "[redacted-identifier]"

    if ($preview.Length -gt 300) {
        return $preview.Substring(0, 300) + "..."
    }

    return $preview
}

function Test-UnsafeAuthenticationContent {
    param(
        [string]$Body,
        [string]$Headers
    )

    $content = "$Headers`n$Body"
    $unsafePatterns = @(
        "(?i)eyJ[A-Za-z0-9_-]{10,}\.[A-Za-z0-9_-]{10,}\.[A-Za-z0-9_-]{10,}",
        "(?i)\berror_description\b",
        "(?i)\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}\b",
        "(?i)\b[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}\b",
        "(?i)\b(preferred_username|given_name|family_name|unique_name|upn|oid|tid|sub|roles?|claims?)\b",
        "(?i)\b(authority|issuer|audience|client[_ -]?id|tenant[_ -]?id|object[_ -]?id)\b"
    )

    return $unsafePatterns.Where({ $content -match $_ }).Count -gt 0
}

function Invoke-BoundaryCheck {
    param(
        [string]$Method,
        [string]$Route,
        [ValidateSet("Success", "Unauthorized")]
        [string]$Expected,
        [string]$Body = ""
    )

    $request = [System.Net.Http.HttpRequestMessage]::new(
        [System.Net.Http.HttpMethod]::new($Method),
        "$BaseUrl$Route")

    if (-not [string]::IsNullOrEmpty($Body)) {
        $request.Content = [System.Net.Http.StringContent]::new(
            $Body,
            [System.Text.Encoding]::UTF8,
            "application/json")
    }

    Write-Host ("Checking {0,-6} {1}" -f $Method, $Route) -NoNewline
    $response = $null

    try {
        $response = $httpClient.SendAsync($request).GetAwaiter().GetResult()
        $responseBody = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
        $responseHeaders = $response.Headers.ToString() + $response.Content.Headers.ToString()
        $actualStatus = [int]$response.StatusCode
        $statusPassed = if ($Expected -eq "Success") {
            $actualStatus -ge 200 -and $actualStatus -le 299
        }
        else {
            $actualStatus -eq 401
        }
        $contentPassed = -not (Test-UnsafeAuthenticationContent -Body $responseBody -Headers $responseHeaders)

        if ($statusPassed -and $contentPassed) {
            Write-Host " PASS ($actualStatus)" -ForegroundColor Green
            return
        }

        Write-Host " FAIL ($actualStatus)" -ForegroundColor Red
        $expectedDescription = if ($Expected -eq "Success") { "2xx success" } else { "401 Unauthorized" }
        $failureReason = if ($contentPassed) { "unexpected status" } else { "unsafe authentication content" }
        $safePreview = Get-SafeBodyPreview -Body $responseBody
        $failures.Add("$Method $Route expected $expectedDescription; received $actualStatus ($failureReason); body: $safePreview")
    }
    catch {
        Write-Host " FAIL (request error)" -ForegroundColor Red
        $failures.Add("$Method $Route expected $Expected; request failed: $($_.Exception.Message)")
    }
    finally {
        $request.Dispose()
        if ($null -ne $response) {
            $response.Dispose()
        }
    }
}

try {
    $profileBody = '{"summary":"Local auth test","skills":"Testing","experience":"Verification"}'
    $jobBody = '{"company":"Example Company","role":"Developer","jobDescription":"Build and test software."}'

    Invoke-BoundaryCheck -Method "GET" -Route "/health" -Expected "Success"
    Invoke-BoundaryCheck -Method "GET" -Route "/api/profile" -Expected "Unauthorized"
    Invoke-BoundaryCheck -Method "POST" -Route "/api/profile" -Expected "Unauthorized" -Body $profileBody
    Invoke-BoundaryCheck -Method "GET" -Route "/api/jobs" -Expected "Unauthorized"
    Invoke-BoundaryCheck -Method "GET" -Route "/api/jobs/1" -Expected "Unauthorized"
    Invoke-BoundaryCheck -Method "POST" -Route "/api/jobs" -Expected "Unauthorized" -Body $jobBody
    Invoke-BoundaryCheck -Method "PUT" -Route "/api/jobs/1" -Expected "Unauthorized" -Body $jobBody
    Invoke-BoundaryCheck -Method "PATCH" -Route "/api/jobs/1/status" -Expected "Unauthorized" -Body '{"status":"Applied"}'
    Invoke-BoundaryCheck -Method "POST" -Route "/api/jobs/1/analyse" -Expected "Unauthorized"
    Invoke-BoundaryCheck -Method "DELETE" -Route "/api/jobs/1" -Expected "Unauthorized"
}
finally {
    $httpClient.Dispose()
}

if ($failures.Count -gt 0) {
    Write-Error ("Local authentication boundary verification failed:`n- " + ($failures -join "`n- ")) -ErrorAction Continue
    exit 1
}

Write-Host "Local authentication boundary verification passed." -ForegroundColor Green
exit 0
