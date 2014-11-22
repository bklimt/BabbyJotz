using System;
using System.Collections.Generic;
using Xamarin.Forms;

namespace BabbyJotz {
    public partial class SharePage : ContentPage {
        private RootViewModel RootViewModel { get; set; }
        private Baby Baby { get; set; }

        public SharePage(RootViewModel model, Baby baby) {
            RootViewModel = model;
            Baby = baby;
            BindingContext = new {
                RootViewModel = RootViewModel,
                Baby = Baby
            };
            InitializeComponent();
        }

        public async void OnShareClicked(object sender, EventArgs args) {
            RootViewModel.CloudStore.LogEvent("SharePage.OnShareClicked");
            try {
                await RootViewModel.CloudStore.InviteAsync(usernameEntry.Text, Baby);
                await DisplayAlert("Share Baby", "Invitation sent!", "Ok");
                await Navigation.PopAsync();
            } catch (CloudException ce) {
                await DisplayAlert("Error", String.Format("Unable to share baby.\n{0}\n", ce.Error), "Ok");
            } catch (Exception e) {
                await DisplayAlert("Error", "Unable to share baby.", "Ok");
            }
        }
    }
}

