param([string]$Out = 'C:\TasOut\parto-pr4-smoke-2026-09-30', [ValidateSet('result', 'design')][string]$Part = 'result')
# SAM_UI PR-4 native smoke (30 Sep 2026), no TAS. The real SAM Analytical.exe opens (a) a saved Part O run model,
# or (b) a design model, and the Part O Hub and Mixed Design are read: whether Run is blocked as "a Part O result",
# and whether Review Results is still offered. Nothing is run, pressed beyond Open/Close, or saved.
$sp = $PSScriptRoot
. (Join-Path $sp 'lib.ps1')
function Abort([string]$m) { Say "ABORT $m"; Stop-Process -Id $script:ProcessId -Force -ErrorAction SilentlyContinue; exit 1 }

$exe = 'C:\Users\michal.dengusiak\Documents\GitHub\SAM-BIM\SAM_UI\build\SAM Analytical.exe'
$model = if ($Part -eq 'result') { 'C:\TasOut\parto-output-folders-accept-2026-09-30\Iteration2\Iteration1a\tas\000000_SAM_AnalyticalModel-It1a-futureZ1.sam' } else { Join-Path $Out 'model\Design.sam' }
$hash_Before = (Get-FileHash $model -Algorithm SHA256).Hash
Say ("exe {0} (WPF dll {1:HH:mm:ss}); model {2}; SHA-256 {3}" -f $exe, (Get-Item 'C:\Users\michal.dengusiak\Documents\GitHub\SAM-BIM\SAM_UI\build\SAM.Analytical.UI.WPF.dll').LastWriteTime, $model, $hash_Before)
Say ("TAS processes before: " + ((@(Get-Process | Where-Object { $_.ProcessName -match '^(TBD|TPD|TSD|Tas(?!k))' } | ForEach-Object { $_.ProcessName }) -join ', ') -replace '^$', 'none'))

$proc = Start-Process -FilePath $exe -ArgumentList "/Path=$model" -PassThru
$script:ProcessId = $proc.Id
$main = $null; $dl = (Get-Date).AddSeconds(180)
do { Start-Sleep -Seconds 2; $main = Windows | Where-Object { $_.Title -like 'SAM Analytical*' } | Select-Object -First 1 } while (-not $main -and (Get-Date) -lt $dl)
if (-not $main) { Abort 'main window did not open' }
$mainEl = $AE::FromHandle($main.Handle)
$tab = $null; $dl = (Get-Date).AddSeconds(120); do { Start-Sleep -Seconds 2; $tab = Find-ById $mainEl 'RibbonTab_Simulate' } while (-not $tab -and (Get-Date) -lt $dl)
Start-Sleep -Seconds 4
[void](Invoke-El $tab); Start-Sleep -Seconds 1

# ---- The Hub -------------------------------------------------------------------------------------------------
Press $main 'RibbonButton_PartOWorkflow'
$hub = Hub; if (-not $hub) { Abort 'hub did not open' }
Start-Sleep -Seconds 3
HubSummary $hub "opened on the $Part model"
Shot $hub "$Part-hub"
Press $hub 'button_Close'; [void](Gone $hub)

# ---- Mixed Design ------------------------------------------------------------------------------------------
Start-Sleep -Seconds 2
Press $main 'RibbonButton_PartOMixedDesign'
$mixed = Await @('Part O*Mixed Design*') 120 'mixed'
if (-not $mixed) { Abort 'Mixed Design did not open' }
Start-Sleep -Seconds 3
$m = $AE::FromHandle($mixed.Handle)
Say ("--- MIXED DESIGN on the $Part model")
Say ("  baseline: " + (Txt $m 'textBlock_Baseline'))
Say ("  readiness: " + (Txt $m 'textBlock_Readiness'))
Shot $mixed "$Part-mixed"
Press $mixed 'button_Close'; [void](Gone $mixed)

Start-Sleep -Seconds 2
Stop-Process -Id $script:ProcessId -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2
$hash_After = (Get-FileHash $model -Algorithm SHA256).Hash
Say ("model SHA-256 after: {0} ({1})" -f $hash_After, $(if ($hash_After -eq $hash_Before) { 'unchanged' } else { 'CHANGED' }))
Say ("TAS processes after: " + ((@(Get-Process | Where-Object { $_.ProcessName -match '^(TBD|TPD|TSD|Tas(?!k))' } | ForEach-Object { $_.ProcessName }) -join ', ') -replace '^$', 'none'))
Say 'DONE'
