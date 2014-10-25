using System;
using System.Collections.Generic;
using Xamarin.Forms;

namespace BabbyJotz {	
    public partial class LogInPage : ContentPage {
        private RootViewModel rootViewModel;

        public LogInPage(RootViewModel model) {
            rootViewModel = model;
            this.BindingContext = rootViewModel;
            InitializeComponent();
        }

        public async void OnLogInClicked(object sender, EventArgs args) {
            try {
                await rootViewModel.LogInAsync(username.Text, password.Text);
                await Navigation.PopAsync();
            } catch (Exception e) {
                var message = String.Format("Unable to log in.\n{0}", e);
                await DisplayAlert("Error", message, "OK");
            }
        }

		public async void OnSignUpClicked(object sender, EventArgs args) {
			try {
				await rootViewModel.SignUpAsync(username.Text, password.Text);
				await Navigation.PopAsync();
            } catch (Exception e) {
                var message = String.Format("Unable to sign up.\n{0}", e);
                await DisplayAlert("Error", message, "OK");
			}
		}
	}
}

