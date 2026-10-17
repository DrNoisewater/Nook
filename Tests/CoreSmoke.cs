using System;
using System.IO;
using Nook;
class CoreSmoke {
 static int passed;
 static void Check(bool x,string label){if(!x)throw new Exception(label);passed++;Console.WriteLine("PASS "+label);}
 static void Reject(Action a,string label){try{a();}catch{Check(true,label);return;}throw new Exception("Expected rejection: "+label);}
 static void Main(){
  string dir=Path.Combine(Path.GetTempPath(),"sliver-test-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);
  try{
   var c=new Config();c.Items.Add(new Shortcut{Name="Website",Target="https://example.com",Deck=1});
   string f=Path.Combine(dir,"settings.json");Store.Write(c,f);Check(Store.Read(f).Items[0].Name=="Website","save/load round trip");
   c.Items[0].Name="Changed";Store.Write(c,f);Check(Store.Read(f+".bak").Items[0].Name=="Website","atomic save retains previous configuration");
   var copy=Store.Clone(c);copy.Items[0].Name="Independent";Check(c.Items[0].Name=="Changed","editing a draft does not mutate live settings");
   c.Position=double.NaN;c.Opacity=double.PositiveInfinity;c.HoverMs=-10;c.CloseMs=99999;c.Validate();Check(c.Position==.5&&c.Opacity==.95&&c.HoverMs==0&&c.CloseMs==3000,"import bounds and nonfinite values normalized");
   Reject(()=>Launcher.Validate("javascript:alert(1)"),"reject script URLs");Reject(()=>Launcher.Validate("data:text/html,test"),"reject data URLs");Reject(()=>Launcher.Validate("relative-folder"),"reject ambiguous relative targets");
   Launcher.Validate("https://example.com");Launcher.Validate("shell:MyComputerFolder");Launcher.Validate("ms-settings:");Check(true,"supported URI targets accepted");
   var invalid=Store.Clone(c);invalid.Version=99;Reject(()=>invalid.Validate(),"reject unknown configuration versions");
   invalid=Store.Clone(c);invalid.Items[0].Deck=7;Reject(()=>invalid.Validate(),"reject unknown list index");
   File.WriteAllText(f,"{ broken");Reject(()=>Store.Read(f),"reject malformed import");
   Check(Store.Read(f+".bak").Items.Count==1,"backup remains readable after primary corruption");
   c.AccentColor="#81c7ff";c.BackgroundColor="#141D2A";Store.Write(c,f);var themed=Store.Read(f);Check(themed.AccentColor=="#81C7FF"&&themed.BackgroundColor=="#141D2A","custom palette persists");
   c.AccentColor=null;c.BackgroundColor=null;c.Validate();Check(c.AccentColor=="#A8F3D0"&&c.BackgroundColor=="#17201E","older configurations receive original colors");
   Check(Config.ColorHex(" abc123 ","#000000")=="#ABC123","hex entry normalized");
   Reject(()=>Config.ColorHex("#xyzxyz","#000000"),"invalid colors rejected");
   var old=Store.Clone(c);old.AccentColor="#C0A4FF";old.BackgroundColor="#20192D";old.NookStyle=false;string target=old.Items[0].Target;
   Check(old.AdoptNook(),"first Nook upgrade adopts cozy theme");
   Check(old.PreviousAccent=="#C0A4FF"&&old.PreviousBackground=="#20192D"&&old.Items[0].Target==target,"upgrade preserves previous colors and shortcut destinations");
   old.AccentColor="#81C7FF";Check(!old.AdoptNook()&&old.AccentColor=="#81C7FF","subsequent launches preserve custom colors");
   Store.Write(old,f);var upgraded=Store.Read(f);Check(upgraded.NookStyle&&upgraded.PreviousAccent=="#C0A4FF","migration state and previous palette persist");
   Console.WriteLine(passed+" checks passed.");
  }finally{Directory.Delete(dir,true);}
 }
}
