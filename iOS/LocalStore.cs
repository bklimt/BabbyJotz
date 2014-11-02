﻿using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Mono.Data.Sqlite;

namespace BabbyJotz.iOS {
    public class LocalStore : ILocalStore {
        private static readonly string databaseFile = "database17.db3";
        private string path;

        public event EventHandler LocallyChanged;
        public event EventHandler RemotelyChanged;

        public LocalStore() {
            path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                databaseFile);

            EnqueueAsync(async () => {
                await CreateDatabaseAsync();
                return true;
            });
        }

        #region Database Files

        // Serializes all access to the database.
        private TaskQueue queue = new TaskQueue();
        private Task<T> EnqueueAsync<T>(Func<Task<T>> func) {
            return queue.EnqueueAsync(async toAwait => {
                await toAwait;
                return await Task.Run(async () => await func());
            });
        }

        private void DeleteDatabase() {
            File.Delete(path);
        }

        private async Task CreateDatabaseAsync() {
            if (!File.Exists(path)) {
                SqliteConnection.CreateFile(path);

                var connection = new SqliteConnection("Data Source=" + path);
                await connection.OpenAsync();
                using (var command = connection.CreateCommand()) {
                    command.CommandText = "CREATE TABLE [Baby] (" +
                        "[Uuid] varchar(255) PRIMARY KEY, " +
                        "[Name] varchar(255) NOT NULL, " +
                        "[Birthday] varchar(255) NOT NULL, " +
                        "[ShowBreastfeeding] bool NOT NULL, " +
                        "[ShowPumped] bool NOT NULL, " +
                        "[ShowFormula] bool NOT NULL, " +
                        "[ProfilePhotoUuid] varchar(255), " +
                        "[ObjectId] varchar(255), " +
                        "[Deleted] varchar(255), " +
                        "[Synced] bool, " +
                        "[LocalVersion] integer NOT NULL);";
                    await command.ExecuteNonQueryAsync();
                }
                using (var command = connection.CreateCommand()) {
                    command.CommandText = "CREATE TABLE [LogEntry] (" +
                        "[Uuid] varchar(255) PRIMARY KEY, " +
                        "[BabyUuid] varchar(255) NOT NULL, " +
                        "[Time] varchar(255), " +
                        "[Text] varchar(255), " +
                        "[Asleep] bool, " +
                        "[Poop] bool, " +
                        "[FormulaEatenOunces] double, " +
                        "[PumpedEatenOunces] double, " +
                        "[LeftBreastEatenMinutes] double, " +
                        "[RightBreastEatenMinutes] double, " +
                        "[ObjectId] varchar(255), " +
                        "[Read] bool, " +
                        "[Deleted] varchar(255), " +
                        "[Synced] bool, " +
                        "[LocalVersion] integer NOT NULL);";
                    await command.ExecuteNonQueryAsync();
                }
                using (var command = connection.CreateCommand()) {
                    command.CommandText = "CREATE TABLE [Photo] (" +
                        "[Uuid] varchar(255) PRIMARY KEY, " +
                        "[BabyUuid] varchar(255) NOT NULL, " +
                        "[ObjectId] varchar(255), " +
                        "[Deleted] varchar(255), " +
                        "[Synced] bool, " +
                        "[LocalVersion] integer NOT NULL);";
                    await command.ExecuteNonQueryAsync();
                }
                using (var command = connection.CreateCommand()) {
                    command.CommandText = "CREATE TABLE [Sync] (" +
                        "[Uuid] varchar(255) PRIMARY KEY, " +
                        "[Finished] varchar(255) NOT NULL, " +
                        "[LastEntryUpdatedAt] varchar(255), " +
                        "[LastBabyUpdatedAt] varchar(255), " +
                        "[LastPhotoUpdatedAt] varchar(255), " +
                        "[EntryCount] integer NOT NULL, " +
                        "[PhotoCount] integer NOT NULL, " +
                        "[BabyCount] integer NOT NULL);";
                    await command.ExecuteNonQueryAsync();
                }
                connection.Close();
            }
        }

        public async Task RecreateDatabaseAsync() {
            await EnqueueAsync(async () => {
                DeleteDatabase();
                await CreateDatabaseAsync();
                // This is more like a syncing event than some local change.
                if (RemotelyChanged != null) {
                    RemotelyChanged(this, EventArgs.Empty);
                }
                return true;
            });
        }

        #endregion
        #region Utilities

        private async Task SaveStreamAsync(string path, byte[] bytes) {
            var file = File.Create(path);
            await file.WriteAsync(bytes, 0, bytes.Length);
            file.Close();
        }

        private string CreateUpdateString(string[] fields) {
            var parts = from field in fields
                        select field + "=:" + field;
            return String.Join(",", parts);
        }

        private string CreateInsertString(string[] fields) {
            return String.Join(",", fields);
        }

        private string CreateValuesString(string[] fields) {
            var parts = from field in fields
                        select ":" + field;
            return String.Join(",", parts);
        }

        private async Task UpsertAsync(
            string table,
            bool onlyUpdateIfSynced,
            SqliteParameter[] sqlParams) {

            var fields = (from param in sqlParams
                select param.ParameterName).ToArray();

            await EnqueueAsync<bool>(async () => {
                var conn = new SqliteConnection("Data Source=" + path);
                await conn.OpenAsync();
                bool isNew = false;

                // There is no race condition with this approach because all of these steps
                // are parts of one block in a serial queue.
                using (var command = conn.CreateCommand()) {
                    command.CommandText = "SELECT COUNT(*) FROM " + table + " WHERE Uuid=:Uuid;";
                    command.Parameters.AddRange(sqlParams);
                    var count = await command.ExecuteScalarAsync();
                    isNew = (((long)count) == 0);
                }

                if (isNew) {
                    using (var command = conn.CreateCommand()) {
                        command.CommandText = "INSERT INTO " + table + " (" +
                            CreateInsertString(fields) +
                            ", LocalVersion" +
                            ") VALUES (" +
                            CreateValuesString(fields) +
                            ", 1);";
                        command.Parameters.AddRange(sqlParams);
                        await command.ExecuteNonQueryAsync();
                    }
                } else {
                    using (var command = conn.CreateCommand()) {
                        // The WHERE Synced clause keeps us from overwriting local changes made since the sync started.
                        command.CommandText = "UPDATE " + table + " SET " +
                            CreateUpdateString(fields) +
                            ", LocalVersion=LocalVersion+1 " +
                            "WHERE Uuid=:Uuid" +
                            (onlyUpdateIfSynced ? " AND Synced=:Synced" : "");
                        command.Parameters.AddRange(sqlParams);
                        await command.ExecuteNonQueryAsync();
                    }
                }

                conn.Close();
                return true;
            });
        }

        public async Task SaveAsync(string table, SqliteParameter[] sqlParams) {
            await UpsertAsync(table, false, sqlParams);
            if (LocallyChanged != null) {
                LocallyChanged(this, EventArgs.Empty);
            }
        }

        private async Task UpdateFromCloudAsync(string table, SqliteParameter[] sqlParams) {
            await UpsertAsync(table, true, sqlParams);
        }

        #endregion

        #region LogEntry

        private SqliteParameter[] CreateSqliteParameters(LogEntry entry, bool read, bool synced) {
            var deleted = (entry.Deleted.HasValue ? entry.Deleted.Value.ToString("O") : null);
            return new SqliteParameter[] {
                new SqliteParameter("Uuid", entry.Uuid),
                new SqliteParameter("BabyUuid", entry.Baby.Uuid),
                new SqliteParameter("Time", entry.DateTime.ToString("O")),
                new SqliteParameter("Text", entry.Text),
                new SqliteParameter("Asleep", entry.IsAsleep),
                new SqliteParameter("Poop", entry.IsPoop),
                new SqliteParameter("FormulaEatenOunces", entry.FormulaEaten),
                new SqliteParameter("PumpedEatenOunces", entry.PumpedEaten),
                new SqliteParameter("LeftBreastEatenMinutes", entry.LeftBreastEaten.TotalMinutes),
                new SqliteParameter("RightBreastEatenMinutes", entry.RightBreastEaten.TotalMinutes),
                new SqliteParameter("ObjectId", entry.ObjectId),
                new SqliteParameter("Read", read),
                new SqliteParameter("Deleted", deleted),
                new SqliteParameter("Synced", synced)
            };
        }

        private LogEntry CreateLogEntry(IDataRecord rec) {
            var deleted = (rec["Deleted"] is DBNull) ? (DateTime?)null : DateTime.Parse((string)rec["Deleted"]);
            return new LogEntry(new Baby((string)rec["BabyUuid"]), (string)rec["Uuid"]) {
                DateTime = DateTime.Parse((string)rec["Time"]),
                Text = (string)rec["Text"],
                IsPoop = (bool)rec["Poop"],
                IsAsleep = (bool)rec["Asleep"],
                FormulaEaten = (double)rec["FormulaEatenOunces"],
                PumpedEaten = (double)rec["PumpedEatenOunces"],
                RightBreastEaten = TimeSpan.FromMinutes((double)rec["RightBreastEatenMinutes"]),
                LeftBreastEaten = TimeSpan.FromMinutes((double)rec["LeftBreastEatenMinutes"]),
                Deleted = deleted,
                ObjectId = rec["ObjectId"] as string
            };
        }

        public async Task SaveAsync(LogEntry entry) {
            await SaveAsync("LogEntry", CreateSqliteParameters(entry, true, false));
        }

        public async Task DeleteAsync(LogEntry entry) {
            entry.Deleted = DateTime.Now;
            await SaveAsync(entry);
        }

        private async Task UpdateFromCloudAsync(LogEntry entry, bool markAsRead) {
            await UpdateFromCloudAsync("LogEntry", CreateSqliteParameters(entry, markAsRead, true));
        }

        public async Task<List<LogEntry>> FetchEntriesAsync(Baby baby, DateTime day) {
            var date = day - day.TimeOfDay;
            var nextDate = date + TimeSpan.FromDays(1);

            return await EnqueueAsync(async () => {
                var connection = new SqliteConnection("Data Source=" + path);
                await connection.OpenAsync();

                var parameters = new SqliteParameter[] {
                    new SqliteParameter("BabyUuid", baby.Uuid),
                    new SqliteParameter("Date", date.ToString("O")),
                    new SqliteParameter("NextDate", nextDate.ToString("O"))
                };

                List<LogEntry> results = null;

                using (var command = connection.CreateCommand()) {
                    command.CommandText =
                        "SELECT * FROM LogEntry WHERE " +
                        "Time>=:Date AND " +
                        "Time<=:NextDate AND " +
                        "BabyUuid=:BabyUuid AND " +
                        "Deleted IS NULL " +
                        "ORDER BY Time DESC";
                    command.Parameters.AddRange(parameters);
                    var reader = await command.ExecuteReaderAsync();

                    var enumerator = from obj in reader.Cast<IDataRecord>()
                        select CreateLogEntry(obj);
                    results = await Task.Run(() => enumerator.ToList());
                    reader.Close();
                }

                connection.Close();
                return results;
            });
        }

        #endregion
        #region Baby

        private SqliteParameter[] CreateSqliteParameters(Baby baby, bool synced) {
            var deleted = (baby.Deleted.HasValue ? baby.Deleted.Value.ToString("O") : null);
            var profilePhotoUuid = (baby.ProfilePhoto != null ? baby.ProfilePhoto.Uuid : null);
            return new SqliteParameter[] {
                new SqliteParameter("Uuid", baby.Uuid),
                new SqliteParameter("Name", baby.Name),
                new SqliteParameter("Birthday", baby.Birthday.ToString("O")),
                new SqliteParameter("ShowBreastfeeding", baby.ShowBreastfeeding),
                new SqliteParameter("ShowPumped", baby.ShowPumped),
                new SqliteParameter("ShowFormula", baby.ShowFormula),
                new SqliteParameter("ProfilePhotoUuid", profilePhotoUuid),
                new SqliteParameter("ObjectId", baby.ObjectId),
                new SqliteParameter("Deleted", deleted),
                new SqliteParameter("Synced", synced)
            };
        }

        private Baby CreateBaby(IDataRecord rec) {
            var deleted = (rec["Deleted"] is DBNull)
                ? (DateTime?)null
                : DateTime.Parse((string)rec["Deleted"]);

            var baby = new Baby((string)rec["Uuid"]) {
                Name = (string)rec["Name"],
                Birthday = DateTime.Parse((string)rec["Birthday"]),
                ShowBreastfeeding = (bool)rec["ShowBreastfeeding"],
                ShowPumped = (bool)rec["ShowPumped"],
                ShowFormula = (bool)rec["ShowFormula"],
                Deleted = deleted,
                ObjectId = rec["ObjectId"] as string
            };

            var profilePhotoUuid = rec["ProfilePhotoUuid"] as string;
            if (profilePhotoUuid != null) {
                baby.ProfilePhoto = new Photo(baby, profilePhotoUuid);
            }

            return baby;
        }

        public async Task SaveAsync(Baby baby) {
            await SaveAsync("Baby", CreateSqliteParameters(baby, false));
        }

        public async Task DeleteAsync(Baby baby) {
            baby.Deleted = DateTime.Now;
            await SaveAsync(baby);
        }

        private async Task UpdateFromCloudAsync(Baby baby) {
            await UpdateFromCloudAsync("Baby", CreateSqliteParameters(baby, true));
        }

        public async Task<List<Baby>> FetchBabiesAsync() {
            return await EnqueueAsync(async () => {
                var connection = new SqliteConnection("Data Source=" + path);
                await connection.OpenAsync();

                var parameters = new SqliteParameter[] {
                };

                List<Baby> results = null;

                using (var command = connection.CreateCommand()) {
                    command.CommandText =
                        "SELECT * FROM Baby WHERE Deleted IS NULL";
                    command.Parameters.AddRange(parameters);
                    var reader = await command.ExecuteReaderAsync();

                    var enumerator = from obj in reader.Cast<IDataRecord>()
                        select CreateBaby(obj);
                    results = await Task.Run(() => enumerator.ToList());
                    reader.Close();
                }

                foreach (var baby in results) {
                    if (baby.ProfilePhoto != null) {
                        await LoadFileAsync(baby.ProfilePhoto);
                    }
                }

                connection.Close();
                return results;
            });
        }

        #endregion

        #region Photo

        public static string GetPath(Photo photo) {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                String.Format("photo_{0}", photo.Uuid));
        }

        private async Task SaveFileAsync(Photo photo) {
            if (photo.Bytes == null) {
                return;
            }
            var path = GetPath(photo);
            await SaveStreamAsync(path, photo.Bytes);
        }

        private async Task LoadFileAsync(Photo photo) {
            var path = GetPath(photo);
            if (!File.Exists(path)) {
                return;
            }
            var stream = new MemoryStream();
            var fileStream = File.OpenRead(path);
            await fileStream.CopyToAsync(stream);
            fileStream.Close();
            photo.Bytes = stream.ToArray();
        }

        private SqliteParameter[] CreateSqliteParameters(Photo photo, bool synced) {
            var deleted = (photo.Deleted.HasValue ? photo.Deleted.Value.ToString("O") : null);
            return new SqliteParameter[] {
                new SqliteParameter("Uuid", photo.Uuid),
                new SqliteParameter("BabyUuid", photo.Baby.Uuid),
                new SqliteParameter("ObjectId", photo.ObjectId),
                new SqliteParameter("Deleted", deleted),
                new SqliteParameter("Synced", synced)
            };
        }

        private Photo CreatePhoto(IDataRecord rec) {
            var deleted = (rec["Deleted"] is DBNull)
                ? (DateTime?)null
                : DateTime.Parse((string)rec["Deleted"]);

            return new Photo(new Baby((string)rec["BabyUuid"]), (string)rec["Uuid"]) {
                Deleted = deleted,
                ObjectId = rec["ObjectId"] as string
            };
        }

        public async Task SaveAsync(Photo photo) {
            await SaveFileAsync(photo);
            await SaveAsync("Photo", CreateSqliteParameters(photo, false));
        }

        public async Task DeleteAsync(Photo photo) {
            photo.Deleted = DateTime.Now;
            await SaveAsync(photo);
        }

        private async Task UpdateFromCloudAsync(Photo photo) {
            await SaveFileAsync(photo);
            await UpdateFromCloudAsync("Photo", CreateSqliteParameters(photo, true));
        }

        #endregion

        #region Statistics

        public async Task<List<LogEntry>> GetEntriesForStatisticsAsync(Baby baby) {
            List<LogEntry> entries = null;

            await EnqueueAsync(async () => {
                var connection = new SqliteConnection("Data Source=" + path);
                await connection.OpenAsync();

                var parameters = new SqliteParameter[] {
                    new SqliteParameter("BabyUuid", baby.Uuid),
                };

                using (var command = connection.CreateCommand()) {
                    command.CommandText =
                        "SELECT * FROM LogEntry WHERE " +
                        "BabyUuid=:BabyUuid AND " +
                        "Deleted IS NULL " +
                        "ORDER BY Time DESC";
                    command.Parameters.AddRange(parameters);
                    var reader = await command.ExecuteReaderAsync();
                    var results = from obj in reader.Cast<IDataRecord>()
                        select CreateLogEntry(obj);
                    entries = await Task.Run(() => results.Reverse().ToList());
                    reader.Close();
                }

                connection.Close();
                return true;
            });

            return entries;
        }

        #endregion
        #region Syncing

        private class ObjectAndVersion<T> where T : StorableObject {
            public T Object { get; set; }
            public long LocalVersion { get; set; }
        }

        private async Task<List<ObjectAndVersion<T>>> GetUnsyncedAsync<T>(
            string table, Func<IDataRecord, T> createFromDataRecord)
            where T : StorableObject {

            List<ObjectAndVersion<T>> unsaved = null;
            await EnqueueAsync<bool>(async () => {
                var connection = new SqliteConnection("Data Source=" + path);
                await connection.OpenAsync();

                // First, save all of the local objects that need to be saved to the cloud.
                using (var selectCommand = connection.CreateCommand()) {
                    selectCommand.CommandText =
                        "SELECT * FROM " + table + " " +
                        "WHERE (NOT Synced) OR ObjectId IS NULL";
                    var reader = await selectCommand.ExecuteReaderAsync();
                    var results = from obj in reader.Cast<IDataRecord>()
                        select new ObjectAndVersion<T> {
                            Object = createFromDataRecord(obj),
                            LocalVersion = (long)obj["LocalVersion"]
                        };
                    unsaved = await Task.Run(() => results.ToList());
                    reader.Close();
                }
                connection.Close();
                return true;
            });
            return unsaved;
        }

        private async Task MarkAsSyncedAsync<T>(ObjectAndVersion<T> objAndVersion, string table)
            where T : StorableObject {
            await EnqueueAsync<bool>(async () => {
                var connection = new SqliteConnection("Data Source=" + path);
                await connection.OpenAsync();

                using (var updateCommand = connection.CreateCommand()) {
                    var parameters = new SqliteParameter[] {
                        new SqliteParameter("Uuid", objAndVersion.Object.Uuid),
                        new SqliteParameter("ObjectId", objAndVersion.Object.ObjectId),
                        new SqliteParameter("Synced", true),
                        new SqliteParameter("LocalVersion", objAndVersion.LocalVersion)
                    };

                    // The LocalVersion clause stops us from overwriting changes made since the row was fetched.
                    updateCommand.CommandText =
                        "UPDATE " + table + " SET ObjectId=:ObjectId, Synced=:Synced " +
                        "WHERE Uuid=:Uuid AND LocalVersion=:LocalVersion";
                    updateCommand.Parameters.AddRange(parameters);
                    await updateCommand.ExecuteNonQueryAsync();
                }
                connection.Close();
                return true;
            });
        }

        public async Task SyncToCloudAsync(ICloudStore cloudStore, bool markNewAsRead) {
            List<ObjectAndVersion<Baby>> unsavedBabies =
                await GetUnsyncedAsync("Baby", CreateBaby);

            List<ObjectAndVersion<Photo>> unsavedPhotos =
                await GetUnsyncedAsync("Photo", CreatePhoto);

            foreach (var photo in unsavedPhotos) {
                await LoadFileAsync(photo.Object);
            }

            List<ObjectAndVersion<LogEntry>> unsavedEntries =
                await GetUnsyncedAsync("LogEntry", CreateLogEntry);

            foreach (var item in unsavedBabies) {
                var baby = item.Object;
                await cloudStore.SaveAsync(baby);
                await MarkAsSyncedAsync(item, "Baby");
            }

            foreach (var item in unsavedPhotos) {
                var photo = item.Object;
                await cloudStore.SaveAsync(photo);
                await MarkAsSyncedAsync(item, "Photo");
            }

            foreach (var item in unsavedEntries) {
                var entry = item.Object;
                await cloudStore.SaveAsync(entry);
                await MarkAsSyncedAsync(item, "LogEntry");
            }

            var req = new CloudFetchChangesRequest {
                LastEntryUpdatedAt = null,
                LastBabyUpdatedAt = null,
                LastPhotoUpdatedAt = null
            };

            // Get the record of when the last sync was.
            await EnqueueAsync<bool>(async () => {
                var connection = new SqliteConnection("Data Source=" + path);
                await connection.OpenAsync();

                // First, save all of the local objects that need to be saved to the cloud.
                using (var selectCommand = connection.CreateCommand()) {
                    selectCommand.CommandText =
                        "SELECT " +
                        "MAX(LastEntryUpdatedAt) AS e, " +
                        "MAX(LastBabyUpdatedAt) AS b, " +
                        "MAX(LastPhotoUpdatedAt) AS p " +
                        "FROM Sync";
                    var reader = await selectCommand.ExecuteReaderAsync();
                    var results = from obj in reader.Cast<IDataRecord>()
                                  select new {
                        LastBabyUpdatedAtString = obj["b"] as string,
                        LastEntryUpdatedAtString = obj["e"] as string,
                        LastPhotoUpdatedAtString = obj["p"] as string
                    };
                    var max = await Task.Run(() => results.FirstOrDefault());
                    if (max.LastBabyUpdatedAtString != null) {
                        req.LastBabyUpdatedAt = (DateTime?)DateTime.Parse(max.LastBabyUpdatedAtString);
                    }
                    if (max.LastEntryUpdatedAtString != null) {
                        req.LastEntryUpdatedAt = (DateTime?)DateTime.Parse(max.LastEntryUpdatedAtString);
                    }
                    if (max.LastPhotoUpdatedAtString != null) {
                        req.LastPhotoUpdatedAt = (DateTime?)DateTime.Parse(max.LastPhotoUpdatedAtString);
                    }
                    reader.Close();
                }
                connection.Close();
                return true;
            });

            // Now, read everything from the cloud and add it to the local store.
            // Yes, we do re-read everything we just wrote, but that won't be much, and at it
            // allows us to catch any changes the server may have made during the write.
            var changes = await cloudStore.FetchChangesAsync(req);
            foreach (var baby in changes.Babies) {
                await UpdateFromCloudAsync(baby);
            }
            foreach (var entry in changes.Entries) {
                await UpdateFromCloudAsync(entry, markNewAsRead);
            }
            foreach (var photo in changes.Photos) {
                await UpdateFromCloudAsync(photo);
            }

            // Record that this sync completed.
            {
                var lastEntryUpdatedAt = (changes.LastEntryUpdatedAt.HasValue
                    ? changes.LastEntryUpdatedAt.Value.ToString("O")
                    : null);
                var lastBabyUpdatedAt = (changes.LastBabyUpdatedAt.HasValue
                    ? changes.LastBabyUpdatedAt.Value.ToString("O")
                    : null);
                var lastPhotoUpdatedAt = (changes.LastPhotoUpdatedAt.HasValue
                    ? changes.LastPhotoUpdatedAt.Value.ToString("O")
                    : null);

                var parameters = new SqliteParameter[] {
                    new SqliteParameter("Uuid", Guid.NewGuid().ToString("D")),
                    new SqliteParameter("Finished", DateTime.Now.ToString("O")),
                    new SqliteParameter("LastEntryUpdatedAt", lastEntryUpdatedAt),
                    new SqliteParameter("LastPhotoUpdatedAt", lastPhotoUpdatedAt),
                    new SqliteParameter("LastBabyUpdatedAt", lastBabyUpdatedAt),
                    new SqliteParameter("EntryCount", changes.Entries.Count),
                    new SqliteParameter("PhotoCount", changes.Photos.Count),
                    new SqliteParameter("BabyCount", changes.Babies.Count)
                };

                await EnqueueAsync<bool>(async () => {
                    var connection = new SqliteConnection("Data Source=" + path);
                    await connection.OpenAsync();
                    using (var command = connection.CreateCommand()) {
                        command.CommandText = "INSERT INTO Sync (" +
                            "Uuid, " +
                            "Finished, " +
                            "LastEntryUpdatedAt, " +
                            "LastBabyUpdatedAt, " +
                            "LastPhotoUpdatedAt, " +
                            "EntryCount, " +
                            "PhotoCount, " +
                            "BabyCount" +
                            ") VALUES (" +
                            ":Uuid, " +
                            ":Finished, " +
                            ":LastEntryUpdatedAt, " +
                            ":LastBabyUpdatedAt, " +
                            ":LastPhotoUpdatedAt, " +
                            ":EntryCount, " +
                            ":PhotoCount, " +
                            ":BabyCount);";
                        command.Parameters.AddRange(parameters);
                        await command.ExecuteNonQueryAsync();
                    }
                    connection.Close();
                    return true;
                });
            }

            // Signal listeners that the database has been updated.
            if (RemotelyChanged != null) {
                RemotelyChanged(this, EventArgs.Empty);
            }

            if (changes.MaybeHasMore) {
                await SyncToCloudAsync(cloudStore, markNewAsRead);
            }
        }

        #endregion
        #region Unread Entries

        public async Task MarkAllAsReadAsync() {
            await EnqueueAsync<bool>(async () => {
                var connection = new SqliteConnection("Data Source=" + path);
                await connection.OpenAsync();

                var parameters = new SqliteParameter[] {
                    new SqliteParameter("Read", true),
                };

                using (var command = connection.CreateCommand()) {
                    command.CommandText = "UPDATE LogEntry SET Read=:Read";
                    command.Parameters.AddRange(parameters);
                    await command.ExecuteNonQueryAsync();
                }
                connection.Close();
                return true;
            });
        }

        public async Task<IEnumerable<LogEntry>> FetchUnreadAsync() {
            return await EnqueueAsync(async () => {
                var connection = new SqliteConnection("Data Source=" + path);
                await connection.OpenAsync();

                var parameters = new SqliteParameter[] {
                    new SqliteParameter("Read", false)
                };

                IEnumerable<LogEntry> results = null;

                using (var command = connection.CreateCommand()) {
                    command.CommandText =
                        "SELECT * FROM LogEntry WHERE Read=:Read AND Deleted IS NULL ORDER BY Time";
                    command.Parameters.AddRange(parameters);
                    var reader = await command.ExecuteReaderAsync();

                    // TODO: Fetch the baby too.

                    results = from obj in reader.Cast<IDataRecord>()
                        select CreateLogEntry(obj);
                    results = await Task.Run(() => results.ToList());
                    reader.Close();
                }

                connection.Close();
                return results;
            });
        }

        #endregion
    }
}

