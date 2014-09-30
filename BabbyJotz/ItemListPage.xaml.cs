using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Xamarin.Forms;

namespace BabbyJotz {
	public partial class ItemListPage : ContentPage {
		private RootViewModel rootViewModel;

		public ItemListPage(RootViewModel model) {
			rootViewModel = model;
			this.BindingContext = rootViewModel;

			InitializeComponent();
		}

		public void OnOkClicked(object sender, EventArgs args) {
			var entry = rootViewModel.NewEntry;
			rootViewModel.AddNewEntry();
			listView.SelectedItem = entry;
		}
	}
}