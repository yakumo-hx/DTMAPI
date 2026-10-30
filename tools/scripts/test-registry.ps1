# One classification source for development test selection. PowerShell 5.1 compatible.
function Read-DtmApiTestRegistry {
    param([string]$RepoRoot=(Get-RepoRoot),[string]$MapPath='')
    if(-not $MapPath) { $MapPath=Join-Path $RepoRoot 'tests/DTMAPI.UnitTests/suites.json' }
    $map=Get-Content -LiteralPath $MapPath -Raw -Encoding UTF8 | ConvertFrom-Json
    if($map.schemaVersion -ne 2) { throw 'Test registry requires schemaVersion 2.' }
    $ids=@{}; $classified=@{}
    $fields=@('id','runner','entry','project','distribution','platforms','requires','arguments','purpose','enabledByDefault','default','focuses','dependencies','inputs')
    foreach($suite in $map.suites) {
        foreach($property in $suite.PSObject.Properties) { if($property.Name -notin $fields){throw "Unknown test registration field: $($property.Name)"} }
        foreach($optional in @('project','arguments','dependencies','inputs')) { if(-not $suite.PSObject.Properties[$optional]){$suite | Add-Member -NotePropertyName $optional -NotePropertyValue $null} }
        foreach($name in @('id','runner','entry','distribution','purpose')) {
            if(-not $suite.PSObject.Properties[$name] -or [string]::IsNullOrWhiteSpace([string]$suite.$name)){throw "Test registration missing $name."}
        }
        if($ids.ContainsKey([string]$suite.id)) { throw "Duplicate test registration: $($suite.id)" }
        $ids[[string]$suite.id]=$true
        if($suite.runner -notin @('unit-exe','existing-exe-focus','powershell','build-only')) { throw "Unknown test runner: $($suite.runner)" }
        if($suite.distribution -notin @('public','internal')) { throw "Unknown test distribution: $($suite.id)" }
        if(@($suite.platforms).Count -eq 0 -or @($suite.platforms | Where-Object {$_ -notin @('Windows','Linux')}).Count -gt 0) { throw "Unknown/empty platforms: $($suite.id)" }
        foreach($requirement in $suite.requires) {
            if($requirement -notin @('DotNet8','PowerShell51','BepInExFixture','AuthorCompatibility','ExactAdvancedReference','ActualLegacySdk','PrivateGame','SelfContainedSdk','InternalWorkspace')) { throw "Unknown test prerequisite: $requirement" }
        }
        foreach($path in @($suite.entry,$suite.project) | Where-Object {$_}) {
            if([IO.Path]::IsPathRooted($path) -or $path -match '(^|/)\.\.(/|$)|\\') { throw "Test registration path must be repository relative: $path" }
            $classified[[string]$path]=$true
        }
        if($suite.runner -eq 'unit-exe' -and -not $suite.PSObject.Properties['focuses']) { throw "Unit focus map missing: $($suite.id)" }
    }
    foreach($input in $map.supportingInputs) {
        if(-not $input.path -or -not $input.purpose -or $input.distribution -notin @('public','internal')) { throw 'Incomplete supporting test input classification.' }
        if($classified.ContainsKey([string]$input.path)) { throw "Duplicate supporting test input: $($input.path)" }
        $classified[[string]$input.path]=$true
    }
    $inputs=@(Get-ChildItem -LiteralPath (Join-Path $RepoRoot 'tests') -Filter '*.csproj' -File -Recurse | Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' })
    $inputs+=@(Get-ChildItem -LiteralPath (Join-Path $RepoRoot 'tools/scripts') -Filter 'test*.ps1' -File)
    foreach($input in $inputs) {
        $relative=$input.FullName.Substring($RepoRoot.Length).TrimStart('\','/').Replace('\','/')
        if(-not $classified.ContainsKey($relative)) { throw "Unclassified test input: $relative. Add its runner, distribution, platform and prerequisites to suites.json." }
    }
    return $map
}

function Get-DtmApiPublicTestSelection {
    param([Parameter(Mandatory=$true)]$Registry,[ValidateSet('PublicWindows','Generic')][string]$Profile)
    $platform=if($Profile -eq 'Generic'){'Linux'}else{'Windows'}
    $selected=@($Registry.suites | Where-Object { $_.distribution -eq 'public' -and $_.enabledByDefault -and $_.platforms -contains $platform })
    if($selected.Count -eq 0) { throw "Empty public test selection: $Profile" }
    return $selected
}
