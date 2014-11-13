﻿using System;
using System.Collections.Generic;
using Xamarin.Forms;

namespace BabbyJotz {
    public partial class VanishingPage : ContentPage {    
        public VanishingPage(RootViewModel model) {
            BindingContext = model;
            InitializeComponent();
            Appearing += async (object sender, EventArgs args) => {
                model.CloudStore.LogEvent("VanishingPage.Appearing");
                await Navigation.PopAsync();
            };
        }
    }
}