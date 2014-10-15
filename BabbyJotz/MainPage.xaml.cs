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

            // Seems like a bug with WebView binding.
            webview.BindingContext = BindingContext;

			ToolbarItems.Add(new ToolbarItem("Add", "content_new_event", async () => {
				await OnAddClicked();
			}));

			CurrentPageChanged += async (object sender, EventArgs e) => {
				if (CurrentPage.Title == "Stats") {
					await rootViewModel.GetStatisticsAsync();
				}
			};
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

		public async void OnSyncClicked(object sender, EventArgs args) {
			try {
				await rootViewModel.SyncAsync();
			} catch (Exception e) {
				await DisplayAlert("Error", String.Format("Unable to sync: {0}", e), "Ok");
				// await DisplayAlert("Error", String.Format("Unable to sync. Check your network connection.", e), "Ok");
			}
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

        public void OnToggleThemeClicked(object sender, EventArgs args) {
            rootViewModel.ToggleTheme();
        }
	}
}