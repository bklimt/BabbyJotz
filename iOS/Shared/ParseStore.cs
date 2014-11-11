using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Parse;

#if __IOS__
using MonoTouch.Foundation;
using MonoTouch.UIKit;
#else
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
#endif

namespace BabbyJotz.iOS {
    public class ParseStore : ICloudStore {
        private IPreferences Preferences { get; set; }

        public ParseStore(IPreferences prefs) {
            Preferences = prefs;

            ParseClient.Initialize(
                "dRJrkKFywmUEYJx10K96Sw848juYyFF01Zlno6Uf",
                "0ICNGpRDtEswmZw8E3nfS08W8RNWbFLExIIw2IvS");

            try {
                ParseAnalytics.TrackAppOpenedAsync();
            } catch (Exception) {
                // Well, we tried our best.
            }
        }

        #region LogEntry

        private LogEntry CreateLogEntry(ParseObject obj) {
            DateTime? deleted = null;
            obj.TryGetValue<DateTime?>("deleted", out deleted);

            // It doesn't really matter if it's a full instance. We only need to know the uuid.
            var baby = new Baby(obj.Get<string>("babyUuid"));

            return new LogEntry(baby, obj.Get<String>("uuid")) {
                DateTime = obj.Get<DateTime>("datetime").ToLocalTime(),
                Text = obj.Get<string>("text"),
                IsPoop = obj.Get<bool>("poop"),
                IsAsleep = obj.Get<bool>("asleep"),
                FormulaEaten = obj.Get<double>("formula"),
                PumpedEaten = obj.ContainsKey("pumped") ? obj.Get<double>("pumped") : 0.0,
                RightBreastEaten = obj.ContainsKey("rightBreast")
                    ? TimeSpan.FromMinutes(obj.Get<double>("rightBreast"))
                    : TimeSpan.FromMinutes(0.0),
                LeftBreastEaten = obj.ContainsKey("leftBreast")
                    ? TimeSpan.FromMinutes(obj.Get<double>("leftBreast"))
                    : TimeSpan.FromMinutes(0.0),
                Deleted = deleted,
                ObjectId = obj.ObjectId as string
            };
        }

        public async Task SaveAsync(LogEntry entry, CancellationToken cancellationToken) {
            if (ParseUser.CurrentUser == null) {
                throw new InvalidOperationException("Tried to sync without logging in.");
            }

            var obj = entry.ObjectId != null
                ? ParseObject.CreateWithoutData("LogEntry", entry.ObjectId)
                : ParseObject.Create("LogEntry");
            obj["uuid"] = entry.Uuid;
            obj["babyUuid"] = entry.Baby.Uuid;
            obj["time"] = entry.DateTime;  // This field is basically deprecated.
            obj["datetime"] = entry.DateTime.ToUniversalTime();
            obj["text"] = entry.Text;
            obj["poop"] = entry.IsPoop;
            obj["asleep"] = entry.IsAsleep;
            obj["formula"] = entry.FormulaEaten;
            obj["pumped"] = entry.PumpedEaten;
            obj["leftBreast"] = entry.LeftBreastEaten.TotalMinutes;
            obj["rightBreast"] = entry.RightBreastEaten.TotalMinutes;
            obj["deleted"] = entry.Deleted;
            obj.ACL = new ParseACL(ParseUser.CurrentUser);
            await obj.SaveAsync(cancellationToken);
            entry.ObjectId = obj.ObjectId;
        }

        #endregion
        #region Photo

        private async Task<byte[]> DownloadFileAsync(ParseFile file) {
            var client = new WebClient();
            var bytes = await client.DownloadDataTaskAsync(file.Url);
            return bytes;
        }

        private async Task<Photo> CreatePhotoAsync(ParseObject obj) {
            DateTime? deleted = null;
            obj.TryGetValue<DateTime?>("deleted", out deleted);

            // It doesn't really matter if it's a full instance. We only need to know the uuid.
            var baby = new Baby(obj.Get<string>("babyUuid"));

            var photo = new Photo(baby, obj.Get<String>("uuid")) {
                Deleted = deleted,
                ObjectId = obj.ObjectId as string
            };

            var file = obj.Get<ParseFile>("thumbnail200");
            photo.Bytes = await DownloadFileAsync(file);

            return photo;
        }

        public async Task SaveAsync(Photo photo, CancellationToken cancellationToken) {
            var obj = photo.ObjectId != null
                ? ParseObject.CreateWithoutData("Photo", photo.ObjectId)
                : ParseObject.Create("Photo");
            obj["uuid"] = photo.Uuid;
            obj["babyUuid"] = photo.Baby.Uuid;
            obj["deleted"] = photo.Deleted;

            var file = new ParseFile("profilePhoto", photo.Bytes);
            await file.SaveAsync(cancellationToken);
            obj["file"] = file;

            // Cloud Code will take care of the ACL.

            await obj.SaveAsync(cancellationToken);
            photo.ObjectId = obj.ObjectId;
        }

        #endregion
        #region Baby

        private Baby CreateBaby(ParseObject obj) {
            DateTime? deleted = null;
            obj.TryGetValue<DateTime?>("deleted", out deleted);
            var baby = new Baby(obj.Get<String>("uuid")) {
                Name = obj.Get<string>("name"),
                Birthday = obj.Get<DateTime>("birthday"),
                ShowBreastfeeding = obj.Get<bool>("showBreastfeeding"),
                ShowPumped = obj.Get<bool>("showPumped"),
                ShowFormula = obj.Get<bool>("showFormula"),
                Deleted = deleted,
                ObjectId = obj.ObjectId as string
            };
            if (obj.ContainsKey("profilePhotoUuid")) {
                baby.ProfilePhoto = new Photo(baby, obj.Get<string>("profilePhotoUuid"));
            }
            return baby;
        }

        public async Task SaveAsync(Baby baby, CancellationToken cancellationToken) {
            if (ParseUser.CurrentUser == null) {
                throw new InvalidOperationException("Tried to sync without logging in.");
            }

            var obj = baby.ObjectId != null
                ? ParseObject.CreateWithoutData("Baby", baby.ObjectId)
                : ParseObject.Create("Baby");
            obj["uuid"] = baby.Uuid;
            obj["name"] = baby.Name;
            obj["birthday"] = baby.Birthday;
            obj["showBreastfeeding"] = baby.ShowBreastfeeding;
            obj["showPumped"] = baby.ShowPumped;
            obj["showFormula"] = baby.ShowFormula;
            obj["deleted"] = baby.Deleted;

            if (baby.ProfilePhoto != null) {
                obj["profilePhotoUuid"] = baby.ProfilePhoto.Uuid;
            } else {
                obj.Remove("profilePhotoUuid");
            }

            // This will be overridden by Cloud Code, but may as well leave it here just in case.
            obj.ACL = new ParseACL(ParseUser.CurrentUser);

            await obj.SaveAsync(cancellationToken);
            baby.ObjectId = obj.ObjectId;
        }

        #endregion
        #region Syncing

        public async Task<List<Baby>> FetchAllBabiesAsync(CancellationToken cancellationToken) {
            var results = new List<Baby>();

            var query = new ParseQuery<ParseObject>("Baby");
            query = query.Limit(1000);
            var enumerator = await query.FindAsync(cancellationToken);
            var objs = await Task.Run(() => enumerator.ToList());
            foreach (var result in objs) {
                results.Add(CreateBaby(result));
            }

            return results;
        }

        private struct FetchSinceRequest<T> {
            public string ClassName;
            public Baby Baby;
            public int Limit;
            public DateTime? LastUpdatedAt;
            public Func<ParseObject, Task<T>> CreateAsync;
        }

        private async Task<CloudFetchSinceResponse<T>> FetchSinceAsync<T>(
            FetchSinceRequest<T> request, CancellationToken cancellationToken)  {

            var response = new CloudFetchSinceResponse<T>();
            response.Results = new List<T>();
            response.NewUpdatedAt = request.LastUpdatedAt;
            response.MaybeHasMore = false;

            var query = from obj in ParseObject.GetQuery(request.ClassName)
                where obj.Get<string>("babyUuid") == request.Baby.Uuid
                orderby obj.UpdatedAt ascending
                select obj;
            query = query.Limit(request.Limit);
            if (request.LastUpdatedAt != null) {
                // TODO: Well, technically this isn't exactly correct, because two items could have the
                // same updatedAt time. They could even go backwards if there's clock skew on the servers.
                // But there's nothing to be done about that, except maybe randomly do a full resync.
                query = query.WhereGreaterThan(
                    "updatedAt", request.LastUpdatedAt.Value);
            }

            var enumerator = await query.FindAsync(cancellationToken);
            var objs = await Task.Run(() => enumerator.ToList());
            foreach (var result in objs) {
                response.Results.Add(await request.CreateAsync(result));
            }
            if (response.Results.Count > 0) {
                response.NewUpdatedAt = objs[objs.Count - 1].UpdatedAt;
            }
            if (response.Results.Count == request.Limit) {
                response.MaybeHasMore = true;
            }

            return response;
        }

        public async Task<CloudFetchSinceResponse<LogEntry>> FetchEntriesSinceAsync(
            Baby baby, DateTime? lastUpdatedAt, CancellationToken cancellationToken) {

            var request = new FetchSinceRequest<LogEntry>() {
                ClassName = "LogEntry",
                Baby = baby,
                Limit = 200,
                LastUpdatedAt = lastUpdatedAt,
                CreateAsync = (obj) => Task.FromResult(CreateLogEntry(obj))
            };

            return await FetchSinceAsync(request, cancellationToken);
        }

        public async Task<CloudFetchSinceResponse<Photo>> FetchPhotosSinceAsync(
            Baby baby, DateTime? lastUpdatedAt, CancellationToken cancellationToken) {

            var request = new FetchSinceRequest<Photo>() {
                ClassName = "Photo",
                Baby = baby,
                Limit = 100,
                LastUpdatedAt = lastUpdatedAt,
                CreateAsync = CreatePhotoAsync
            };

            return await FetchSinceAsync(request, cancellationToken);
        }

        #endregion
        #region Log in / Log out

        public event EventHandler UserChanged;

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

        public async Task LogInAsync(string username, string password) {
            LogOut();
            try {
                var retries = 3;
                var runAgain = true;
                while (runAgain) {
                    runAgain = false;
                    try {
                        await ParseUser.LogInAsync(username, password);
                    } catch (NullReferenceException) {
                        retries--;
                        if (retries > 0) {
                            runAgain = true;
                        } else {
                            throw;
                        }
                    }
                }
                RegisterForPush();
            } finally {
                if (UserChanged != null) {
                    UserChanged(this, EventArgs.Empty);
                }
            }
        }

        public async Task SignUpAsync(string username, string password) {
            LogOut();
            var user = new ParseUser();
            user.Username = username;
            user.Password = password;
            user.Email = username;
            user.ACL = new ParseACL();
            try {
                var retries = 3;
                var runAgain = true;
                while (runAgain) {
                    runAgain = false;
                    try {
                        await user.SignUpAsync();
                    } catch (NullReferenceException) {
                        retries--;
                        if (retries > 0) {
                            runAgain = true;
                        } else {
                            throw;
                        }
                    }
                }
                RegisterForPush();
            } finally {
                if (UserChanged != null) {
                    UserChanged(this, EventArgs.Empty);
                }
            }
        }

        public void LogOut() {
            try {
                ParseUser.LogOut();
                UnregisterForPush();
            } finally {
                if (UserChanged != null) {
                    UserChanged(this, EventArgs.Empty);
                }
            }
        }

        #endregion
        #region Invites / Baby Sharing

        public async Task InviteAsync(string username, Baby baby) {
            await ParseCloud.CallFunctionAsync<bool>("invite", new Dictionary<string, object>() {
                { "username", username },
                { "babyUuid", baby.Uuid }
            });
        }

        public async Task<List<Invite>> GetInvitesAsync() {
            var results = await ParseCloud.CallFunctionAsync<Dictionary<string, object>>(
                              "listInvites", new Dictionary<string, object>());
            var babyList = results["babies"] as List<object>;
            var invites = new List<Invite>();
            foreach (var babyObj in babyList) {
                var baby = babyObj as Dictionary<string, object>;
                var inviteId = baby["inviteId"] as string;
                var babyName = baby["babyName"] as string;
                var babyUuid = baby["babyUuid"] as string;
                invites.Add(new Invite(inviteId, babyName, babyUuid));
            }
            return invites;
        }

        public async Task AcceptInviteAsync(Invite invite) {
            if (invite.Id == null) {
                return;
            }
            await ParseCloud.CallFunctionAsync<bool>("acceptInvite", new Dictionary<string, object>() {
                { "inviteId", invite.Id }
            });
        }

        public async Task UnlinkAsync(Baby baby) {
            await ParseCloud.CallFunctionAsync<bool>("unlink", new Dictionary<string, object>() {
                { "babyUuid", baby.Uuid }
            });
        }

        #endregion
        #region Push Notifications

        // Call this to initiate the push process.
        private void RegisterForPush() {
            #if __IOS__
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
            #else
                // From: https://groups.google.com/forum/#!msg/parse-developers/ku8-r91_o6s/Hk_YZQVgK6MJ
                var context = Application.Context;
                Intent intent = new Intent("com.google.android.c2dm.intent.REGISTER");
                intent.SetPackage("com.google.android.gsf");
                intent.PutExtra("app", PendingIntent.GetBroadcast(context, 0, new Intent(), 0));
                intent.PutExtra("sender", "1076345567071");
                // intent.PutExtra("sender", "earnest-math-732");
                context.StartService(intent);
            #endif
        }

        private void UnregisterForPush() {
            #if __IOS__
                UIApplication.SharedApplication.UnregisterForRemoteNotifications();
            #else
                var context = Application.Context;
                // TODO: Implicit intents are unsafe.
                Intent intent = new Intent("com.google.android.c2dm.intent.UNREGISTER");
                intent.PutExtra("app", PendingIntent.GetBroadcast(context, 0, new Intent(), 0));
                context.StartService(intent);
            #endif
        }

        // Call this once you get a device token.
        public async Task RegisterForPushAsync(string deviceToken) {
            // From https://groups.google.com/forum/#!topic/parse-developers/pPatDDkzcEc
            // And https://gist.github.com/gfosco/a526cbc3061398d50e8b
            var objectId = Preferences.Get(PreferenceKey.ParseInstallationObjectId);

            var obj = (objectId == null)
                ? ParseObject.Create("_Installation")
                : ParseObject.CreateWithoutData("_Installation", objectId);

            obj["deviceToken"] = deviceToken;
            obj["appIdentifier"] = "com.bklimt.BabbyJotz";
            // obj["timeZone"] = TimeZone.CurrentTimeZone.ToString();
            obj["appName"] = "BabbyJotz";
            obj["appVersion"] = "1.0.0";
            obj["parseVersion"] = "1.3.0";
            obj["userId"] = UserId;

            #if __IOS__
                obj["deviceType"] = "ios";
            #else
                obj["deviceType"] = "android";
                obj["pushType"] = "gcm";
            #endif

            await obj.SaveAsync();
            Preferences.Set(PreferenceKey.ParseInstallationObjectId, obj.ObjectId);
        }

        #endregion
        #region Logging

        public async Task LogSyncReportAsync(string report) {
            await ParseCloud.CallFunctionAsync<bool>("logSyncReport", new Dictionary<string, object>() {
                { "report", report }
            });
        }

        #endregion
    }
}

