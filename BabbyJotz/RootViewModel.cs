using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;

namespace BabbyJotz {
	public class RootViewModel : BindableObject {
		private IDataStore DataStore { get; set; }
        private IPreferences Preferences { get; set; }
		private TaskQueue syncQueue = new TaskQueue();

        private static void SaveTheme(BindableObject obj) {
            var model = obj as RootViewModel;
            model.Preferences.SetBool("light", model.Theme == Theme.Light);
        }

        private static void SaveNotificationsEnabled(BindableObject obj) {
            var model = obj as RootViewModel;
            model.Preferences.SetBool("dontNotify", !model.NotificationsEnabled);
        }

        private static void SaveVibrate(BindableObject obj) {
            var model = obj as RootViewModel;
            model.Preferences.SetBool("dontVibrate", !model.Vibrate);
        }

		public ObservableCollection<LogEntry> Entries { get; private set; }

		public static readonly BindableProperty DateProperty =
			BindableProperty.Create<RootViewModel, DateTime>(p => p.Date, default(DateTime));
		public DateTime Date {
			get { return (DateTime)base.GetValue(DateProperty); }
			set { SetValue(DateProperty, value); }
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

        public RootViewModel(IDataStore dataStore, IPreferences preferences) {
			DataStore = dataStore;
            Preferences = preferences;
			Entries = new ObservableCollection<LogEntry>();
			CloudUserName = DataStore.CloudUserName;

			PropertyChanged += (sender, e) => {
				if (e.PropertyName == "Date") {
					Entries.Clear();
					// RefreshEntriesAsync is too slow, and this sleep lets the rest of the UI update.
					// await Task.Delay(10);
					RefreshEntriesAsync();
				}
			};

			DataStore.Changed += (sender, e) => RefreshEntriesAsync();
            var now = DateTime.Now;
            if (Device.OS == TargetPlatform.Android) {
                // Fix a Xamarin.Android bug.
                if (TimeZoneInfo.Local.IsDaylightSavingTime(now)) {
                    now += TimeSpan.FromHours(1);
                }
            }
            Date = now - now.TimeOfDay;

            Theme = Preferences.GetBool("light") ? Theme.Light : Theme.Dark;
            NotificationsEnabled = !Preferences.GetBool("dontNotify");
            Vibrate = !Preferences.GetBool("dontVibrate");

            TryToSyncEventually();
        }

        public async Task SyncAsync(bool markNewAsRead) {
			await syncQueue.EnqueueAsync(async toAwait => {
				await toAwait;
				if (CloudUserName == null) {
					return false;
				}
				Syncing = true;
				try {
                    await DataStore.SyncToCloudAsync(markNewAsRead);
				} finally {
					Syncing = false;
				}
				return true;
			});
		}

		public async void TryToSyncEventually() {
			try {
				await SyncAsync(true);
			} catch (Exception) {
				// Just ignore it.
                // TODO: Add some logging here at least.
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

		public async void RefreshEntriesAsync() {
			var newEntries = await DataStore.FetchAsync(Date);
			UpdateEntries(newEntries);
		}

		public async Task SaveAsync(LogEntry entry) {
			await DataStore.SaveAsync(entry);
			TryToSyncEventually();
		}

		public async Task DeleteAsync(LogEntry entry) {
			await DataStore.DeleteAsync(entry);
			TryToSyncEventually();
		}

		public async Task LogInAsync(string username, string password) {
			CloudUserName = null;
			try {
				await DataStore.LogInAsync(username, password);
			} finally {
				CloudUserName = DataStore.CloudUserName;
			}
            TryToSyncEventually();
            // TODO: Mark all as read after this.
		}

		public async Task SignUpAsync(string username, string password) {
			CloudUserName = null;
			try {
				await DataStore.SignUpAsync(username, password);
			} finally {
				CloudUserName = DataStore.CloudUserName;
			}
            TryToSyncEventually();
		}

        public string CloudUserId {
            get {
                return DataStore.CloudUserId;
            }
        }

		public void LogOut() {
			CloudUserName = null;
			DataStore.LogOut();
		}

        public async Task<List<LogEntry>> GetEntriesForStatisticsAsync() {
            return await DataStore.GetEntriesForStatisticsAsync();
        }

        public async Task MarkAllAsReadAsync() {
            await DataStore.MarkAllAsReadAsync();
        }

        public async Task<IEnumerable<LogEntry>> FetchUnreadAsync() {
            return await DataStore.FetchUnreadAsync();
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

