using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Xamarin.Forms;

namespace BabbyJotz {	
    public partial class SignUpPage : ContentPage {
        private RootViewModel RootViewModel;

        public SignUpPage(RootViewModel model, string initialUsername, string initialPassword) {
            RootViewModel = model;
            BindingContext = RootViewModel;
            InitializeComponent();
            username.Text = initialUsername;
            password.Text = initialPassword;
        }

        public async void OnCancelClicked(object sender, EventArgs args) {
            RootViewModel.CloudStore.LogEvent("SignUpPage.OnCancelClicked");
            await Navigation.PopAsync();
        }

        public async void OnSignUpClicked(object sender, EventArgs args) {
            RootViewModel.CloudStore.LogEvent("SignUpPage.OnSignUpClicked");
            string email = username.Text.Trim();
            if (email == "") {
                var message = String.Format("Email cannot be empty.");
                await DisplayAlert("Error", message, "OK");
                return;
            }

            if (email.Length < 3) {
                var message = String.Format("Email cannot be less than 3 characters.");
                await DisplayAlert("Error", message, "OK");
                return;
            }

            if (!email.Substring(1, email.Length - 2).Contains("@")) {
                var message = String.Format("Invalid email address.");
                await DisplayAlert("Error", message, "OK");
                return;
            }

            if (retypePassword.Text != password.Text) {
                var message = String.Format("Passwords don't match.");
                await DisplayAlert("Error", message, "OK");
                return;
            }
            try {
                await RootViewModel.CloudStore.SignUpAsync(email, password.Text);
                await Navigation.PopAsync();
            } catch (Exception) {
                var message = String.Format("Unable to sign up.");
                // var message = String.Format("Unable to sign up.\n{0}", e);
                await DisplayAlert("Error", message, "OK");
            }
        }

        public async void OnViewPrivacyPolicyClicked(object sender, EventArgs args) {
            RootViewModel.CloudStore.LogEvent("SignUpPage.OnViewPrivacyPolicyClicked");
            var page = new WebViewPage("Privacy Policy", () => {
                return Task.FromResult(App.PrivacyPolicy);
            });
            await Navigation.PushAsync(page);
        }
    }
}

