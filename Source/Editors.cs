using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Forms=System.Windows.Forms;

namespace Nook {
internal sealed class ShortcutEditor:Window {
    public ShortcutEditor(Shortcut item) {
        Title="Nook • Shortcut";Width=560;Height=670;MinWidth=460;MinHeight=450;Background=Ui.B("#17201E");WindowStartupLocation=WindowStartupLocation.CenterScreen;
        var root=Ui.DialogShell(this,"Shortcut","Choose a destination and make it easy to recognize.");
        var buttons=new StackPanel {Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right,Margin=new Thickness(0,16,0,0)};DockPanel.SetDock(buttons,System.Windows.Controls.Dock.Bottom);root.Children.Add(buttons);
        var form=new StackPanel();root.Children.Add(new ScrollViewer{Content=form,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled,Padding=new Thickness(0,0,10,0)});
        var name=Field(form,"NAME",item.Name);
        var target=Field(form,"DESTINATION",item.Target);
        var browse=new WrapPanel();browse.Children.Add(Ui.Button("Choose file / app",()=>{var d=new OpenFileDialog{Filter="Files and apps|*.*"};if(d.ShowDialog()==true){target.Text=d.FileName;if(String.IsNullOrWhiteSpace(name.Text))name.Text=Path.GetFileNameWithoutExtension(d.FileName);}}));
        browse.Children.Add(Ui.Button("Choose folder",()=>{using(var d=new Forms.FolderBrowserDialog()){d.Description="Choose a shortcut folder";if(d.ShowDialog()==Forms.DialogResult.OK){target.Text=d.SelectedPath;if(String.IsNullOrWhiteSpace(name.Text))name.Text=Path.GetFileName(d.SelectedPath);}}}));form.Children.Add(browse);
        var group=Field(form,"SECTION  /  OPTIONAL",item.Group);
        var args=Field(form,"APP ARGUMENTS  /  OPTIONAL",item.Arguments);
        var icon=Field(form,"CUSTOM ICON  /  PNG, JPG OR ICO PATH",item.Icon);
        form.Children.Add(Ui.Button("Choose icon",()=>{var d=new OpenFileDialog{Filter="Images|*.png;*.jpg;*.jpeg;*.ico;*.bmp"};if(d.ShowDialog()==true)icon.Text=d.FileName;}));
        var secondary=new CheckBox {Content="Put in the second list",IsChecked=item.Deck==1};form.Children.Add(secondary);
        var info=Ui.Text("Use a full path or URL. Environment variables such as %USERPROFILE% work. Network paths are supported. Arguments are passed only when you launch the shortcut.",11,Ui.Muted);info.TextWrapping=TextWrapping.Wrap;info.Margin=new Thickness(0,10,0,8);form.Children.Add(info);
        buttons.Children.Add(Ui.Button("Cancel",()=>{DialogResult=false;}));
        buttons.Children.Add(Ui.Button("Save shortcut",()=>{
            try {
                if(String.IsNullOrWhiteSpace(name.Text)||name.Text.Trim().Length>100)throw new InvalidDataException("Enter a name of 1–100 characters.");
                if(target.Text.Trim().Length>4096)throw new InvalidDataException("Destination is too long.");Launcher.Validate(target.Text);
                string custom=icon.Text.Trim();
                if(custom.Length>0) { string expanded=Launcher.Expand(custom);if(!Path.IsPathRooted(expanded)||expanded.StartsWith(@"\\")||!File.Exists(expanded))throw new InvalidDataException("Choose an existing local icon image."); }
                item.Name=name.Text.Trim();item.Target=target.Text.Trim();item.Group=group.Text.Trim();item.Arguments=args.Text;item.Icon=custom;item.Deck=secondary.IsChecked==true?1:0;DialogResult=true;
            }catch(Exception ex){Ui.Error(ex);}
        }));
    }
    internal static TextBox Field(Panel p,string label,string value) {var t=Ui.Text(label,10,Ui.Muted);t.Margin=new Thickness(0,10,0,1);p.Children.Add(t);var box=new TextBox {Text=value??""};System.Windows.Automation.AutomationProperties.SetName(box,label);p.Children.Add(box);return box;}
}
internal sealed class SettingsWindow:Window {
    public Config Result;
    Config draft;
    StackPanel form;
    bool startup;
    public SettingsWindow(Config current) {
        draft=Store.Clone(current);startup=Startup.Enabled;
        Title="Nook • Preferences";Width=600;Height=740;MinWidth=560;MinHeight=540;Background=Ui.B("#17201E");WindowStartupLocation=WindowStartupLocation.CenterScreen;
        Build();
    }
    void Build() {
        var root=Ui.DialogShell(this,"Preferences","Your shortcuts, your screen, your pace.");
        var buttons=new StackPanel {Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right,Margin=new Thickness(0,16,0,0)};DockPanel.SetDock(buttons,System.Windows.Controls.Dock.Bottom);root.Children.Add(buttons);
        var tabs=new TabControl();root.Children.Add(tabs);
        form=Page(tabs,"Appearance");
        Ui.Section(form,"LIST NAMES");
        var main=ShortcutEditor.Field(form,"PRIMARY LIST",draft.MainName);var alt=ShortcutEditor.Field(form,"SECOND LIST  /  HOLD CTRL",draft.AltName);
        Ui.Section(form,"SCREEN & POSITION");
        form.Children.Add(Ui.Text("MONITOR",10,Ui.Muted));var monitors=new ComboBox {Margin=new Thickness(0,6,0,10),Padding=new Thickness(8)};
        var screens=Forms.Screen.AllScreens;
        for(int n=0;n<screens.Length;n++)monitors.Items.Add("Display "+(n+1)+(screens[n].Primary?" · Primary":"")+"   ·   "+screens[n].Bounds.Width+" × "+screens[n].Bounds.Height);
        int index=Array.FindIndex(screens,s=>s.DeviceName==draft.Monitor);monitors.SelectedIndex=index<0?Array.FindIndex(screens,s=>s.Primary):index;form.Children.Add(monitors);
        var edge=Check("Use the left edge instead of the right",draft.LeftEdge);
        var position=Slider("VERTICAL POSITION",draft.Position*100,0,100,"%",0);
        var destinations=Check("Show destinations below shortcut names",draft.ShowDestinations);
        var opacity=Slider("GLASS OPACITY",draft.Opacity*100,75,100,"%",0);
        form=Page(tabs,"Colors");
        Ui.Section(form,"CHOOSE A PALETTE");
        var presets=new WrapPanel {Margin=new Thickness(0,0,0,12)};form.Children.Add(presets);
        var accent=ShortcutEditor.Field(form,"ACCENT COLOR",draft.AccentColor);
        var background=ShortcutEditor.Field(form,"BACKGROUND COLOR",draft.BackgroundColor);
        var choose=new WrapPanel();form.Children.Add(choose);
        choose.Children.Add(Ui.Button("Pick accent…",()=>PickColor(accent)));
        choose.Children.Add(Ui.Button("Pick background…",()=>PickColor(background)));
        foreach(var preset in new string[][] {
            new[]{"Cozy","#F5B45F","#231B16"},new[]{"Mint","#A8F3D0","#17201E"},new[]{"Ocean","#81C7FF","#141D2A"},new[]{"Violet","#C0A4FF","#20192D"},
            new[]{"Amber","#F3C375","#251E16"},new[]{"Rose","#F4A3BB","#291B24"},new[]{"Graphite","#D4D8E2","#202124"}
        }) {
            var values=preset;var button=Ui.Button(values[0],()=>{accent.Text=values[1];background.Text=values[2];});
            button.BorderBrush=new System.Windows.Media.SolidColorBrush(Palette.Parse(values[1]));presets.Children.Add(button);
        }
        if(!String.IsNullOrWhiteSpace(draft.PreviousAccent)&&!String.IsNullOrWhiteSpace(draft.PreviousBackground))
            presets.Children.Add(Ui.Button("Previous colors",()=>{accent.Text=draft.PreviousAccent;background.Text=draft.PreviousBackground;}));
        Ui.Section(form,"PREVIEW");
        var sample=new StackPanel();
        var sampleTitle=Ui.Text("Your everyday places",17,Ui.Ink);sample.Children.Add(sampleTitle);
        var sampleSub=Ui.Text("Menus and controls follow your palette.",12,Ui.Muted);sampleSub.Margin=new Thickness(0,6,0,14);sample.Children.Add(sampleSub);
        var sampleRow=Ui.Text("◇   Documents                       ↗",14,Ui.Ink);
        var rowBorder=new Border {Padding=new Thickness(12),CornerRadius=new CornerRadius(8),Child=sampleRow};sample.Children.Add(rowBorder);
        var preview=new Border {CornerRadius=new CornerRadius(12),BorderThickness=new Thickness(1),Padding=new Thickness(18),Margin=new Thickness(0,0,0,12),Child=sample};form.Children.Add(preview);
        var colorStatus=Ui.Text("",11,Ui.Muted);colorStatus.TextWrapping=TextWrapping.Wrap;form.Children.Add(colorStatus);
        Action previewColors=()=>{
            try {
                string a=Config.ColorHex(accent.Text,"#A8F3D0"),b=Config.ColorHex(background.Text,"#17201E");
                Func<string,System.Windows.Media.Brush> brush=hex=>new System.Windows.Media.SolidColorBrush(Palette.Resolve(hex,a,b));
                preview.Background=brush("#17201E");preview.BorderBrush=brush("#40554C");sampleTitle.Foreground=brush("#EEF5F1");sampleSub.Foreground=brush("#95AAA3");rowBorder.Background=brush("#365044");sampleRow.Foreground=brush("#A8F3D0");
                colorStatus.Text="Preview only. Save preferences to apply everywhere. Text and highlights adjust for contrast.";
            }catch {colorStatus.Text="Use six-digit hex colors, such as #81C7FF.";}
        };
        accent.TextChanged+=(s,e)=>previewColors();background.TextChanged+=(s,e)=>previewColors();previewColors();
        form=Page(tabs,"Behavior");
        Ui.Section(form,"HOVER & AUTO-HIDE");
        var hover=Slider("HOVER DELAY",draft.HoverMs,0,1000," ms",0);
        var close=Slider("AUTO-HIDE DELAY",draft.CloseMs,200,3000," ms",0);
        Ui.Section(form,"EVERYDAY USE");
        var anim=Check("Animate the panel opening",draft.Animations);
        var launch=Check("Hide after opening a shortcut (unless pinned)",draft.CloseOnLaunch);
        var full=Check("Hide edge tab over full-screen applications",draft.SuppressFullscreen);
        var start=Check("Start Nook when I sign in",startup);
        form=Page(tabs,"Backup & help");
        Ui.Section(form,"YOUR SHORTCUTS & PREFERENCES");
        var backupNote=Ui.Text("Export your setup, or restore it from a saved backup.",12,Ui.Muted);backupNote.TextWrapping=TextWrapping.Wrap;form.Children.Add(backupNote);
        var actions=new WrapPanel {Margin=new Thickness(0,14,0,8)};form.Children.Add(actions);
        Action capture=()=>{
            draft.AccentColor=Config.ColorHex(accent.Text,"#A8F3D0");draft.BackgroundColor=Config.ColorHex(background.Text,"#17201E");
            draft.ShowDestinations=destinations.IsChecked==true;draft.NookStyle=true;
            draft.MainName=main.Text;draft.AltName=alt.Text;draft.Monitor=screens[Math.Max(0,monitors.SelectedIndex)].DeviceName;
            draft.LeftEdge=edge.IsChecked==true;draft.Position=position.Value/100;draft.Opacity=opacity.Value/100;draft.HoverMs=(int)hover.Value;draft.CloseMs=(int)close.Value;draft.Animations=anim.IsChecked==true;draft.CloseOnLaunch=launch.IsChecked==true;draft.SuppressFullscreen=full.IsChecked==true;startup=start.IsChecked==true;draft.Validate();
        };
        actions.Children.Add(Ui.Button("Export backup",()=>{try{capture();var d=new SaveFileDialog {Filter="Nook configuration|*.json",FileName="Nook-backup.json"};if(d.ShowDialog()==true)Store.Write(draft,d.FileName);}catch(Exception e){Ui.Error(e);}}));
        actions.Children.Add(Ui.Button("Import backup",()=>{try{var d=new OpenFileDialog {Filter="Nook configuration|*.json"};if(d.ShowDialog()==true){Config incoming=Store.Read(d.FileName);if(MessageBox.Show(this,"Replace the current lists with "+incoming.Items.Count+" imported shortcuts? Only import configurations you trust. Nothing is launched by importing.","Import backup",MessageBoxButton.YesNo)==MessageBoxResult.Yes){draft=incoming;Build();}}}catch(Exception e){Ui.Error(e);}}));
        Ui.Section(form,"KEYBOARD & MOUSE");
        var help=Ui.Text("Win + Alt + Space: open / close\nHold Ctrl: temporary second list\nShift + drag the edge tab: change its height\nRight-click a shortcut: edit, reorder, move, or remove\nTray icon: pause hover, preferences, and quit\n\nGlass uses translucency, not desktop blur. Custom icons remain linked to their local files; keep those files in a permanent location.",12,Ui.Muted);help.TextWrapping=TextWrapping.Wrap;help.Margin=new Thickness(0,12,0,16);form.Children.Add(help);
        buttons.Children.Add(Ui.Button("Cancel",()=>{DialogResult=false;}));buttons.Children.Add(Ui.Button("Save preferences",()=>{try{capture();Store.Write(draft,Store.FilePath);if(startup!=Startup.Enabled)Startup.Set(startup);Result=draft;DialogResult=true;}catch(Exception e){Ui.Error(e);}}));
    }
    void PickColor(TextBox target) {
        try {
            using(var picker=new Forms.ColorDialog {FullOpen=true,AnyColor=true}) {
                var c=Palette.Parse(Config.ColorHex(target.Text,"#A8F3D0"));picker.Color=System.Drawing.Color.FromArgb(c.R,c.G,c.B);
                if(picker.ShowDialog()==Forms.DialogResult.OK)target.Text="#"+picker.Color.R.ToString("X2")+picker.Color.G.ToString("X2")+picker.Color.B.ToString("X2");
            }
        }catch(Exception e){Ui.Error(e);}
    }
    StackPanel Page(TabControl tabs,string title) {
        var content=new StackPanel();tabs.Items.Add(new TabItem {Header=title,Content=new ScrollViewer {Content=content,Padding=new Thickness(0,0,12,0),VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled}});return content;
    }
    CheckBox Check(string text,bool value){var label=Ui.Text(text,13,Ui.Ink);label.TextWrapping=TextWrapping.Wrap;var c=new CheckBox{Content=label,IsChecked=value};System.Windows.Automation.AutomationProperties.SetName(c,text);form.Children.Add(c);return c;}
    Slider Slider(string label,double value,double min,double max,string unit,int digits) {
        var line=new DockPanel {Margin=new Thickness(0,16,0,3)};
        var number=Ui.Text("",12,Ui.Mint);DockPanel.SetDock(number,System.Windows.Controls.Dock.Right);line.Children.Add(number);line.Children.Add(Ui.Text(label,10,Ui.Muted));form.Children.Add(line);
        var slider=new Slider{Minimum=min,Maximum=max,Value=value,Margin=new Thickness(0,4,0,6),SmallChange=1,LargeChange=10};
        System.Windows.Automation.AutomationProperties.SetName(slider,label);
        Action update=()=>number.Text=Math.Round(slider.Value,digits)+unit;slider.ValueChanged+=(s,e)=>update();update();form.Children.Add(slider);return slider;
    }
}
}
