
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BabbyJotz {
    public class InstrumentedProcess {
        private object mutex;

        public string Name { get; private set; }
        public IProgress<double> ProgressTracker { get; private set; }
        public CancellationToken CancellationToken { get; private set; }

        private double selfProgress = 0.0;
        private List<Tuple<double, InstrumentedProcess>> subprocesses;

        private DateTime startTime;
        private DateTime finishTime;
        private bool finished;

        public double Progress {
            get {
                lock (mutex) {
                    double result = selfProgress;
                    foreach (var sub in subprocesses) {
                        result += (sub.Item1 * sub.Item2.Progress);
                    }
                    if (result > 1.0) {
                        var builder = new StringBuilder();
                        builder.AppendFormat("Progress for {0}: {1} > 1.0\n", Name, result);
                        builder.AppendFormat("  self: {0}\n", selfProgress);
                        foreach (var sub in subprocesses) {
                            builder.AppendFormat("  {0}: {1} * {2}\n", sub.Item2.Name, sub.Item1, sub.Item2.Progress);
                        }
                        throw new InvalidOperationException(builder.ToString());
                    }
                    return result;
                }
            }
            set {
                lock (mutex) {
                    try {
                        CancellationToken.ThrowIfCancellationRequested();
                    } catch (TaskCanceledException) {
                        finishTime = DateTime.Now;
                        finished = true;
                        throw;
                    }
                    selfProgress = value;
                    ProgressTracker.Report(Progress);
                }
            }
        }

        public InstrumentedProcess(string name, IProgress<double> progress, CancellationToken token) {
            mutex = new object();

            Name = name;
            ProgressTracker = progress;
            CancellationToken = token;
            subprocesses = new List<Tuple<double, InstrumentedProcess>>();
            startTime = DateTime.Now;
        }

        public InstrumentedProcess SubProcess(string name, double fraction) {
            lock (mutex) {
                var sub = new InstrumentedProcess(name, new Progress<double>((p) => {
                    ProgressTracker.Report(Progress);
                }), CancellationToken);
                subprocesses.Add(new Tuple<double, InstrumentedProcess>(fraction, sub));
                return sub;
            }
        }

        public void AssertFinished() {
            lock (mutex) {
                if (finished) {
                    throw new InvalidOperationException(
                        String.Format("Process {0} finished multiple times.", Name));
                }
                finished = true;
                finishTime = DateTime.Now;
                double remaining = 1.0;
                foreach (var sub in subprocesses) {
                    if (!sub.Item2.finished) {
                        throw new InvalidOperationException(
                            String.Format("Operation {0} marked finished before its child {1}.",
                                Name, sub.Item2.Name));
                    }
                    remaining -= sub.Item1;
                }
                Progress = remaining;
            }
        }

        public string GenerateReport() {
            var builder = new StringBuilder(3000);
            GenerateReport(builder, "");
            return builder.ToString();
        }

        private void GenerateReport(StringBuilder builder, string prefix) {
            lock (mutex) {
                if (!finished) {
                    throw new InvalidOperationException("You can only generate reports for finished processes.");
                }
                builder.AppendFormat("{0}{1:n2}s - {2} [{3} - {4}]\n",
                    prefix, (finishTime - startTime).TotalSeconds, Name, startTime, finishTime);
                foreach (var sub in subprocesses) {
                    sub.Item2.GenerateReport(builder, prefix + "  ");
                }
            }
        }
    }
}