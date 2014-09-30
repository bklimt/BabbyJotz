using System;
using System.Collections.ObjectModel;
using Xamarin.Forms;

namespace BabbyJotz {
	public class App {
		public static Page GetMainPage(ICloudStore cloudStore) {
			RootViewModel model = new RootViewModel(cloudStore);
			var page = new ItemListPage(model);
			return page;
		}
	}
}

