using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;

// TODO: Add a dark theme.
namespace BabbyJotz {
	public class RootViewModel : BindableObject {
		private IDataStore DataStore { get; set; }
		private TaskQueue syncQueue = new TaskQueue();

		public ObservableCollection<LogEntry> Entries { get; private set; }
		public Statistics Statistics { get; private set; }

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

		public RootViewModel(IDataStore dataStore) {
			DataStore = dataStore;
			Entries = new ObservableCollection<LogEntry>();
			Statistics = new Statistics();
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
			Date = now - now.TimeOfDay;

			TryToSyncEventually();
		}

		public async Task SyncAsync() {
			await syncQueue.EnqueueAsync(async toAwait => {
				await toAwait;
				if (CloudUserName == null) {
					return false;
				}
				Syncing = true;
				try {
					await DataStore.SyncToCloudAsync();
				} finally {
					Syncing = false;
				}
				return true;
			});
		}

		public async void TryToSyncEventually() {
			try {
				await SyncAsync();
			} catch (Exception) {
				// Just ignore it.
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
				// TODO: This should really insert in sorted order.
				Entries.Add(entry);
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
		}

		public async Task SignUpAsync(string username, string password) {
			CloudUserName = null;
			try {
				await DataStore.SignUpAsync(username, password);
			} finally {
				CloudUserName = DataStore.CloudUserName;
			}
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

		public async Task GetStatisticsAsync() {
			await DataStore.GetStatisticsAsync(Statistics);
		}
	}
}

