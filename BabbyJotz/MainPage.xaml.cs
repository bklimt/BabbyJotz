﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace BabbyJotz {
	public partial class MainPage : TabbedPage {
        private RootViewModel RootViewModel { get; set; }

		public MainPage(RootViewModel model) {
			RootViewModel = model;
			BindingContext = RootViewModel;
			InitializeComponent();

            // TODO: Bind this icon to a theme.
			ToolbarItems.Add(new ToolbarItem("Add", "toolbar_new_entry", async () => {
				await OnAddClicked();
			}));
		}

        protected override void OnAppearing() {
            base.OnAppearing();
            // TODO: Change this to a bindable model property on the root view model.
            if (RootViewModel.Preferences.Get(PreferenceKey.CurrentBabyUUID) == null) {
                var page = new NavigationPage(new NuxPage(RootViewModel));
                page.BindingContext = RootViewModel;
                page.SetBinding(NavigationPage.BarBackgroundColorProperty, "Theme.Title");
                page.SetBinding(NavigationPage.BarTextColorProperty, "Theme.Text");
                Navigation.PushModalAsync(page);
            }
        }

		private async Task OnAddClicked() {
			await Navigation.PushAsync(new EntryPage(RootViewModel, new LogEntry()));
		}

		public async void OnEntryTapped(object sender, EventArgs args) {
			var tappedArgs = args as ItemTappedEventArgs;
			var entry = tappedArgs.Item as LogEntry;
            await Navigation.PushAsync(new EntryPage(RootViewModel, new LogEntry(entry)));
            ((ListView)sender).SelectedItem = null;
		}

		public void OnPreviousDayClicked(object sender, EventArgs args) {
			RootViewModel.Date -= TimeSpan.FromDays(1);
		}

		public void OnNextDayClicked(object sender, EventArgs args) {
			RootViewModel.Date += TimeSpan.FromDays(1);
		}

		public async void OnSyncClicked(object sender, EventArgs args) {
			try {
				await RootViewModel.SyncAsync(true);
			} catch (Exception e) {
				await DisplayAlert("Error", String.Format("Unable to sync: {0}", e), "Ok");
				// await DisplayAlert("Error", String.Format("Unable to sync. Check your network connection.", e), "Ok");
			}
		}

		public void OnLogInClicked(object sender, EventArgs args) {
			Navigation.PushAsync(new LogInPage(RootViewModel));
		}

		public async void OnLogOutClicked(object sender, EventArgs args) {
            // TODO: Maybe remove synced items on log out?
			var ok = await DisplayAlert("Are you sure?", "Future syncing may fail.", "OK", "Cancel");
			if (ok) {
				RootViewModel.LogOut();
			}
		}

        public async void OnToggleThemeClicked(object sender, EventArgs args) {
            RootViewModel.ToggleTheme();
            if (Device.OS == TargetPlatform.iOS) {
                // This is a stupid hack to force the tabs on the bottom to update.
                // On Android, this breaks everything. Go figure.
                await Navigation.PushAsync(new VanishingPage(RootViewModel));
            }
        }

        public async void OnSleepingBarChartClicked(object sender, EventArgs args) {
            await Navigation.PushAsync(new WebViewPage("Sleeping", () =>
                StatisticsHtmlBuilder.GetSleepingBarChartHtmlAsync(RootViewModel)));
        }

        public async void OnSleepingDayHeatMapClicked(object sender, EventArgs args) {
            await Navigation.PushAsync(new WebViewPage("Sleeping", () =>
                StatisticsHtmlBuilder.GetSleepingDayHeatMapHtmlAsync(RootViewModel)));
        }

        public async void OnSleepingNightHeatMapClicked(object sender, EventArgs args) {
            await Navigation.PushAsync(new WebViewPage("Sleeping", () =>
                StatisticsHtmlBuilder.GetSleepingNightHeatMapHtmlAsync(RootViewModel)));
        }

        public async void OnEatingBarChartClicked(object sender, EventArgs args) {
            await Navigation.PushAsync(new WebViewPage("Eating", () =>
                StatisticsHtmlBuilder.GetEatingBarChartHtmlAsync(RootViewModel)));
        }

        public async void OnEatingHeatMapClicked(object sender, EventArgs args) {
            await Navigation.PushAsync(new WebViewPage("Eating", () =>
                StatisticsHtmlBuilder.GetEatingHeatMapHtmlAsync(RootViewModel)));
        }

        public async void OnPoopingBarChartClicked(object sender, EventArgs args) {
            await Navigation.PushAsync(new WebViewPage("Pooping", () =>
                StatisticsHtmlBuilder.GetPoopingBarChartHtmlAsync(RootViewModel)));
        }

        public async void OnPoopingHeatMapClicked(object sender, EventArgs args) {
            await Navigation.PushAsync(new WebViewPage("Pooping", () =>
                StatisticsHtmlBuilder.GetPoopingHeatMapHtmlAsync(RootViewModel)));
        }
    }
}