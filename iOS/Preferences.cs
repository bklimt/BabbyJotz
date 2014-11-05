using System;
using MonoTouch.Foundation;

namespace BabbyJotz.iOS {
    public class Preferences : IPreferences {
        public Preferences() {
        }

        public bool Get(PreferenceKey<bool> key) {
            var defaults = NSUserDefaults.StandardUserDefaults;
            return defaults.BoolForKey(key.Key);
        }

        public void Set(PreferenceKey<bool> key, bool value) {
            var defaults = NSUserDefaults.StandardUserDefaults;
            defaults.SetBool(value, key.Key);
        }

        public string Get(PreferenceKey<string> key) {
            var defaults = NSUserDefaults.StandardUserDefaults;
            return defaults.StringForKey(key.Key);
        }

        public void Set(PreferenceKey<string> key, string value) {
            var defaults = NSUserDefaults.StandardUserDefaults;
            if (value == null) {
                defaults.RemoveObject(key.Key);
            } else {
                defaults.SetString(value, key.Key);
            }
        }
    }
}

