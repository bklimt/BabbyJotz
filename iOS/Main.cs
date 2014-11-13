using System;
using System.Collections.Generic;
using System.Linq;

using MonoTouch.Foundation;
using MonoTouch.UIKit;

namespace BabbyJotz.iOS {
    public class Application {
        // This is the main entry point of the application.
        static void Main(string[] args) {
            // if you want to use a different Application Delegate class from "AppDelegate"
            // you can specify it here.
            try {
                UIApplication.Main(args, null, "AppDelegate");
            } catch (Exception e) {
                // Well, we got an unhandled exception. Try to send it somewhere for debugging.
                var prefs = new Preferences();
                var cloud = new ParseStore(prefs);
                cloud.LogExceptionAsync(e).Wait(TimeSpan.FromSeconds(30));
                throw;
            }
        }
    }
}

