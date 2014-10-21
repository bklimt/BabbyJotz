﻿using System;
using Xamarin.Forms;

namespace BabbyJotz {
    public class LogEntry : BindableObject {
        private bool updatingDateTime = false;

        private static void UpdateDateTime(BindableObject obj) {
            var entry = obj as LogEntry;
            if (entry.updatingDateTime) {
                return;
            }
            entry.updatingDateTime = true;
            entry.DateTime = entry.Date + entry.Time;
            entry.updatingDateTime = false;
            UpdateDescription(obj);
        }

        private static void UpdateDateAndTime(BindableObject obj) {
            var entry = obj as LogEntry;
            if (entry.updatingDateTime) {
                return;
            }
            entry.updatingDateTime = true;
            entry.Date = entry.DateTime - entry.DateTime.TimeOfDay;
            entry.Time = entry.DateTime.TimeOfDay;
            entry.updatingDateTime = false;
            UpdateDescription(obj);
        }

        private static void UpdateDescription(BindableObject obj) {
            var entry = obj as LogEntry;
            entry.Description =
                entry.DateTime.ToString("hh:mm tt") + " - " +
                (entry.IsAsleep ? "\ud83d\udca4 " : "") +
                (entry.IsPoop ? "\ud83d\udca9 " : "") +
                (entry.FormulaEaten > 0 ? ("\ud83c\udf7c " + entry.FormulaEaten + "oz ") : "") +
                entry.Text;
        }

        // Properties

        public string Uuid { get; private set; }

        public string ObjectId { get; set; }

        public DateTime? Deleted { get; set; }

        public static readonly BindableProperty TextProperty =
            BindableProperty.Create<LogEntry, string>(p => p.Text, "",
                BindingMode.Default, null, (p, _1, _2) => UpdateDescription(p), null, null);

        public string Text {
            get { return (string)base.GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public static readonly BindableProperty DateProperty =
            BindableProperty.Create<LogEntry, DateTime>(p => p.Date, DateTime.Now,
                BindingMode.Default, null, (p, _1, _2) => UpdateDateTime(p), null, null);

        public DateTime Date {
            get { return (DateTime)base.GetValue(DateProperty); }
            set { SetValue(DateProperty, value); }
        }

        public static readonly BindableProperty TimeProperty =
            BindableProperty.Create<LogEntry, TimeSpan>(p => p.Time, TimeSpan.Zero,
                BindingMode.Default, null, (p, _1, _2) => UpdateDateTime(p), null, null);

        public TimeSpan Time {
            get { return (TimeSpan)base.GetValue(TimeProperty); }
            set { SetValue(TimeProperty, value); }
        }

        public static readonly BindableProperty IsPoopProperty =
            BindableProperty.Create<LogEntry, bool>(p => p.IsPoop, false,
                BindingMode.Default, null, (p, _1, _2) => UpdateDescription(p), null, null);

        public bool IsPoop {
            get { return (bool)base.GetValue(IsPoopProperty); }
            set { SetValue(IsPoopProperty, value); }
        }

        public static readonly BindableProperty IsAsleepProperty =
            BindableProperty.Create<LogEntry, bool>(p => p.IsAsleep, false,
                BindingMode.Default, null, (p, _1, _2) => UpdateDescription(p), null, null);

        public bool IsAsleep {
            get { return (bool)base.GetValue(IsAsleepProperty); }
            set { SetValue(IsAsleepProperty, value); }
        }

        public static readonly BindableProperty FormulaEatenProperty =
            BindableProperty.Create<LogEntry, decimal>(p => p.FormulaEaten, 0.0m,
                BindingMode.Default, null, (p, _1, _2) => UpdateDescription(p), null, null);

        // TODO: Get rid of decimal everywhere.
        public decimal FormulaEaten {
            get { return (decimal)base.GetValue(FormulaEatenProperty); }
            set { SetValue(FormulaEatenProperty, value); }
        }

        // Derived properties

        public static readonly BindableProperty DateTimeProperty =
            BindableProperty.Create<LogEntry, DateTime>(p => p.DateTime, DateTime.Now,
                BindingMode.Default, null, (p, _1, _2) => UpdateDateAndTime(p), null, null);

        public DateTime DateTime {
            get { return (DateTime)base.GetValue(DateTimeProperty); }
            set { SetValue(DateTimeProperty, value); }
        }

        public static readonly BindableProperty DescriptionProperty =
            BindableProperty.Create<LogEntry, string>(p => p.Description, "");

        public string Description {
            get { return (string)base.GetValue(DescriptionProperty); }
            private set { SetValue(DescriptionProperty, value); }
        }

        public LogEntry() {
            DateTime = DateTime.Now;
            if (Device.OS == TargetPlatform.Android) {
                // Work around a bug in Xamarin.Android.
                if (TimeZoneInfo.Local.IsDaylightSavingTime(DateTime)) {
                    DateTime += TimeSpan.FromHours(1);
                }
            }
            Uuid = Guid.NewGuid().ToString("D");
        }

        public LogEntry(string uuid) {
            DateTime = DateTime.Now;
            Uuid = uuid;
        }

        public LogEntry(LogEntry other) {
            DateTime = other.DateTime;
            FormulaEaten = other.FormulaEaten;
            IsPoop = other.IsPoop;
            IsAsleep = other.IsAsleep;
            Text = other.Text;
            Uuid = other.Uuid;
            ObjectId = other.ObjectId;
            Deleted = other.Deleted;
        }
    }
}

