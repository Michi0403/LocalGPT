param(
    [string]$Subject = "CN=micha",
    [string]$FriendlyName = "LocalGPT Local Dev Package Signing",
    [string]$PfxFileName = "LocalGPTWebviewWrapper.LocalDevKey.pfx",
    [switch]$ExportPfx,
    [switch]$RemoveOldPushedCertificate
)

$ErrorActionPreference = "Stop"

$packageProjectDir = Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..\LocalGPTWebviewWrapper (Package)")
$localPropsPath = Join-Path $packageProjectDir "LocalGPTWebviewWrapper (Package).local.props"
$pfxPath = Join-Path $packageProjectDir $PfxFileName
$oldPushedThumbprint = "5D490988D95615FDAA531D4B956D272B7479D407"

if ($RemoveOldPushedCertificate) {
    $oldCert = Get-ChildItem Cert:\CurrentUser\My -ErrorAction SilentlyContinue |
        Where-Object { $_.Thumbprint -eq $oldPushedThumbprint }

    foreach ($cert in $oldCert) {
        Remove-Item -LiteralPath $cert.PSPath -Force
        Write-Host "Removed old pushed certificate from CurrentUser\My: $($cert.Thumbprint)"
    }
}

$cert = New-SelfSignedCertificate `
    -Type Custom `
    -Subject $Subject `
    -FriendlyName $FriendlyName `
    -KeyAlgorithm RSA `
    -KeyLength 2048 `
    -KeyExportPolicy Exportable `
    -KeyUsage DigitalSignature `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3")

if ($ExportPfx) {
    $password = Read-Host "Enter a password for the local PFX" -AsSecureString
    Export-PfxCertificate -Cert $cert -FilePath $pfxPath -Password $password | Out-Null
}

$props = @"
<?xml version="1.0" encoding="utf-8"?>
<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <PropertyGroup>
    <AppxPackageSigningEnabled>True</AppxPackageSigningEnabled>
    <PackageCertificateThumbprint>$($cert.Thumbprint)</PackageCertificateThumbprint>
  </PropertyGroup>
</Project>
"@

Set-Content -LiteralPath $localPropsPath -Value $props -Encoding UTF8

Write-Host ""
Write-Host "Created local package certificate:"
Write-Host "  Thumbprint: $($cert.Thumbprint)"
Write-Host "  MSBuild:    $localPropsPath"
if ($ExportPfx) {
    Write-Host "  PFX:        $pfxPath"
}
Write-Host ""
Write-Host "The local props file and optional PFX are ignored by git. Do not commit them."
