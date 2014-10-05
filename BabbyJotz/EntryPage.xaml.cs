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
			BindingContext = entry;
			InitializeComponent();
		}

		public async void OnSaveClicked(object sender, EventArgs args) {
			await RootViewModel.SaveAsync(Entry);
			await Navigation.PopAsync();
		}

		public async void OnDeleteClicked(object sender, EventArgs args) {
			var ok = await DisplayAlert("Are you sure?", "Delete \"" + Entry.Description + "\"?", "Delete", "Cancel");
			if (ok) {
				await RootViewModel.DeleteAsync(Entry);
			}
			await Navigation.PopAsync();
		}

		public void OnNotesFocused(object sender, EventArgs args) {
		}
	}
}

