using System;
using System.Collections.Generic;
using Xamarin.Forms;

namespace BabbyJotz {
    public partial class BabyListPage : ContentPage {
        private RootViewModel RootViewModel { get; set; }

        public BabyListPage(RootViewModel model) {
            RootViewModel = model;
            BindingContext = model;
            InitializeComponent();
        }

        private async void OnBabyTapped(object sender, EventArgs args) {
            var parent = Parent as MasterDetailPage;
            parent.IsPresented = false;
            await parent.Detail.Navigation.PushAsync(
                new EditBabyPage(RootViewModel, new Baby(RootViewModel.Baby)));
        }

        public async void OnBabyListTapped(object sender, EventArgs args) {
            var tappedArgs = args as ItemTappedEventArgs;
            var baby = tappedArgs.Item as Baby;
            if (baby.Uuid == null) {
                var parent = Parent as MasterDetailPage;
                parent.IsPresented = false;
                await parent.Detail.Navigation.PushAsync(new NuxPage(RootViewModel));
            } else {
                RootViewModel.Baby = baby;
                // var parent = Parent as MasterDetailPage;
                // parent.IsPresented = false;
            }
            ((ListView)sender).SelectedItem = null;
        }
    }
}

