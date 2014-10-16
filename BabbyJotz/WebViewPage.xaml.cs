using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Xamarin.Forms;

namespace BabbyJotz {
    public partial class WebViewPage : ContentPage {
        public WebViewPage(string title, Func<Task<string>> htmlFunc) {
            InitializeComponent();
            Title = title;
            ((HtmlWebViewSource)webview.Source).Html = "<html><body>Loading...</body></html>";
            doFunc(htmlFunc);
        }

        private async void doFunc(Func<Task<string>> htmlFunc) {
            ((HtmlWebViewSource)webview.Source).Html = await htmlFunc();
        }
    }
}