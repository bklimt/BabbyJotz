using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Xamarin.Forms;

namespace BabbyJotz {	
    public partial class LogInPage : ContentPage {
        private RootViewModel RootViewModel;

        public LogInPage(RootViewModel model) {
            RootViewModel = model;
            BindingContext = RootViewModel;
            InitializeComponent();
        }

        protected override async void OnAppearing() {
            base.OnAppearing();
            RootViewModel.CloudStore.LogEvent("LogInPage.OnAppearing");
            if (RootViewModel.CloudStore.UserName != null) {
                // They're already logged in, so don't show this page.
                await Navigation.PopAsync();
            }
        }

        public async void OnLogInClicked(object sender, EventArgs args) {
            RootViewModel.CloudStore.LogEvent("LogInPage.OnLogInClicked");
            try {
                await RootViewModel.CloudStore.LogInAsync(username.Text, password.Text);
                await Navigation.PopAsync();
            } catch (Exception e) {
                RootViewModel.CloudStore.LogException("OnLogInClicked", e);
                var message = String.Format("Unable to log in.");
                //var message = String.Format("Unable to log in.\n{0}", e);
                await DisplayAlert("Error", message, "OK");
            }
        }

        public async void OnForgotClicked(object sender, EventArgs args) {
            RootViewModel.CloudStore.LogEvent("LogInPage.OnForgotClicked");
            var page = new ForgotPasswordPage(RootViewModel, username.Text);
            await Navigation.PushAsync(page);
        }

        public async void OnSignUpClicked(object sender, EventArgs args) {
            RootViewModel.CloudStore.LogEvent("LogInPage.OnSignUpClicked");
            var page = new SignUpPage(RootViewModel, username.Text, password.Text);
            await Navigation.PushAsync(page);
        }

        public async void OnViewPrivacyPolicyClicked(object sender, EventArgs args) {
            RootViewModel.CloudStore.LogEvent("LogInPage.OnViewPrivacyPolicyClicked");
            var page = new WebViewPage("Privacy Policy", () => {
                return Task.FromResult(App.PrivacyPolicy);
            });
            await Navigation.PushAsync(page);
        }
    }
}

