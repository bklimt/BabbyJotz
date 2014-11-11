using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

using Xamarin.Forms;

namespace BabbyJotz {
    public class RootViewModel : BindableObject {
        #region Property Helpers

        private static void SaveTheme(BindableObject obj) {
            var model = obj as RootViewModel;
            model.Preferences.Set(PreferenceKey.LightTheme, model.Theme == Theme.Light);
        }

        private static void SaveCurrentBaby(BindableObject obj) {
            var model = obj as RootViewModel;
            if (model.Baby == null) {
                model.Preferences.Set(PreferenceKey.CurrentBabyUUID, null);
            } else {
                model.Preferences.Set(PreferenceKey.CurrentBabyUUID, model.Baby.Uuid);
            }
        }

        private static void SaveIsSyncingDisabled(BindableObject obj) {
            var model = obj as RootViewModel;
            model.Preferences.Set(PreferenceKey.DoNotSync, model.IsSyncingDisabled);
        }

        private static void SaveNotificationsEnabled(BindableObject obj) {
            var model = obj as RootViewModel;
            model.Preferences.Set(PreferenceKey.DoNotNotify, !model.NotificationsEnabled);
        }

        private static void SaveVibrate(BindableObject obj) {
            var model = obj as RootViewModel;
            model.Preferences.Set(PreferenceKey.DoNotVibrate, !model.Vibrate);
        }

        #endregion
        #region Properties

        private TaskQueue syncQueue = new TaskQueue();
        private CancellationTokenSource syncCancellationTokenSource = null;

        public ILocalStore LocalStore { get; set; }
        public ICloudStore CloudStore { get; set; }
        public IPreferences Preferences { get; private set; }

        private object mutex = new object();
        public ObservableCollection<Baby> Babies { get; private set; }
        public ObservableCollection<LogEntry> Entries { get; private set; }

        #endregion
        #region Bindable Properties

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

        public static readonly BindableProperty SyncProgressProperty =
            BindableProperty.Create<RootViewModel, double>(p => p.SyncProgress, 0.0);
        public double SyncProgress {
            get { return (double)base.GetValue(SyncProgressProperty); }
            set { SetValue(SyncProgressProperty, value); }
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

        // This is whether it's been manually turned off.
        public static readonly BindableProperty IsSyncingDisabledProperty =
            BindableProperty.Create<RootViewModel, bool>(p => p.IsSyncingDisabled, false,
                BindingMode.Default, null, (p, _1, _2) => SaveIsSyncingDisabled(p), null, null);
        public bool IsSyncingDisabled {
            get { return (bool)base.GetValue(IsSyncingDisabledProperty); }
            set { SetValue(IsSyncingDisabledProperty, value); }
        }

        // This is !IsSyncingDisabled && CloudUserName != null.
        public static readonly BindableProperty IsSyncingEnabledProperty =
            BindableProperty.Create<RootViewModel, bool>(p => p.IsSyncingEnabled, false,
                BindingMode.OneWay);
        public bool IsSyncingEnabled {
            get { return (bool)base.GetValue(IsSyncingEnabledProperty); }
            private set { SetValue(IsSyncingEnabledProperty, value); }
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

        #endregion
        #region Constructor

        public RootViewModel(ILocalStore dataStore, ICloudStore cloudStore, IPreferences preferences) {
            LocalStore = dataStore;
            CloudStore = cloudStore;
            Preferences = preferences;
            Entries = new ObservableCollection<LogEntry>();
            Babies = new ObservableCollection<Baby>();
            CloudUserName = CloudStore.UserName;

            var babyUuid = Preferences.Get(PreferenceKey.CurrentBabyUUID);
            if (babyUuid != null) {
                Baby = new Baby(babyUuid);
            }

            LocalStore.RemotelyChanged += (sender, e) => RefreshAsync();

            LocalStore.LocallyChanged += (sender, e) => {
                RefreshAsync();
                TryToSyncEventually("LocallyChanged");
            };

            CloudStore.UserChanged += (sender, e) => {
                CloudUserName = CloudStore.UserName;
                if (CloudUserName != null) {
                    IsSyncingEnabled = !IsSyncingDisabled;
                    TryToSyncEventually("UserChanged");
                } else {
                    IsSyncingEnabled = false;
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
                if (e.PropertyName == RootViewModel.DateProperty.PropertyName) {
                    Entries.Clear();
                    RefreshEntriesAsync();
                }
                if (e.PropertyName == RootViewModel.BabyProperty.PropertyName) {
                    Entries.Clear();
                    RefreshEntriesAsync();
                }
                if (e.PropertyName == RootViewModel.IsSyncingDisabledProperty.PropertyName) {
                    IsSyncingEnabled = !IsSyncingDisabled && CloudStore.UserName != null;
                }
            };

            Theme = Preferences.Get(PreferenceKey.LightTheme) ? Theme.Light : Theme.Dark;
            IsSyncingDisabled = Preferences.Get(PreferenceKey.DoNotSync);
            IsSyncingEnabled = !IsSyncingDisabled && CloudStore.UserName != null;
            NotificationsEnabled = !Preferences.Get(PreferenceKey.DoNotNotify);
            Vibrate = !Preferences.Get(PreferenceKey.DoNotVibrate);

            TryToSyncEventually("RootViewModel Constructor");
            RefreshBabiesAsync();
            RefreshEntriesAsync();
        }

        #endregion
        #region Syncing

        /**
         * name - A description of the source of this sync, for debugging. 
         * markNewAsRead - Whether to count new items as unread notifications.
         */
        public async Task SyncAsync(string reason, bool markNewAsRead) {
            if (!IsSyncingEnabled) {
                return;
            }
            var name = "Sync: " + reason + " - " + DateTime.Now.ToString("O");
            await syncQueue.EnqueueAsync(async toAwait => {
                await toAwait;
                if (CloudUserName == null) {
                    return false;
                }

                var progress = new Progress<double>((p) => {
                    SyncProgress = p;
                });

                syncCancellationTokenSource = new CancellationTokenSource();
                var token = syncCancellationTokenSource.Token;

                var process = new InstrumentedProcess(name, progress, token);

                Syncing = true;
                try {
                    await LocalStore.SyncToCloudAsync(CloudStore, markNewAsRead, process);
                } catch (TaskCanceledException) {
                    Debug.WriteLine("Sync was cancelled.");
                } finally {
                    Syncing = false;
                    syncCancellationTokenSource = null;
                }

                return true;
            });
        }

        public void CancelSync() {
            var source = syncCancellationTokenSource;
            if (source != null) {
                source.Cancel();
            }
        }

        public async void TryToSyncEventually(string reason) {
            try {
                await SyncAsync(reason, true);
            } catch (Exception e) {
                // Just ignore it.
                Debug.WriteLine(String.Format("Error syncing: {0}", e));
            }
        }

        private void UpdateEntries(IEnumerable<LogEntry> updatedEntries) {
            lock (mutex) {
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
                    item.OldEntry.CopyFrom(item.NewEntry);
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
        }

        private void UpdateBabies(IEnumerable<Baby> updatedBabies) {
            lock (mutex) {
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
                    item.OldBaby.CopyFrom(item.NewBaby);
                }
                foreach (var baby in oldBabies) {
                    Babies.Remove(baby);
                }
                foreach (var baby in newBabies) {
                    Babies.Add(baby);
                }

                // Update the current baby.
                bool currentBabyFound = false;
                if (Baby != null) {
                    foreach (var baby in updatedBabies) {
                        if (Baby.Uuid == baby.Uuid) {
                            currentBabyFound = true;
                            Baby.CopyFrom(baby);
                        }
                    }
                }

                var addNewFound = false;
                foreach (var baby in Babies) {
                    if (baby.Uuid == null) {
                        addNewFound = true;
                        Babies.Move(Babies.IndexOf(baby), Babies.Count - 1);
                        break;
                    }
                }
                if (!addNewFound) {
                    Babies.Add(new Baby((string)null) {
                        Name = "Add Baby"
                    });
                }

                if (!currentBabyFound) {
                    if (Babies.Count > 1) {
                        Baby = Babies[0];
                    } else if (Baby != null) {
                        Baby = null;
                    }
                }
            }
        }

        public async void RefreshBabiesAsync() {
            var newBabies = await LocalStore.FetchBabiesAsync();
            UpdateBabies(newBabies);
        }

        public async void RefreshEntriesAsync() {
            var newEntries = new List<LogEntry>();
            if (Baby != null) {
                newEntries = await LocalStore.FetchEntriesAsync(Baby, Date);
            }
            UpdateEntries(newEntries);
        }

        public async void RefreshAsync() {
            var newBabies = await LocalStore.FetchBabiesAsync();
            UpdateBabies(newBabies);

            var newEntries = new List<LogEntry>();
            if (Baby != null) {
                newEntries = await LocalStore.FetchEntriesAsync(Baby, Date);
            }
            UpdateEntries(newEntries);
        }

        #endregion

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

