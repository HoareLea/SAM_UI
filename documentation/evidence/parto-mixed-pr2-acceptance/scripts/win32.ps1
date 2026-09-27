Add-Type -Namespace W -Name N -MemberDefinition @'
public delegate bool EnumProc(System.IntPtr h, System.IntPtr l);
[System.Runtime.InteropServices.DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc cb, System.IntPtr l);
[System.Runtime.InteropServices.DllImport("user32.dll")] public static extern bool EnumChildWindows(System.IntPtr parent, EnumProc cb, System.IntPtr l);
[System.Runtime.InteropServices.DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(System.IntPtr h, out uint pid);
[System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)] public static extern int GetClassName(System.IntPtr h, System.Text.StringBuilder s, int n);
[System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)] public static extern int GetWindowText(System.IntPtr h, System.Text.StringBuilder s, int n);
[System.Runtime.InteropServices.DllImport("user32.dll")] public static extern bool IsWindowVisible(System.IntPtr h);
[System.Runtime.InteropServices.DllImport("user32.dll")] public static extern int GetDlgCtrlID(System.IntPtr h);
[System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)] public static extern System.IntPtr SendMessage(System.IntPtr h, uint m, System.IntPtr w, string l);
[System.Runtime.InteropServices.DllImport("user32.dll")] public static extern bool PostMessage(System.IntPtr h, uint m, System.IntPtr w, System.IntPtr l);
'@

function Get-TopWindows([int]$ProcessId) {
    $list = New-Object System.Collections.Generic.List[object]
    $cb = [W.N+EnumProc]{ param($h, $l)
        [uint32]$p = 0; [void][W.N]::GetWindowThreadProcessId($h, [ref]$p)
        if ($p -eq $ProcessId -and [W.N]::IsWindowVisible($h)) {
            $c = New-Object System.Text.StringBuilder 256; [void][W.N]::GetClassName($h, $c, 256)
            $t = New-Object System.Text.StringBuilder 512; [void][W.N]::GetWindowText($h, $t, 512)
            $list.Add([pscustomobject]@{ Handle = $h; Class = $c.ToString(); Title = $t.ToString() })
        }
        return $true }
    [void][W.N]::EnumWindows($cb, [System.IntPtr]::Zero)
    return $list
}

function Get-Children([System.IntPtr]$Parent) {
    $list = New-Object System.Collections.Generic.List[object]
    $cb = [W.N+EnumProc]{ param($h, $l)
        $c = New-Object System.Text.StringBuilder 256; [void][W.N]::GetClassName($h, $c, 256)
        $list.Add([pscustomobject]@{ Handle = $h; Class = $c.ToString(); Id = [W.N]::GetDlgCtrlID($h) })
        return $true }
    [void][W.N]::EnumChildWindows($Parent, $cb, [System.IntPtr]::Zero)
    return $list
}
