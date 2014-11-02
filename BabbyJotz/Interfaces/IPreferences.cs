using System;

namespace BabbyJotz {
    public interface IPreferences {
        bool Get(PreferenceKey<bool> key);
        void Set(PreferenceKey<bool> key, bool value);

        string Get(PreferenceKey<string> key);
        void Set(PreferenceKey<string> key, string value);
    }
}

