# Quick Fix for JWT Audience Validation Issues
# Run this script to temporarily disable audience validation during development

Write-Host "🔧 Quick Fix for JWT Audience Validation Issues" -ForegroundColor Green
Write-Host ""

# Check if appsettings.Development.json exists
$devSettingsPath = "appsettings.Development.json"

if (Test-Path $devSettingsPath) {
    Write-Host "✅ Found appsettings.Development.json" -ForegroundColor Green
    
    # Read current content
    $content = Get-Content $devSettingsPath -Raw | ConvertFrom-Json
    
    # Add or update JWT settings
    if (-not $content.Jwt) {
        $content | Add-Member -NotePropertyName "Jwt" -NotePropertyValue @{}
    }
    
    $content.Jwt | Add-Member -NotePropertyName "ValidateAudience" -NotePropertyValue $false -Force
    
    # Write back to file
    $content | ConvertTo-Json -Depth 10 | Set-Content $devSettingsPath
    
    Write-Host "✅ Updated appsettings.Development.json to disable audience validation" -ForegroundColor Green
} else {
    Write-Host "⚠️  appsettings.Development.json not found, creating new one..." -ForegroundColor Yellow
    
    $newContent = @{
        "Logging" = @{
            "LogLevel" = @{
                "Default" = "Information"
                "Microsoft.AspNetCore" = "Warning"
                "Microsoft.AspNetCore.Authentication" = "Debug"
            }
        }
        "Jwt" = @{
            "ValidateAudience" = $false
        }
    }
    
    $newContent | ConvertTo-Json -Depth 10 | Set-Content $devSettingsPath
    Write-Host "✅ Created appsettings.Development.json with disabled audience validation" -ForegroundColor Green
}

Write-Host ""
Write-Host "🔄 Please restart your API server for changes to take effect" -ForegroundColor Yellow
Write-Host ""
Write-Host "📝 To re-enable audience validation later, change ValidateAudience to true" -ForegroundColor Cyan
Write-Host ""
Write-Host "🐛 Debug tips:" -ForegroundColor Cyan
Write-Host "   - Check console logs for JWT Debug messages" -ForegroundColor White
Write-Host "   - Look for 'JWT Debug: All claims in token' output" -ForegroundColor White
Write-Host "   - Verify Keycloak realm and client settings" -ForegroundColor White 