using System;

namespace BabbyJotz {
    public class PreferenceKey {
        public static readonly PreferenceKey<bool> LightTheme = new PreferenceKey<bool>("light");
        public static readonly PreferenceKey<bool> DoNotNotify = new PreferenceKey<bool>("dontNotify");
        public static readonly PreferenceKey<bool> DoNotVibrate = new PreferenceKey<bool>("dontVibrate");

        public static readonly PreferenceKey<string> ParseInstallationObjectId =
            new PreferenceKey<string>("ParseInstallationObjectId");
    }

    public class PreferenceKey<T> {
        public string Key { get; private set; } 

        internal PreferenceKey(string key) {
            Key = key;
        }
    }
}

