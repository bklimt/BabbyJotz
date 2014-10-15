using System;

namespace BabbyJotz {
    public interface IPreferences {
        bool GetBool(string key);
        void SetBool(string key, bool value);
    }
}

