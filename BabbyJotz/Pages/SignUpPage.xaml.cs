using System;
using System.Collections.Generic;
using Xamarin.Forms;

namespace BabbyJotz {	
    public partial class SignUpPage : ContentPage {
        private RootViewModel rootViewModel;

        public SignUpPage(RootViewModel model, string initialUsername, string initialPassword) {
            rootViewModel = model;
            BindingContext = rootViewModel;
            InitializeComponent();
            username.Text = initialUsername;
            password.Text = initialPassword;
        }

        public async void OnCancelClicked(object sender, EventArgs args) {
            await Navigation.PopAsync();
        }

        public async void OnSignUpClicked(object sender, EventArgs args) {
            if (retypePassword.Text != password.Text) {
                var message = String.Format("Passwords don't match.");
                await DisplayAlert("Error", message, "OK");
                return;
            }
            try {
                await rootViewModel.CloudStore.SignUpAsync(username.Text, password.Text);
                await Navigation.PopAsync();
            } catch (Exception e) {
                var message = String.Format("Unable to sign up.\n{0}", e);
                await DisplayAlert("Error", message, "OK");
            }
        }
    }
}

