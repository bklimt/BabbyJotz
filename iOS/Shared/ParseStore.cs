﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Parse;

#if __IOS__
using MonoTouch.Foundation;
using MonoTouch.UIKit;
#else
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
#endif

namespace BabbyJotz.iOS {
    public class ParseStore : ICloudStore {
        private IPreferences Preferences { get; set; }
        private string InstanceUuid { get; set; }

        #region Parse Ids
        private string ApplicationId { get; set; }
        private string RestApiKey { get; set; }
        #endregion

        #region Current User State

        private object mutex;

        private string sessionToken;
        private string SessionToken {
            get {
                lock (mutex) {
                    return sessionToken;
                }
            }
            set {
                lock (mutex) {
                    sessionToken = value;
                    Preferences.Set(PreferenceKey.ParseSessionToken, sessionToken);
                }
            }
        }

        private string userName;
        public string UserName {
            get {
                lock (mutex) {
                    return userName;
                }
            }
            private set {
                lock (mutex) {
                    userName = value;
                    Preferences.Set(PreferenceKey.ParseUserName, userName);
                }
            }
        }

        private string userId;
        public string UserId {
            get {
                lock (mutex) {
                    return userId;
                }
            }
            private set {
                lock (mutex) {
                    userId = value;
                    Preferences.Set(PreferenceKey.ParseUserObjectId, userId);
                }
            }
        }

        #endregion
        #region Feature Preferences

        private bool LogEvents { get; set; }
        private bool LogExceptions { get; set; }
        private bool LogSyncReports { get; set; }

        #endregion

        #if __ANDROID__
        private Context Context { get; set; }
        #endif

        public ParseStore(
            #if __ANDROID__
            Context context,
            #endif
            IPreferences prefs
        ) {
            #if __ANDROID__
            Context = context.ApplicationContext;
            #endif
            Preferences = prefs;
            InstanceUuid = Guid.NewGuid().ToString("D");

            mutex = new object();
            sessionToken = prefs.Get(PreferenceKey.ParseSessionToken);
            userName = prefs.Get(PreferenceKey.ParseUserName);
            userId = prefs.Get(PreferenceKey.ParseUserObjectId);

            LogEvents = !prefs.Get(PreferenceKey.DoNotLogEvents);
            LogExceptions = !prefs.Get(PreferenceKey.DoNotLogExceptions);
            LogSyncReports = !prefs.Get(PreferenceKey.DoNotLogSyncReports);

            ParseClient.Initialize(
                "dRJrkKFywmUEYJx10K96Sw848juYyFF01Zlno6Uf",
                "0ICNGpRDtEswmZw8E3nfS08W8RNWbFLExIIw2IvS");

            ApplicationId = "dRJrkKFywmUEYJx10K96Sw848juYyFF01Zlno6Uf";
            RestApiKey = "YP2slH5715xfScvCG64cediDeZzCre4ytxgTyn2l";

            try {
                ParseAnalytics.TrackAppOpenedAsync();
            } catch (Exception e) {
                // Well, we tried our best.
                LogException("TrackAppOpenedAsync", e);
            }

            UpdateConfig();
        }

        private async void UpdateConfig() {
            try {
                var config = await ParseConfig.GetAsync();

                bool value;
                lock (mutex) {
                    if (config.TryGetValue<bool>("logEvents", out value)) {
                        LogEvents = value;
                        Preferences.Set(PreferenceKey.DoNotLogEvents, !value);
                    }
                    if (config.TryGetValue<bool>("logExceptions", out value)) {
                        LogExceptions = value;
                        Preferences.Set(PreferenceKey.DoNotLogExceptions, !value);
                    }
                    if (config.TryGetValue<bool>("logSyncReports", out value)) {
                        LogSyncReports = value;
                        Preferences.Set(PreferenceKey.DoNotLogSyncReports, !value);
                    }
                    if (config.TryGetValue<bool>("logCrashReports", out value)) {
                        Preferences.Set(PreferenceKey.DoNotLogCrashReports, !value);
                    }
                }
            } catch (Exception e) {
                // Oh well...
                LogException("UpdateConfig", e);
                Console.WriteLine("Unable to update config: {0}\n", e);
            }
        }

        #region Utils

        private class CloudResult<T> {
            public T Result { get; set; }
        }

        private class SaveResult {
            public string ObjectId { get; set; }
        }

        // TODO: Support cancellation tokens.
        private async Task<T> RunCloudFunctionAsync<T>(string name, Dictionary<string, object> parameters) {
            var encoding = new UTF8Encoding();
            var requestBodyString = JsonConvert.SerializeObject(parameters);
            var requestBodyBytes = encoding.GetBytes(requestBodyString);

            string sessionToken = null;
            lock (mutex) {
                sessionToken = SessionToken;
            }

            var client = WebRequest.Create(new Uri("https://api.parse.com/1/functions/" + name));
            client.Method = WebRequestMethods.Http.Post;
            client.ContentType = "application/json";
            client.ContentLength = requestBodyBytes.Length;
            client.Headers.Add("X-Parse-Application-Id", ApplicationId);
            client.Headers.Add("X-Parse-REST-API-Key", RestApiKey);
            if (sessionToken != null) {
                client.Headers.Add("X-Parse-Session-Token", sessionToken);
            }
            using (var stream = await client.GetRequestStreamAsync()) {
                await stream.WriteAsync(requestBodyBytes, 0, requestBodyBytes.Length);
            }
            HttpWebResponse response = null;
            try {
                response = await client.GetResponseAsync() as HttpWebResponse;
            } catch (WebException we) {
                response = we.Response as HttpWebResponse;
            }
            using (var stream = response.GetResponseStream()) {
                var reader = new StreamReader(stream);
                var responseBodyString = await reader.ReadToEndAsync();
                if (response.StatusCode == HttpStatusCode.OK) {
                    var result = JsonConvert.DeserializeObject<CloudResult<T>>(responseBodyString);
                    return result.Result;
                } else {
                    var e = JsonConvert.DeserializeObject<CloudException>(responseBodyString);
                    throw e;
                }
            }
        }

        #endregion
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

            var obj = new Dictionary<string, object>();
            obj["uuid"] = baby.Uuid;
            obj["name"] = baby.Name;
            obj["birthday"] = baby.Birthday;
            obj["showBreastfeeding"] = baby.ShowBreastfeeding;
            obj["showPumped"] = baby.ShowPumped;
            obj["showFormula"] = baby.ShowFormula;
            obj["deleted"] = baby.Deleted;

            if (baby.ObjectId != null) {
                obj["objectId"] = baby.ObjectId;
            }

            if (baby.ProfilePhoto != null) {
                obj["profilePhotoUuid"] = baby.ProfilePhoto.Uuid;
            } else {
                // TODO: Well, that's not right at all.
                obj.Remove("profilePhotoUuid");dasd
            }

            var result = await RunCloudFunctionAsync<SaveResult>("saveBaby", obj, cancellationToken);
            baby.ObjectId = result.ObjectId;
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

        public async Task LogInAsync(string username, string password) {
            LogOut();
            try {
                var result = await RunCloudFunctionAsync<Dictionary<string, string>>(
                    "login", new Dictionary<string, object> {
                        { "username", username },
                        { "password", password }
                    });
                UserId = result["objectId"];
                SessionToken = result["sessionToken"];
                UserName = username;
                await ParseUser.BecomeAsync(SessionToken);
                RegisterForPush();
            } finally {
                if (UserChanged != null) {
                    UserChanged(this, EventArgs.Empty);
                }
            }
        }

        public async Task SignUpAsync(string username, string password) {
            LogOut();
            try {
                var result = await RunCloudFunctionAsync<Dictionary<string, string>>(
                    "signup", new Dictionary<string, object> {
                        { "username", username },
                        { "password", password },
                        { "email", username }
                    });
                UserId = result["objectId"];
                SessionToken = result["sessionToken"];
                UserName = username;
                await ParseUser.BecomeAsync(SessionToken);
                RegisterForPush();
            } finally {
                if (UserChanged != null) {
                    UserChanged(this, EventArgs.Empty);
                }
            }
        }

        public void LogOut() {
            try {
                SessionToken = null;
                UserName = null;
                UserId = null;
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
            await RunCloudFunctionAsync<bool>("invite", new Dictionary<string, object>() {
                { "username", username },
                { "babyUuid", baby.Uuid }
            });
        }

        public async Task<List<Invite>> GetInvitesAsync() {
            var results = await RunCloudFunctionAsync<Dictionary<string, List<Invite>>>(
                              "listInvites", new Dictionary<string, object>());
            return results["babies"];
        }

        public async Task AcceptInviteAsync(Invite invite) {
            if (invite.Id == null) {
                return;
            }
            await RunCloudFunctionAsync<bool>("acceptInvite", new Dictionary<string, object>() {
                { "inviteId", invite.Id }
            });
        }

        public async Task UnlinkAsync(Baby baby) {
            await RunCloudFunctionAsync<bool>("unlink", new Dictionary<string, object>() {
                { "babyUuid", baby.Uuid }
            });
        }

        #endregion
        #region Push Notifications

        // Call this to initiate the push process.
        private void RegisterForPush() {
            LogEvent("ParseStore.RegisterForPush");
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
            LogEvent("ParseStore.UnregisterForPush");
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
            LogEvent("ParseStore.RegisterForPushAsync");
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

            var systemData = AddSystemData(new Dictionary<string, object>());
            foreach (var data in systemData) {
                obj[data.Key] = data.Value;
            }

            await obj.SaveAsync();
            Preferences.Set(PreferenceKey.ParseInstallationObjectId, obj.ObjectId);
        }

        #endregion
        #region Logging

        private Dictionary<string, object> AddSystemData(Dictionary<string, object> dict) {
            #if __IOS__
            var info = NSBundle.MainBundle.InfoDictionary;
            dict.Add("platform", "ios");
            dict.Add("osVersionString", NSProcessInfo.ProcessInfo.OperatingSystemVersionString);
            dict.Add("osName", NSProcessInfo.ProcessInfo.OperatingSystemName);
            dict.Add("displayName", ((NSString)info["CFBundleDisplayName"]).ToString());
            dict.Add("shortVersionString", ((NSString)info["CFBundleShortVersionString"]).ToString());
            dict.Add("version", ((NSString)info["CFBundleVersion"]).ToString());
            #else
            var pkgInfo = Context.PackageManager.GetPackageInfo(Context.PackageName, 0);
            dict.Add("platform", "android");
            dict.Add("packageName", Context.PackageName);
            dict.Add("versionName", pkgInfo.VersionName);
            dict.Add("versionCodeString", String.Format("{0}", pkgInfo.VersionCode));
            dict.Add("androidVersionCodename", Build.VERSION.Codename);
            dict.Add("androidVersionIncremental", Build.VERSION.Incremental);
            dict.Add("androidVersionRelease", Build.VERSION.Release);
            #endif
            return dict;
        }

        public async Task LogSyncReportAsync(string report) {
            if (!LogSyncReports) {
                return;
            }
            LogEvent("ParseStore.LogSyncReportAsync");
            await RunCloudFunctionAsync<bool>("logSyncReport",
                AddSystemData(new Dictionary<string, object>() {
                    { "report", report },
                    { "instance", InstanceUuid },
                }));
        }

        public async void LogException(string tag, Exception e) {
            if (!LogExceptions) {
                return;
            }
            try {
                await RunCloudFunctionAsync<bool>("logException",
                    AddSystemData(new Dictionary<string, object>() {
                        { "exception", e.ToString() },
                        { "instance", InstanceUuid },
                        { "tag", tag }
                    }));
            } catch (Exception e2) {
                // Oh, so ironic.
                Console.WriteLine("Unable to log exception: {0}", e2);
            }
        }

        public async void LogEvent(string name) {
            if (!LogEvents) {
                return;
            }
            try {
                await RunCloudFunctionAsync<bool>("logEvent",
                    AddSystemData(new Dictionary<string, object>() {
                        { "name", name },
                        { "instance", InstanceUuid }
                    }));
            } catch (Exception e) {
                LogException("LogEvent", e);
                Console.WriteLine("Unable to log event: {0}", e);
            }
        }

        #endregion
    }
}

