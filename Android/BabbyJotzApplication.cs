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
            var cloudStore = new BabbyJotz.iOS.ParseStore(this, prefs);

            if (!prefs.Get(PreferenceKey.DoNotLogCrashReports)) {
                // Handle the events and Save the Managed Exceptions to HockeyApp.     
                AppDomain.CurrentDomain.UnhandledException += (sender, e) => {
                    var ex = e.ExceptionObject as Exception;
                    if (ex != null) {
                        cloudStore.LogException("UnhandledException", ex);
                    }
                    HockeyApp.ManagedExceptionHandler.SaveException(e.ExceptionObject);
                };
                TaskScheduler.UnobservedTaskException += (sender, e) => {
                    cloudStore.LogException("UnobservedTaskException", e.Exception);
                    HockeyApp.ManagedExceptionHandler.SaveException(e.Exception);
                };
            }

            var localStore = new BabbyJotz.iOS.LocalStore();
            RootViewModel = new RootViewModel(localStore, cloudStore, prefs);
        }
    }
}

