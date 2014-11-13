using System;

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
            var localStore = new BabbyJotz.iOS.LocalStore();
            RootViewModel = new RootViewModel(localStore, cloudStore, prefs);
        }
    }
}

