function Assert-DtmApiCurrentProductQaSelection {
    param([Parameter(Mandatory=$true)][System.Collections.IDictionary]$Parameters)
    # Historical CLI names also carry the generic Core-only no-demand measurement.
    # It has no product observer, Pilot, product source, or native fishing action.
    $coreOnlyNoDemand=$Parameters['AutoFishingPerformanceProfile'] -ceq 'InactiveNoConsumer' -and
        [int]$Parameters['AutoFishingPerformanceWarmupFrames'] -gt 0 -and [int]$Parameters['AutoFishingPerformanceTargetFrames'] -gt 0
    $noDemandParameters=@('AutoFishingPerformance','AutoFishingPerformanceProfile','AutoFishingPerformanceWarmupFrames','AutoFishingPerformanceTargetFrames')
    foreach ($row in $Parameters.GetEnumerator()) {
        if($coreOnlyNoDemand -and $row.Key -in $noDemandParameters){continue}
        if ($row.Key -eq 'AssertProductOwnerRefresh' -and $row.Value) {
            throw 'Retired product QA selection: the owner-refresh fixture depends on the ActionSpeed 1.x restore observer. Current candidates use product NativeProbe lifecycle receipts.'
        }
        if ($row.Key -match '^(AutoExerciseActionSpeed|AutoExerciseAutoFishing|AutoFishing|Batch6AutoFishing)' -and
            $row.Value -and [string]$row.Value -ne 'None') {
            throw "Retired product QA selection '$($row.Key)': AutoFishing 1.1+ and ActionSpeed 2.0+ use tests/DTMAPI.<Product>.Tests/NativeProbe and build-<product>-short-probe.ps1. Historical Pilot/GC evidence cannot accept this candidate."
        }
        if ($row.Key -eq 'Batch5GcLadderDomain' -and [string]$row.Value -ne 'None') {
            throw 'Retired product QA selection: Batch 5 GC ladder belongs to the pre-rebuild product commits; use the current product NativeProbe.'
        }
        if ($row.Key -eq 'AdvancedProductOwnerDeactivationOwnerIds' -and @($row.Value) -contains 'Yuuka.DTMAPI.ActionSpeed') {
            throw 'Retired product QA selection: ActionSpeed 1.x owner observer cannot accept ActionSpeed 2.x; use its NativeProbe.'
        }
    }
}
