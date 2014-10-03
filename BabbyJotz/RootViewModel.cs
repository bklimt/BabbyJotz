using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace BabbyJotz {
	public class RootViewModel : BindableObject {
		private IDataStore DataStore { get; set; }
		private TaskQueue syncQueue = new TaskQueue();

		public ObservableCollection<LogEntry> Entries { get; private set; }

		public static readonly BindableProperty NewEntryProperty =
			BindableProperty.Create<RootViewModel, LogEntry>(p => p.NewEntry, default(LogEntry));
		public LogEntry NewEntry {
			get { return (LogEntry)base.GetValue(NewEntryProperty); }
			set { SetValue(NewEntryProperty, value); }
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

			CloudUserName = DataStore.CloudUserName;

			PropertyChanged += (sender, e) => {
				if (e.PropertyName == "NewEntry") {
					this.NewEntry.PropertyChanged += (sender2, e2) => {
						if (e2.PropertyName == "Date") {
							RefreshEntriesAsync();
						}
					};
				}
			};

			DataStore.Changed += (sender, e) => RefreshEntriesAsync();

			NewEntry = new LogEntry();

			SyncEventually();
		}

		public async Task SyncAsync() {
			await syncQueue.EnqueueAsync(async toAwait => {
				await toAwait;
				if (CloudUserName == null) {
					return false;
				}
				Syncing = true;
				await DataStore.SyncToCloudAsync();
				Syncing = false;
				return true;
			});
		}

		public async void SyncEventually() {
			await SyncAsync();
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
			var newEntries = await DataStore.FetchAsync(NewEntry.Date);
			UpdateEntries(newEntries);
		}

		public async void AddNewEntry() {
			var entry = NewEntry;
			NewEntry = new LogEntry();
			await DataStore.SaveAsync(entry);
			await SyncAsync();
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

		public void LogOut() {
			CloudUserName = null;
			DataStore.LogOut();
		}
	}
}

