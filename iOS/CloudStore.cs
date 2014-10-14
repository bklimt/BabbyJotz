using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Parse;

#if __IOS__
using MonoTouch.Foundation;
using MonoTouch.UIKit;
#endif

#if __ANDROID__
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
#endif

namespace BabbyJotz.iOS {
    public class CloudStore {
        private LogEntry CreateLogEntryFromParseObject(ParseObject obj) {
            DateTime? deleted = null;
            obj.TryGetValue<DateTime?>("deleted", out deleted);

            return new LogEntry(obj.Get<String>("uuid")) {
                DateTime = obj.Get<DateTime>("time"),
                Text = obj.Get<string>("text"),
                IsPoop = obj.Get<bool>("poop"),
                IsAsleep = obj.Get<bool>("asleep"),
                FormulaEaten = (decimal)obj.Get<double>("formula"),
                Deleted = deleted,
                ObjectId = obj.ObjectId as string
            };
        }

        #if __IOS__
        private void RegisterForPushIOS() {
            if (new Version(UIDevice.CurrentDevice.SystemVersion) < new Version(8, 0)) {
                var notificationTypes =
                    UIRemoteNotificationType.Alert |
                    UIRemoteNotificationType.Badge |
                    UIRemoteNotificationType.Sound;

                UIApplication.SharedApplication.RegisterForRemoteNotificationTypes(notificationTypes);
            } else {
                var notificationTypes =
                    UIUserNotificationType.Alert |
                    UIUserNotificationType.Badge |
                    UIUserNotificationType.Sound;

                var userNoticationSettings = UIUserNotificationSettings.GetSettingsForTypes(
                    notificationTypes, new NSSet());

                UIApplication.SharedApplication.RegisterUserNotificationSettings(userNoticationSettings);
                UIApplication.SharedApplication.RegisterForRemoteNotifications();
            }
        }
        #endif

        #if __ANDROID__
        private void RegisterForPushAndroid() {
            // From: https://groups.google.com/forum/#!msg/parse-developers/ku8-r91_o6s/Hk_YZQVgK6MJ
            var context = Application.Context;
            Intent intent = new Intent("com.google.android.c2dm.intent.REGISTER");
            intent.SetPackage("com.google.android.gsf");
            intent.PutExtra("app", PendingIntent.GetBroadcast(context, 0, new Intent(), 0));
            intent.PutExtra("sender", "1076345567071");
            // intent.PutExtra("sender", "earnest-math-732");
            context.StartService(intent);
        }
        #endif

        private void RegisterForPush() {
            #if __IOS__
            RegisterForPushIOS();
            #endif
            #if __ANDROID__
            RegisterForPushAndroid();
            #endif
        }

        #if __IOS__
        private void UnregisterForPushIOS() {
            UIApplication.SharedApplication.UnregisterForRemoteNotifications();
        }
        #endif

        #if __ANDROID__
        private void UnregisterForPushAndroid() {
            var context = Application.Context;
            // TODO: Implicit intents are unsafe.
            Intent intent = new Intent("com.google.android.c2dm.intent.UNREGISTER");
            intent.PutExtra("app", PendingIntent.GetBroadcast(context, 0, new Intent(), 0));
            context.StartService(intent);
        }
        #endif
        
        private void UnregisterForPush() {
            #if __IOS__
            UnregisterForPushIOS();
            #endif
            #if __ANDROID__
            UnregisterForPushAndroid();
            #endif
        }

        public CloudStore() {
        }

        public string UserName {
            get {
                return ParseUser.CurrentUser != null ? ParseUser.CurrentUser.Username : null;
            }
        }

        public string UserId {
            get {
                return ParseUser.CurrentUser != null ? ParseUser.CurrentUser.ObjectId : null;
            }
        }

        // Returns the ObjectId of the object.
        public async Task SaveAsync(LogEntry entry) {
            if (ParseUser.CurrentUser == null) {
                throw new InvalidOperationException("Tried to sync without logging in.");
            }

            var obj = entry.ObjectId != null
                ? ParseObject.CreateWithoutData("LogEntry", entry.ObjectId)
                : ParseObject.Create("LogEntry");
            obj["uuid"] = entry.Uuid;
            obj["time"] = entry.DateTime;
            obj["text"] = entry.Text;
            obj["poop"] = entry.IsPoop;
            obj["asleep"] = entry.IsAsleep;
            obj["formula"] = (double)entry.FormulaEaten;
            obj["deleted"] = entry.Deleted;
            obj.ACL = new ParseACL(ParseUser.CurrentUser);
            await obj.SaveAsync();
            entry.ObjectId = obj.ObjectId;
        }

        public async Task<Tuple<IEnumerable<LogEntry>, DateTime?>> FetchChangesAsync(DateTime? lastUpdatedAt) {
            var query = from entry in ParseObject.GetQuery("LogEntry")
                        orderby entry.UpdatedAt ascending
                        select entry;
            query = query.Limit(1000);
            if (lastUpdatedAt != null) {
                query = query.WhereGreaterThanOrEqualTo("updatedAt", lastUpdatedAt.Value);
            }

            var results = (await query.FindAsync()).ToList();
            var objs = from result in results
                       select CreateLogEntryFromParseObject(result);
            if (results.Count > 0) {
                lastUpdatedAt = results[results.Count - 1].UpdatedAt;
            }
            return new Tuple<IEnumerable<LogEntry>, DateTime?>(objs.ToList(), lastUpdatedAt);
        }

        public async Task LogInAsync(string username, string password) {
            // Fix this weird issue with a NPE half the time when logging in.
            LogOut();
            await ParseUser.LogInAsync(username, password);
            RegisterForPush();
        }

        public async Task SignUpAsync(string username, string password) {
            ParseUser.LogOut();
            var user = new ParseUser();
            user.Username = username;
            user.Password = password;
            user.ACL = new ParseACL();
            await user.SignUpAsync();
        }

        public void LogOut() {
            ParseUser.LogOut();
            UnregisterForPush();
        }
    }
}

