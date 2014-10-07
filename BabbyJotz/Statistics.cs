using System;
using Xamarin.Forms;

namespace BabbyJotz {
	public class Statistics : BindableObject {
		// TODO: Maybe some graphs or something would be better.

		public static readonly BindableProperty TotalEatenLastDayProperty =
			BindableProperty.Create<Statistics, decimal>(p => p.TotalEatenLastDay, 0m);
		public decimal TotalEatenLastDay {
			get { return (decimal)base.GetValue(TotalEatenLastDayProperty); }
			set { SetValue(TotalEatenLastDayProperty, value); }
		}

		public static readonly BindableProperty TotalEatenLastThreeDaysProperty =
			BindableProperty.Create<Statistics, decimal>(p => p.TotalEatenLastThreeDays, 0m);
		public decimal TotalEatenLastThreeDays {
			get { return (decimal)base.GetValue(TotalEatenLastThreeDaysProperty); }
			set { SetValue(TotalEatenLastThreeDaysProperty, value); }
		}

		public static readonly BindableProperty AverageEatenLastThreeDaysProperty =
			BindableProperty.Create<Statistics, decimal>(p => p.AverageEatenLastThreeDays, 0m);
		public decimal AverageEatenLastThreeDays {
			get { return (decimal)base.GetValue(AverageEatenLastThreeDaysProperty); }
			set { SetValue(AverageEatenLastThreeDaysProperty, value); }
		}

		public static readonly BindableProperty TotalEatenLastWeekProperty =
			BindableProperty.Create<Statistics, decimal>(p => p.TotalEatenLastWeek, 0m);
		public decimal TotalEatenLastWeek {
			get { return (decimal)base.GetValue(TotalEatenLastWeekProperty); }
			set { SetValue(TotalEatenLastWeekProperty, value); }
		}

		public static readonly BindableProperty AverageEatenLastWeekProperty =
			BindableProperty.Create<Statistics, decimal>(p => p.AverageEatenLastWeek, 0m);
		public decimal AverageEatenLastWeek {
			get { return (decimal)base.GetValue(AverageEatenLastWeekProperty); }
			set { SetValue(AverageEatenLastWeekProperty, value); }
		}

		public static readonly BindableProperty TotalEatenLastMonthProperty =
			BindableProperty.Create<Statistics, decimal>(p => p.TotalEatenLastMonth, 0m);
		public decimal TotalEatenLastMonth {
			get { return (decimal)base.GetValue(TotalEatenLastMonthProperty); }
			set { SetValue(TotalEatenLastMonthProperty, value); }
		}

		public static readonly BindableProperty AverageEatenLastMonthProperty =
			BindableProperty.Create<Statistics, decimal>(p => p.AverageEatenLastMonth, 0m);
		public decimal AverageEatenLastMonth {
			get { return (decimal)base.GetValue(AverageEatenLastMonthProperty); }
			set { SetValue(AverageEatenLastMonthProperty, value); }
		}

		public Statistics() {
		}
	}
}

