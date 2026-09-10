$ErrorActionPreference = 'Stop'
. "$PSScriptRoot/common.ps1"
$repo=Get-RepoRoot
$root=Join-Path $repo ('temp/audit-output-' + [Guid]::NewGuid().ToString('N'))
$checks=0
function Assert-AuditTest { param([bool]$Condition,[string]$Message); if(-not $Condition){throw $Message}; $script:checks++ }
try {
    $script:resolvedSourceRoot=Join-Path $root 'source'
    $script:resolvedOutputRoot=Join-Path $root 'output'
    $script:LegacyEvidenceSet=$false
    New-Item -ItemType Directory -Force -Path $script:resolvedSourceRoot,$script:resolvedOutputRoot | Out-Null
    $tokens=$null;$errors=$null
    $ast=[Management.Automation.Language.Parser]::ParseFile((Join-Path $PSScriptRoot 'update-audit-package.ps1'),[ref]$tokens,[ref]$errors)
    if($errors.Count){throw $errors[0]}
    foreach($name in @('Assert-AuditOutputPath','New-CleanDirectory','Replace-PackageDirectory','Copy-ReportPayloads','Get-EvidenceItems')){
        $node=$ast.Find({param($n) $n -is [Management.Automation.Language.FunctionDefinitionAst] -and $n.Name -eq $name},$true)
        . ([scriptblock]::Create($node.Extent.Text))
    }
    $sourceFile=Join-Path $script:resolvedSourceRoot 'unique.txt'
    [IO.File]::WriteAllText($sourceFile,'unique source')
    $rejected=$false;try{New-CleanDirectory -Path $script:resolvedSourceRoot}catch{$rejected=$true}
    Assert-AuditTest ($rejected -and [IO.File]::ReadAllText($sourceFile) -ceq 'unique source') 'Outside/source path was touched.'
    $staging=Join-Path $script:resolvedOutputRoot 'package.new'
    New-CleanDirectory $staging
    [IO.File]::WriteAllText((Join-Path $staging 'unique.txt'),'interrupted stage')
    $rejected=$false;try{New-CleanDirectory $staging}catch{$rejected=$true}
    Assert-AuditTest ($rejected -and [IO.File]::ReadAllText((Join-Path $staging 'unique.txt')) -ceq 'interrupted stage') 'Interrupted/unknown staging was deleted.'
    $final=Join-Path $script:resolvedOutputRoot 'package'
    New-Item -ItemType Directory -Path $final | Out-Null
    [IO.File]::WriteAllText((Join-Path $final 'old.txt'),'old delivery')
    $missing=Join-Path $script:resolvedOutputRoot 'missing-stage'
    $rejected=$false;try{Replace-PackageDirectory -TemporaryPath $missing -FinalPath $final}catch{$rejected=$true}
    Assert-AuditTest ($rejected -and (Test-Path -LiteralPath (Join-Path $final 'old.txt')) -and -not (Test-Path -LiteralPath ($final+'.bak'))) 'Failed replacement did not restore the previous delivery.'
    Replace-PackageDirectory -TemporaryPath $staging -FinalPath $final
    Assert-AuditTest ((Test-Path -LiteralPath (Join-Path $final 'unique.txt')) -and (Test-Path -LiteralPath (Join-Path ($final+'.bak') 'old.txt'))) 'Replacement lost previous unique evidence.'
    $junction=Join-Path $script:resolvedOutputRoot 'redirect'
    New-Item -ItemType Junction -Path $junction -Target $script:resolvedSourceRoot | Out-Null
    try {
        $rejected=$false;try{Assert-AuditOutputPath (Join-Path $junction 'child')}catch{$rejected=$true}
        Assert-AuditTest $rejected 'Reparse output was accepted.'
    } finally {
        $junctionItem = Get-Item -LiteralPath $junction -Force
        $expectedJunction = [IO.Path]::GetFullPath((Join-Path $script:resolvedOutputRoot 'redirect'))
        $targetPaths = @($junctionItem.Target)
        if ($junctionItem.FullName -ine $expectedJunction -or
            ($junctionItem.Attributes -band [IO.FileAttributes]::ReparsePoint) -eq 0 -or
            $targetPaths.Count -ne 1 -or
            [IO.Path]::GetFullPath([string]$targetPaths[0]) -ine [IO.Path]::GetFullPath($script:resolvedSourceRoot)) {
            throw 'Refusing to remove an unrecognized audit fixture junction.'
        }
        # Remove only this verified link. WinPS 5.1 Remove-Item can fail for a
        # nonempty junction; nonrecursive Delete never removes its target tree.
        [IO.Directory]::Delete($junctionItem.FullName, $false)
    }
    Assert-AuditTest ((@(Get-EvidenceItems -Ids @())).Count -eq 0) 'Ordinary source audit pulled in the historical smoke set.'
    $rejected=$false;try{Get-EvidenceItems -Ids @('../outside')}catch{$rejected=$true}
    Assert-AuditTest $rejected 'Escaping evidence id was accepted.'
    foreach($dir in @('one','two')){New-Item -ItemType Directory -Path (Join-Path $root $dir)|Out-Null;[IO.File]::WriteAllText((Join-Path $root "$dir/report.zip"),$dir)}
    $rejected=$false;try{Copy-ReportPayloads -Root $script:resolvedSourceRoot -PackageRoot (Join-Path $root 'reports') -Items @() -ExplicitReportZipPaths @((Join-Path $root 'one/report.zip'),(Join-Path $root 'two/report.zip')) -PreviousReportPath ''}catch{$rejected=$true}
    Assert-AuditTest $rejected 'Distinct evidence ZIPs with the same name overwrote one another.'
    Assert-AuditTest ([IO.File]::ReadAllText($sourceFile) -ceq 'unique source') 'Source changed during output tests.'
    Write-Host "Audit output boundaries: PASS ($checks checks)."
}
finally {
    $resolved=[IO.Path]::GetFullPath($root);$prefix=[IO.Path]::GetFullPath((Join-Path $repo 'temp'))+[IO.Path]::DirectorySeparatorChar
    if(-not $resolved.StartsWith($prefix,[StringComparison]::OrdinalIgnoreCase)){throw 'Refusing audit fixture cleanup outside temp.'}
    if(Test-Path -LiteralPath $resolved){
        if(@(Get-Item -LiteralPath $resolved -Force; Get-ChildItem -LiteralPath $resolved -Recurse -Force) | Where-Object{$_.Attributes -band [IO.FileAttributes]::ReparsePoint}){throw 'Refusing audit fixture cleanup through a reparse point.'}
        Remove-Item -LiteralPath $resolved -Recurse -Force
    }
}
