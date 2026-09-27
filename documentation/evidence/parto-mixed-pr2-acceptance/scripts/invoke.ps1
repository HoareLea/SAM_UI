param([int]$ProcessId, [string]$WindowLike, [string]$Id, [string]$Name, [long]$Handle = 0)
# Invokes one control, detached from the caller, because invoking a control that opens a modal dialog
# can block for as long as that dialog is up. -Handle addresses the window directly, so the heavy main
# window's tree is never walked.
. (Join-Path $PSScriptRoot 'uia.ps1')
$log = Join-Path $PSScriptRoot 'invoke.log'
$w = if ($Handle -ne 0) { $AE::FromHandle([System.IntPtr]$Handle) } else { Find-Window $ProcessId $WindowLike }
if ($null -eq $w) { "window '$WindowLike' not found" | Out-File $log -Append; exit 1 }
$el = if ($Id) { Find-ById $w $Id } else { Find-ByName $w $Name }
if ($null -eq $el) { "element '$Id$Name' not found in '$($w.Current.Name)'" | Out-File $log -Append; exit 1 }
$r = Invoke-El $el
"{0:HH:mm:ss} {1}/{2}{3}: {4}" -f (Get-Date), $w.Current.Name, $Id, $Name, $r | Out-File $log -Append
