﻿using System;

namespace BabbyJotz {
    public class PreferenceKey {
        public static readonly PreferenceKey<bool> LightTheme = new PreferenceKey<bool>("light");
        public static readonly PreferenceKey<bool> DoNotSync = new PreferenceKey<bool>("dontSync");
        public static readonly PreferenceKey<bool> DoNotNotify = new PreferenceKey<bool>("dontNotify");
        public static readonly PreferenceKey<bool> DoNotVibrate = new PreferenceKey<bool>("dontVibrate");

        public static readonly PreferenceKey<bool> DoNotLogEvents =
            new PreferenceKey<bool>("dontLogEvents");
        public static readonly PreferenceKey<bool> DoNotLogExceptions =
            new PreferenceKey<bool>("dontLogExceptions");
        public static readonly PreferenceKey<bool> DoNotLogSyncReports =
            new PreferenceKey<bool>("dontLogSyncReports");
        public static readonly PreferenceKey<bool> DoNotLogCrashReports =
            new PreferenceKey<bool>("dontLogCrashReports");

        public static readonly PreferenceKey<string> ParseInstallationObjectId =
            new PreferenceKey<string>("ParseInstallationObjectId");

        public static readonly PreferenceKey<string> CurrentBabyUUID =
            new PreferenceKey<string>("CurrentBabyUUID");
    }

    public class PreferenceKey<T> {
        public string Key { get; private set; } 

        internal PreferenceKey(string key) {
            Key = key;
        }
    }
}

