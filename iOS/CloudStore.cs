using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Parse;

#if __IOS__
using MonoTouch.Foundation;
using MonoTouch.UIKit;
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

        private void RegisterForPush() {
            #if __IOS__
            RegisterForPushIOS();
            #endif
        }

        #if __IOS__
        private void UnregisterForPushIOS() {
            UIApplication.SharedApplication.UnregisterForRemoteNotifications();
        }
        #endif
        
        private void UnregisterForPush() {
            #if __IOS__
            UnregisterForPushIOS();
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

