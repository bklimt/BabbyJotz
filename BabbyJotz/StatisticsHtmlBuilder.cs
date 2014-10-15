using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
      }
      h2 {
        font-size: 12pt;
        margin-bottom: 8px;
      }
      table {
        padding: 0px;
        margin: 0px;
        border-collapse: collapse;
        border: solid black 1px;
        width: 100%;
      }
      th {
        font-size: 4pt;
        border; 0px;
      }
      td {
        padding: 0px;
        font-size: 4pt;
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
        border-left: solid black 1px;
        border-right: solid black 1px;
        border-bottom: solid black 1px;
        text-align: left;
      }
      .top-cell {
        border-top: solid black 1px;
      }
      .left {
        border-left: solid black 1px;
        border-right: solid black 1px;
        width: 4%
      }
      .right {
        border-right: solid black 1px;
      }
      .bottom {
        border-bottom: solid black 1px;
      }
    </style>
  </head>
  <body>
";

        private static readonly string footer = @"
  </body>
</html>
";

        public StatisticsHtmlBuilder() {
        }

        public static Task<string> GenerateStatisticsHtmlAsync(List<LogEntry> entries) {
            return Task.Run(() => {
                StringBuilder html = new StringBuilder(3000);  // The HTML is usually about 2.5k.
                html.Append(header);
                if (entries.Count == 0) {
                    html.Append("<h2>No Data</h2>\n");
                    html.Append("<p>Add some entries to see analytics.</p>");
                } else {
                    GenerateFormulaGraph(html, entries, 50);
                    GenerateSleepHeatMap(html, entries, 12, 15);
                }
                html.Append(footer);
                return html.ToString();
            });
        }

        private static void GenerateFormulaGraph(StringBuilder html, List<LogEntry> entries, int steps) {
            var tableHeader = "<h2>Feeding</h2><table>\n";
            var tableFooter = "</table>\n";

            html.Append(tableHeader);

            var eaten = from entry in entries
                        where entry.FormulaEaten > 0.0m
                        group entry.FormulaEaten by entry.Date into day
                        select new {
                Date = day.Key,
                FormulaEaten = day.ToList().Sum()
            };

            var maxEaten = (from day in eaten
                            select day.FormulaEaten).Max();

            var minDate = (from day in eaten
                           select day.Date).Min();

            var maxDate = (from day in eaten
                           select day.Date).Max();

            var step = maxEaten / steps;

            foreach (var day in eaten) {
                html.AppendFormat("<tr><th class=\"left\">{0}</th>\n", day.Date.ToString("MM/dd"));
                var rowClass = "";
                if (day.Date == minDate) {
                    rowClass = "top-cell";
                }
                if (day.Date == maxDate) {
                    rowClass = "bottom";
                }
                for (int i = 0; i < steps; ++i) {
                    var colorClass = "white";
                    if (day.FormulaEaten > (i * step)) {
                        if (day.FormulaEaten >= ((i + 1) * step)) {
                            colorClass = "black";
                        } else {
                            colorClass = "dark";
                        }
                    }
                    html.AppendFormat("<td class=\"{0} {1}\">&nbsp;</td>", colorClass, rowClass);
                }
                html.AppendFormat("<td class=\"white right {1}\">{0}</td>\n", day.FormulaEaten, rowClass);
                html.AppendFormat("</tr>\n");
            }

            html.Append(tableFooter);
        }

        // TODO: Rewrite this to be simpler.
        private static void GenerateSleepHeatMap(StringBuilder html, List<LogEntry> entries, int startHour, int quantum) {
            var tableHeader = "<h2>Sleeping</h2><table><tr><th>&nbsp;</th>\n";
            var tableFooter = "</tr></table>\n";

            html.Append(tableHeader);

            // TODO: The times listed need to be based on startHour.
            html.AppendFormat("<th class=\"top\" colspan=\"{0}\">Noon</th>\n", (7 * 60) / quantum);
            html.AppendFormat("<th class=\"top\" colspan=\"{0}\">7pm</th>\n", (5 * 60) / quantum);
            html.AppendFormat("<th class=\"top\" colspan=\"{0}\">Midnight</th>\n", (7 * 60) / quantum);
            html.AppendFormat("<th class=\"top\" colspan=\"{0}\">7am</th>\n", (5 * 60) / quantum);

            var startDate = entries[0].DateTime;
            var endDate = entries[entries.Count - 1].DateTime;

            // Fast forward to the next noon (or startHour).
            var startDateNoon = (startDate - startDate.TimeOfDay).AddHours(startHour);
            if (startDate > startDateNoon) {
                startDateNoon = startDateNoon.AddDays(1.0);
            }
            startDate = startDateNoon;

            // Rewind to the previous noon (or startHour).                                               
            var endDateNoon = (endDate - endDate.TimeOfDay).AddHours(startHour);
            if (endDate < endDateNoon) {
                endDateNoon = endDateNoon.AddDays(-1.0);
            }
            endDate = endDateNoon;

            var t = startDate;
            var i = 0;
            var hasProcessedEntry = false;
            var isAsleep = false;
            var wasEverAsleep = false;
            var wasAlwaysAsleep = false;
            var count = 0;
            var outputting = false;

            while (true) {
                if (t <= entries[i].DateTime) {
                    // The next entry is in the future, just propagate the asleep state.
                    if (hasProcessedEntry) {
                        var cssClass = "";
                        if (wasAlwaysAsleep) {
                            if (count > 1) {
                                cssClass = "dark";
                            } else {
                                cssClass = "black";
                            }
                        } else {
                            if (wasEverAsleep) {
                                cssClass = "light";
                            } else {
                                cssClass = "white";
                            }
                        }
                        if (t.Minute == 0 && (t.Hour == 0 || t.Hour == 7 || t.Hour == 12 || t.Hour == 19)) {
                            cssClass += " right";
                        }
                        if (t > endDate.AddDays(-1)) {
                            cssClass += " bottom";
                        }
                        if (outputting) {
                            html.AppendFormat("<td class=\"{0}\">&nbsp;</td>\n", cssClass);
                        }
                        if (t.Hour == startHour && t.Minute == 0) {
                            outputting = (t != endDate);
                            if (outputting) {
                                html.AppendFormat("</tr><tr><th class=\"left\">{0}</th>\n", t.ToString("MM/dd"));
                            }
                        }
                        wasEverAsleep = isAsleep;
                        wasAlwaysAsleep = isAsleep;
                        count = 1;
                    }
                    t = t.AddMinutes(quantum);
                } else {
                    // The next entry is in this time span. Record that there was sleeping
                    // and move to the next entry.
                    isAsleep = entries[i].IsAsleep;
                    wasEverAsleep = wasEverAsleep || isAsleep;
                    wasAlwaysAsleep = wasAlwaysAsleep && isAsleep;
                    hasProcessedEntry = true;
                    count++;
                    i++;
                    if (i >= entries.Count) {
                        break;
                    }                                                                         
                }
            }
            html.Append(tableFooter);
        }
    }
}

