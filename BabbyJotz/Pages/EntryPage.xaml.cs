using System;
using System.Collections.Generic;
using Xamarin.Forms;

namespace BabbyJotz {
    public partial class EntryPage : ContentPage {	
        private LogEntry Entry { get; set; }
        private RootViewModel RootViewModel { get; set; }

        public EntryPage(RootViewModel rootViewModel, LogEntry entry) {
            RootViewModel = rootViewModel;
            Entry = entry;
            BindingContext = new {
                RootViewModel = rootViewModel,
                Baby = entry.Baby,
                LogEntry = entry
            };
            InitializeComponent();
        }

        public async void OnSaveClicked(object sender, EventArgs args) {
            RootViewModel.CloudStore.LogEvent("EntryPage.OnSaveClicked");
            // TODO: Disable the Save and Delete buttons while this is happening.
            await RootViewModel.LocalStore.SaveAsync(Entry);
            try {
                await Navigation.PopAsync();
            } catch (Exception) {
                // This can happen if they navigate back manually before it finishes saving.
                // No need to get too upset about it.
            }
        }

        public async void OnDeleteClicked(object sender, EventArgs args) {
            RootViewModel.CloudStore.LogEvent("EntryPage.OnDeleteClicked");
            // TODO: Disable the Save and Delete buttons while this is happening.
            var ok = await DisplayAlert("Are you sure?", "Delete \"" + Entry.Description + "\"?", "Delete", "Cancel");
            if (ok) {
                await RootViewModel.LocalStore.DeleteAsync(Entry);
                try {
                    await Navigation.PopAsync();
                } catch (Exception) {
                    // Meh.
                }
            }
        }

        public void OnNotesFocused(object sender, EventArgs args) {
            RootViewModel.CloudStore.LogEvent("EntryPage.OnNotesFocused");
        }
    }
}

