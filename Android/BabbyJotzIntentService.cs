﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

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
                    var app = (BabbyJotzApplication)Application;
                    await app.RootViewModel.CloudStore.RegisterForPushAsync(registrationId);
                } catch (Exception e) {
                    Console.WriteLine("Unable to save device token. {0}", e);
                }
            }
        }

        private class ParsePushData {
            public string Alert { get; set; }
            public string ObjectId { get; set; }
        }

        private async Task HandleMessage(Context context, Intent intent) {
            var app = (BabbyJotzApplication)Application;
            try {
                await app.RootViewModel.SyncAsync("HandleMessage", false);
            } catch {
                // Well, we tried. :-(
            }

            if (!app.RootViewModel.NotificationsEnabled) {
                return;
            }

            var json = intent.GetStringExtra("data") ?? "{}";
            var data = JsonConvert.DeserializeObject<ParsePushData>(json);
            var title = "New Babby Jotz";
            var desc = data.Alert;
            // var objectId = data.ObjectId;
            // int id = objectId.GetHashCode();
            int id = 123;

            var manager = (NotificationManager)GetSystemService(Context.NotificationService);

            var unreadEnumerator = await app.RootViewModel.LocalStore.FetchUnreadAsync();
            var unread = (from entry in unreadEnumerator
                          select entry).ToList();
            if (unread.Count == 0) {
                manager.Cancel(id);
                return;
            } else if (unread.Count > 1) {
                title = String.Format("{0} New Babby Jotz", unread.Count);
            }

            var resultIntent = new Intent(this, typeof(SplashActivity));

            var stackBuilder = global::Android.Support.V4.App.TaskStackBuilder.Create(this);
            stackBuilder.AddParentStack(Java.Lang.Class.FromType(typeof(SplashActivity)));
            stackBuilder.AddNextIntent(resultIntent);

            var pendingIntent = stackBuilder.GetPendingIntent(0, (int)PendingIntentFlags.UpdateCurrent);

            var builder = new NotificationCompat.Builder(context)
                .SetContentTitle(title)
                .SetSmallIcon(Resource.Drawable.ic_stat_notify)
                .SetContentText(desc)
                .SetAutoCancel(true)
                .SetContentIntent(pendingIntent);

            if (app.RootViewModel.Vibrate) {
                builder.SetVibrate(new long[] { 0, 750 });
            }

            var notification = builder.Build();

            // TODO: Does this run before the app is opened?

            manager.Notify(id, notification);
        }
    }
}

