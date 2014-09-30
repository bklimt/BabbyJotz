using System;
using System.ComponentModel;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using BabbyJotz;
using BabbyJotz.iOS;

[assembly: ExportRenderer(typeof(ScrollingListView), typeof(ScrollingListRenderer))]
namespace BabbyJotz.iOS {
	public class ScrollingListRenderer : ListViewRenderer {
		protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e) {
			base.OnElementPropertyChanged(sender, e);
			if (e.PropertyName == ListView.SelectedItemProperty.PropertyName) {
				var table = (UITableView)Control;

				// This doesn't work?
				table.ScrollToNearestSelected(UITableViewScrollPosition.Bottom, true);

				// Scroll to the last row.
				int lastRow = table.NumberOfRowsInSection(0) - 1;
				var path = NSIndexPath.FromRowSection(lastRow, 0);
				table.ScrollToRow(path, UITableViewScrollPosition.Top, true);
			}
		}
	}
}

