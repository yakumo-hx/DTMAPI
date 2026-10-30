function Get-DtmApiHookProbeEvidence {
    param([Parameter(Mandatory=$true)][string[]]$Lines, [switch]$Saved, [switch]$Reloaded)
    # This probe calls the public APIs. It does not operate config controls.
    $required = [ordered]@{
        Entry='HookProbe Entry OK'; GameLaunched='HookProbe GameLaunched OK'
        Input='HookProbe InputRegistrations OK keys=F6,F9,F10,F11'
        Update='HookProbe UpdateTicked OK'; OneSecond='HookProbe OneSecondUpdateTicked OK'
        SaveLoaded='HookProbe SaveLoaded OK'
    }
    foreach($name in @('Status','Mods','Config','Errors','Hooks','ExportLogs')) { $required['Ui'+$name]='HookProbe UI '+$name+' OK' }
    foreach($id in @('Yuuka.DTMAPI.ActionSpeed','Yuuka.DTMAPI.AutoFishing','Yuuka.DTMAPI.OneActionComplete')) {
        $required['ConfigPublicApi:'+ $id]='HookProbe ConfigMenu PublicApi OK '+$id+' conflicts='
    }
    if($Saved){$required['SaveSaving']='HookProbe SaveSaving OK'; $required['SaveSaved']='HookProbe SaveSaved OK'}
    if($Reloaded){$required['Reloaded']='HookProbe WorkshopModListChanged OK'}
    $result=[ordered]@{}
    foreach($row in $required.GetEnumerator()) { $result[$row.Key]=[bool](@($Lines | Where-Object { $_.Contains($row.Value) }).Count) }
    return $result
}
