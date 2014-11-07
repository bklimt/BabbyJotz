using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace BabbyJotz {
    public partial class MainPage : TabbedPage {
        private RootViewModel RootViewModel { get; set; }

        // TODO: Remove the statistics for the feeding options that are disabled.

        public MainPage(RootViewModel model) {
            RootViewModel = model;
            BindingContext = RootViewModel;
            InitializeComponent();

            ToolbarItems.Add(new ToolbarItem("Add", "toolbar_new_entry", async () => {
                await OnAddClicked();
            }));

            PropertyChangedEventHandler rebuild = (object sender, PropertyChangedEventArgs args) => {
                if (sender != RootViewModel.Baby) {
                    return;
                }
                RebuildStatsMenu();
            };

            RootViewModel.PropertyChanging += (object sender, PropertyChangingEventArgs e) => {
                if (e.PropertyName == "Baby") {
                    if (RootViewModel.Baby != null) {
                        RootViewModel.Baby.PropertyChanged -= rebuild;
                    }
                }
            };

            RootViewModel.PropertyChanged += (object sender, PropertyChangedEventArgs e) =>  {
                if (e.PropertyName == "Baby") {
                    Device.BeginInvokeOnMainThread(() => {
                        MaybeShowNux();
                        RebuildStatsMenu();
                        if (RootViewModel.Baby != null) {
                            RootViewModel.Baby.PropertyChanged += rebuild;
                        }
                    });
                }
            };

            if (RootViewModel.Baby != null) {
                RootViewModel.Baby.PropertyChanged += rebuild;
            }

            RebuildStatsMenu();
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

        private async Task OnAddClicked() {
            await Navigation.PushAsync(new EntryPage(RootViewModel, new LogEntry(RootViewModel.Baby)));
        }

        //private async void OnBabyTapped(object sender, EventArgs args) {
        //    await Navigation.PushAsync(new EditBabyPage(RootViewModel, new Baby(RootViewModel.Baby)));
        //}

        #region Log Tab

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

        #endregion
        #region Sync Tab

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

        #endregion

        public async void OnToggleThemeClicked(object sender, EventArgs args) {
            RootViewModel.ToggleTheme();
            if (Device.OS == TargetPlatform.iOS) {
                // This is a stupid hack to force the tabs on the bottom to update.
                // On Android, this breaks everything. Go figure.
                await Navigation.PushAsync(new VanishingPage(RootViewModel));
            }
        }

        #region Stats Tab

        // We could get rid of this if there was a binding to remove TableView sections.
        private void RebuildStatsMenu() {
            var sleepingBarChart = new TextCell { Text = "Bar Chart" };
            sleepingBarChart.SetBinding(TextCell.TextColorProperty, "Theme.ButtonText");
            sleepingBarChart.Tapped += OnSleepingBarChartClicked;

            var sleepingNightHeatMap = new TextCell { Text = "Night Heat Map" };
            sleepingNightHeatMap.SetBinding(TextCell.TextColorProperty, "Theme.ButtonText");
            sleepingNightHeatMap.Tapped += OnSleepingNightHeatMapClicked;

            var sleepingDayHeatMap = new TextCell { Text = "Day Heat Map" };
            sleepingDayHeatMap.SetBinding(TextCell.TextColorProperty, "Theme.ButtonText");
            sleepingDayHeatMap.Tapped += OnSleepingDayHeatMapClicked;

            var sleepingSection = new TableSection("Sleeping") {
                sleepingBarChart,
                sleepingNightHeatMap,
                sleepingDayHeatMap
            };

            var breastfeedingBarChart = new TextCell { Text = "Bar Chart" };
            breastfeedingBarChart.SetBinding(TextCell.TextColorProperty, "Theme.ButtonText");
            breastfeedingBarChart.Tapped += OnBreastfeedingBarChartClicked;

            var breastfeedingHeatMap = new TextCell { Text = "Heat Map" };
            breastfeedingHeatMap.SetBinding(TextCell.TextColorProperty, "Theme.ButtonText");
            breastfeedingHeatMap.Tapped += OnBreastfeedingHeatMapClicked;

            var breastfeedingSection = new TableSection("Breastfeeding") {
                breastfeedingBarChart,
                breastfeedingHeatMap
            };

            var pumpedBarChart = new TextCell { Text = "Bar Chart" };
            pumpedBarChart.SetBinding(TextCell.TextColorProperty, "Theme.ButtonText");
            pumpedBarChart.Tapped += OnPumpedBarChartClicked;

            var pumpedHeatMap = new TextCell { Text = "Heat Map" };
            pumpedHeatMap.SetBinding(TextCell.TextColorProperty, "Theme.ButtonText");
            pumpedHeatMap.Tapped += OnPumpedHeatMapClicked;

            var pumpedSection = new TableSection("Pumped") {
                pumpedBarChart,
                pumpedHeatMap
            };

            var formulaBarChart = new TextCell { Text = "Bar Chart" };
            formulaBarChart.SetBinding(TextCell.TextColorProperty, "Theme.ButtonText");
            formulaBarChart.Tapped += OnFormulaBarChartClicked;

            var formulaHeatMap = new TextCell { Text = "Heat Map" };
            formulaHeatMap.SetBinding(TextCell.TextColorProperty, "Theme.ButtonText");
            formulaHeatMap.Tapped += OnFormulaHeatMapClicked;

            var formulaSection = new TableSection("Formula") {
                formulaBarChart,
                formulaHeatMap
            };

            var poopingBarChart = new TextCell { Text = "Bar Chart" };
            poopingBarChart.SetBinding(TextCell.TextColorProperty, "Theme.ButtonText");
            poopingBarChart.Tapped += OnPoopingBarChartClicked;

            var poopingHeatMap = new TextCell { Text = "Heat Map" };
            poopingHeatMap.SetBinding(TextCell.TextColorProperty, "Theme.ButtonText");
            poopingHeatMap.Tapped += OnPoopingHeatMapClicked;

            var poopingSection = new TableSection("Pooping") {
                poopingBarChart,
                poopingHeatMap
            };

            statsMenu.Root.Clear();
            statsMenu.Root.Add(sleepingSection);
            if (RootViewModel.Baby == null || RootViewModel.Baby.ShowBreastfeeding) {
                statsMenu.Root.Add(breastfeedingSection);
            }
            if (RootViewModel.Baby == null || RootViewModel.Baby.ShowPumped) {
                statsMenu.Root.Add(pumpedSection);
            }
            if (RootViewModel.Baby == null || RootViewModel.Baby.ShowFormula) {
                statsMenu.Root.Add(formulaSection);
            }
            statsMenu.Root.Add(poopingSection);
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

        #endregion
    }
}