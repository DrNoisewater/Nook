using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using System.Text;

namespace Nook {
internal static class Native {
    [DllImport("user32.dll")] public static extern short GetAsyncKeyState(int vKey);
    [DllImport("user32.dll")] public static extern bool RegisterHotKey(IntPtr h,int id,uint modifiers,uint key);
    [DllImport("user32.dll")] public static extern bool UnregisterHotKey(IntPtr h,int id);
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern int GetClassName(IntPtr h,StringBuilder name,int size);
    public static bool IsDesktop(IntPtr h) { var name=new StringBuilder(256);GetClassName(h,name,256);return name.ToString()=="Progman"||name.ToString()=="WorkerW"||name.ToString()=="Shell_TrayWnd"; }
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h,out RECT rect);
    [DllImport("user32.dll")] public static extern bool GetCursorPos(out POINT point);
    [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr h,IntPtr after,int x,int y,int width,int height,uint flags);
    [DllImport("user32.dll")] static extern uint GetDpiForWindow(IntPtr h);
    [DllImport("user32.dll")] static extern bool DestroyIcon(IntPtr h);
    [DllImport("shell32.dll",CharSet=CharSet.Unicode)] static extern IntPtr SHGetFileInfo(string path,uint attributes,out SHFILEINFO info,uint size,uint flags);
    [StructLayout(LayoutKind.Sequential)] public struct POINT { public int X,Y; }
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left,Top,Right,Bottom; }
    [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)] struct SHFILEINFO {
        public IntPtr hIcon; public int iIcon; public uint dwAttributes;
        [MarshalAs(UnmanagedType.ByValTStr,SizeConst=260)] public string display;
        [MarshalAs(UnmanagedType.ByValTStr,SizeConst=80)] public string type;
    }
    public static double Scale(IntPtr h) { try { return GetDpiForWindow(h)/96.0; } catch(EntryPointNotFoundException){ return 1; } }
    public static bool Control { get { return (GetAsyncKeyState(0x11)&0x8000)!=0; } }
    public static bool Inside(Window w,int margin) {
        POINT p; RECT r; GetCursorPos(out p); GetWindowRect(new WindowInteropHelper(w).Handle,out r);
        return p.X>=r.Left-margin && p.X<=r.Right+margin && p.Y>=r.Top-margin && p.Y<=r.Bottom+margin;
    }
    public static ImageSource FileIcon(Shortcut s) {
        try {
            if(!String.IsNullOrWhiteSpace(s.Icon)) {
                string path=Launcher.Expand(s.Icon);
                if(!Path.IsPathRooted(path)||path.StartsWith(@"\\"))return null;
                BitmapImage b=new BitmapImage(); b.BeginInit(); b.CacheOption=BitmapCacheOption.OnLoad; b.UriSource=new Uri(path); b.DecodePixelWidth=48; b.EndInit(); b.Freeze(); return b;
            }
            string t=Launcher.Expand(s.Target); if(!Path.IsPathRooted(t)) return null;
            // Use extension icons, never probe network shares while drawing the panel.
            uint attributes=0x80; string lookup=Path.GetExtension(t);
            if(String.IsNullOrEmpty(lookup)) { lookup="folder"; attributes=0x10; }
            SHFILEINFO info;
            uint flags=0x100|0x1|0x10;
            if(!t.StartsWith(@"\\") && String.Equals(Path.GetExtension(t),".exe",StringComparison.OrdinalIgnoreCase) && File.Exists(t)) { lookup=t;flags=0x100|0x1; }
            SHGetFileInfo(lookup,attributes,out info,(uint)Marshal.SizeOf(typeof(SHFILEINFO)),flags);
            if(info.hIcon==IntPtr.Zero)return null;
            try { var img=Imaging.CreateBitmapSourceFromHIcon(info.hIcon,Int32Rect.Empty,BitmapSizeOptions.FromEmptyOptions()); img.Freeze(); return img; }
            finally { DestroyIcon(info.hIcon); }
        } catch { return null; }
    }
}
}
