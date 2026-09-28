param([string]$File = '', [switch]$Cancel)
# Opens Accept optimised airflow..., reports the dialog's folder and file name, then either cancels or opens $File.
. (Join-Path $PSScriptRoot 'lib.ps1')
$w = Top 'Part O*Mixed Design*'; Press $w 'button_AcceptOptimised'
$dlg = $null; $dl = (Get-Date).AddSeconds(30); do { Start-Sleep -Milliseconds 500; $dlg = Windows | Where-Object { $_.Class -eq '#32770' -and $_.Title -like 'Accept optimised airflow*' } | Select-Object -First 1 } while (-not $dlg -and (Get-Date) -lt $dl)
if (-not $dlg) { Say 'FAIL file dialog did not open'; exit 1 }
Start-Sleep -Milliseconds 800; Shot $dlg 'dialog-open'
$edit = @(Get-Children $dlg.Handle | Where-Object { $_.Class -eq 'Edit' }) | Select-Object -First 1
$t = New-Object System.Text.StringBuilder 1024; [void][W.N]::SendMessage($edit.Handle, 0x000D, [System.IntPtr]1024, $null)
$sb = New-Object System.Text.StringBuilder 1024; [void][W.N]::GetWindowText($edit.Handle, $sb, 1024)
$bread = @(Get-Children $dlg.Handle | Where-Object { $_.Class -eq 'ToolbarWindow32' } | ForEach-Object { $x = New-Object System.Text.StringBuilder 512; [void][W.N]::GetWindowText($_.Handle, $x, 512); $x.ToString() } | Where-Object { $_ -like 'Address*' })
Say ("  file name box: '{0}'  address: '{1}'" -f $sb.ToString(), ($bread -join ' | '))
if ($Cancel) { $c = @(Get-Children $dlg.Handle | Where-Object { $_.Class -eq 'Button' -and [W.N]::GetDlgCtrlID($_.Handle) -eq 2 }) | Select-Object -First 1; [void][W.N]::SendMessage($c.Handle, 0x00F5, [System.IntPtr]::Zero, $null); Say '  cancelled'; exit 0 }
[void][W.N]::SendMessage($edit.Handle, 0x000C, [System.IntPtr]::Zero, $File); Start-Sleep -Milliseconds 400
$open = @(Get-Children $dlg.Handle | Where-Object { $_.Class -eq 'Button' -and [W.N]::GetDlgCtrlID($_.Handle) -eq 1 }) | Select-Object -First 1
[void][W.N]::SendMessage($open.Handle, 0x00F5, [System.IntPtr]::Zero, $null)
$box = $null; $dl = (Get-Date).AddSeconds(60); do { Start-Sleep -Milliseconds 500; $box = Windows | Where-Object { $_.Class -eq '#32770' -and $_.Title -like 'Part O*Accept optimised airflow' } | Select-Object -First 1 } while (-not $box -and (Get-Date) -lt $dl)
Start-Sleep -Milliseconds 500; Shot $box 'dialog-answer'; Say ("  BOX: " + (BoxText $box))
$btns = @(Get-Children $box.Handle | Where-Object { $_.Class -eq 'Button' }); $b = $btns | Where-Object { @(2, 7) -contains [W.N]::GetDlgCtrlID($_.Handle) } | Select-Object -First 1; if (-not $b) { $b = $btns | Select-Object -First 1 }
[void][W.N]::SendMessage($b.Handle, 0x00F5, [System.IntPtr]::Zero, $null); Say ("  answered button id {0}" -f [W.N]::GetDlgCtrlID($b.Handle))
