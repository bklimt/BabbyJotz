using System;
using MonoTouch.Foundation;

namespace BabbyJotz.iOS {
    public class Preferences : IPreferences {
        public Preferences() {
        }

        public bool GetBool(string key) {
            var defaults = NSUserDefaults.StandardUserDefaults;
            return defaults.BoolForKey(key);
        }

        public void SetBool(string key, bool value) {
            var defaults = NSUserDefaults.StandardUserDefaults;
            defaults.SetBool(value, key);
        }
    }
}

