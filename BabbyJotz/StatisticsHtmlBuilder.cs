﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;

namespace BabbyJotz {
    public class StatisticsHtmlBuilder {
        private static readonly string header = @"
<html>                                                         
  <head>
    <meta name=""""viewport"""" content=""""width=device-width"""" />
    <style>
      body {
        padding: 0px;
        margin: 8px;
        font-family: Helvetica;
        background-color: {Binding Background};
      }
      h2 {
        font-size: 12pt;
        margin-bottom: 8px;
      }
      table {
        padding: 0px;
        margin: 0px;
        border-collapse: collapse;
        border: solid {Binding Text} 1px;
        width: 100%;
      }
      th {
        font-size: 8pt;
        border; 0px;
        color: {Binding Text};
      }
      td {
        padding: 0px;
        font-size: 4pt;
        color: black;
        width: 1%;
        height: 4px;
        border-right: none;
        border-left: none;
        border-bottom: solid #ddddff 1px;
      }

      .white { background-color: #ddddff; }
      .light { background-color: #9999bb; }
      .dark  { background-color: #444466; }
      .black { background-color: #000022; }

      .top {
        border-left: solid {Binding Text} 1px;
        border-right: solid {Binding Text} 1px;
        border-bottom: solid {Binding Text} 1px;
        text-align: left;
      }
      .top-cell {
        border-top: solid {Binding Text} 1px;
      }
      .left {
        border-left: solid {Binding Text} 1px;
        border-right: solid {Binding Text} 1px;
        width: 4%;
      }
      .right {
        border-right: solid {Binding Text} 1px;
      }
      .right-label {
        border-right: solid {Binding Text} 1px;
        font-size: 8pt;
      }
      .bottom {
        border-bottom: solid {Binding Text} 1px;
      }
    </style>
  </head>
  <body>
";

        private static readonly string footer = @"
  </body>
</html>
";

        private enum Metric { Sleeping, Pooping, Breastfeeding, Formula, Pumped, Bottle };

        private StatisticsHtmlBuilder() {
        }

        private static string ToHexString(Color color) {
            return String.Format("#{0:x2}{1:x2}{2:x2}",
                (int)(color.R * 255), (int)(color.G * 255), (int)(color.B * 255));
        }

        private static string Header(Theme theme) {
            var result = header;
            result = result.Replace("{Binding Background}", ToHexString(theme.Background));
            result = result.Replace("{Binding Text}", ToHexString(theme.Text));
            return result;
        }

        private delegate void HtmlBuilderFunc(StringBuilder html, List<LogEntry> entries);

        private static async Task<string> GetHtmlAsync(RootViewModel model, HtmlBuilderFunc func) {
            var entries = await model.LocalStore.GetEntriesForStatisticsAsync(model.Baby);
            return await Task.Run(() => {
                var html = new StringBuilder(5000);
                html.Append(Header(model.Theme));
                if (entries.Count == 0) {
                    html.Append("<h2>No Data</h2>\n");
                    html.Append("<p>Add some entries to see analytics.</p>");
                } else {
                    func(html, entries);
                }
                html.Append(footer);
                return html.ToString();
            });
        }

        public static async Task<string> GetSleepingBarChartHtmlAsync(RootViewModel model) {
            return await GetHtmlAsync(model, (html, entries) => GenerateBarChart(html, entries, Metric.Sleeping, 50));
        }

        public static async Task<string> GetSleepingNightHeatMapHtmlAsync(RootViewModel model) {
            return await GetHtmlAsync(model, (html, entries) => GenerateHeatMap(html, entries, Metric.Sleeping, true, 15));
        }

        public static async Task<string> GetSleepingDayHeatMapHtmlAsync(RootViewModel model) {
            return await GetHtmlAsync(model, (html, entries) => GenerateHeatMap(html, entries, Metric.Sleeping, false, 15));
        }

        public static async Task<string> GetBreastfeedingBarChartHtmlAsync(RootViewModel model) {
            return await GetHtmlAsync(model, (html, entries) => GenerateBarChart(html, entries, Metric.Breastfeeding, 50));
        }

        public static async Task<string> GetFormulaBarChartHtmlAsync(RootViewModel model) {
            return await GetHtmlAsync(model, (html, entries) => GenerateBarChart(html, entries, Metric.Formula, 50));
        }

        public static async Task<string> GetPumpedBarChartHtmlAsync(RootViewModel model) {
            return await GetHtmlAsync(model, (html, entries) => GenerateBarChart(html, entries, Metric.Pumped, 50));
        }

        public static async Task<string> GetBottleBarChartHtmlAsync(RootViewModel model) {
            return await GetHtmlAsync(model, (html, entries) => GenerateBarChart(html, entries, Metric.Bottle, 50));
        }

        public static async Task<string> GetBreastfeedingHeatMapHtmlAsync(RootViewModel model) {
            return await GetHtmlAsync(model, (html, entries) => GenerateHeatMap(html, entries, Metric.Breastfeeding, false, 30));
        }

        public static async Task<string> GetPumpedHeatMapHtmlAsync(RootViewModel model) {
            return await GetHtmlAsync(model, (html, entries) => GenerateHeatMap(html, entries, Metric.Pumped, false, 30));
        }

        public static async Task<string> GetFormulaHeatMapHtmlAsync(RootViewModel model) {
            return await GetHtmlAsync(model, (html, entries) => GenerateHeatMap(html, entries, Metric.Formula, false, 30));
        }

        public static async Task<string> GetBottleHeatMapHtmlAsync(RootViewModel model) {
            return await GetHtmlAsync(model, (html, entries) => GenerateHeatMap(html, entries, Metric.Bottle, false, 30));
        }

        public static async Task<string> GetPoopingBarChartHtmlAsync(RootViewModel model) {
            return await GetHtmlAsync(model, (html, entries) => GenerateBarChart(html, entries, Metric.Pooping, 50));
        }

        public static async Task<string> GetPoopingHeatMapHtmlAsync(RootViewModel model) {
            return await GetHtmlAsync(model, (html, entries) => GenerateHeatMap(html, entries, Metric.Pooping, false, 30));
        }

        private static DateTime DatetimeModMinutes(DateTime date, int minutes) {
            return (date - date.TimeOfDay) +
                TimeSpan.FromHours(date.Hour) +
                TimeSpan.FromMinutes(date.Minute % minutes);
        }

        private static void GenerateHeatMap(StringBuilder html, List<LogEntry> entries, Metric metric, bool night, int quantum) {
            html.Append("<table>");

            var headerRowBuilder = new StringBuilder(500);
            headerRowBuilder.Append("<tr><th>&nbsp;</th>\n");
            for (int i = 0; i < 2; ++i) {
                if ((night ? (1 - i) : i) == 0) {
                    headerRowBuilder.AppendFormat("<th class=\"top\" colspan=\"{0}\">Midnight</th>\n", (7 * 60) / quantum);
                    headerRowBuilder.AppendFormat("<th class=\"top\" colspan=\"{0}\">7am</th>\n", (5 * 60) / quantum);
                } else {
                    headerRowBuilder.AppendFormat("<th class=\"top\" colspan=\"{0}\">Noon</th>\n", (7 * 60) / quantum);
                    headerRowBuilder.AppendFormat("<th class=\"top\" colspan=\"{0}\">7pm</th>\n", (5 * 60) / quantum);
                }
            }
            headerRowBuilder.Append("</tr>\n");
            var headerRow = headerRowBuilder.ToString();

            var maxEaten = 0.0;
            if (metric == Metric.Formula) {
                maxEaten = (from e in entries
                    group e.FormulaEaten by DatetimeModMinutes(e.DateTime, quantum) into day
                    select day.ToList().Sum()).Max();
            } else if (metric == Metric.Pumped) {
                maxEaten = (from e in entries
                    group e.PumpedEaten by DatetimeModMinutes(e.DateTime, quantum) into day
                    select day.ToList().Sum()).Max();
            } else if (metric == Metric.Bottle) {
                maxEaten = (from e in entries
                    group e.PumpedEaten + e.FormulaEaten
                    by DatetimeModMinutes(e.DateTime, quantum)
                    into day
                    select day.ToList().Sum()).Max();
            } else if (metric == Metric.Breastfeeding) {
                maxEaten = (from e in entries
                    group e.LeftBreastEaten.TotalMinutes + e.RightBreastEaten.TotalMinutes
                    by DatetimeModMinutes(e.DateTime, quantum)
                    into day
                    select day.ToList().Sum()).Max();
            }

            bool wasJustAsleep = false;

            var startDay = entries[0].Date;
            var lastDay = entries[entries.Count - 1].Date;

            if (night) {
                var startDateTime = entries[0].DateTime;
                var startDayNoon = (startDateTime - startDateTime.TimeOfDay).AddHours(12);
                if (startDateTime < startDayNoon) {
                    startDayNoon = startDayNoon.AddDays(-1.0);
                }
                startDay = startDayNoon;

                var lastDateTime = entries[entries.Count - 1].DateTime;
                var lastDayNoon = (lastDateTime - lastDateTime.TimeOfDay).AddHours(12);
                if (lastDateTime < lastDayNoon) {
                    lastDayNoon = lastDayNoon.AddDays(-1.0);
                }
                lastDay = lastDayNoon;
            }

            var endDay = lastDay.AddDays(1);

            var entry = entries.GetEnumerator();
            var entryValid = entry.MoveNext();

            var nextDay = startDay;
            int rowNumber = 0;
            for (var day = startDay; day != endDay; day = nextDay) {
                if (rowNumber++ % 30 == 0) {
                    html.Append(headerRow);
                }
                html.AppendFormat("<tr><th class=\"left\">{0}</th>\n", day.ToString("MM/dd"));
                nextDay = day.AddDays(1);
                var nextTime = day;
                for (var time = day; time != nextDay; time = nextTime) {
                    nextTime = time.AddMinutes(quantum);
                    var eaten = 0.0;
                    var pooped = false;
                    // If the entry lines up perfectly with the time, don't use the last asleep value.
                    if (entry.Current.DateTime == time) {
                        wasJustAsleep = entry.Current.IsAsleep;
                    }
                    var wasEverAsleep = wasJustAsleep;
                    var wasAlwaysAsleep = wasJustAsleep;
                    var hadEntries = false;
                    while (entryValid && entry.Current.DateTime < nextTime) {
                        if (metric == Metric.Formula || metric == Metric.Bottle) {
                            eaten += entry.Current.FormulaEaten;
                        }
                        if (metric == Metric.Pumped || metric == Metric.Bottle) {
                            eaten += entry.Current.PumpedEaten;
                        }
                        if (metric == Metric.Breastfeeding) {
                            eaten += entry.Current.RightBreastEaten.TotalMinutes;
                            eaten += entry.Current.LeftBreastEaten.TotalMinutes;
                        }
                        pooped = pooped || entry.Current.IsPoop;
                        wasJustAsleep = entry.Current.IsAsleep;
                        wasEverAsleep = wasEverAsleep || entry.Current.IsAsleep;
                        wasAlwaysAsleep = wasAlwaysAsleep && entry.Current.IsAsleep;
                        hadEntries = true;
                        entryValid = entry.MoveNext();
                    }

                    var cssClass = "white";

                    if (metric == Metric.Formula ||
                        metric == Metric.Pumped ||
                        metric == Metric.Bottle ||
                        metric == Metric.Breastfeeding) {

                        // TODO: Make this split formula and pumped if "bottle".

                        var eatenRatio = Math.Min(eaten / maxEaten, 1.0);

                        // TODO: Make this grayscale or something.
                        if (eatenRatio > 0.66) {
                            cssClass = "black";
                        } else if (eatenRatio >= 0.33) {
                            cssClass = "dark";
                        } else if (eatenRatio > 0) {
                            cssClass = "light";
                        }
                    }

                    if (metric == Metric.Pooping) {
                        if (pooped) {
                            cssClass = "black";
                        }
                    }

                    if (metric == Metric.Sleeping) {
                        if (wasAlwaysAsleep && !hadEntries) {
                            cssClass = "black";
                        } else if (wasAlwaysAsleep) {
                            cssClass = "dark";
                        } else if (wasEverAsleep) {
                            cssClass = "light";
                        }
                    }

                    if (nextTime.Minute == 0 &&
                        (nextTime.Hour == 0 || nextTime.Hour == 7 || nextTime.Hour == 12 || nextTime.Hour == 19)) {
                        cssClass += " right";
                    }
                    if (day == lastDay) {
                        cssClass += " bottom";
                    }

                    html.AppendFormat("<td class=\"{0}\">&nbsp;</td>", cssClass);
                }
                html.AppendFormat("</tr>\n");
            }

            html.Append("</table>\n");
        }

        private struct DailyTotal {
            public DateTime Date;
            public double Amount;
        }

        // TODO: Separate out day sleep and night sleep.
        private static List<DailyTotal> ComputeSleepDailyTotals(List<LogEntry> entries) {
            var startDay = entries[0].Date;
            var lastDay = entries[entries.Count - 1].Date;
            var endDay = lastDay.AddDays(1);

            var entry = entries.GetEnumerator();
            var entryValid = entry.MoveNext();

            var totals = new List<DailyTotal>();

            DateTime? napTime = null;
            var nextDay = startDay;
            for (var day = startDay; day != endDay; day = nextDay) {
                var timeSlept = TimeSpan.FromMinutes(0);
                nextDay = day.AddDays(1);
                while (entryValid && entry.Current.DateTime < nextDay) {
                    if (napTime != null && !entry.Current.IsAsleep) {
                        // He woke up.
                        timeSlept += (entry.Current.DateTime - napTime.Value);
                        napTime = null;
                    } else if (napTime == null && entry.Current.IsAsleep) {
                        // He fell asleep.
                        napTime = entry.Current.DateTime;
                    }
                    entryValid = entry.MoveNext();
                }
                if (napTime != null) {
                    // He was asleep at the end of the day.
                    timeSlept += (nextDay - napTime.Value);
                    napTime = nextDay;
                }

                totals.Add(new DailyTotal() {
                    Date = day,
                    Amount = (double)timeSlept.TotalMinutes
                });
            }

            return totals;
        }

        private static void GenerateBarChart(StringBuilder html, List<LogEntry> entries, Metric metric, int steps) {
            html.Append("<table>\n");

            IEnumerable<DailyTotal> dailyTotals = null;

            if (metric == Metric.Formula) {
                dailyTotals = from entry in entries
                              where entry.FormulaEaten > 0.0
                              group entry.FormulaEaten by entry.Date into day
                              select new DailyTotal {
                    Date = day.Key,
                    Amount = day.ToList().Sum()
                };
            }

            if (metric == Metric.Pumped) {
                dailyTotals = from entry in entries
                              where entry.PumpedEaten > 0.0
                              group entry.PumpedEaten by entry.Date into day
                              select new DailyTotal {
                    Date = day.Key,
                    Amount = day.ToList().Sum()
                };
            }

            if (metric == Metric.Bottle) {
                // TODO: Separate bottle and pumped and add a legend.
                dailyTotals = from entry in entries
                              group entry.FormulaEaten + entry.PumpedEaten
                              by entry.Date into day
                              select new DailyTotal {
                    Date = day.Key,
                    Amount = day.ToList().Sum()
                };
            }

            if (metric == Metric.Breastfeeding) {
                dailyTotals = from entry in entries
                              group entry.RightBreastEaten.TotalMinutes + 
                                    entry.LeftBreastEaten.TotalMinutes
                              by entry.Date into day
                              select new DailyTotal {
                    Date = day.Key,
                    Amount = day.ToList().Sum()
                };
            }

            if (metric == Metric.Pooping) {
                dailyTotals = from entry in entries
                              where entry.IsPoop
                              group entry.IsPoop by entry.Date into day
                              select new DailyTotal {
                    Date = day.Key,
                    Amount = day.ToList().Count()
                };
            }

            if (metric == Metric.Sleeping) {
                dailyTotals = ComputeSleepDailyTotals(entries);
            }

            var maxAmount = (from day in dailyTotals select day.Amount).Max();

            var minDate = entries[0].Date;
            var maxDate = entries[entries.Count - 1].Date;

            if (metric == Metric.Pooping) {
                maxAmount += 1.0;
                steps = (int)maxAmount;
            }
            var step = maxAmount / steps;

            var endDate = maxDate.AddDays(1);

            var columnWidthString = "";
            if (metric == Metric.Pooping) {
                var columnWidth = 100 / ((int)maxAmount + 1);
                columnWidthString = String.Format(" style=\"width: {0}%\"", columnWidth);
            }

            var dailyTotal = dailyTotals.GetEnumerator();
            var dailyTotalValid = dailyTotal.MoveNext();
            for (var date = minDate; date != endDate; date = date.AddDays(1)) {
                while (dailyTotalValid && dailyTotal.Current.Date < date) {
                    dailyTotalValid = dailyTotal.MoveNext();
                }
                var amount = 0.0;
                if (dailyTotalValid && dailyTotal.Current.Date == date) {
                    amount = dailyTotal.Current.Amount;
                }

                html.AppendFormat("<tr><th class=\"left\"{1}>{0}</th>\n", date.ToString("MM/dd"), columnWidthString);

                var rowClass = "";
                if (date == minDate) {
                    rowClass = "top-cell";
                }
                if (date == maxDate) {
                    rowClass = "bottom";
                }
                for (int i = 0; i < steps; ++i) {
                    var colorClass = "white";

                    if (amount > (i * step)) {
                        if (amount >= ((i + 1) * step)) {
                            colorClass = "black";
                        } else {
                            colorClass = "dark";
                        }
                    }

                    html.AppendFormat("<td class=\"{0} {1}\"{2}>&nbsp;</td>", colorClass, rowClass, columnWidthString);
                }

                var amountString = String.Format("{0}", dailyTotal.Current.Amount);
                if (metric == Metric.Sleeping || metric == Metric.Breastfeeding) {
                    var span = TimeSpan.FromMinutes((double)dailyTotal.Current.Amount);
                    amountString = span.ToString(@"hh\:mm");
                }

                html.AppendFormat(
                    "<td class=\"white right-label {1}\"{2}>{0}</td>\n",
                    amountString,
                    rowClass,
                    columnWidthString);

                html.AppendFormat("</tr>\n");
            }

            html.Append("</table>\n");
        }
    }
}

