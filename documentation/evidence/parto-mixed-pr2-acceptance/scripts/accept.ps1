param([string]$File, [switch]$No)
# Presses Accept optimised airflowâ€¦, fills the file dialog, captures the confirmation, answers Yes (or No).
. (Join-Path $PSScriptRoot 'lib.ps1')
$w = Top 'Part O*Mixed Design*'; Press $w 'button_AcceptOptimised'
$dlg = $null; $dl = (Get-Date).AddSeconds(30); do { Start-Sleep -Milliseconds 500; $dlg = Windows | Where-Object { $_.Class -eq '#32770' -and $_.Title -like 'Accept optimised airflow*' } | Select-Object -First 1 } while (-not $dlg -and (Get-Date) -lt $dl)
if (-not $dlg) { Say 'FAIL file dialog did not open'; Windows | % { Say ("  window '{0}' {1}" -f $_.Title, $_.Class) }; exit 1 }
Say ("  file dialog '{0}'" -f $dlg.Title); Shot $dlg 'accept-filedialog'
$edit = @(Get-Children $dlg.Handle | Where-Object { $_.Class -eq 'Edit' }) | Select-Object -First 1
if (-not $edit) { Say 'FAIL no file-name edit'; exit 1 }
[void][W.N]::SendMessage($edit.Handle, 0x000C, [System.IntPtr]::Zero, $File); Say ("  file name set: $File")
Start-Sleep -Milliseconds 400
$open = @(Get-Children $dlg.Handle | Where-Object { $_.Class -eq 'Button' -and [W.N]::GetDlgCtrlID($_.Handle) -eq 1 }) | Select-Object -First 1
[void][W.N]::SendMessage($open.Handle, 0x00F5, [System.IntPtr]::Zero, $null); Say '  open clicked'
$box = $null; $dl = (Get-Date).AddSeconds(60); do { Start-Sleep -Milliseconds 500; $box = Windows | Where-Object { $_.Class -eq '#32770' -and $_.Title -like 'Part O*Accept optimised airflow' } | Select-Object -First 1 } while (-not $box -and (Get-Date) -lt $dl)
if (-not $box) { Say 'FAIL no confirmation'; exit 1 }
Start-Sleep -Milliseconds 500; Shot $box 'accept-confirm'; Say ("  CONFIRM: " + (BoxText $box))
$be = $AE::FromHandle($box.Handle); $btn = Find-ById $be $(if ($No) { '7' } else { '6' }); Say ("  answer {0}: {1}" -f $(if ($No) { 'No' } else { 'Yes' }), (Invoke-El $btn)); Start-Sleep -Seconds 2
