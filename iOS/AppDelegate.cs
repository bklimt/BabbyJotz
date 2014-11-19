using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

using Xamarin.Forms;
using Xamarin.Forms.Labs.iOS.Services.Media;
using Xamarin.Forms.Platform.iOS;
using MonoTouch.Foundation;
using MonoTouch.UIKit;

using HockeyApp;
using Parse;

namespace BabbyJotz.iOS {
    [Register("AppDelegate")]
    public partial class AppDelegate : UIApplicationDelegate {
        UIWindow window;
        RootViewModel model;

        public MediaPicker UnusedMediaPicker() {
            return new MediaPicker();
        }

        private void UpdateForTheme(Theme theme) {
            UITabBar.Appearance.BarTintColor = theme.Title.ToUIColor();
            UITabBar.Appearance.TintColor = theme.Text.ToUIColor();
            // TODO: Does this even do anything? The unselected icons are invisible.
            UITabBar.Appearance.BackgroundColor = theme.Background.ToUIColor();

            UINavigationBar.Appearance.TintColor = theme.ButtonText.ToUIColor();
            UINavigationBar.Appearance.BackgroundColor = theme.Background.ToUIColor();

            if (theme == Theme.Dark) {
                UIApplication.SharedApplication.StatusBarStyle =
                    UIStatusBarStyle.Default;
            } else {
                UIApplication.SharedApplication.StatusBarStyle =
                    UIStatusBarStyle.LightContent;
            }
        }

        private void setupCrashReporting() {
            // We MUST wrap our setup in this block to wire up
            // Mono's SIGSEGV and SIGBUS signals.
            HockeyApp.Setup.EnableCustomCrashReporting(() => {
                var manager = BITHockeyManager.SharedHockeyManager;
                manager.Configure("9e9451e734b85c44a7d83a6d745df265");
                manager.StartManager();
                manager.Authenticator.AuthenticateInstallation();

                // Rethrow any unhandled .NET exceptions as native iOS 
                // exceptions so the stack traces appear nicely in HockeyApp.
                AppDomain.CurrentDomain.UnhandledException += (sender, e) => 
                    Setup.ThrowExceptionAsNative(e.ExceptionObject);

                TaskScheduler.UnobservedTaskException += (sender, e) => 
                    Setup.ThrowExceptionAsNative(e.Exception);
            });
        }

        public override bool FinishedLaunching(UIApplication app, NSDictionary options) {
            var prefs = new Preferences();
            if (!prefs.Get(PreferenceKey.DoNotLogCrashReports)) {
                setupCrashReporting();
            }

            window = new UIWindow(UIScreen.MainScreen.Bounds);

            Forms.Init();

            var cloudStore = new ParseStore(prefs);
            var localStore = new LocalStore();
            model = new RootViewModel(localStore, cloudStore, prefs);
            model.PropertyChanged += (object sender, PropertyChangedEventArgs e) => {
                if (e.PropertyName == "Theme") {
                    UpdateForTheme(model.Theme);
                }
            };
            UpdateForTheme(model.Theme);

            window.RootViewController = App.GetMainPage(model).CreateViewController();
            window.MakeKeyAndVisible();

            return true;
        }

        public override void WillEnterForeground(UIApplication application) {
            // NOTE: Don't call the base implementation on a Model class
            // see http://docs.xamarin.com/guides/ios/application_fundamentals/delegates,_protocols,_and_events
            model.TryToSyncEventually("Will Enter Foreground");
        }

        public override void DidRegisterUserNotificationSettings(
            UIApplication application, UIUserNotificationSettings notificationSettings) {
            // NOTE: Don't call the base implementation on a Model class
            // see http://docs.xamarin.com/guides/ios/application_fundamentals/delegates,_protocols,_and_events
            // UIApplication.SharedApplication.RegisterForRemoteNotifications();
        }

        public override async void RegisteredForRemoteNotifications(
            UIApplication application, NSData deviceToken) {
            // NOTE: Don't call the base implementation on a Model class
            // see http://docs.xamarin.com/guides/ios/application_fundamentals/delegates,_protocols,_and_events
            try {
                // From https://groups.google.com/forum/#!topic/parse-developers/pPatDDkzcEc
                // And https://gist.github.com/gfosco/a526cbc3061398d50e8b
                string dt = deviceToken.ToString().Replace("<", "").Replace(">", "").Replace(" ", "");
                await model.CloudStore.RegisterForPushAsync(dt);
            } catch (Exception e) {
                model.CloudStore.LogException("RegisteredForRemoteNotifications", e);
                Console.WriteLine("Unable to save device token. {0}", e);
            }
        }

        public override void FailedToRegisterForRemoteNotifications(
            UIApplication application, NSError error) {
            new UIAlertView("Error registering for push notifications.",
                error.LocalizedDescription, null, "OK", null).Show();
        }

        public override void DidReceiveRemoteNotification(
            UIApplication application, NSDictionary userInfo, Action<UIBackgroundFetchResult> completionHandler) {
            // NOTE: Don't call the base implementation on a Model class
            // see http://docs.xamarin.com/guides/ios/application_fundamentals/delegates,_protocols,_and_events
            model.TryToSyncEventually("Did Receive Remote Notification");
        }

        public override void HandleAction(
            UIApplication application, string actionIdentifier, NSDictionary remoteNotificationInfo, Action completionHandler) {
            // NOTE: Don't call the base implementation on a Model class
            // see http://docs.xamarin.com/guides/ios/application_fundamentals/delegates,_protocols,_and_events
            model.TryToSyncEventually("Handle Action");
        }
    }
}

