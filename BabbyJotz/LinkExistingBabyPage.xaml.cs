using System;
using System.Collections.Generic;
using Xamarin.Forms;

namespace BabbyJotz {
    public partial class LinkExistingBabyPage : ContentPage {
        private RootViewModel RootViewModel { get; set; }

        public LinkExistingBabyPage(RootViewModel model) {
            RootViewModel = model;
            BindingContext = RootViewModel;
            InitializeComponent();
        }

        public async void OnLogInClicked(object sender, EventArgs args) {
            var page = new LogInPage(RootViewModel);
            await Navigation.PushAsync(page);
        }

        public async void OnLogOutClicked(object sender, EventArgs args) {
            var page = new LogOutPage(RootViewModel);
            await Navigation.PushAsync(page);
        }
    }
}

