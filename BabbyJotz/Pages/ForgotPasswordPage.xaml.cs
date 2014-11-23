using System;
using System.Collections.Generic;
using Xamarin.Forms;

namespace BabbyJotz {    
    public partial class ForgotPasswordPage : ContentPage {
        private RootViewModel RootViewModel { get; set; }

        public ForgotPasswordPage(RootViewModel model, string username) {
            RootViewModel = model;
            BindingContext = new {
                RootViewModel = RootViewModel,
                UserName = username
            };
            InitializeComponent();
        }

        public async void OnSendClicked(object sender, EventArgs args) {
            RootViewModel.CloudStore.LogEvent("ForgotPasswordPage.OnSendClicked");
            try {
                await RootViewModel.CloudStore.SendPasswordResetEmailAsync(usernameEntry.Text);
                await DisplayAlert("Forgot Password", "Password reset email sent!", "Ok");
                await Navigation.PopAsync();
            } catch (Exception e) {
                RootViewModel.CloudStore.LogException("ForgotPasswordPage.OnSendClicked", e);
                await DisplayAlert("Forgot Password", "Unable to send password reset email.", "Ok");
            }
        }
    }
}