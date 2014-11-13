using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

using Xamarin.Forms;

namespace BabbyJotz {
    public partial class LinkExistingBabyPage : ContentPage {
        private RootViewModel RootViewModel { get; set; }
        private ObservableCollection<Invite> Invites { get; set; }

        public LinkExistingBabyPage(RootViewModel model) {
            RootViewModel = model;
            Invites = new ObservableCollection<Invite>();
            BindingContext = new {
                RootViewModel = RootViewModel,
                Invites = Invites
            };
            InitializeComponent();
        }

        protected override void OnAppearing() {
            base.OnAppearing();
            RootViewModel.CloudStore.LogEvent("LinkExistingBabyPage.OnAppearing");
            if (RootViewModel.CloudUserName != null) {
                UpdateInvites();
            }
        }

        public async void OnLogInClicked(object sender, EventArgs args) {
            RootViewModel.CloudStore.LogEvent("LinkExistingBabyPage.OnLogInClicked");
            var page = new LogInPage(RootViewModel);
            await Navigation.PushAsync(page);
        }

        public async void OnViewPrivacyPolicyClicked(object sender, EventArgs args) {
            RootViewModel.CloudStore.LogEvent("LinkExistingBabyPage.OnViewPrivacyPolicyClicked");
            var page = new WebViewPage("Privacy Policy", () => {
                return Task.FromResult(App.PrivacyPolicy);
            });
            await Navigation.PushAsync(page);
        }

        public async void OnLogOutClicked(object sender, EventArgs args) {
            RootViewModel.CloudStore.LogEvent("LinkExistingBabyPage.OnLogOutClicked");
            var page = new LogOutPage(RootViewModel);
            await Navigation.PushAsync(page);
        }

        private async void UpdateInvites() {
            RootViewModel.CloudStore.LogEvent("LinkExistingBabyPage.UpdateInvites");
            var invites = await RootViewModel.CloudStore.GetInvitesAsync();
            Invites.Clear();
            foreach (var invite in invites) {
                Invites.Add(invite);
            }
        }

        public async void OnInviteTapped(object sender, EventArgs args) {
            var tappedArgs = args as ItemTappedEventArgs;
            var invite = tappedArgs.Item as Invite;
            await RootViewModel.CloudStore.AcceptInviteAsync(invite);
            RootViewModel.Baby = new Baby(invite.BabyUuid);
            RootViewModel.TryToSyncEventually("Invite Tapped");
            try {
                await Navigation.PopModalAsync();
            } catch (Exception) {
                await Navigation.PopToRootAsync();
            }
        }
    }
}

