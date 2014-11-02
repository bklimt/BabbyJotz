using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Labs;
using Xamarin.Forms.Labs.Services;
using Xamarin.Forms.Labs.Services.Media;

namespace BabbyJotz {
    public partial class EditBabyPage : ContentPage {
        private RootViewModel RootViewModel { get; set; }
        private Baby Baby { get; set; }

        public EditBabyPage(RootViewModel model, Baby baby) {
            RootViewModel = model;
            var IsNew = (baby == null);
            if (IsNew) {
                baby = new Baby();
            }
            Baby = baby;
            BindingContext = new {
                RootViewModel = RootViewModel,
                Baby = Baby,
                IsNew = IsNew
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
                await Task.Delay(1000);
                if (camera) {
                    file = await picker.TakePhotoAsync(options);
                } else {
                    file = await picker.SelectPhotoAsync(options);
                }
            } catch (TaskCanceledException) {
                return;
            }
            var stream = new MemoryStream();
            await file.Source.CopyToAsync(stream);
            var photo = new Photo(Baby);
            photo.Bytes = stream.ToArray();
            Baby.ProfilePhoto = photo;
        }

        public async void OnSaveClicked(object sender, EventArgs args) {
            // TODO: Disable the Save and Delete buttons while this is happening.
            // Photos are treated as immutable, so only save new ones.
            if (Baby.ProfilePhoto != null && Baby.ProfilePhoto.ObjectId == null) {
                await RootViewModel.LocalStore.SaveAsync(Baby.ProfilePhoto);
            }
            await RootViewModel.LocalStore.SaveAsync(Baby);
            RootViewModel.Baby = Baby;
            try {
                await Navigation.PopModalAsync();
            } catch (Exception) {
                await Navigation.PopToRootAsync();
            }
        }

        public async void OnShareClicked(object sender, EventArgs args) {
            await Navigation.PushAsync(new SharePage(RootViewModel, Baby));
        }

        public async void OnUnlinkClicked(object sender, EventArgs args) {
            // TODO: Disable the Save and Delete buttons while this is happening.
            var ok = await DisplayAlert("Are you sure?", "Unlink " + Baby.Name + "?", "Unlink", "Cancel");
            if (ok) {
                await RootViewModel.CloudStore.UnlinkAsync(Baby);
            }
        }

        public async void OnDeleteClicked(object sender, EventArgs args) {
            // TODO: Disable the Save and Delete buttons while this is happening.
            var ok = await DisplayAlert("Are you sure?", "Delete " + Baby.Name + "?", "Delete", "Cancel");
            if (ok) {
                await RootViewModel.LocalStore.DeleteAsync(Baby);
                // The RootViewModel will have to deal with the current baby getting deleted itself.
                try {
                    await Navigation.PopModalAsync();
                } catch (Exception) {
                    await Navigation.PopToRootAsync();
                }
            }
        }
    }
}

