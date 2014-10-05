using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace BabbyJotz {
	public partial class MainPage : TabbedPage {
		private RootViewModel rootViewModel;

		public MainPage(RootViewModel model) {
			rootViewModel = model;
			this.BindingContext = rootViewModel;
			InitializeComponent();

			ToolbarItems.Add(new ToolbarItem("Add", "content_new_event", async () => {
				await OnAddClicked();
			}));
		}

		private async Task OnAddClicked() {
			await Navigation.PushAsync(new EntryPage(rootViewModel, new LogEntry()));
		}

		public async void OnEntryTapped(object sender, EventArgs args) {
			var tappedArgs = args as ItemTappedEventArgs;
			var entry = tappedArgs.Item as LogEntry;
			await Navigation.PushAsync(new EntryPage(rootViewModel, entry));
		}

		public void OnPreviousDayClicked(object sender, EventArgs args) {
			rootViewModel.Date -= TimeSpan.FromDays(1);
		}

		public void OnNextDayClicked(object sender, EventArgs args) {
			rootViewModel.Date += TimeSpan.FromDays(1);
		}

		public void OnSyncClicked(object sender, EventArgs args) {
			rootViewModel.SyncEventually();
		}

		public void OnLogInClicked(object sender, EventArgs args) {
			Navigation.PushAsync(new LogInPage(rootViewModel));
		}

		public async void OnLogOutClicked(object sender, EventArgs args) {
			var ok = await DisplayAlert("Are you sure?", "Future syncing may fail.", "OK", "Cancel");
			if (ok) {
				rootViewModel.LogOut();
			}
		}
	}
}