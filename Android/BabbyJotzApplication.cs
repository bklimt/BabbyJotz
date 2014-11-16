using System;
using System.Threading.Tasks;

using Android.App;
using Android.Runtime;

using Parse;

namespace BabbyJotz.Android {
    [Application]
    public class BabbyJotzApplication : Application {
        public RootViewModel RootViewModel { get; private set; }

        public BabbyJotzApplication(IntPtr handle, JniHandleOwnership transfer)
            : base(handle, transfer) {
        }

        public override void OnCreate() {
            base.OnCreate();

            var prefs = new Preferences(this);

            if (!prefs.Get(PreferenceKey.DoNotLogCrashReports)) {
                // Handle the events and Save the Managed Exceptions to HockeyApp.     
                AppDomain.CurrentDomain.UnhandledException += (sender, e) => 
                    HockeyApp.ManagedExceptionHandler.SaveException(e.ExceptionObject);
                TaskScheduler.UnobservedTaskException += (sender, e) => 
                    HockeyApp.ManagedExceptionHandler.SaveException(e.Exception);
            }

            var cloudStore = new BabbyJotz.iOS.ParseStore(this, prefs);
            var localStore = new BabbyJotz.iOS.LocalStore();
            RootViewModel = new RootViewModel(localStore, cloudStore, prefs);
        }
    }
}

