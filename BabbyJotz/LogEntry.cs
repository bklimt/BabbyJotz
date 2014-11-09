using System;
using Xamarin.Forms;

namespace BabbyJotz {
    public class LogEntry : StorableObject {
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
                ((entry.FormulaEaten + entry.PumpedEaten) > 0
                    ? String.Format("\ud83c\udf7c {0}oz ", entry.FormulaEaten + entry.PumpedEaten)
                    : "") +
                ((entry.RightBreastEaten.Minutes + entry.LeftBreastEaten.Minutes) > 0
                    ? String.Format("[bf {0}min] ", entry.RightBreastEaten.TotalMinutes + entry.LeftBreastEaten.TotalMinutes)
                    : "") +
                entry.Text;
        }

        // Properties

        public Baby Baby { get; private set; }

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
            BindableProperty.Create<LogEntry, double>(p => p.FormulaEaten, 0.0,
                BindingMode.Default, null, (p, _1, _2) => UpdateDescription(p), null, null);

        public double FormulaEaten {
            get { return (double)base.GetValue(FormulaEatenProperty); }
            set { SetValue(FormulaEatenProperty, value); }
        }

        public static readonly BindableProperty PumpedEatenProperty =
            BindableProperty.Create<LogEntry, double>(p => p.PumpedEaten, 0.0,
                BindingMode.Default, null, (p, _1, _2) => UpdateDescription(p), null, null);

        public double PumpedEaten {
            get { return (double)base.GetValue(PumpedEatenProperty); }
            set { SetValue(PumpedEatenProperty, value); }
        }

        public static readonly BindableProperty LeftBreastEatenProperty =
            BindableProperty.Create<LogEntry, TimeSpan>(p => p.LeftBreastEaten, TimeSpan.FromMinutes(0),
                BindingMode.Default, null, (p, _1, _2) => UpdateDescription(p), null, null);

        public TimeSpan LeftBreastEaten {
            get { return (TimeSpan)base.GetValue(LeftBreastEatenProperty); }
            set { SetValue(LeftBreastEatenProperty, value); }
        }

        public static readonly BindableProperty RightBreastEatenProperty =
            BindableProperty.Create<LogEntry, TimeSpan>(p => p.RightBreastEaten, TimeSpan.FromMinutes(0),
                BindingMode.Default, null, (p, _1, _2) => UpdateDescription(p), null, null);
      
        public TimeSpan RightBreastEaten {
            get { return (TimeSpan)base.GetValue(RightBreastEatenProperty); }
            set { SetValue(RightBreastEatenProperty, value); }
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

        public LogEntry(Baby baby) {
            Baby = baby;
            DateTime = DateTime.Now;
            //if (Device.OS == TargetPlatform.Android) {
                // Work around a bug in Xamarin.Android.
                //if (TimeZoneInfo.Local.IsDaylightSavingTime(DateTime)) {
                //    DateTime += TimeSpan.FromHours(1);
                //}
            //}
            Uuid = Guid.NewGuid().ToString("D");
        }

        public LogEntry(Baby baby, string uuid) {
            DateTime = DateTime.Now;
            Baby = baby;
            Uuid = uuid;
        }

        public LogEntry(LogEntry other) {
            CopyFrom(other);
        }

        public void CopyFrom(LogEntry other) {
            Baby = other.Baby;
            DateTime = other.DateTime;
            FormulaEaten = other.FormulaEaten;
            PumpedEaten = other.PumpedEaten;
            RightBreastEaten = other.RightBreastEaten;
            LeftBreastEaten = other.LeftBreastEaten;
            IsPoop = other.IsPoop;
            IsAsleep = other.IsAsleep;
            Text = other.Text;
            Uuid = other.Uuid;
            ObjectId = other.ObjectId;
            Deleted = other.Deleted;
        }
    }
}

