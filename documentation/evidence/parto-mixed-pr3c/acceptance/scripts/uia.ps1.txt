Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes
$AE = [System.Windows.Automation.AutomationElement]
$TS = [System.Windows.Automation.TreeScope]
$CT = [System.Windows.Automation.ControlType]

function Get-Windows([int]$ProcessId) {
    $cond = New-Object System.Windows.Automation.PropertyCondition($AE::ProcessIdProperty, $ProcessId)
    $AE::RootElement.FindAll($TS::Children, $cond)
}

function Find-Window([int]$ProcessId, [string]$Like) {
    foreach ($w in (Get-Windows $ProcessId)) {
        if ($w.Current.Name -like $Like) { return $w }
        $sub = $w.FindAll($TS::Descendants, (New-Object System.Windows.Automation.PropertyCondition($AE::ControlTypeProperty, $CT::Window)))
        foreach ($s in $sub) { if ($s.Current.Name -like $Like) { return $s } }
    }
    return $null
}

function Dump($el, [int]$depth = 0, [int]$max = 12) {
    if ($null -eq $el -or $depth -gt $max) { return }
    $c = $el.Current
    $name = $c.Name; if ($name.Length -gt 110) { $name = $name.Substring(0, 110) + '...' }
    "{0}{1} [{2}] id='{3}' en={4} '{5}'" -f ('  ' * $depth), $c.ControlType.ProgrammaticName.Replace('ControlType.', ''), $c.ClassName, $c.AutomationId, $c.IsEnabled, $name
    $walker = [System.Windows.Automation.TreeWalker]::ControlViewWalker
    $child = $walker.GetFirstChild($el)
    while ($null -ne $child) { Dump $child ($depth + 1) $max; $child = $walker.GetNextSibling($child) }
}

function Find-ById($root, [string]$Id) {
    $root.FindFirst($TS::Descendants, (New-Object System.Windows.Automation.PropertyCondition($AE::AutomationIdProperty, $Id)))
}

function Find-ByName($root, [string]$Name) {
    $root.FindFirst($TS::Descendants, (New-Object System.Windows.Automation.PropertyCondition($AE::NameProperty, $Name)))
}

function Invoke-El($el) {
    $p = $null
    if ($el.TryGetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern, [ref]$p)) { $p.Invoke(); return 'invoked' }
    if ($el.TryGetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern, [ref]$p)) { $p.Toggle(); return 'toggled' }
    if ($el.TryGetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern, [ref]$p)) { $p.Select(); return 'selected' }
    return 'no pattern'
}

function Set-Text($el, [string]$Text) {
    $p = $null
    if ($el.TryGetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern, [ref]$p)) { $p.SetValue($Text); return 'set' }
    return 'no value pattern'
}

function Select-ComboItem($combo, [string]$Like) {
    $p = $null
    if ($combo.TryGetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern, [ref]$p)) { $p.Expand(); Start-Sleep -Milliseconds 400 }
    $items = $combo.FindAll($TS::Descendants, (New-Object System.Windows.Automation.PropertyCondition($AE::ControlTypeProperty, $CT::ListItem)))
    foreach ($i in $items) {
        $text = $i.Current.Name
        if ([string]::IsNullOrEmpty($text)) { $t = $i.FindFirst($TS::Descendants, (New-Object System.Windows.Automation.PropertyCondition($AE::ControlTypeProperty, $CT::Text))); if ($t) { $text = $t.Current.Name } }
        if ($text -like $Like) {
            $s = $null
            if ($i.TryGetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern, [ref]$s)) { $s.Select() }
            if ($combo.TryGetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern, [ref]$p)) { $p.Collapse() }
            return "selected '$text'"
        }
    }
    if ($combo.TryGetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern, [ref]$p)) { $p.Collapse() }
    return "not found; items: " + (($items | ForEach-Object { $_.Current.Name }) -join ' | ')
}
