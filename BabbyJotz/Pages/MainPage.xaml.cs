using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace BabbyJotz {
    public partial class MainPage : MasterDetailPage {
        private RootViewModel RootViewModel { get; set; }

        // TODO: Remove the statics for the feeding options that are disabled.

        public MainPage(RootViewModel model) {
            RootViewModel = model;
            BindingContext = RootViewModel;
            InitializeComponent();

            ToolbarItems.Add(new ToolbarItem("Add", "toolbar_new_entry", async () => {
                await OnAddClicked();
            }));

            RootViewModel.PropertyChanged += (object sender, PropertyChangedEventArgs e) =>  {
                if (e.PropertyName == "Baby") {
                    Device.BeginInvokeOnMainThread(() => {
                        MaybeShowNux();
                    });
                }
            };
        }

        private void MaybeShowNux() {
            if (RootViewModel.Baby == null) {
                var page = new NavigationPage(new NuxPage(RootViewModel));
                page.BindingContext = RootViewModel;
                page.SetBinding(NavigationPage.BarBackgroundColorProperty, "Theme.Title");
                page.SetBinding(NavigationPage.BarTextColorProperty, "Theme.Text");
                Navigation.PushModalAsync(page);
            }
        }

        protected override void OnAppearing() {
            base.OnAppearing();
            MaybeShowNux();
        }

        private void OnBabiesClicked() {
            IsPresented = !IsPresented;
        }

        private void OnBabyTapped(object sender, EventArgs args) {
            OnBabiesClicked();
        }

        private async Task OnAddClicked() {
            await Navigation.PushAsync(new EntryPage(RootViewModel, new LogEntry(RootViewModel.Baby)));
        }

        private async void OnManageBabyClicked(object sender, EventArgs args) {
            await Navigation.PushAsync(new EditBabyPage(RootViewModel, new Baby(RootViewModel.Baby)));
        }

        public async void OnBabyListTapped(object sender, EventArgs args) {
            var tappedArgs = args as ItemTappedEventArgs;
            var baby = tappedArgs.Item as Baby;
            if (baby.Uuid == null) {
                await Navigation.PushAsync(new NuxPage(RootViewModel));
            } else {
                RootViewModel.Baby = baby;
                IsPresented = false;
            }
            ((ListView)sender).SelectedItem = null;
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

        public async void OnSyncToggled(object sender, EventArgs args) {
            var toggle = sender as Switch;

            // Bail if this was just updated by the binding.
            if (toggle.IsToggled && RootViewModel.IsSyncingEnabled) {
                return;
            }
            if (!toggle.IsToggled && !RootViewModel.IsSyncingEnabled) {
                return;
            }

            if (!RootViewModel.IsSyncingEnabled) {
                // It was turned off. First, turn off the forced override.
                if (RootViewModel.IsSyncingDisabled) {
                    RootViewModel.IsSyncingDisabled = false;
                }
                // Now check again, if it is on now, then they had already logged in.
                if (RootViewModel.IsSyncingEnabled) {
                    RootViewModel.TryToSyncEventually();
                } else {
                    // It's still not on, so they need to log in.
                    toggle.IsToggled = false;
                    await Navigation.PushAsync(new LogInPage(RootViewModel));
                }
            } else {
                // If was turned on, so force it off.
                RootViewModel.IsSyncingDisabled = true;
            }
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
            await Navigation.PushAsync(new LogOutPage(RootViewModel));
        }

        public async void OnViewPrivacyPolicyClicked(object sender, EventArgs args) {
            var page = new WebViewPage("Privacy Policy", () => {
                return Task.FromResult(App.PrivacyPolicy);
            });
            await Navigation.PushAsync(page);
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

        public async void OnFormulaBarChartClicked(object sender, EventArgs args) {
            await Navigation.PushAsync(new WebViewPage("Eating", () =>
                StatisticsHtmlBuilder.GetFormulaBarChartHtmlAsync(RootViewModel)));
        }

        public async void OnFormulaHeatMapClicked(object sender, EventArgs args) {
            await Navigation.PushAsync(new WebViewPage("Eating", () =>
                StatisticsHtmlBuilder.GetFormulaHeatMapHtmlAsync(RootViewModel)));
        }

        public async void OnPumpedBarChartClicked(object sender, EventArgs args) {
            await Navigation.PushAsync(new WebViewPage("Eating", () =>
                StatisticsHtmlBuilder.GetPumpedBarChartHtmlAsync(RootViewModel)));
        }

        public async void OnPumpedHeatMapClicked(object sender, EventArgs args) {
            await Navigation.PushAsync(new WebViewPage("Eating", () =>
                StatisticsHtmlBuilder.GetPumpedHeatMapHtmlAsync(RootViewModel)));
        }

        public async void OnBottleBarChartClicked(object sender, EventArgs args) {
            await Navigation.PushAsync(new WebViewPage("Eating", () =>
                StatisticsHtmlBuilder.GetBottleBarChartHtmlAsync(RootViewModel)));
        }

        public async void OnBottleHeatMapClicked(object sender, EventArgs args) {
            await Navigation.PushAsync(new WebViewPage("Eating", () =>
                StatisticsHtmlBuilder.GetBottleHeatMapHtmlAsync(RootViewModel)));
        }

        public async void OnBreastfeedingBarChartClicked(object sender, EventArgs args) {
            await Navigation.PushAsync(new WebViewPage("Eating", () =>
                StatisticsHtmlBuilder.GetBreastfeedingBarChartHtmlAsync(RootViewModel)));
        }

        public async void OnBreastfeedingHeatMapClicked(object sender, EventArgs args) {
            await Navigation.PushAsync(new WebViewPage("Eating", () =>
                StatisticsHtmlBuilder.GetBreastfeedingHeatMapHtmlAsync(RootViewModel)));
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