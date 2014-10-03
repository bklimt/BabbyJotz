using System;
using System.Collections.Generic;
using Xamarin.Forms;

namespace BabbyJotz {
	public partial class MainPage : TabbedPage {
		private RootViewModel rootViewModel;

		public MainPage(RootViewModel model) {
			rootViewModel = model;
			this.BindingContext = rootViewModel;
			InitializeComponent();
		}

		public void OnOkClicked(object sender, EventArgs args) {
			var entry = rootViewModel.NewEntry;
			rootViewModel.AddNewEntry();
			listView.SelectedItem = entry;
		}

		public void OnPreviousDayClicked(object sender, EventArgs args) {
			rootViewModel.NewEntry.Date -= TimeSpan.FromDays(1);
		}

		public void OnNextDayClicked(object sender, EventArgs args) {
			rootViewModel.NewEntry.Date += TimeSpan.FromDays(1);
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