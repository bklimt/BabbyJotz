using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace BabbyJotz {
	public class RootViewModel : BindableObject {
		public ICloudStore CloudStore { get; private set; }

		public ObservableCollection<LogEntry> Entries { get; private set; }

		public static readonly BindableProperty NewEntryProperty =
			BindableProperty.Create<RootViewModel, LogEntry>(p => p.NewEntry, default(LogEntry));

		public LogEntry NewEntry {
			get { return (LogEntry)base.GetValue(NewEntryProperty); }
			set { SetValue(NewEntryProperty, value); }
		}

		public RootViewModel(ICloudStore cloudStore) {
			CloudStore = cloudStore;
			Entries = new ObservableCollection<LogEntry>();
			NewEntry = new LogEntry {
				DateTime = DateTime.Now
			};

			RefreshEntriesAsync();
		}

		public async void RefreshEntriesAsync() {
			var newEntries = await CloudStore.Fetch();
			Entries.Clear();
			foreach (var entry in newEntries) {
				Entries.Add(entry);
			}
		}

		public async void AddNewEntry() {
			var entry = NewEntry;
			Entries.Add(entry);
			NewEntry = new LogEntry {
				DateTime = DateTime.Now
			};
			await CloudStore.SaveAsync(entry);
		}
	}
}

