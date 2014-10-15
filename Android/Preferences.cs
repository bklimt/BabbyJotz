using System;

using Android.Content;

namespace BabbyJotz.Android {
    public class Preferences : IPreferences {
        private ISharedPreferences prefs;

        public Preferences(Context context) {
            prefs = context.GetSharedPreferences("prefs", FileCreationMode.Private);
        }

        public bool GetBool(string key) {
            return prefs.GetBoolean(key, false);
        }

        public void SetBool(string key, bool value) {
            prefs.Edit().PutBoolean(key, value).Commit();
        }
    }
}

