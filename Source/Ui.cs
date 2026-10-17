using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
namespace Nook {
internal static class Ui {
    public static Brush Ink=B("#EEF5F1"), Muted=B("#95AAA3"), Mint=B("#A8F3D0"), Surface=B("#202A29");
    public static Brush B(string hex) { return Palette.Brush(hex); }
    public static TextBlock Text(string value,double size,Brush color) { return new TextBlock { Text=value,FontSize=size,Foreground=color,VerticalAlignment=VerticalAlignment.Center,TextTrimming=TextTrimming.CharacterEllipsis }; }
    public static Button Button(string label,Action action) { var b=new Button { Content=label,Margin=new Thickness(3),Padding=new Thickness(12,8,12,8),Cursor=System.Windows.Input.Cursors.Hand }; b.Click+=(s,e)=>action(); return b; }
    public static void Install(Application app) {
        using(var stream=System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("Nook.Theme.xaml"))
            app.Resources=(ResourceDictionary)XamlReader.Load(stream);
        foreach(string hex in new string[]{"#151F1C","#183427","#202D27","#293734","#2B4034","#30443A","#31483B","#365044","#3A4E44","#3A5144","#3B5048","#40554C","#527864","#536B5D","#536D5E","#577061","#71867B","#95AAA3","#9DB3A6","#9DCEB2","#A8F3D0","#CEFFE4","#CFFFE4","#D8FFE9","#DDF7EC","#EEF5F1","#FFFFFF"})B(hex);
        Palette.Apply(new Config());
    }

    static Color WithAlpha(Color c,double opacity){c.A=(byte)(255*opacity);return c;}
    public static Border Glass(UIElement child,double opacity) {
        var grad=new LinearGradientBrush(); grad.StartPoint=new Point(0,0);grad.EndPoint=new Point(1,1);
        grad.GradientStops.Add(new GradientStop(WithAlpha(((SolidColorBrush)B("#293734")).Color,opacity),0));
        grad.GradientStops.Add(new GradientStop(WithAlpha(((SolidColorBrush)B("#17201E")).Color,opacity),.65));
        return new Border { CornerRadius=new CornerRadius(20),Background=grad,BorderBrush=B("#668CADA0"),BorderThickness=new Thickness(1),Child=child,Padding=new Thickness(18) };
    }
    public static DockPanel DialogShell(Window w,string title,string subtitle) {
        w.WindowStyle=WindowStyle.None;w.ResizeMode=ResizeMode.CanResize;
        System.Windows.Shell.WindowChrome.SetWindowChrome(w,new System.Windows.Shell.WindowChrome {
            CaptionHeight=48,ResizeBorderThickness=new Thickness(6),GlassFrameThickness=new Thickness(0),CornerRadius=new CornerRadius(0),UseAeroCaptionButtons=false
        });
        var shell=new DockPanel();
        w.Content=new Border{Background=B("#17201E"),BorderBrush=B("#40554C"),BorderThickness=new Thickness(1),Child=shell};
        var bar=new DockPanel {Background=B("#1E2B24"),Height=48,LastChildFill=true};DockPanel.SetDock(bar,System.Windows.Controls.Dock.Top);shell.Children.Add(bar);
        var close=Button("×",()=>w.Close());close.Width=36;close.Height=30;close.Margin=new Thickness(0,0,10,0);close.ToolTip="Close";
        System.Windows.Shell.WindowChrome.SetIsHitTestVisibleInChrome(close,true);DockPanel.SetDock(close,System.Windows.Controls.Dock.Right);bar.Children.Add(close);
        var label=Text("NOOK  /  "+title,12,Ink);label.Margin=new Thickness(20,0,0,0);bar.Children.Add(label);
        var body=new DockPanel {Margin=new Thickness(24,20,24,20)};shell.Children.Add(body);
        var heading=new StackPanel {Margin=new Thickness(0,0,0,20)};DockPanel.SetDock(heading,System.Windows.Controls.Dock.Top);body.Children.Add(heading);
        heading.Children.Add(Text(title,25,Ink));var sub=Text(subtitle,12,Muted);sub.Margin=new Thickness(0,6,0,0);sub.TextWrapping=TextWrapping.Wrap;heading.Children.Add(sub);
        return body;
    }
    public static FrameworkElement Arch(double size) {
        return new System.Windows.Shapes.Path {Data=Geometry.Parse("M 3,22 L 3,11 C 3,0 21,0 21,11 L 21,22"),Stroke=Mint,StrokeThickness=6,StrokeStartLineCap=PenLineCap.Round,StrokeEndLineCap=PenLineCap.Round,Stretch=Stretch.Uniform,Width=size,Height=size,Margin=new Thickness(1)};
    }
    public static FrameworkElement ShortcutIcon(Shortcut item) {
        string t=item.Target.ToLowerInvariant(),data;
        if(t.StartsWith("http"))data="M 12,2 A 10,10 0 1 1 11.99,2 M 2,12 L 22,12 M 12,2 C 5,7 5,17 12,22 C 19,17 19,7 12,2";
        else if(t.Contains("mycomputer")||t.Contains("desktop"))data="M 2,3 L 22,3 L 22,17 L 2,17 Z M 12,17 L 12,22 M 7,22 L 17,22";
        else if(t.Contains("download"))data="M 12,2 L 12,17 M 6,11 L 12,17 L 18,11 M 3,17 L 3,22 L 21,22 L 21,17";
        else if(t.Contains("document"))data="M 5,2 L 15,2 L 20,7 L 20,22 L 5,22 Z M 15,2 L 15,8 L 20,8 M 8,12 L 16,12 M 8,16 L 16,16";
        else if(t.StartsWith("ms-settings"))data="M 3,6 L 21,6 M 3,12 L 21,12 M 3,18 L 21,18 M 8,3 L 8,9 M 16,9 L 16,15 M 10,15 L 10,21";
        else data="M 2,5 L 9,5 L 12,8 L 22,8 L 22,21 L 2,21 Z";
        return new System.Windows.Shapes.Path {Data=Geometry.Parse(data),Stroke=Mint,StrokeThickness=1.5,StrokeLineJoin=PenLineJoin.Round,StrokeStartLineCap=PenLineCap.Round,StrokeEndLineCap=PenLineCap.Round,Width=22,Height=22,Stretch=Stretch.Uniform,HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Center};
    }
    public static void Section(Panel panel,string title) {
        var label=Text(title,11,Mint);label.Margin=new Thickness(0,18,0,9);panel.Children.Add(label);
    }
    public static void Error(Exception e) { Store.Log(e); MessageBox.Show(e.Message,"Nook",MessageBoxButton.OK,MessageBoxImage.Warning); }
    public static MenuItem Menu(string label,Action action) { var m=new MenuItem {Header=label};m.Click+=(s,e)=>action();return m; }
}
}
