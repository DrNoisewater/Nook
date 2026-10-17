using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Threading;
using Microsoft.Win32;

namespace Nook {
[DataContract] public sealed class Shortcut {
    [DataMember] public string Name = "";
    [DataMember] public string Target = "";
    [DataMember] public string Arguments = "";
    [DataMember] public string Icon = "";
    [DataMember] public string Group = "";
    [DataMember] public int Deck;
    public Shortcut Copy() { return (Shortcut)MemberwiseClone(); }
}
[DataContract] public sealed class Config {
    [DataMember] public int Version = 1;
    [DataMember] public List<Shortcut> Items = new List<Shortcut>();
    [DataMember] public string MainName = "Everyday";
    [DataMember] public string AltName = "Workspace";
    [DataMember] public string Monitor = "";
    [DataMember] public string AccentColor = "#F5B45F";
    [DataMember] public bool NookStyle;
    [DataMember] public bool ShowDestinations;
    [DataMember] public string PreviousAccent;
    [DataMember] public string PreviousBackground;
    [DataMember] public string BackgroundColor = "#231B16";
    [DataMember] public bool LeftEdge;
    [DataMember] public double Position = .5;
    [DataMember] public double Opacity = .95;
    [DataMember] public int HoverMs = 180;
    [DataMember] public int CloseMs = 650;
    [DataMember] public bool Animations = true;
    [DataMember] public bool CloseOnLaunch = true;
    [DataMember] public bool SuppressFullscreen = true;
    public static Config Defaults() {
        Config c = new Config();
        c.Items.Add(new Shortcut { Name="This PC", Target="shell:MyComputerFolder", Group="PLACES" });
        c.Items.Add(new Shortcut { Name="Desktop", Target=Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), Group="PLACES" });
        c.Items.Add(new Shortcut { Name="Downloads", Target=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),"Downloads"), Group="PLACES" });
        c.Items.Add(new Shortcut { Name="Documents", Target=Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), Group="PLACES" });
        c.Items.Add(new Shortcut { Name="Pictures", Target=Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), Group="PLACES" });
        c.Items.Add(new Shortcut { Name="Settings", Target="ms-settings:", Group="SYSTEM" });
        c.Items.Add(new Shortcut { Name="ChatGPT", Target="https://chatgpt.com", Deck=1, Group="TOOLS" });
        c.Items.Add(new Shortcut { Name="Claude", Target="https://claude.ai", Deck=1, Group="TOOLS" });
        return c;
    }
    public bool AdoptNook() {
        if(NookStyle)return false;
        PreviousAccent=AccentColor;PreviousBackground=BackgroundColor;
        AccentColor="#F5B45F";BackgroundColor="#231B16";NookStyle=true;return true;
    }
    public static string ColorHex(string value,string fallback) {
        if(String.IsNullOrWhiteSpace(value))return fallback;
        string hex=value.Trim().ToUpperInvariant();if(!hex.StartsWith("#"))hex="#"+hex;
        if(!System.Text.RegularExpressions.Regex.IsMatch(hex,"^#[0-9A-F]{6}$"))throw new InvalidDataException("Enter a six-digit color, such as #A8F3D0.");
        return hex;
    }
    public void Validate() {
        AccentColor=ColorHex(AccentColor,"#A8F3D0");BackgroundColor=ColorHex(BackgroundColor,"#17201E");
        if (Version != 1) throw new InvalidDataException("This configuration uses an unsupported version.");
        if (Items == null || Items.Count > 500) throw new InvalidDataException("A configuration can contain up to 500 shortcuts.");
        if (String.IsNullOrWhiteSpace(MainName) || String.IsNullOrWhiteSpace(AltName)) throw new InvalidDataException("Both lists need a name.");
        MainName=MainName.Trim(); AltName=AltName.Trim();
        if(MainName.Length>30 || AltName.Length>30) throw new InvalidDataException("List names must be 30 characters or fewer.");
        foreach(Shortcut s in Items) {
            if(s==null || String.IsNullOrWhiteSpace(s.Name) || String.IsNullOrWhiteSpace(s.Target)) throw new InvalidDataException("Each shortcut needs a name and destination.");
            if(s.Name.Length>100 || s.Target.Length>4096 || s.Deck<0 || s.Deck>1) throw new InvalidDataException("Invalid shortcut values.");
            Launcher.Validate(s.Target);
            s.Icon=s.Icon??""; s.Group=s.Group??""; s.Arguments=s.Arguments??"";
        }
        if(Double.IsNaN(Position)||Double.IsInfinity(Position)) Position=.5;
        if(Double.IsNaN(Opacity)||Double.IsInfinity(Opacity)) Opacity=.95;
        Position=Math.Max(0,Math.Min(1,Position)); Opacity=Math.Max(.75,Math.Min(1,Opacity));
        HoverMs=Math.Max(0,Math.Min(1000,HoverMs)); CloseMs=Math.Max(200,Math.Min(3000,CloseMs));
    }
}
public static class Store {
    public static readonly string Folder=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Noisewater Labs","Sliver");
    public static readonly string FilePath=Path.Combine(Folder,"settings.json");
    public static string LoadNotice="";
    public static Config Load() {
        Directory.CreateDirectory(Folder);
        if(!File.Exists(FilePath)) return Config.Defaults();
        try { return Read(FilePath); }
        catch(Exception e) {
            string quarantine=FilePath+".unreadable-"+DateTime.Now.ToString("yyyyMMdd-HHmmss");
            File.Copy(FilePath,quarantine,true);
            LoadNotice="Nook preserved an unreadable settings file. "+e.Message;
            try { Config c=Read(FilePath+".bak"); LoadNotice+=" The last backup was restored."; return c; }
            catch { LoadNotice+=" Default shortcuts are loaded."; return Config.Defaults(); }
        }
    }
    public static Config Read(string path) {
        if(new FileInfo(path).Length>2*1024*1024) throw new InvalidDataException("Settings file is too large.");
        using(FileStream f=File.OpenRead(path)) { Config c=(Config)new DataContractJsonSerializer(typeof(Config)).ReadObject(f); if(c==null)throw new InvalidDataException("Empty settings."); c.Validate(); return c; }
    }
    public static void Write(Config c,string path) {
        c.Validate(); Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
        string temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";
        try {
            using(FileStream f=new FileStream(temp,FileMode.CreateNew,FileAccess.Write)) { new DataContractJsonSerializer(typeof(Config)).WriteObject(f,c); f.Flush(true); }
            if(File.Exists(path)) File.Replace(temp,path,path+".bak"); else File.Move(temp,path);
        } finally { if(File.Exists(temp))File.Delete(temp); }
    }
    public static Config Clone(Config c) {
        using(MemoryStream m=new MemoryStream()) { var s=new DataContractJsonSerializer(typeof(Config)); s.WriteObject(m,c); m.Position=0; return (Config)s.ReadObject(m); }
    }
    public static void Log(Exception e) { try { Directory.CreateDirectory(Folder); File.AppendAllText(Path.Combine(Folder,"errors.log"),DateTime.Now.ToString("u")+" "+e+Environment.NewLine); } catch {} }
}
public static class Launcher {
    public static string Expand(string s) { return Environment.ExpandEnvironmentVariables(s.Trim()); }
    public static void Validate(string target) {
        string t=Expand(target); Uri u;
        if(Path.IsPathRooted(t)) return;
        if(Uri.TryCreate(t,UriKind.Absolute,out u) && (u.Scheme=="https"||u.Scheme=="http"||u.Scheme=="shell"||u.Scheme=="ms-settings")) return;
        throw new InvalidDataException("Use a full file/folder path, a network path, https:// address, shell: location, or ms-settings: address.");
    }
    public static void Open(Shortcut s) {
        Validate(s.Target); string t=Expand(s.Target);
        if(t.StartsWith("shell:",StringComparison.OrdinalIgnoreCase)) Process.Start(new ProcessStartInfo("explorer.exe",t){UseShellExecute=true});
        else Process.Start(new ProcessStartInfo(t,s.Arguments??""){UseShellExecute=true});
    }
}
public static class Startup {
    const string Key=@"Software\Microsoft\Windows\CurrentVersion\Run";
    public static bool Enabled { get { using(var k=Registry.CurrentUser.OpenSubKey(Key)) return k!=null && (k.GetValue("Nook")!=null||k.GetValue("Sliver")!=null); } }
    public static void Set(bool value) {
        using(var k=Registry.CurrentUser.CreateSubKey(Key)) {
            if(value) k.SetValue("Nook","\""+Process.GetCurrentProcess().MainModule.FileName+"\"");
            else k.DeleteValue("Nook",false);
            k.DeleteValue("Sliver",false);
        }
    }
}
}
