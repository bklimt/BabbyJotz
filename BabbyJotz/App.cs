﻿using System;
using System.Collections.ObjectModel;
using Xamarin.Forms;

namespace BabbyJotz {
    public class App {
        public static readonly string PrivacyPolicy = @"
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
<p>Babby Jotz was written by one guy in what little spare time he had while also helping
take care of his newborn child. I don't have the time or money to have a lawyer write a ""real""
privacy policy, so I'll just tell you the situation in laymen's terms.</p>

<h2>Usage Information</h2>
<p>If you don't log in to sync with the Babby Cloud, then only minimal anonymous usage information
will be sent back to the developer. This information may include, but is not limited to, such items 
as the version of Babby Jotz you are running and the platform and version of your device.
For example, it could tell us that you are running Babby Jotz 1.0 on iOS 8.1. This information is
used by the developer to determine how much usage the app is getting so that I can decide how much
to invest in further maintenance and features.</p>

<h2>Personal Information</h2>
<p>If you do choose to sync with the Babby Cloud, of course the app will be sending the information
you enter to our servers. I will not sell this data to third parties. The data will only be used
in unaggregated form for the purpose of operating and maintaining the app. For example, I may need
to view some of your data for debugging issues that arise with your account. In those cases, I will
make every effort to learn as little as necessary about your account in order to fix the problem.</p>

<p>If a sufficient number of users sync their data to the Babby Cloud, then the developer may
choose to use the data in an aggregated form. For example, one non-limiting example would be to
publish interesting facts about the distribution of birthdays among babies. Or I could publish
a breakdown of percentage of babies that are breastfed vs formula-fed. I have no plans to do anything
specific with aggregated data at this time, but it is possible in the future.</p>

<h2>Third Parties</h2>
<p>Your data synced to the Babby Cloud will have to be shared with certain third parties for the
express purpose of providing the service itself. For example, push notifications will have to be
shared with Apple (for iOS) or Google (for Android) in order for them to be delivered. Your data
will be stored on Parse (operated by Facebook) for persistence and syncing. While I trust these
service providers to be responsible stewards of your data and not resell it or share it without
our permission, I cannot be held liable for their actions. By agreeing to this privacy policy,
you are also agreeing to the privacy policies for those services.</p>

<h2>Email Address</h2>
<p>Your email address will only be used by the developer for transactions that you opt into. For
example, the app can send you an email to recover your account when you have forgotten your password.
Similar features that use your email address may be developed in the future, such as push
notifications sent by email. However, the developer will not sell your email address to third parties.
I will also not send you spam. If I decide to send you marketing materials related to new features
or bugfixes with Babby Jotz, they will have a clearly labeled opt-out mechanism.</p>

<h2>Advertising</h2>
<p>This app uses banner advertisements to help offset the cost of maintenance and operation of the
Babby Cloud service. These ads are provided using the Google Mobile Ads / AdMob service. By agreeing
to this privacy policy, you are also agreeing to the privacy policy for that service.</p>

<h2>Retention Policy</h2>
<p>For now, I make no guarantees about how long data synced with the Babby Cloud will be retained
after it is deleted. This is not ideal, but it is difficult to make guarantees about data retention
when using third-party services to store data. Likewise, it is difficult to purge data regularly while
also keeping the comprehensive backups needed for reliability. This may change in the future.</p>

<h2>Warranty</h2>
<p>This software is provided as-is with no warranty or guarantee of any kind. If the software
somehow causes your phone to collapse into itself, forming a super-dense blackhole that sucks
all the change out of your pockets and destroys your trousers, the developer will not be held
responsible. If your data is suddenly missing and you have no way of recovering it, the developer
will feel a little sad, but will not be held responsible.</p>

</body>
</html>";

        public static Page GetMainPage(RootViewModel model) {
            var page = new MasterDetailPage();
            page.Master = new BabyListPage(model);
            page.Detail = new NavigationPage(new MainPage(model));
            page.Detail.BindingContext = model;
            page.Detail.SetBinding(NavigationPage.BarBackgroundColorProperty, "Theme.Title");
            page.Detail.SetBinding(NavigationPage.BarTextColorProperty, "Theme.Text");
            return page;
        }
    }
}

