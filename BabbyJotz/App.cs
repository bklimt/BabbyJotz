using System;
using System.Collections.ObjectModel;
using Xamarin.Forms;

namespace BabbyJotz {
	public class App {
        public static Page GetMainPage(RootViewModel model) {
			var page = new NavigationPage(new MainPage(model)) {
				BarBackgroundColor = Color.FromHex("#ffddff")
			};
			return page;
		}
	}
}

