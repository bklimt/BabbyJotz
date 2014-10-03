using System;
using System.ComponentModel;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using BabbyJotz;

[assembly: ExportRenderer(typeof(ScrollingListView), typeof(ScrollingListRenderer))]
namespace BabbyJotz.Android {
	public class ScrollingListRenderer : ListViewRenderer {
		protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e) {
			base.OnElementPropertyChanged(sender, e);
			if (e.PropertyName == ListView.SelectedItemProperty.PropertyName) {
			}
		}
	}
}

