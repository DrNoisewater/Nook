using System;
using System.Threading;
using System.Windows;
namespace Nook {
public static class Program {
    [STAThread] public static void Main() {
        bool created;
        using(Mutex mutex=new Mutex(true,@"Local\NoisewaterLabs.Sliver",out created)) {
            if(!created) { MessageBox.Show("Nook is already running. Use its tray icon or Win + Alt + Space.","Nook"); return; }
            try {
                Application app=new Application { ShutdownMode=ShutdownMode.OnExplicitShutdown };
                app.DispatcherUnhandledException+=(s,e)=>{ Store.Log(e.Exception); MessageBox.Show(e.Exception.Message,"Nook — error"); e.Handled=true; };
                Ui.Install(app);
                Config config=Store.Load();if(config.AdoptNook())Store.Write(config,Store.FilePath);Palette.Apply(config);
                try{if(Startup.Enabled)Startup.Set(true);}catch(Exception e){Store.Log(e);Store.LoadNotice+=" Could not update the startup entry. Toggle startup in Preferences to retry.";}
                Dock dock=new Dock(config); app.Run(dock);
            } catch(Exception e) { Store.Log(e); MessageBox.Show(e.Message,"Nook could not start"); }
        }
    }
}
}
