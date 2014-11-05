using System;

using Android.Content;

namespace BabbyJotz.Android {
    public class Preferences : IPreferences {
        private ISharedPreferences prefs;

        public Preferences(Context context) {
            prefs = context.GetSharedPreferences("prefs", FileCreationMode.Private);
        }

        public bool Get(PreferenceKey<bool> key) {
            return prefs.GetBoolean(key.Key, false);
        }

        public void Set(PreferenceKey<bool> key, bool value) {
            prefs.Edit().PutBoolean(key.Key, value).Commit();
        }

        public string Get(PreferenceKey<string> key) {
            return prefs.GetString(key.Key, null);
        }

        public void Set(PreferenceKey<string> key, string value) {
            if (value == null) {
                prefs.Edit().Remove(key.Key).Commit();
            } else {
                prefs.Edit().PutString(key.Key, value).Commit();
            }
        }
    }
}

