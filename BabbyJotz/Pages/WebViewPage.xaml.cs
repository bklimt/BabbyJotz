using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Xamarin.Forms;

namespace BabbyJotz {
    public partial class WebViewPage : ContentPage {
        public WebViewPage(string title, Func<Task<string>> htmlFunc) {
            InitializeComponent();
            Title = title;
            ((HtmlWebViewSource)webview.Source).Html = @"
<html>
  <head>
    <style>
      body {
        padding: 0px;
        margin: 8px;
        font-family: Helvetica;
        background-color: #333333;
        color: #ffffff;
      }
    </style>
  </head>
  <body>
    Loading...
  </body>
</html>";
            doFunc(htmlFunc);
        }

        private async void doFunc(Func<Task<string>> htmlFunc) {
            ((HtmlWebViewSource)webview.Source).Html = await htmlFunc();
        }
    }
}