using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;

namespace BabbyJotz {
    public class RootViewModel : BindableObject {
        public ILocalStore LocalStore { get; set; }
        public ICloudStore CloudStore { get; set; }
        private TaskQueue syncQueue = new TaskQueue();

        private static void SaveTheme(BindableObject obj) {
            var model = obj as RootViewModel;
            model.Preferences.Set(PreferenceKey.LightTheme, model.Theme == Theme.Light);
        }

        private static void SaveCurrentBaby(BindableObject obj) {
            var model = obj as RootViewModel;
            model.Preferences.Set(PreferenceKey.CurrentBabyUUID, model.Baby.Uuid);
        }

        private static void SaveNotificationsEnabled(BindableObject obj) {
            var model = obj as RootViewModel;
            model.Preferences.Set(PreferenceKey.DoNotNotify, !model.NotificationsEnabled);
        }

        private static void SaveVibrate(BindableObject obj) {
            var model = obj as RootViewModel;
            model.Preferences.Set(PreferenceKey.DoNotVibrate, !model.Vibrate);
        }

        public IPreferences Preferences { get; private set; }
        public ObservableCollection<Baby> Babies { get; private set; }
        public ObservableCollection<LogEntry> Entries { get; private set; }

        public static readonly BindableProperty DateProperty =
            BindableProperty.Create<RootViewModel, DateTime>(p => p.Date, default(DateTime));
        public DateTime Date {
            get { return (DateTime)base.GetValue(DateProperty); }
            set { SetValue(DateProperty, value); }
        }

        public static readonly BindableProperty BabyProperty =
            BindableProperty.Create<RootViewModel, Baby>(p => p.Baby, null,
                BindingMode.Default, null, (p, _1, _2) => SaveCurrentBaby(p), null, null);
        public Baby Baby {
            get { return (Baby)base.GetValue(BabyProperty); }
            set { SetValue(BabyProperty, value); }
        }

        public static readonly BindableProperty SyncingProperty =
            BindableProperty.Create<RootViewModel, bool>(p => p.Syncing, false);
        public bool Syncing {
            get { return (bool)base.GetValue(SyncingProperty); }
            set { SetValue(SyncingProperty, value); }
        }

        public static readonly BindableProperty CloudUserNameProperty =
            BindableProperty.Create<RootViewModel, string>(p => p.CloudUserName, null);
        public string CloudUserName {
            get { return (string)base.GetValue(CloudUserNameProperty); }
            set { SetValue(CloudUserNameProperty, value); }
        }

        public static readonly BindableProperty ThemeProperty =
            BindableProperty.Create<RootViewModel, Theme>(p => p.Theme, Theme.Dark,
                BindingMode.Default, null, (p, _1, _2) => SaveTheme(p), null, null);
        public Theme Theme {
            get { return (Theme)base.GetValue(ThemeProperty); }
            set { SetValue(ThemeProperty, value); }
        }

        public static readonly BindableProperty NotificationsEnabledProperty =
            BindableProperty.Create<RootViewModel, bool>(p => p.NotificationsEnabled, true,
                BindingMode.Default, null, (p, _1, _2) => SaveNotificationsEnabled(p), null, null);
        public bool NotificationsEnabled {
            get { return (bool)base.GetValue(NotificationsEnabledProperty); }
            set { SetValue(NotificationsEnabledProperty, value); }
        }

        public static readonly BindableProperty VibrateProperty =
            BindableProperty.Create<RootViewModel, bool>(p => p.Vibrate, true,
                BindingMode.Default, null, (p, _1, _2) => SaveVibrate(p), null, null);
        public bool Vibrate {
            get { return (bool)base.GetValue(VibrateProperty); }
            set { SetValue(VibrateProperty, value); }
        }

        public RootViewModel(ILocalStore dataStore, ICloudStore cloudStore, IPreferences preferences) {
            LocalStore = dataStore;
            CloudStore = cloudStore;
            Preferences = preferences;
            Entries = new ObservableCollection<LogEntry>();
            Babies = new ObservableCollection<Baby>();
            CloudUserName = CloudStore.UserName;

            Baby = new Baby(Preferences.Get(PreferenceKey.CurrentBabyUUID));

            LocalStore.RemotelyChanged += (sender, e) => RefreshAsync();

            LocalStore.LocallyChanged += (sender, e) => {
                RefreshAsync();
                TryToSyncEventually();
            };

            CloudStore.UserChanged += (sender, e) => {
                CloudUserName = CloudStore.UserName;
                if (CloudUserName != null) {
                    TryToSyncEventually();
                }
            };

            var now = DateTime.Now;
            if (Device.OS == TargetPlatform.Android) {
                // Fix a Xamarin.Android bug.
                if (TimeZoneInfo.Local.IsDaylightSavingTime(now)) {
                    now += TimeSpan.FromHours(1);
                }
            }
            Date = now - now.TimeOfDay;

            PropertyChanged += (sender, e) => {
                if (e.PropertyName == "Date") {
                    Entries.Clear();
                    RefreshEntriesAsync();
                }
                if (e.PropertyName == "Baby") {
                    Preferences.Set(PreferenceKey.CurrentBabyUUID, Baby.Uuid);
                    Entries.Clear();
                    RefreshEntriesAsync();
                }
            };

            Theme = Preferences.Get(PreferenceKey.LightTheme) ? Theme.Light : Theme.Dark;
            NotificationsEnabled = !Preferences.Get(PreferenceKey.DoNotNotify);
            Vibrate = !Preferences.Get(PreferenceKey.DoNotVibrate);

            TryToSyncEventually();
            RefreshBabiesAsync();
            RefreshEntriesAsync();
        }

        public async Task SyncAsync(bool markNewAsRead) {
            await syncQueue.EnqueueAsync(async toAwait => {
                await toAwait;
                if (CloudUserName == null) {
                    return false;
                }
                Syncing = true;
                try {
                    await LocalStore.SyncToCloudAsync(CloudStore, markNewAsRead);
                } finally {
                    Syncing = false;
                }
                return true;
            });
        }

        public async void TryToSyncEventually() {
            try {
                await SyncAsync(true);
            } catch (Exception e) {
                // Just ignore it.
                System.Diagnostics.Debug.WriteLine(String.Format("Error syncing: {0}", e));
            }
        }

        private void UpdateEntries(IEnumerable<LogEntry> updatedEntries) {
            var sameEntries = from entry1 in Entries
                join entry2 in updatedEntries on entry1.Uuid equals entry2.Uuid
                select new {
                OldEntry = entry1,
                NewEntry = entry2
            };

            var sameUuids = from item in sameEntries
                select item.NewEntry.Uuid;

            var oldUuids = (from entry in Entries
                select entry.Uuid).Except(sameUuids);

            var oldEntries = from entry in Entries
                join uuid in oldUuids on entry.Uuid equals uuid
                select entry;

            var newUuids = (from entry in updatedEntries
                select entry.Uuid).Except(sameUuids);

            var newEntries = from entry in updatedEntries
                join uuid in newUuids on entry.Uuid equals uuid
                select entry;

            sameEntries = sameEntries.ToList();
            oldEntries = oldEntries.ToList();
            newEntries = newEntries.ToList();

            foreach (var item in sameEntries) {
                Entries[Entries.IndexOf(item.OldEntry)] = item.NewEntry;
            }
            foreach (var entry in oldEntries) {
                Entries.Remove(entry);
            }
            foreach (var entry in newEntries) {
                // Insert in sorted order.
                var position = 0;
                while (position < Entries.Count && Entries.ElementAt(position).DateTime > entry.DateTime) {
                    position++;
                }
                Entries.Insert(position, entry);
            }
        }

        private void UpdateBabies(IEnumerable<Baby> updatedBabies) {
            var sameBabies = from baby1 in Babies
                join baby2 in updatedBabies on baby1.Uuid equals baby2.Uuid
                select new {
                OldBaby = baby1,
                NewBaby = baby2
            };

            var sameUuids = from item in sameBabies
                select item.NewBaby.Uuid;

            var oldUuids = (from baby in Babies
                select baby.Uuid).Except(sameUuids);

            var oldBabies = from baby in Babies
                join uuid in oldUuids on baby.Uuid equals uuid
                select baby;

            var newUuids = (from baby in updatedBabies
                select baby.Uuid).Except(sameUuids);

            var newBabies = from baby in updatedBabies
                join uuid in newUuids on baby.Uuid equals uuid
                select baby;

            sameBabies = sameBabies.ToList();
            oldBabies = oldBabies.ToList();
            newBabies = newBabies.ToList();

            foreach (var item in sameBabies) {
                // TODO: Maybe copy in place?
                Babies[Babies.IndexOf(item.OldBaby)] = item.NewBaby;
            }
            foreach (var baby in oldBabies) {
                Babies.Remove(baby);
            }
            foreach (var baby in newBabies) {
                Babies.Add(baby);
            }

            foreach (var baby in Babies) {
                if (baby.Uuid == null) {
                    Babies.Remove(baby);
                    break;
                }
            }
            Babies.Add(new Baby((string)null) {
                Name = "Add Baby"
            });

            // Update the current baby.
            foreach (var baby in updatedBabies) {
                if (Baby.Uuid == baby.Uuid) {
                    Baby.CopyFrom(baby);
                }
            }

            // TODO: Deal with the current baby disappearing.
            // TODO: Deal with all babies disappearing.
        }

        public async void RefreshBabiesAsync() {
            var newBabies = await LocalStore.FetchBabiesAsync();
            UpdateBabies(newBabies);
        }

        public async void RefreshEntriesAsync() {
            var newEntries = await LocalStore.FetchEntriesAsync(Baby, Date);
            UpdateEntries(newEntries);
        }

        public async void RefreshAsync() {
            var newBabies = await LocalStore.FetchBabiesAsync();
            UpdateBabies(newBabies);
            var newEntries = await LocalStore.FetchEntriesAsync(Baby, Date);
            UpdateEntries(newEntries);
        }

        public void ToggleTheme() {
            if (Theme == Theme.Light) {
                Theme = Theme.Dark;
            } else {
                Theme = Theme.Light;
            }

            // This is just a hack to make the ListView redraw with the right colors.
            var originalDate = Date;
            Date = originalDate.AddDays(1);
            Date = originalDate;
        }
    }
}

