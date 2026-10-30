# Compatibility entry point. Importing helpers does not run game, SDK or installer work.
param([ValidateSet('all','uninstall')] [string] $DtmApiCommonAction = 'all')
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$dtmApiCommonParts = if ($DtmApiCommonAction -eq 'uninstall') { @('repository','files','game-session','installer') } else { @('repository','files','toolchain','runtime-lock','game-session','installer') }
foreach ($dtmApiCommonPart in $dtmApiCommonParts) {
    . (Join-Path $PSScriptRoot ("common/$dtmApiCommonPart.ps1"))
}
