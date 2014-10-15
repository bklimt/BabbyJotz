using System;
using System.Collections.ObjectModel;
using Xamarin.Forms;

namespace BabbyJotz {
	public class App {
        public static Page GetMainPage(RootViewModel model) {
            var page = new NavigationPage(new MainPage(model));
            page.BindingContext = model;
            page.SetBinding(NavigationPage.BarBackgroundColorProperty, "Theme.Title");
            page.SetBinding(NavigationPage.BarTextColorProperty, "Theme.Text");
			return page;
		}
	}
}

