using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Forms=System.Windows.Forms;

namespace Nook {
public sealed class Dock:Window {
    internal Config Config;
    readonly DispatcherTimer timer=new DispatcherTimer { Interval=TimeSpan.FromMilliseconds(50) };
    Forms.NotifyIcon tray;
    Window panel;
    TextBox search;
    StackPanel rows;
    TextBlock deckTitle, hint;
    Button mainButton, altButton, pinButton;
    bool pinned, modal, menuOpen, paused, quitting;
    int selectedDeck, renderedDeck=-1;
    DateTime entered=DateTime.MinValue, left=DateTime.MinValue;
    readonly Dictionary<string,ImageSource> iconCache=new Dictionary<string,ImageSource>();
    public Dock(Config config) {
        Config=config;
        Title="Nook"; Width=16;Height=110;WindowStyle=WindowStyle.None;ResizeMode=ResizeMode.NoResize;
        AllowsTransparency=true;Background=Brushes.Transparent;Topmost=true;ShowInTaskbar=false;ShowActivated=false;
        var gleam=Palette.Handle;
        var core=new Border {CornerRadius=new CornerRadius(8),BorderBrush=Ui.B("#A6DCFFEA"),BorderThickness=new Thickness(1),Background=gleam,Margin=new Thickness(2),Child=Ui.Text("│",18,Ui.Ink)};
        ((TextBlock)core.Child).HorizontalAlignment=HorizontalAlignment.Center;
        Content=core;ToolTip="Nook • hover to open • Ctrl for Workspace";
        MouseLeftButtonDown+=(s,e)=>{ if((Keyboard.Modifiers&ModifierKeys.Shift)!=0) { DragMove(); SaveHandlePosition(); } else Open(false); };
        AllowDrop=true; DragEnter+=(s,e)=>{ Open(false);e.Effects=DragDropEffects.Copy; };Drop+=Dropped;
        ContextMenu=new ContextMenu();ContextMenu.Items.Add(Ui.Menu("Open Nook",()=>Open(true)));ContextMenu.Items.Add(Ui.Menu("Settings",Settings));ContextMenu.Items.Add(Ui.Menu("Quit Nook",Quit));
        ContextMenu.Opened+=(s,e)=>menuOpen=true;ContextMenu.Closed+=(s,e)=>menuOpen=false;
        SourceInitialized+=(s,e)=> {
            IntPtr h=new WindowInteropHelper(this).Handle;
            HwndSource.FromHwnd(h).AddHook(Hook);
            if(!Native.RegisterHotKey(h,1,0x4000|0x1|0x8,0x20)) Store.LoadNotice+=" Win + Alt + Space is already in use; the tray icon is still available.";
        };
        Loaded+=(s,e)=>{
            PlaceHandle();CreateTray();timer.Tick+=Tick;timer.Start();
            if(!String.IsNullOrEmpty(Store.LoadNotice)) tray.ShowBalloonTip(7000,"Nook",Store.LoadNotice,Forms.ToolTipIcon.Warning);
        };
        Microsoft.Win32.SystemEvents.DisplaySettingsChanged+=DisplaysChanged;
        Closed+=(s,e)=>{
            timer.Stop();Microsoft.Win32.SystemEvents.DisplaySettingsChanged-=DisplaysChanged;
            Native.UnregisterHotKey(new WindowInteropHelper(this).Handle,1);
            if(tray!=null) {tray.Visible=false;tray.Dispose();}
        };
        Closing+=(s,e)=>{ if(!quitting) {e.Cancel=true;ClosePanel();} };
    }
    IntPtr Hook(IntPtr h,int msg,IntPtr w,IntPtr l,ref bool handled) {
        if(msg==0x312) { if(panel==null||!panel.IsVisible)Open(true);else ClosePanel();handled=true; }
        if(msg==0x2E0) Dispatcher.BeginInvoke(new Action(()=>{PlaceHandle();PlacePanel();}));
        return IntPtr.Zero;
    }
    void DisplaysChanged(object s,EventArgs e) {Dispatcher.BeginInvoke(new Action(()=>{PlaceHandle();PlacePanel();}));}
    Forms.Screen Screen {get {return Forms.Screen.AllScreens.FirstOrDefault(x=>x.DeviceName==Config.Monitor)??Forms.Screen.PrimaryScreen;}}
    void PlaceHandle() {
        IntPtr h=new WindowInteropHelper(this).Handle;if(h==IntPtr.Zero)return;
        var a=Screen.WorkingArea;double d=Native.Scale(h);int w=(int)(Width*d),ht=(int)(Height*d);
        Native.SetWindowPos(h,new IntPtr(-1),Config.LeftEdge?a.Left:a.Right-w,a.Top+(int)((a.Height-ht)*Config.Position),w,ht,0x10);
    }
    void SaveHandlePosition() {
        Native.RECT r;Native.GetWindowRect(new WindowInteropHelper(this).Handle,out r);
        var a=Screen.WorkingArea;Config.Position=Math.Max(0,Math.Min(1,(double)(r.Top-a.Top)/Math.Max(1,a.Height-(r.Bottom-r.Top))));Save();PlaceHandle();PlacePanel();
    }
    void PlacePanel() {
        if(panel==null||!panel.IsVisible)return;
        var a=Screen.WorkingArea;var h=new WindowInteropHelper(panel).Handle;double d=Native.Scale(h);
        int w=Math.Min((int)(368*d),a.Width-24),ht=Math.Min((int)(650*d),a.Height-24);
        int x=Config.LeftEdge?a.Left+18:a.Right-w-18;
        int y=a.Top+Math.Max(12,(int)((a.Height-ht)*Config.Position));
        Native.SetWindowPos(h,new IntPtr(-1),x,y,w,ht,0x10);
    }
    bool Fullscreen() {
        IntPtr h=Native.GetForegroundWindow();if(h==IntPtr.Zero||Native.IsDesktop(h)||h==new WindowInteropHelper(this).Handle || (panel!=null && h==new WindowInteropHelper(panel).Handle))return false;
        Native.RECT r;if(!Native.GetWindowRect(h,out r))return false;
        var b=Screen.Bounds;return r.Left<=b.Left&&r.Top<=b.Top&&r.Right>=b.Right&&r.Bottom>=b.Bottom;
    }
    void Tick(object sender,EventArgs e) {
        if(modal||menuOpen)return;
        bool shown=panel!=null&&panel.IsVisible;
        bool suppress=paused||(!shown&&Config.SuppressFullscreen&&Fullscreen());
        Opacity=suppress?0:1;IsHitTestVisible=!suppress;
        if(!shown) {
            if(!suppress&&Native.Inside(this,0)) {
                if(entered==DateTime.MinValue)entered=DateTime.UtcNow;
                if((DateTime.UtcNow-entered).TotalMilliseconds>=Config.HoverMs)Open(false);
            } else entered=DateTime.MinValue;
            return;
        }
        int deck=Native.Control?1:selectedDeck;
        if(deck!=renderedDeck)RenderRows();
        if(Native.Inside(this,8)||Native.Inside(panel,8)||pinned)left=DateTime.MinValue;
        else {
            if(left==DateTime.MinValue)left=DateTime.UtcNow;
            if((DateTime.UtcNow-left).TotalMilliseconds>Config.CloseMs)ClosePanel();
        }
    }
    void CreateTray() {
        tray=new Forms.NotifyIcon {Text="Nook • Noisewater Labs",Visible=true};
        try {tray.Icon=System.Drawing.Icon.ExtractAssociatedIcon(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);} catch {tray.Icon=System.Drawing.SystemIcons.Application;}
        var menu=new Forms.ContextMenuStrip {ShowImageMargin=false,ShowCheckMargin=false,BackColor=Palette.Drawing("#202D27"),ForeColor=Palette.Drawing("#EEF5F1"),Padding=new Forms.Padding(5),Renderer=new TrayRenderer()};
        menu.Font=new System.Drawing.Font("Segoe UI",10);
        menu.Items.Add("Open Nook",null,(s,e)=>Open(true));menu.Items.Add("Settings",null,(s,e)=>Settings());
        var pause=menu.Items.Add("Pause hover",null,(s,e)=>{paused=!paused;ClosePanel();});
        menu.Opening+=(s,e)=>pause.Text=paused?"Resume hover":"Pause hover";
        menu.Items.Add("Open settings folder",null,(s,e)=>{try{System.Diagnostics.Process.Start(Store.Folder);}catch(Exception ex){Ui.Error(ex);}});
        menu.Items.Add("Quit Nook",null,(s,e)=>Quit());foreach(Forms.ToolStripItem entry in menu.Items)entry.Padding=new Forms.Padding(9,6,9,6);tray.ContextMenuStrip=menu;tray.DoubleClick+=(s,e)=>Open(true);
    }
    void Open(bool keyboard) {
        if(modal)return;
        if(panel!=null&&panel.IsVisible) { if(keyboard){panel.Activate();search.Focus();} return; }
        selectedDeck=0;renderedDeck=-1;entered=DateTime.MinValue;left=DateTime.MinValue;
        panel=new Window {Title="Nook shortcuts",Width=368,Height=650,WindowStyle=WindowStyle.None,ResizeMode=ResizeMode.NoResize,AllowsTransparency=true,Background=Brushes.Transparent,ShowInTaskbar=false,ShowActivated=keyboard,Topmost=true,Owner=this,AllowDrop=true};
        var layout=new DockPanel {LastChildFill=true};
        var header=new StackPanel();DockPanel.SetDock(header,System.Windows.Controls.Dock.Top);layout.Children.Add(header);
        var brand=new DockPanel();var close=Ui.Button("×",ClosePanel);DockPanel.SetDock(close,System.Windows.Controls.Dock.Right);close.Width=34;close.Height=34;close.Padding=new Thickness(0);brand.Children.Add(close);
        var settings=Ui.Button("⚙",Settings);settings.ToolTip="Settings";DockPanel.SetDock(settings,System.Windows.Controls.Dock.Right);settings.Width=34;settings.Height=34;settings.Padding=new Thickness(0);brand.Children.Add(settings);
        var branding=new StackPanel();var wordmark=new StackPanel {Orientation=Orientation.Horizontal};
        var logo=Ui.Arch(29);logo.Margin=new Thickness(0,0,10,0);wordmark.Children.Add(logo);var name=Ui.Text("nook",31,Ui.Ink);name.FontWeight=FontWeights.Light;wordmark.Children.Add(name);branding.Children.Add(wordmark);
        var byline=Ui.Text("N O I S E W A T E R   L A B S",8,Ui.Muted);byline.Margin=new Thickness(39,3,0,0);branding.Children.Add(byline);brand.Children.Add(branding);header.Children.Add(brand);
        var tabs=new Grid {Margin=new Thickness(0,19,0,10)};tabs.ColumnDefinitions.Add(new ColumnDefinition());tabs.ColumnDefinitions.Add(new ColumnDefinition());
        mainButton=Ui.Button(Config.MainName,()=>{selectedDeck=0;RenderRows();});altButton=Ui.Button(Config.AltName,()=>{selectedDeck=1;RenderRows();});Grid.SetColumn(altButton,1);tabs.Children.Add(mainButton);tabs.Children.Add(altButton);header.Children.Add(tabs);

        search=new TextBox {ToolTip="Search shortcuts in the current list"};
        var searchBox=new Grid();searchBox.Children.Add(search);var placeholder=Ui.Text("⌕   Find your place…",13,Ui.Muted);placeholder.Margin=new Thickness(12,0,0,4);placeholder.IsHitTestVisible=false;searchBox.Children.Add(placeholder);
        header.Children.Add(searchBox);search.TextChanged+=(s,e)=>{placeholder.Visibility=search.Text.Length==0?Visibility.Visible:Visibility.Collapsed;RenderRows();};
        System.Windows.Automation.AutomationProperties.SetName(search,"Search shortcuts");
        deckTitle=Ui.Text("",11,Ui.Muted);deckTitle.Margin=new Thickness(4,4,0,10);header.Children.Add(deckTitle);
        var footer=new StackPanel {Margin=new Thickness(0,10,0,0)};DockPanel.SetDock(footer,System.Windows.Controls.Dock.Bottom);layout.Children.Add(footer);
        var actions=new DockPanel();pinButton=Ui.Button(pinned?"Unpin":"Pin",()=>{pinned=!pinned;pinButton.Content=pinned?"Unpin":"Pin";});DockPanel.SetDock(pinButton,System.Windows.Controls.Dock.Right);actions.Children.Add(pinButton);
        actions.Children.Add(Ui.Button("＋  Add shortcut",()=>Edit(null)));footer.Children.Add(actions);
        hint=Ui.Text("Hold Ctrl for Workspace · drop files to add",10,Ui.Muted);hint.Margin=new Thickness(3,8,0,0);footer.Children.Add(hint);
        rows=new StackPanel();var scroll=new ScrollViewer {Content=rows,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled};layout.Children.Add(scroll);
        panel.Content=Ui.Glass(layout,Config.Opacity);
        panel.PreviewKeyDown+=(s,e)=>{
            if(e.Key==Key.K && (Keyboard.Modifiers&ModifierKeys.Control)!=0){search.Focus();e.Handled=true;}
            if(e.Key==Key.Escape){ClosePanel();e.Handled=true;}
            if(e.Key==Key.Enter && search.IsKeyboardFocused) {var first=VisibleItems().FirstOrDefault();if(first!=null)Launch(first);e.Handled=true;}
        };
        panel.PreviewDrop+=Dropped;panel.PreviewDragOver+=(s,e)=>{e.Effects=e.Data.GetDataPresent(DataFormats.FileDrop)||e.Data.GetDataPresent(DataFormats.UnicodeText)?DragDropEffects.Copy:DragDropEffects.None;e.Handled=true;};
        panel.SourceInitialized+=(s,e)=>{ HwndSource.FromHwnd(new WindowInteropHelper(panel).Handle).AddHook((IntPtr a,int msg,IntPtr b,IntPtr c,ref bool handled)=>{if(msg==0x2E0)Dispatcher.BeginInvoke(new Action(PlacePanel));return IntPtr.Zero;}); };
        panel.Show();PlacePanel();RenderRows();
        if(Config.Animations) panel.BeginAnimation(Window.OpacityProperty,new DoubleAnimation(0,1,TimeSpan.FromMilliseconds(140)));
        if(keyboard){panel.Activate();search.Focus();}
    }
    int Deck {get{return Native.Control?1:selectedDeck;}}
    IEnumerable<Shortcut> VisibleItems() {
        string q=search==null?"":search.Text.Trim();
        return Config.Items.Where(x=>x.Deck==Deck && (q.Length==0||x.Name.IndexOf(q,StringComparison.OrdinalIgnoreCase)>=0||x.Target.IndexOf(q,StringComparison.OrdinalIgnoreCase)>=0||x.Group.IndexOf(q,StringComparison.OrdinalIgnoreCase)>=0));
    }
    void RenderRows() {
        if(rows==null||panel==null)return;
        renderedDeck=Deck;rows.Children.Clear();
        mainButton.Style=(Style)Application.Current.Resources[renderedDeck==0?"SelectedPill":"Pill"];
        altButton.Style=(Style)Application.Current.Resources[renderedDeck==1?"SelectedPill":"Pill"];
        var items=VisibleItems().ToList();deckTitle.Text=(renderedDeck==0?Config.MainName:Config.AltName).ToUpperInvariant()+"   /   "+items.Count+" PLACES";
        hint.Text="Hold Ctrl for "+Config.AltName;
        if(items.Count==0) {var empty=Ui.Text("Nothing here yet.\nAdd a shortcut or drop a file or folder.",14,Ui.Muted);empty.Margin=new Thickness(10,25,10,25);empty.TextWrapping=TextWrapping.Wrap;rows.Children.Add(empty);}
        string lastGroup=null;
        foreach(Shortcut item in items) {
            if(item.Group!=lastGroup&&!String.IsNullOrWhiteSpace(item.Group)) {var group=Ui.Text(item.Group.ToUpperInvariant(),10,Ui.Muted);group.Margin=new Thickness(8,14,0,7);rows.Children.Add(group);} lastGroup=item.Group;
            var grid=new Grid();grid.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(34)});grid.ColumnDefinitions.Add(new ColumnDefinition());grid.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(16)});
            string key=item.Target+"|"+item.Icon;ImageSource icon;
            if(!iconCache.TryGetValue(key,out icon)){icon=Native.FileIcon(item);iconCache[key]=icon;}
            bool artwork=!String.IsNullOrWhiteSpace(item.Icon)||item.Target.EndsWith(".exe",StringComparison.OrdinalIgnoreCase)||item.Target.EndsWith(".lnk",StringComparison.OrdinalIgnoreCase);
            UIElement mark=artwork&&icon!=null?(UIElement)new Image {Source=icon,Width=22,Height=22,HorizontalAlignment=HorizontalAlignment.Left}:Ui.ShortcutIcon(item);grid.Children.Add(mark);
            var labels=new StackPanel {VerticalAlignment=VerticalAlignment.Center};labels.Children.Add(Ui.Text(item.Name,14,Ui.Ink));var sub=Ui.Text(item.Target,10,Ui.Muted);sub.Margin=new Thickness(0,3,0,0);if(Config.ShowDestinations)labels.Children.Add(sub);Grid.SetColumn(labels,1);grid.Children.Add(labels);
            var arrow=Ui.Text("›",22,Ui.Muted);Grid.SetColumn(arrow,2);grid.Children.Add(arrow);
            var button=Ui.Button("",()=>Launch(item));button.Content=grid;button.MinHeight=49;button.HorizontalContentAlignment=HorizontalAlignment.Stretch;button.Padding=new Thickness(12,10,12,10);button.Background=Ui.B("#293734");button.BorderBrush=Ui.B("#253E574A");button.ToolTip=item.Target;
            System.Windows.Automation.AutomationProperties.SetName(button,item.Name);
            var cm=new ContextMenu();cm.Items.Add(Ui.Menu("Edit shortcut",()=>Edit(item)));cm.Items.Add(Ui.Menu("Move up",()=>Move(item,-1)));cm.Items.Add(Ui.Menu("Move down",()=>Move(item,1)));cm.Items.Add(Ui.Menu("Move to other list",()=>{item.Deck=1-item.Deck;Save();RenderRows();}));cm.Items.Add(Ui.Menu("Copy destination",()=>Clipboard.SetText(item.Target)));cm.Items.Add(new Separator());cm.Items.Add(Ui.Menu("Remove shortcut",()=>Remove(item)));
            cm.Opened+=(s,e)=>menuOpen=true;cm.Closed+=(s,e)=>{menuOpen=false;left=DateTime.MinValue;};button.ContextMenu=cm;rows.Children.Add(button);
        }
    }
    void Move(Shortcut item,int delta) {
        var same=Config.Items.Where(x=>x.Deck==item.Deck).ToList();int next=same.IndexOf(item)+delta;if(next<0||next>=same.Count)return;
        int a=Config.Items.IndexOf(item),b=Config.Items.IndexOf(same[next]);Config.Items[a]=same[next];Config.Items[b]=item;Save();RenderRows();
    }
    void Remove(Shortcut item) {modal=true;try {if(MessageBox.Show(panel,"Remove ‘"+item.Name+"’ from Nook? The original file is untouched.","Remove shortcut",MessageBoxButton.YesNo)==MessageBoxResult.Yes){Config.Items.Remove(item);Save();RenderRows();}}finally{modal=false;}}
    void Launch(Shortcut item) {
        try{Launcher.Open(item);if(Config.CloseOnLaunch&&!pinned)ClosePanel();}
        catch(Exception ex){modal=true;try{Ui.Error(ex);}finally{modal=false;}}
    }
    internal void Save() {try{Store.Write(Config,Store.FilePath);}catch(Exception e){Ui.Error(e);}}
    void Edit(Shortcut original) {
        if(original==null&&Config.Items.Count>=500){MessageBox.Show("Limit: 500 shortcuts.","Nook");return;}
        int deck=Deck;modal=true;
        try {
            Shortcut draft=original==null?new Shortcut{Deck=deck}:original.Copy();
            var editor=new ShortcutEditor(draft);if(panel!=null&&panel.IsVisible)editor.Owner=panel;
            if(editor.ShowDialog()==true) {if(original==null)Config.Items.Add(draft);else Config.Items[Config.Items.IndexOf(original)]=draft;Save();iconCache.Clear();RenderRows();}
        }finally{modal=false;left=DateTime.MinValue;}
    }
    void Dropped(object sender,DragEventArgs e) {
        e.Handled=true;int deck=Deck;var additions=new List<Shortcut>();
        if(e.Data.GetDataPresent(DataFormats.FileDrop)) {
            foreach(string path in (string[])e.Data.GetData(DataFormats.FileDrop)) additions.Add(new Shortcut{Name=Path.GetFileNameWithoutExtension(path).Length==0?path:Path.GetFileNameWithoutExtension(path),Target=path,Deck=deck});
        } else if(e.Data.GetDataPresent(DataFormats.UnicodeText)) {
            string t=((string)e.Data.GetData(DataFormats.UnicodeText)).Trim();Uri u;
            if(Uri.TryCreate(t,UriKind.Absolute,out u)&&(u.Scheme=="http"||u.Scheme=="https"))additions.Add(new Shortcut{Name=u.Host,Target=t,Deck=deck});
        }
        if(Config.Items.Count+additions.Count>500){MessageBox.Show("Limit: 500 shortcuts.","Nook");return;}
        foreach(var x in additions) if(!Config.Items.Any(y=>y.Deck==deck&&y.Target.Equals(x.Target,StringComparison.OrdinalIgnoreCase)))Config.Items.Add(x);
        if(additions.Count>0){Save();Open(false);RenderRows();}
    }
    void Settings() {
        modal=true;try {
            var w=new SettingsWindow(Config);if(w.ShowDialog()==true){Config=w.Result;Palette.Apply(Config);tray.ContextMenuStrip.BackColor=Palette.Drawing("#202D27");tray.ContextMenuStrip.ForeColor=Palette.Drawing("#EEF5F1");iconCache.Clear();ClosePanel();PlaceHandle();}
        }finally{modal=false;left=DateTime.MinValue;}
    }
    void ClosePanel(){if(panel!=null){panel.Close();panel=null;}rows=null;search=null;entered=DateTime.MinValue;left=DateTime.MinValue;}
    void Quit(){quitting=true;ClosePanel();Close();Application.Current.Shutdown();}
}
internal static class BrushExtensions {public static Color ToColor(this Brush b){return ((SolidColorBrush)b).Color;}}
}
