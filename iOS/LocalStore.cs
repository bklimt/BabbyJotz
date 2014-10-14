using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Mono.Data.Sqlite;

namespace BabbyJotz.iOS {
    public class LocalStore : IDataStore {
        private string path;
        private TaskQueue queue = new TaskQueue();
        private CloudStore cloudStore;

        private Task<T> EnqueueAsync<T>(Func<Task<T>> func) {
            return queue.EnqueueAsync(async toAwait => {
                await toAwait;
                return await func();
            });
        }

        public event EventHandler Changed;

        public LocalStore(CloudStore store) {
            cloudStore = store;

            path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                "database7.db3");

            EnqueueAsync(async () => {
                if (!File.Exists(path)) {
                    SqliteConnection.CreateFile(path);

                    var connection = new SqliteConnection("Data Source=" + path);
                    await connection.OpenAsync();
                    using (var command = connection.CreateCommand()) {
                        command.CommandText = "CREATE TABLE [LogEntry] (" +
                        "[Uuid] varchar(255) PRIMARY KEY, [Time] varchar(255), [Text] varchar(255), " +
                        "[Asleep] bool, [Poop] bool, [Formula] double, [ObjectId] varchar(255), " +
                        "[Deleted] varchar(255), [Synced] bool, [LocalVersion] integer NOT NULL);";
                        await command.ExecuteNonQueryAsync();
                    }
                    using (var command = connection.CreateCommand()) {
                        command.CommandText = "CREATE TABLE [Sync] (" +
                        "[Uuid] varchar(255) PRIMARY KEY, [Finished] varchar(255) NOT NULL, " +
                        "[LastUpdatedAt] varchar(255) NOT NULL, [Count] integer NOT NULL);";
                        await command.ExecuteNonQueryAsync();
                    }
                    connection.Close();
                }
                return true;
            });
        }

        public async Task SaveAsync(LogEntry entry) {
            await EnqueueAsync(async () => {
                var connection = new SqliteConnection("Data Source=" + path);
                await connection.OpenAsync();

                var deleted = (entry.Deleted.HasValue ? entry.Deleted.Value.ToString("O") : null);

                var parameters = new SqliteParameter[] {
                    new SqliteParameter("Uuid", entry.Uuid),
                    new SqliteParameter("Time", entry.DateTime.ToString("O")),
                    new SqliteParameter("Deleted", deleted),
                    new SqliteParameter("Text", entry.Text),
                    new SqliteParameter("Asleep", entry.IsAsleep),
                    new SqliteParameter("Poop", entry.IsPoop),
                    new SqliteParameter("Formula", entry.FormulaEaten),
                    new SqliteParameter("Deleted", deleted),
                    new SqliteParameter("Synced", false)
                };
                int rowsChanged = 0;

                using (var command = connection.CreateCommand()) {
                    command.CommandText = "UPDATE LogEntry SET " +
                    "Time=:Time, Text=:Text, Asleep=:Asleep, Poop=:Poop, Formula=:Formula, " +
                    "Deleted=:Deleted, Synced=:Synced, LocalVersion=LocalVersion+1 WHERE Uuid=:Uuid";
                    command.Parameters.AddRange(parameters);
                    rowsChanged = await command.ExecuteNonQueryAsync();
                }

                if (rowsChanged < 1) {
                    using (var command = connection.CreateCommand()) {
                        command.CommandText =
							"INSERT INTO LogEntry(Uuid, Time, Text, Asleep, Poop, Formula, Deleted, Synced, LocalVersion) " +
                        "VALUES (:Uuid, :Time, :Text, :Asleep, :Poop, :Formula, :Deleted, :Synced, 1);";
                        command.Parameters.AddRange(parameters);
                        rowsChanged = await command.ExecuteNonQueryAsync();
                    }

                }

                connection.Close();
                return true;
            });

            if (Changed != null) {
                Changed(this, EventArgs.Empty);
            }
        }

        public async Task DeleteAsync(LogEntry entry) {
            entry.Deleted = DateTime.Now;
            await SaveAsync(entry);
        }

        private LogEntry CreateEntryFromDataRecord(IDataRecord rec) {
            var deleted = (rec["Deleted"] is DBNull) ? (DateTime?)null : DateTime.Parse((string)rec["Deleted"]);

            return new LogEntry((string)rec["Uuid"]) {
                DateTime = DateTime.Parse((string)rec["Time"]),
                Text = (string)rec["Text"],
                IsPoop = (bool)rec["Poop"],
                IsAsleep = (bool)rec["Asleep"],
                FormulaEaten = (decimal)((double)rec["Formula"]),
                Deleted = deleted,
                ObjectId = rec["ObjectId"] as string
            };
        }

        /*
        private async Task<decimal> GetAggregateFormulaEatenAsync(string func, DateTime since) {
            return await EnqueueAsync(async () => {
                var connection = new SqliteConnection("Data Source=" + path);
                await connection.OpenAsync();

                var parameters = new SqliteParameter[] {
                    new SqliteParameter("Since", since.ToString("O"))
                };

                decimal result = 0.0m;

                using (var command = connection.CreateCommand()) {
                    command.CommandText =
                        "SELECT " + func + " FROM LogEntry WHERE Time>=:SINCE AND Deleted IS NULL";
                    command.Parameters.AddRange(parameters);
                    var obj = await command.ExecuteScalarAsync();
                    result = (decimal)((double)obj);
                }

                connection.Close();
                return result;
            });
        }
        */

        public async Task<string> GetStatisticsHtmlAsync() {
            List<LogEntry> entries = null;

            await EnqueueAsync(async () => {
                var connection = new SqliteConnection("Data Source=" + path);
                await connection.OpenAsync();

                var parameters = new SqliteParameter[] {
                };


                using (var command = connection.CreateCommand()) {
                    command.CommandText =
                        "SELECT * FROM LogEntry WHERE Deleted IS NULL ORDER BY Time DESC";
                    command.Parameters.AddRange(parameters);
                    var reader = await command.ExecuteReaderAsync();
                    // TODO: Do this in a more async-friendly way.
                    var results = from obj in reader.Cast<IDataRecord>()
                        select CreateEntryFromDataRecord(obj);
                    entries = results.Reverse().ToList();
                    reader.Close();
                }

                connection.Close();
                return true;
            });

            return await StatisticsHtmlBuilder.GenerateStatisticsHtmlAsync(entries);
        }

        public async Task GetStatisticsAsync(Statistics stats) {
            /*
            var now = DateTime.Now;
            var yesterday = now - TimeSpan.FromDays(1);
            var threeDaysAgo = now - TimeSpan.FromDays(3);
            var aWeekAgo = now - TimeSpan.FromDays(7);
            var aMonthAgo = now - TimeSpan.FromDays(30);

            stats.TotalEatenLastDay = await GetAggregateFormulaEatenAsync("SUM(Formula)", yesterday);
            stats.TotalEatenLastThreeDays = await GetAggregateFormulaEatenAsync("SUM(Formula)", threeDaysAgo);
            stats.TotalEatenLastWeek = await GetAggregateFormulaEatenAsync("SUM(Formula)", aWeekAgo);
            stats.TotalEatenLastMonth = await GetAggregateFormulaEatenAsync("SUM(Formula)", aMonthAgo);

            stats.AverageEatenLastThreeDays = stats.TotalEatenLastThreeDays / 3;
            stats.AverageEatenLastWeek = stats.TotalEatenLastWeek / 7;
            stats.AverageEatenLastMonth = stats.TotalEatenLastMonth / 30;
            */

            stats.Html = await GetStatisticsHtmlAsync();
        }

        public async Task<IEnumerable<LogEntry>> FetchAsync(DateTime day) {
            var date = day - day.TimeOfDay;
            var nextDate = date + TimeSpan.FromDays(1);

            return await EnqueueAsync(async () => {
                var connection = new SqliteConnection("Data Source=" + path);
                await connection.OpenAsync();

                var parameters = new SqliteParameter[] {
                    new SqliteParameter("Date", date.ToString("O")),
                    new SqliteParameter("NextDate", nextDate.ToString("O"))
                };

                IEnumerable<LogEntry> results = null;

                using (var command = connection.CreateCommand()) {
                    command.CommandText =
                        "SELECT * FROM LogEntry WHERE Time>=:Date AND Time<=:NextDate AND Deleted IS NULL ORDER BY Time";
                    command.Parameters.AddRange(parameters);
                    var reader = await command.ExecuteReaderAsync();

                    results = from obj in reader.Cast<IDataRecord>()
                                             select CreateEntryFromDataRecord(obj);
                    results = results.ToList();
                    reader.Close();
                }

                connection.Close();
                return results;
            });
        }

        public string CloudUserName {
            get {
                return cloudStore.UserName;
            }
        }

        public string CloudUserId {
            get {
                return cloudStore.UserId;
            }
        }

        public async Task LogInAsync(string username, string password) {
            await cloudStore.LogInAsync(username, password);
        }

        public async Task SignUpAsync(string username, string password) {
            await cloudStore.SignUpAsync(username, password);
        }

        public void LogOut() {
            cloudStore.LogOut();
        }

        public async Task UpdateFromCloudAsync(LogEntry entry) {
            await EnqueueAsync<bool>(async () => {
                var conn = new SqliteConnection("Data Source=" + path);
                await conn.OpenAsync();

                var deleted = entry.Deleted.HasValue ? entry.Deleted.Value.ToString("O") : null;

                var parameters = new SqliteParameter[] {
                    new SqliteParameter("Uuid", entry.Uuid),
                    new SqliteParameter("ObjectId", entry.ObjectId),
                    new SqliteParameter("Time", entry.DateTime.ToString("O")),
                    new SqliteParameter("Text", entry.Text),
                    new SqliteParameter("Asleep", entry.IsAsleep),
                    new SqliteParameter("Poop", entry.IsPoop),
                    new SqliteParameter("Formula", entry.FormulaEaten),
                    new SqliteParameter("Deleted", deleted),
                    new SqliteParameter("Synced", true)
                };

                bool isNew = false;

                // There is no race condition with this approach because all of these steps
                // are parts of one block in a serial queue.
                using (var command = conn.CreateCommand()) {
                    command.CommandText = "SELECT COUNT(*) FROM LogEntry WHERE Uuid=:Uuid;";
                    command.Parameters.AddRange(parameters);
                    var count = await command.ExecuteScalarAsync();
                    isNew = (((long)count) == 0);
                }

                if (isNew) {
                    using (var command = conn.CreateCommand()) {
                        command.CommandText = "INSERT INTO LogEntry " +
                        "(Uuid, ObjectId, Time, Text, Asleep, Poop, Formula, Deleted, Synced, LocalVersion) " +
                        "VALUES (:Uuid, :ObjectId, :Time, :Text, :Asleep, :Poop, :Formula, :Deleted, :Synced, 1);";
                        command.Parameters.AddRange(parameters);
                        await command.ExecuteNonQueryAsync();
                    }
                } else {
                    using (var command = conn.CreateCommand()) {
                        // The WHERE Synced clause keeps us from overwriting local changes made since the sync started.
                        command.CommandText = "UPDATE LogEntry SET " +
                        "ObjectId=:ObjectId, Time=:Time, Text=:Text, Asleep=:Asleep, Poop=:Poop, Formula=:Formula, " +
                        "Deleted=:Deleted, LocalVersion=LocalVersion+1 WHERE Uuid=:Uuid AND Synced=:Synced";
                        command.Parameters.AddRange(parameters);
                        await command.ExecuteNonQueryAsync();
                    }
                }

                conn.Close();
                return true;
            });
        }

        private class EntryAndVersion {
            public LogEntry Entry { get; set; }

            public long LocalVersion { get; set; }
        }

        public async Task SyncToCloudAsync() {
            IEnumerable<EntryAndVersion> unsavedEntries = null;

            await EnqueueAsync<bool>(async () => {
                var connection = new SqliteConnection("Data Source=" + path);
                await connection.OpenAsync();

                // First, save all of the local objects that need to be saved to the cloud.
                using (var selectCommand = connection.CreateCommand()) {
                    selectCommand.CommandText = "SELECT * FROM LogEntry WHERE (NOT Synced) OR ObjectId IS NULL";
                    var reader = await selectCommand.ExecuteReaderAsync();
                    var results = from obj in reader.Cast<IDataRecord>()
                                                 select new EntryAndVersion {
                        Entry = CreateEntryFromDataRecord(obj),
                        LocalVersion = (long)obj["LocalVersion"]
                    };
                    // It would be nice to do this in an async-friendly way.
                    unsavedEntries = results.ToList();
                    reader.Close();
                }
                connection.Close();
                return true;
            });

            foreach (var item in unsavedEntries) {
                var entry = item.Entry;
                await cloudStore.SaveAsync(entry);

                await EnqueueAsync<bool>(async () => {
                    var connection = new SqliteConnection("Data Source=" + path);
                    await connection.OpenAsync();

                    using (var updateCommand = connection.CreateCommand()) {
                        var parameters = new SqliteParameter[] {
                            new SqliteParameter("Uuid", entry.Uuid),
                            new SqliteParameter("ObjectId", entry.ObjectId),
                            new SqliteParameter("Synced", true),
                            new SqliteParameter("LocalVersion", item.LocalVersion)
                        };
                        // The LocalVersion clause stops us from overwriting changes made since the row was fetched.
                        updateCommand.CommandText =
                            "UPDATE LogEntry SET ObjectId=:ObjectId, Synced=:Synced " +
                        "WHERE Uuid=:Uuid AND LocalVersion=:LocalVersion";
                        updateCommand.Parameters.AddRange(parameters);
                        await updateCommand.ExecuteNonQueryAsync();
                    }
                    connection.Close();
                    return true;
                });
            }

            // Get the record of when the last sync was.
            DateTime? lastUpdatedAt = null;
            await EnqueueAsync<bool>(async () => {
                var connection = new SqliteConnection("Data Source=" + path);
                await connection.OpenAsync();

                // First, save all of the local objects that need to be saved to the cloud.
                using (var selectCommand = connection.CreateCommand()) {
                    selectCommand.CommandText = "SELECT LastUpdatedAt FROM Sync ORDER BY LastUpdatedAt DESC LIMIT 1";
                    var reader = await selectCommand.ExecuteReaderAsync();
                    var results = from obj in reader.Cast<IDataRecord>()
                                                 select (DateTime?)DateTime.Parse((string)obj["LastUpdatedAt"]);
                    lastUpdatedAt = results.FirstOrDefault();
                    reader.Close();
                }
                connection.Close();
                return true;
            });

            // Now, read everything from the cloud and add it to the local store.
            // Yes, we do re-read everything we just wrote, but that won't be much, and at it
            // allows us to catch any changes the server may have made during the write.
            var objsAndLastUpdatedAt = await cloudStore.FetchChangesAsync(lastUpdatedAt);
            var objs = objsAndLastUpdatedAt.Item1.ToList();
            lastUpdatedAt = objsAndLastUpdatedAt.Item2;
            foreach (var obj in objs) {
                await UpdateFromCloudAsync(obj);
            }

            // Record that this sync completed.
            if (lastUpdatedAt != null) {
                var parameters = new SqliteParameter[] {
                    new SqliteParameter("Uuid", Guid.NewGuid().ToString("D")),
                    new SqliteParameter("Finished", DateTime.Now.ToString("O")),
                    new SqliteParameter("LastUpdatedAt", lastUpdatedAt.Value.ToString("O")),
                    new SqliteParameter("Count", objs.Count)
                };

                await EnqueueAsync<bool>(async () => {
                    var connection = new SqliteConnection("Data Source=" + path);
                    await connection.OpenAsync();
                    using (var command = connection.CreateCommand()) {
                        command.CommandText = "INSERT INTO Sync " +
                        "(Uuid, Finished, LastUpdatedAt, Count) " +
                        "VALUES (:Uuid, :Finished, :LastUpdatedAt, :Count);";
                        command.Parameters.AddRange(parameters);
                        await command.ExecuteNonQueryAsync();
                    }
                    connection.Close();
                    return true;
                });
            }

            // TODO: If there were 1000 results, go ahead and try to sync again.

            // Signal listeners that the database has been updated.
            if (Changed != null) {
                Changed(this, EventArgs.Empty);
            }
        }
    }
}

