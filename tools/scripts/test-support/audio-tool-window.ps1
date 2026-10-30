Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
using System.Text;
public static class AudioToolTestWindow {
    private delegate bool EnumWindow(IntPtr window, IntPtr parameter);
    [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindow callback, IntPtr parameter);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window, out uint process);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] private static extern int GetWindowText(IntPtr window, StringBuilder text, int count);
    [DllImport("user32.dll")] private static extern bool PostMessage(IntPtr window, uint message, IntPtr w, IntPtr l);
    public static IntPtr Find(int process) {
        IntPtr result=IntPtr.Zero;
        EnumWindows((window, _) => { uint owner; GetWindowThreadProcessId(window, out owner);
            if(owner != process) return true;
            var title=new StringBuilder(128); GetWindowText(window,title,128);
            if(title.ToString() != "DTMAPI 音频准备工具") return true;
            result=window; return false;
        }, IntPtr.Zero); return result;
    }
    public static void Close(IntPtr window) { if(window!=IntPtr.Zero) PostMessage(window,0x10,IntPtr.Zero,IntPtr.Zero); }
}
'@
