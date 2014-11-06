using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Xamarin.Forms;

namespace BabbyJotz {	
    public partial class LogInPage : ContentPage {
        private RootViewModel rootViewModel;

        public LogInPage(RootViewModel model) {
            rootViewModel = model;
            this.BindingContext = rootViewModel;
            InitializeComponent();
        }

        protected override async void OnAppearing() {
            base.OnAppearing();
            if (rootViewModel.CloudStore.UserName != null) {
                // They're already logged in, so don't show this page.
                await Navigation.PopAsync();
            }
        }

        public async void OnLogInClicked(object sender, EventArgs args) {
            try {
                await rootViewModel.CloudStore.LogInAsync(username.Text, password.Text);
                await Navigation.PopAsync();
            } catch (Exception e) {
                var message = String.Format("Unable to log in.\n{0}", e);
                await DisplayAlert("Error", message, "OK");
            }
        }

        public async void OnSignUpClicked(object sender, EventArgs args) {
            var page = new SignUpPage(rootViewModel, username.Text, password.Text);
            await Navigation.PushAsync(page);
        }

        public async void OnViewPrivacyPolicyClicked(object sender, EventArgs args) {
            var page = new WebViewPage("Privacy Policy", () => {
                return Task.FromResult(App.PrivacyPolicy);
            });
            await Navigation.PushAsync(page);
        }
    }
}

