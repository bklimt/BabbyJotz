using System;
using System.Collections.Generic;
using Xamarin.Forms;

namespace BabbyJotz { 
    public partial class NuxPage : ContentPage {
        private RootViewModel RootViewModel { get; set; }

        public NuxPage(RootViewModel model) {
            RootViewModel = model;
            BindingContext = RootViewModel;
            InitializeComponent();
        }

        public async void OnNewBabyClicked(object sender, EventArgs args) {
            RootViewModel.CloudStore.LogEvent("NuxPage.OnNewBabyClicked");
            var page = new EditBabyPage(RootViewModel, null);
            await Navigation.PushAsync(page);
        }

        public async void OnLinkExistingBabyClicked(object sender, EventArgs args) {
            RootViewModel.CloudStore.LogEvent("NuxPage.OnLinkExistingBabyClicked");
            var page = new LinkExistingBabyPage(RootViewModel);
            await Navigation.PushAsync(page);
        }
    }
}

