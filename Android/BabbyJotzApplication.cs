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

            ParseClient.Initialize(
                "dRJrkKFywmUEYJx10K96Sw848juYyFF01Zlno6Uf",
                "0ICNGpRDtEswmZw8E3nfS08W8RNWbFLExIIw2IvS");
            var cloudStore = new BabbyJotz.iOS.CloudStore();
            var localStore = new BabbyJotz.iOS.LocalStore(cloudStore);
            RootViewModel = new RootViewModel(localStore);
        }
    }
}

