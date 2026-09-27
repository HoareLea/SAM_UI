. (Join-Path $PSScriptRoot 'lib.ps1')
$m = Main; $el = $AE::FromHandle($m.Handle)
$tab = Find-ById $el 'RibbonTab_Simulate'; [void](Invoke-El $tab); Start-Sleep -Seconds 1
$b = Find-ById $el 'RibbonButton_PartOMixedDesign'; Say ("Mixed Design button enabled={0} tooltip='{1}'" -f $b.Current.IsEnabled, $b.Current.HelpText)
Press $m 'RibbonButton_PartOMixedDesign'
$w = $null; $dl = (Get-Date).AddSeconds(60); do { Start-Sleep -Milliseconds 500; $w = Top 'Part O*Mixed Design*' } while (-not $w -and (Get-Date) -lt $dl)
if (-not $w) { Say 'FAIL Mixed Design window did not open'; Windows | ForEach-Object { Say ("  window '{0}' {1}" -f $_.Title, $_.Class) }; exit 1 }
Start-Sleep -Seconds 2; Shot $w 'mixed-open'
