using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
namespace Nook {
internal static class Palette {
    static string accentHex="#A8F3D0",baseHex="#17201E";
    static readonly Dictionary<string,SolidColorBrush> brushes=new Dictionary<string,SolidColorBrush>();
    public static readonly LinearGradientBrush Handle=new LinearGradientBrush();
    public static Color Parse(string hex){return (Color)ColorConverter.ConvertFromString(hex);}
    static Color Mix(Color a,Color b,double t){return Color.FromRgb((byte)(a.R+(b.R-a.R)*t),(byte)(a.G+(b.G-a.G)*t),(byte)(a.B+(b.B-a.B)*t));}
    static double L(Color c){return .2126*Channel(c.R)+.7152*Channel(c.G)+.0722*Channel(c.B);}
    static double Channel(byte c){double v=c/255.0;return v<=.04045?v/12.92:Math.Pow((v+.055)/1.055,2.4);}
    public static Color Resolve(string hex,string accent,string background) {
        Color original=Parse(hex); if(accent=="#A8F3D0"&&background=="#17201E")return original;
        Color b=Parse(background),a=Parse(accent),ink=L(b)>.179?Color.FromRgb(22,27,25):Mix(Color.FromRgb(250,247,241),a,.06);
        string key="#"+original.R.ToString("X2")+original.G.ToString("X2")+original.B.ToString("X2");Color result;
        switch(key) {
            case "#17201E":result=b;break;
            case "#151F1C":result=Mix(b,L(b)>.179?Colors.White:Colors.Black,.14);break;
            case "#EEF5F1":case "#DDF7EC":case "#FFFFFF":result=ink;break;
            case "#95AAA3":case "#71867B":case "#9DB3A6":result=Mix(b,ink,.67);break;
            case "#A8F3D0":case "#CEFFE4":case "#CFFFE4":case "#D8FFE9":case "#CDFCE6":case "#DCFFEA":case "#9DCEB2":
                result=a;while((Math.Max(L(result),L(b))+.05)/(Math.Min(L(result),L(b))+.05)<3) {Color next=Mix(result,ink,.15);if(next==result)break;result=next;}break;
            case "#183427":result=L(Resolve("#A8F3D0",accent,background))>.179?Color.FromRgb(17,25,21):Colors.White;break;
            case "#202D27":case "#202A29":case "#1E2B24":result=Mix(b,ink,.035);break;
            case "#293734":case "#2D4038":result=Mix(b,ink,.075);break;
            case "#2B4034":case "#31483B":case "#365044":case "#3B5048":result=Mix(b,a,.21);break;
            default:result=Mix(b,ink,.29);break;
        }
        result.A=original.A;return result;
    }
    public static Brush Brush(string hex) {
        hex=hex.ToUpperInvariant();SolidColorBrush brush;
        if(!brushes.TryGetValue(hex,out brush)){brush=new SolidColorBrush(Resolve(hex,accentHex,baseHex));brushes[hex]=brush;}
        return brush;
    }
    public static System.Drawing.Color Drawing(string hex){Color c=Resolve(hex,accentHex,baseHex);return System.Drawing.Color.FromArgb(c.A,c.R,c.G,c.B);}
    public static void Apply(Config c) {
        accentHex=c.AccentColor;baseHex=c.BackgroundColor;
        foreach(var entry in brushes)if(!entry.Value.IsFrozen)entry.Value.Color=Resolve(entry.Key,accentHex,baseHex);
        foreach(string key in new List<string>(brushes.Keys))Application.Current.Resources["Color"+key.Substring(1)]=new SolidColorBrush(Resolve(key,accentHex,baseHex));
        Handle.StartPoint=new Point(0,0);Handle.EndPoint=new Point(0,1);Handle.GradientStops.Clear();
        Handle.GradientStops.Add(new GradientStop(Resolve("#D0CDFCE6",accentHex,baseHex),0));Handle.GradientStops.Add(new GradientStop(Resolve("#604C7567",accentHex,baseHex),1));
    }
}
}
