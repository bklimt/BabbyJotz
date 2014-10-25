using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Labs;
using Xamarin.Forms.Labs.Services;
using Xamarin.Forms.Labs.Services.Media;

namespace BabbyJotz {
    public partial class NewBabyPage : ContentPage {
        private RootViewModel RootViewModel { get; set; }
        private Baby Baby { get; set; }

        public NewBabyPage(RootViewModel model) {
            RootViewModel = model;
            Baby = new Baby();
            BindingContext = new {
                RootViewModel = RootViewModel,
                Baby = Baby
            };
            InitializeComponent();
        }

        public async void OnPhotoClicked(object sender, EventArgs args) {
            var picker = DependencyService.Get<IMediaPicker>();

            bool camera = false;
            if (picker.IsCameraAvailable && picker.IsPhotosSupported) {
                var action = await DisplayActionSheet("Baby Profile Photo", "Cancel", null,
                                 "Take a Photo...", "Choose Existing...");
                if (action == "Take a Photo...") {
                    camera = true;
                }
            } else if (picker.IsCameraAvailable) {
                camera = true;
            }

            MediaFile file = null;
            var options = new CameraMediaStorageOptions();
            try {
                if (camera) {
                    file = await picker.TakePhotoAsync(options);
                } else {
                    file = await picker.SelectPhotoAsync(options);
                }
            } catch (TaskCanceledException) {
                return;
            }
            Baby.ProfilePhotoStream = () => file.Source;
        }

        public async void OnCreateClicked(object sender, EventArgs args) {
            await RootViewModel.SaveAsync(Baby);
            RootViewModel.Preferences.Set(PreferenceKey.CurrentBabyUUID, Baby.Uuid);
            await Navigation.PopModalAsync();
        }
    }
}

