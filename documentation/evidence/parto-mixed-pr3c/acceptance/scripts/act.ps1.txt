param([string[]]$Rows = @(), [string]$Press = '', [string]$PressDetached = '', [string]$Combo = '', [string]$ComboItem = '', [string]$Shot = '', [string]$Window = 'Part O*Mixed Design*', [switch]$State)
. (Join-Path $PSScriptRoot 'lib.ps1')
$w = Top $Window; if (-not $w) { Say "FAIL window '$Window' not found"; exit 1 }
$root = $AE::FromHandle($w.Handle)
function GridRows() { $g = Find-ById $root 'dataGrid_Dwellings'; if (-not $g) { return @() }; @($g.FindAll($TS::Children, (New-Object System.Windows.Automation.PropertyCondition($AE::ControlTypeProperty, $CT::DataItem)))) }
function RowText($r) { (@($r.FindAll($TS::Descendants, (New-Object System.Windows.Automation.PropertyCondition($AE::ControlTypeProperty, $CT::Text))) | ForEach-Object { $_.Current.Name }) -join ' | ') }
if ($Rows.Count -gt 0) {
    $first = $true
    foreach ($r in (GridRows)) {
        $t = RowText $r; $name = ($t -split ' \| ')[0]
        if ($Rows -contains $name) { $p = $null; if ($r.TryGetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern, [ref]$p)) { if ($first) { $p.Select(); $first = $false } else { $p.AddToSelection() } } }
    }
    Start-Sleep -Milliseconds 500; Say ("  selected rows: {0} -> '{1}'" -f ($Rows -join ','), (Txt $root 'textBlock_SelectionCount'))
}
if ($Combo) { Say ("  combo {0}: {1}" -f $Combo, (Select-ComboItem (Find-ById $root $Combo) $ComboItem)); Start-Sleep -Milliseconds 500 }
if ($Press) { $e = Find-ById $root $Press; Say ("  invoke {0} enabled={1}: {2}" -f $Press, $e.Current.IsEnabled, (Invoke-El $e)); Start-Sleep -Seconds 1 }
if ($PressDetached) { Press $w $PressDetached; Start-Sleep -Seconds 2 }
if ($State -or $Press -or $Rows.Count -gt 0) {
    $w = Top $Window
    if ($w) {
        $root = $AE::FromHandle($w.Handle)
        foreach ($id in 'textBlock_Baseline','textBlock_Outcome','textBlock_Readiness','textBlock_Final','textBlock_Screening','textBlock_Refusals','textBlock_BulkMessage','textBlock_Next') { Say ("    {0}: '{1}'" -f $id, (Txt $root $id)) }
        foreach ($id in 'button_Save','button_Build','button_ReviewFinal','button_ApplySuggestions','button_SetRetained') { Say ("    {0}" -f (En $root $id)) }
        foreach ($r in (GridRows)) { Say ("    ROW {0}" -f (RowText $r)) }
    }
}
foreach ($x in (Windows)) { if ($x.Class -eq '#32770') { Say ("  MESSAGE BOX '{0}': {1}" -f $x.Title, (BoxText $x)) } elseif ($x.Title -notlike 'SAM Analytical*' -and $x.Title -notlike 'Part O*Mixed Design*') { Say ("  other window '{0}'" -f $x.Title) } }
if ($Shot) { $w = Top $Window; if ($w) { Shot $w $Shot } }
