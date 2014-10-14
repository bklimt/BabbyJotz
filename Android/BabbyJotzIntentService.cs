using System;
using System.Threading.Tasks;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Widget;
using Android.Support.V4.App;

using Newtonsoft.Json;
using Parse;

namespace BabbyJotz.Android {
    [Service]
    public class BabbyJotzIntentService : IntentService {
        private static PowerManager.WakeLock wakeLock;
        private static object mutex = new object();

        public static void RunIntentInService(Context context, Intent intent) {
            lock (mutex) {
                if (wakeLock == null) {
                    var pm = PowerManager.FromContext(context);
                    wakeLock = pm.NewWakeLock(WakeLockFlags.Partial, "BabbyJotz");
                }
            }

            wakeLock.Acquire();
            intent.SetClass(context, typeof(BabbyJotzIntentService));
            context.StartService(intent);
        }

        protected override void OnHandleIntent(Intent intent) {
            try {
                Context context = this.ApplicationContext;
                string action = intent.Action;

                if (action.Equals("com.google.android.c2dm.intent.REGISTRATION")) {
                    HandleRegistration(context, intent).Wait();
                } else if (action.Equals("com.google.android.c2dm.intent.RECEIVE")) {
                    HandleMessage(context, intent).Wait();
                }
            } finally {
                lock (mutex) {
                    if (wakeLock != null) {
                        wakeLock.Release();
                    }
                }
            }
        }

        private async Task HandleRegistration(Context context, Intent intent) {
            string registrationId = intent.GetStringExtra("registration_id");
            string error = intent.GetStringExtra("error");
            // string unregistered = intent.GetStringExtra("unregistered");

            if (error != null) {
                // TODO: Well, this won't work.
                var message = String.Format("Unable to register for push: {0}", error);
                Toast.MakeText(context, message, ToastLength.Short).Show();
            }

            if (registrationId != null) {
                try {
                    var prefsKey = "ParseInstallationObjectId";
                    var prefs = GetSharedPreferences("default", FileCreationMode.Private);
                    var objectId = prefs.GetString(prefsKey, null);

                    // TODO: Move this logic into CloudStore.
                    var obj = (objectId == null)
                        ? ParseObject.Create("_Installation")
                        : ParseObject.CreateWithoutData("_Installation", objectId);
                    // From https://groups.google.com/forum/#!topic/parse-developers/pPatDDkzcEc
                    // And https://gist.github.com/gfosco/a526cbc3061398d50e8b
                    obj["deviceToken"] = registrationId;
                    obj["deviceType"] = "android";
                    obj["pushType"] = "gcm";
                    obj["appIdentifier"] = "com.bklimt.BabbyJotz";
                    // obj["timeZone"] = TimeZone.CurrentTimeZone.ToString();
                    obj["appName"] = "BabbyJotz";
                    obj["appVersion"] = "1.0.0";
                    obj["parseVersion"] = "1.3.0";
                    // TODO: It sucks having to hook into Parse directly here.
                    // It would be better to go through the CloudStore interface.
                    obj["userId"] = ParseUser.CurrentUser.ObjectId;
                    await obj.SaveAsync();

                    prefs.Edit().PutString(prefsKey, obj.ObjectId).Commit();
                } catch (Exception e) {
                    Console.WriteLine("Unable to save device token. {0}", e);
                }
            }
        }

        private class ParsePushData {
            public string Alert { get; set; }
        }

        private async Task HandleMessage(Context context, Intent intent) {
            var app = (BabbyJotzApplication)Application;
            try {
                await app.RootViewModel.SyncAsync();
            } catch {
                // Well, we tried. :-(
            }

            var json = intent.GetStringExtra("data") ?? "{}";
            var data = JsonConvert.DeserializeObject<ParsePushData>(json);
            var desc = data.Alert ?? "<empty>";

            var notification = new NotificationCompat.Builder(context)
                .SetContentTitle("New BabbyJotz Event")
                .SetSmallIcon(Resource.Drawable.ic_launcher)
                .SetContentText(desc)
                .Build();

            // TODO: An intent to open the app.
            // TODO: A better title.
            // TODO: Vibrate?
            // TODO: Collapse?
            // TODO: Don't show alert if the app is open?
            // TODO: Settings for notifications.
            // TODO: Does this run before the app is opened?
            // TODO: Deal with deleted items correctly.

            var manager = (NotificationManager)GetSystemService(Context.NotificationService);
            // TODO: Figure out how to deal with this id.
            manager.Notify(314159265, notification);
        }
    }
}

