using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Mono.Data.Sqlite;

namespace BabbyJotz.iOS {
    public class LocalStore : ILocalStore {
        private static readonly string databaseFile = "database.db3";
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

                string UUID = "varchar(72)";
                string DATE_TIME = "varchar(72)";
                string OBJECT_ID = "varchar(72)";

                var connection = new SqliteConnection("Data Source=" + path);
                await connection.OpenAsync();
                using (var command = connection.CreateCommand()) {
                    command.CommandText = "CREATE TABLE [Baby] (" +
                        "[Uuid] " + UUID + " PRIMARY KEY, " +
                        "[Name] varchar(255) NOT NULL, " +
                        "[Birthday] " + DATE_TIME + " NOT NULL, " +
                        "[ShowBreastfeeding] bool NOT NULL, " +
                        "[ShowPumped] bool NOT NULL, " +
                        "[ShowFormula] bool NOT NULL, " +
                        "[ProfilePhotoUuid] " + UUID + ", " +
                        "[ObjectId] " + OBJECT_ID + ", " +
                        "[Deleted] " + DATE_TIME + ", " +
                        "[LastSyncDate] " + DATE_TIME + ", " +
                        "[Synced] bool, " +
                        "[Extra] text, " +
                        "[LocalVersion] integer NOT NULL);";
                    await command.ExecuteNonQueryAsync();
                }
                using (var command = connection.CreateCommand()) {
                    command.CommandText = "CREATE TABLE [LogEntry] (" +
                        "[Uuid] " + UUID + " PRIMARY KEY, " +
                        "[BabyUuid] " + UUID + " NOT NULL, " +
                        "[Time] " + DATE_TIME + ", " +
                        "[Text] text, " +
                        "[Asleep] bool, " +
                        "[Poop] bool, " +
                        "[FormulaEatenOunces] double, " +
                        "[PumpedEatenOunces] double, " +
                        "[LeftBreastEatenMinutes] double, " +
                        "[RightBreastEatenMinutes] double, " +
                        "[ObjectId] " + OBJECT_ID + ", " +
                        "[Read] bool, " +
                        "[Deleted] " + DATE_TIME + ", " +
                        "[Synced] bool, " +
                        "[Extra] text, " +
                        "[LocalVersion] integer NOT NULL);";
                    await command.ExecuteNonQueryAsync();
                }
                using (var command = connection.CreateCommand()) {
                    command.CommandText = "CREATE TABLE [Photo] (" +
                        "[Uuid] " + UUID + " PRIMARY KEY, " +
                        "[BabyUuid] " + UUID + " NOT NULL, " +
                        "[ObjectId] " + OBJECT_ID + ", " +
                        "[Deleted] " + DATE_TIME + ", " +
                        "[Synced] bool, " +
                        "[Extra] text, " +
                        "[LocalVersion] integer NOT NULL);";
                    await command.ExecuteNonQueryAsync();
                }
                using (var command = connection.CreateCommand()) {
                    command.CommandText = "CREATE TABLE [Sync] (" +
                        "[Uuid] " + UUID + " PRIMARY KEY, " +
                        "[Started] " + DATE_TIME + " NOT NULL, " +
                        "[Finished] " + DATE_TIME + " NOT NULL, " +
                        "[Extra] text);";
                    await command.ExecuteNonQueryAsync();
                }
                using (var command = connection.CreateCommand()) {
                    command.CommandText = "CREATE TABLE [BabySync] (" +
                        "[ClassName] varchar(255) NOT NULL, " +
                        "[BabyUuid] " + UUID + " NOT NULL, " +
                        "[LastUpdatedAt] " + DATE_TIME + " NULL, " +
                        "PRIMARY KEY (ClassName, BabyUuid));";
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

        private LogEntry CreateLogEntry(IDataRecord rec, Baby baby) {
            var deleted = (rec["Deleted"] is DBNull) ? (DateTime?)null : DateTime.Parse((string)rec["Deleted"]);
            if (baby == null) {
                baby = new Baby((string)rec["BabyUuid"]);
            }
            return new LogEntry(baby, (string)rec["Uuid"]) {
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
                        select CreateLogEntry(obj, baby);
                    results = await Task.Run(() => enumerator.ToList());
                    reader.Close();
                }

                connection.Close();
                return results;
            });
        }

        #endregion
        #region Baby

        private SqliteParameter[] CreateSqliteParameters(Baby baby, string lastSyncDate) {
            var synced = (lastSyncDate != null);
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
                new SqliteParameter("lastSyncDate", lastSyncDate),
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
            await SaveAsync("Baby", CreateSqliteParameters(baby, null));
        }

        public async Task DeleteAsync(Baby baby) {
            baby.Deleted = DateTime.Now;
            await SaveAsync(baby);
        }

        private async Task UpdateFromCloudAsync(Baby baby, string syncDate) {
            await UpdateFromCloudAsync("Baby", CreateSqliteParameters(baby, syncDate));
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
                        select CreateLogEntry(obj, baby);
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

        private async Task SaveChangesToCloudAsync(ICloudStore cloudStore) {
            List<ObjectAndVersion<Baby>> unsavedBabies =
                await GetUnsyncedAsync("Baby", CreateBaby);

            List<ObjectAndVersion<Photo>> unsavedPhotos =
                await GetUnsyncedAsync("Photo", CreatePhoto);

            foreach (var photo in unsavedPhotos) {
                await LoadFileAsync(photo.Object);
            }

            List<ObjectAndVersion<LogEntry>> unsavedEntries =
                await GetUnsyncedAsync("LogEntry", (obj) => CreateLogEntry(obj, null));

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
        }

        private async Task DeleteBabiesNotSeenSinceAsync(ICloudStore cloudStore, string syncDate) {
            await EnqueueAsync<bool>(async () => {
                var connection = new SqliteConnection("Data Source=" + path);
                await connection.OpenAsync();
                using (var command = connection.CreateCommand()) {
                    var parameters = new SqliteParameter[] {
                        new SqliteParameter("lastSyncDate", syncDate),
                    };

                    command.CommandText = "DELETE FROM Baby " +
                        "WHERE lastSyncDate IS NOT NULL " +
                        "AND lastSyncDate <> :lastSyncDate;";
                    command.Parameters.AddRange(parameters);
                    await command.ExecuteNonQueryAsync();
                }
                connection.Close();
                return true;
            });
        }

        private async Task<DateTime?> GetLastUpdatedAtAsync(
            ICloudStore cloudStore, string className, Baby baby) {

            DateTime? lastUpdatedAt = null;
            await EnqueueAsync<bool>(async () => {
                var connection = new SqliteConnection("Data Source=" + path);
                await connection.OpenAsync();

                // First, save all of the local objects that need to be saved to the cloud.
                using (var selectCommand = connection.CreateCommand()) {
                    var parameters = new SqliteParameter[] {
                        new SqliteParameter("ClassName", className),
                        new SqliteParameter("BabyUuid", baby.Uuid)
                    };

                    selectCommand.CommandText =
                        "SELECT LastUpdatedAt FROM BabySync " +
                        "WHERE ClassName=:ClassName " +
                        "AND BabyUuid=:BabyUuid";
                    selectCommand.Parameters.AddRange(parameters);

                    var reader = await selectCommand.ExecuteReaderAsync();
                    var results = from obj in reader.Cast<IDataRecord>()
                        select obj["LastUpdatedAt"] as string;
                    var lastUpdatedAtString = await Task.Run(() => results.FirstOrDefault());
                    if (lastUpdatedAtString != null) {
                        lastUpdatedAt = (DateTime?)DateTime.Parse(lastUpdatedAtString);
                    }
                    reader.Close();
                }
                connection.Close();
                return true;
            });
            return lastUpdatedAt;
        }

        private async Task SetLastUpdatedAtAsync(
            ICloudStore cloudStore, string className, Baby baby, DateTime? lastUpdatedAt) {

            await EnqueueAsync<bool>(async () => {
                var connection = new SqliteConnection("Data Source=" + path);
                await connection.OpenAsync();
                bool isNew = false;

                string lastUpdatedAtString = null;
                if (lastUpdatedAt != null) {
                    lastUpdatedAtString = lastUpdatedAt.Value.ToString("O");
                }

                var parameters = new SqliteParameter[] {
                    new SqliteParameter("ClassName", className),
                    new SqliteParameter("BabyUuid", baby.Uuid),
                    new SqliteParameter("LastUpdatedAt", lastUpdatedAtString)
                };

                var fields = (from param in parameters
                    select param.ParameterName).ToArray();

                // There is no race condition with this approach because all of these steps
                // are parts of one block in a serial queue.
                using (var command = connection.CreateCommand()) {
                    command.CommandText = "SELECT COUNT(*) FROM BabySync " +
                        "WHERE ClassName=:ClassName " +
                        "AND BabyUuid=:BabyUuid;";
                    command.Parameters.AddRange(parameters);
                    var count = await command.ExecuteScalarAsync();
                    isNew = (((long)count) == 0);
                }

                if (isNew) {
                    using (var command = connection.CreateCommand()) {
                        command.CommandText = "INSERT INTO BabySync (" +
                            CreateInsertString(fields) +
                            ") VALUES (" +
                            CreateValuesString(fields) +
                            ");";
                        command.Parameters.AddRange(parameters);
                        await command.ExecuteNonQueryAsync();
                    }
                } else {
                    using (var command = connection.CreateCommand()) {
                        // The WHERE Synced clause keeps us from overwriting local changes made since the sync started.
                        command.CommandText = "UPDATE BabySync SET " +
                            "LastUpdatedAt=:LastUpdatedAt " +
                            "WHERE ClassName=:ClassName AND BabyUuid=:BabyUuid;";
                        command.Parameters.AddRange(parameters);
                        await command.ExecuteNonQueryAsync();
                    }
                }

                return true;
            });
        }

        private async Task<bool> SyncEntriesForBaby(ICloudStore cloudStore, Baby baby, bool markAsRead) {
            var lastUpdatedAt = await GetLastUpdatedAtAsync(cloudStore, "LogEntry", baby);
            var response = await cloudStore.FetchEntriesSinceAsync(baby, lastUpdatedAt);
            foreach (var entry in response.Results) {
                await UpdateFromCloudAsync(entry, markAsRead);
            }
            await SetLastUpdatedAtAsync(cloudStore, "LogEntry", baby, response.NewUpdatedAt);
            return response.MaybeHasMore;
        }

        private async Task<bool> SyncPhotosForBaby(ICloudStore cloudStore, Baby baby) {
            var lastUpdatedAt = await GetLastUpdatedAtAsync(cloudStore, "Photo", baby);
            var response = await cloudStore.FetchPhotosSinceAsync(baby, lastUpdatedAt);
            foreach (var photo in response.Results) {
                await UpdateFromCloudAsync(photo);
            }
            await SetLastUpdatedAtAsync(cloudStore, "Photo", baby, response.NewUpdatedAt);
            return response.MaybeHasMore;
        }

        private async Task SyncBabiesFromCloudAsync(ICloudStore cloudStore, bool markAsRead) {
            var syncDate = DateTime.Now.ToString("O");
            var babies = await cloudStore.FetchAllBabiesAsync();
            foreach (var baby in babies) {
                await UpdateFromCloudAsync(baby, syncDate);
                while (await SyncEntriesForBaby(cloudStore, baby, markAsRead)) {
                }
                while (await SyncPhotosForBaby(cloudStore, baby)) {
                }
            }
            await DeleteBabiesNotSeenSinceAsync(cloudStore, syncDate);
        }

        public async Task SyncToCloudAsync(ICloudStore cloudStore, bool markNewAsRead) {
            DateTime startTime = DateTime.Now;
            await SaveChangesToCloudAsync(cloudStore);
            await SyncBabiesFromCloudAsync(cloudStore, markNewAsRead);
            DateTime finishedTime = DateTime.Now;

            {
                var parameters = new SqliteParameter[] {
                    new SqliteParameter("Uuid", Guid.NewGuid().ToString("D")),
                    new SqliteParameter("Started", startTime.ToString("O")),
                    new SqliteParameter("Finished", finishedTime.ToString("O"))
                };

                await EnqueueAsync<bool>(async () => {
                    var connection = new SqliteConnection("Data Source=" + path);
                    await connection.OpenAsync();
                    using (var command = connection.CreateCommand()) {
                        command.CommandText = "INSERT INTO Sync (" +
                            "Uuid, " +
                            "Started, " +
                            "Finished" +
                            ") VALUES (" +
                            ":Uuid, " +
                            ":Started, " +
                            ":Finished);";
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
                        select CreateLogEntry(obj, null);
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

