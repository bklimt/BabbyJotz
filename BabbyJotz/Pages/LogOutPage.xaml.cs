using System;
using System.Collections.Generic;
using Xamarin.Forms;

namespace BabbyJotz {
    public partial class LogOutPage : ContentPage {
        private RootViewModel RootViewModel { get; set; }

        public LogOutPage(RootViewModel model) {
            RootViewModel = model;
            BindingContext = RootViewModel;
            InitializeComponent();
        }

        public async void OnLogOutClicked(object sender, EventArgs args) {
            RootViewModel.CloudStore.LogOut();
            await RootViewModel.LocalStore.RecreateDatabaseAsync();
            await Navigation.PopAsync();
        }

        public async void OnCancelClicked(object sender, EventArgs args) {
            await Navigation.PopAsync();
        }
    }
}

