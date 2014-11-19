using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Mono.Data.Sqlite;

#if __IOS__
using MonoTouch.CoreFoundation;
using MonoTouch.Foundation;
#else
using Android.OS;
#endif

namespace BabbyJotz.iOS {
    public class LocalStore : ILocalStore {
        private static readonly string databaseFile = "database.db3";
        private string path;

        public event EventHandler LocallyChanged;
        public event EventHandler RemotelyChanged;

        public LocalStore() {
            path = Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal),
                databaseFile);

            EnqueueAsync(async () => {
                await CreateDatabaseAsync();
                return true;
            });
        }

        public static Task RunOnMainThreadAsync(Action action) {
            var tcs = new TaskCompletionSource<object>();
            #if __IOS__
            DispatchQueue.MainQueue.DispatchAsync(() => {
            #else
            var handler = new Handler(Looper.MainLooper);
            handler.Post(() => {
            #endif
                try {
                    action();
                    tcs.SetResult(null);
                } catch (Exception e) {
                    tcs.SetException(e);
                }
            });

            return tcs.Task;
        }

        private async Task NotifyRemotelyChangedAsync() {
            if (RemotelyChanged != null) {
                await RunOnMainThreadAsync(() => {
                    RemotelyChanged(this, EventArgs.Empty);
                });
            }
        }

        private async Task NotifyLocallyChangedAsync() {
            if (LocallyChanged != null) {
                await RunOnMainThreadAsync(() => {
                    LocallyChanged(this, EventArgs.Empty);
                });
            }
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
                await NotifyRemotelyChangedAsync();
                return true;
            });
        }

        #endregion
        #region Utilities

        private async Task SaveStreamAsync(string path, byte[] bytes) {
            await EnqueueAsync(async () => {
                var file = File.Create(path);
                await file.WriteAsync(bytes, 0, bytes.Length);
                file.Close();
                return true;
            });
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

        private async Task UpsertAllAsync<T>(
            string table,
            List<T> items,
            bool onlyUpdateIfSynced,
            InstrumentedProcess process,
            Func<T, SqliteParameter[]> sqlParamsFunc) {

            var i = 0;

            await EnqueueAsync<bool>(async () => {
                var conn = new SqliteConnection("Data Source=" + path);
                await conn.OpenAsync();

                foreach (var item in items) {
                    if (process != null) {
                        process.Progress = (double)i / items.Count;
                    }
                    i++;

                    bool isNew = false;
                    var sqlParams = sqlParamsFunc(item);
                    var fields = (from param in sqlParams
                                  select param.ParameterName).ToArray();

                    // There is no race condition with this approach because all of these steps
                    // are parts of one block in a serial queue.
                    using (var command = conn.CreateCommand()) {
                        // TODO: This is almost always an INSERT, so let's try that first instead.
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
                }

                if (process != null) {
                    process.AssertFinished();
                }
                conn.Close();
                return true;
            });
        }

        public async Task SaveAsync<T>(string table, T item, Func<T, SqliteParameter[]> sqlParams) {
            await UpsertAllAsync(table, new List<T> { item }, false, null, sqlParams);
            await NotifyLocallyChangedAsync();
        }

        private async Task UpdateAllFromCloudAsync<T>(
            string table,
            List<T> items,
            InstrumentedProcess process,
            Func<T, SqliteParameter[]> sqlParams) {

            await UpsertAllAsync(table, items, true, process, sqlParams);
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
            await SaveAsync("LogEntry", entry, e => CreateSqliteParameters(e, true, false));
        }

        public async Task DeleteAsync(LogEntry entry) {
            entry.Deleted = DateTime.Now;
            await SaveAsync(entry);
        }

        private async Task UpdateAllFromCloudAsync(List<LogEntry> entries, bool markAsRead, InstrumentedProcess process) {
            await UpdateAllFromCloudAsync(
                "LogEntry", entries, process, entry => CreateSqliteParameters(entry, markAsRead, true));
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
            await SaveAsync("Baby", baby, b => CreateSqliteParameters(b, null));
        }

        public async Task DeleteAsync(Baby baby) {
            baby.Deleted = DateTime.Now;
            await SaveAsync(baby);
        }

        private async Task UpdateFromCloudAsync(Baby baby, string syncDate) {
            await UpdateAllFromCloudAsync("Baby", new List<Baby> { baby }, null, b => CreateSqliteParameters(b, syncDate));
        }

        public async Task<List<Baby>> FetchBabiesAsync() {
            List<Baby> results = null;

            await EnqueueAsync(async () => {
                var connection = new SqliteConnection("Data Source=" + path);
                await connection.OpenAsync();

                var parameters = new SqliteParameter[] {
                };

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

                connection.Close();
                return true;
            });

            foreach (var baby in results) {
                if (baby.ProfilePhoto != null) {
                    await LoadFileAsync(baby.ProfilePhoto);
                }
            }

            return results;
        }

        #endregion

        #region Photo

        private static string GetPath(Photo photo) {
            return Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal),
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
            await EnqueueAsync(async () => {
                var path = GetPath(photo);
                if (!File.Exists(path)) {
                    return true;
                }
                var stream = new MemoryStream();
                var fileStream = File.OpenRead(path);
                await fileStream.CopyToAsync(stream);
                fileStream.Close();
                photo.Bytes = stream.ToArray();
                return true;
            });
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
            await SaveAsync("Photo", photo, p => CreateSqliteParameters(p, false));
        }

        public async Task DeleteAsync(Photo photo) {
            photo.Deleted = DateTime.Now;
            await SaveAsync(photo);
        }

        private async Task UpdateAllFromCloudAsync(List<Photo> photos, InstrumentedProcess process) {
            var saveFileProcess = process.SubProcess("SaveFileAsync", 0.1);
            var i = 0;
            foreach (var photo in photos) {
                saveFileProcess.Progress = (double)i / photos.Count;
                i++;
                await SaveFileAsync(photo);
            }
            saveFileProcess.AssertFinished();

            var updateProcess = process.SubProcess("UpdateAllFromCloud", 0.9);
            await UpdateAllFromCloudAsync(
                "Photo", photos, updateProcess, photo => CreateSqliteParameters(photo, true));

            process.AssertFinished();
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

        private async Task SaveChangesToCloudAsync(ICloudStore cloudStore, InstrumentedProcess process) {

            /*
             * Progress breakdown is like this:
             * 0.1 - Getting unsynced babies.
             * 0.1 - Getting unsynced photos.
             * 0.1 - Loading unsaved photos.
             * 0.1 - Getting unsynced entries.
             * 0.2 - Saving babies.
             * 0.2 - Saving photos.
             * 0.2 - Saving entries.
             */

            process.Progress = 0.0;

            var unsavedBabiesProcess = process.SubProcess("GetUnsynced(Baby)", 0.1);
            List<ObjectAndVersion<Baby>> unsavedBabies = await GetUnsyncedAsync("Baby", CreateBaby);
            unsavedBabiesProcess.AssertFinished();

            var unsavedPhotosProcess = process.SubProcess("GetUnsynced(Photo)", 0.1);
            List<ObjectAndVersion<Photo>> unsavedPhotos = await GetUnsyncedAsync("Photo", CreatePhoto);
            unsavedPhotosProcess.AssertFinished();

            var loadFilesProcess = process.SubProcess("LoadFileAsync", 0.1);
            foreach (var photo in unsavedPhotos) {
                await LoadFileAsync(photo.Object);
            }
            loadFilesProcess.AssertFinished();

            var unsavedEntriesProcess = process.SubProcess("GetUnsynced(LogEntry)", 0.1);
            List<ObjectAndVersion<LogEntry>> unsavedEntries =
                await GetUnsyncedAsync("LogEntry", (obj) => CreateLogEntry(obj, null));
            unsavedEntriesProcess.AssertFinished();

            var saveBabiesProcess = process.SubProcess("Save Babies", 0.2);
            foreach (var item in unsavedBabies) {
                var baby = item.Object;
                var babyProcess = saveBabiesProcess.SubProcess("Baby " + baby.Uuid, 1.0 / unsavedBabies.Count);

                var saveBabyProcess = babyProcess.SubProcess("Save", 0.5);
                await cloudStore.SaveAsync(baby, saveBabyProcess.CancellationToken);
                saveBabyProcess.AssertFinished();

                var markBabySyncedProcess = babyProcess.SubProcess("MarkAsSynced", 0.5);
                await MarkAsSyncedAsync(item, "Baby");
                markBabySyncedProcess.AssertFinished();

                babyProcess.AssertFinished();
            }
            saveBabiesProcess.AssertFinished();

            var savePhotosProcess = process.SubProcess("Save Photos", 0.2);
            foreach (var item in unsavedPhotos) {
                var photo = item.Object;
                var photoProcess = savePhotosProcess.SubProcess("Photo " + photo.Uuid, 1.0 / unsavedPhotos.Count);

                var savePhotoProcess = photoProcess.SubProcess("Save", 0.5);
                await cloudStore.SaveAsync(photo, savePhotoProcess.CancellationToken);
                savePhotoProcess.AssertFinished();

                var markPhotoSyncedProcess = photoProcess.SubProcess("MarkAsSynced", 0.5);
                await MarkAsSyncedAsync(item, "Photo");
                markPhotoSyncedProcess.AssertFinished();
            }
            savePhotosProcess.AssertFinished();

            var saveEntriesProcess = process.SubProcess("Save Entries", 0.2);
            foreach (var item in unsavedEntries) {
                var entry = item.Object;
                var entryProcess = saveEntriesProcess.SubProcess("Entry " + entry.Uuid, 1.0 / unsavedEntries.Count);

                var saveEntryProcess = entryProcess.SubProcess("Save", 0.5);
                await cloudStore.SaveAsync(entry, saveEntryProcess.CancellationToken);
                saveEntryProcess.AssertFinished();

                var markEntrySyncedProcess = entryProcess.SubProcess("MarkAsSynced", 0.5);
                await MarkAsSyncedAsync(item, "LogEntry");
                markEntrySyncedProcess.AssertFinished();
            }
            saveEntriesProcess.AssertFinished();

            process.AssertFinished();
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

        private async Task<bool> SyncEntriesForBaby(
            ICloudStore cloudStore, Baby baby, bool markAsRead, InstrumentedProcess process) {

            /*
             * Progress is allocated like this:
             * 0.05 - GetLastUpdatedAt
             * 0.25 - FetchEntriesSince
             * 0.65 - UpdateFromCloud
             * 0.05 - SetLastUpdatedAt
             */

            var lastUpdatedAtProcess = process.SubProcess("GetLastUpdatedAt", 0.05);
            var lastUpdatedAt = await GetLastUpdatedAtAsync(cloudStore, "LogEntry", baby);
            lastUpdatedAtProcess.AssertFinished();

            var fetchProcess = process.SubProcess("FetchEntriesSince", 0.25);
            var response = await cloudStore.FetchEntriesSinceAsync(baby, lastUpdatedAt, fetchProcess.CancellationToken);
            fetchProcess.AssertFinished();

            var updateProcess = process.SubProcess("UpdateFromCloud", 0.65);
            await UpdateAllFromCloudAsync(response.Results, markAsRead, updateProcess);

            var setLastUpdatedAtProcess = process.SubProcess("SetLastUpdatedAt", 0.05);
            await SetLastUpdatedAtAsync(cloudStore, "LogEntry", baby, response.NewUpdatedAt);
            setLastUpdatedAtProcess.AssertFinished();

            process.AssertFinished();
            return response.MaybeHasMore;
        }

        private async Task<bool> SyncPhotosForBaby(ICloudStore cloudStore, Baby baby, InstrumentedProcess process) {
            var lastUpdatedAtProcess = process.SubProcess("GetLastUpdatedAt", 0.05);
            var lastUpdatedAt = await GetLastUpdatedAtAsync(cloudStore, "Photo", baby);
            lastUpdatedAtProcess.AssertFinished();

            var fetchProcess = process.SubProcess("FetchPhotosSince", 0.25);
            var response = await cloudStore.FetchPhotosSinceAsync(baby, lastUpdatedAt, fetchProcess.CancellationToken);
            fetchProcess.AssertFinished();

            var updateProcess = process.SubProcess("UpdateFromCloud", 0.65);
            await UpdateAllFromCloudAsync(response.Results, updateProcess);

            var setLastUpdatedAtProcess = process.SubProcess("SetLastUpdatedAt", 0.05);
            await SetLastUpdatedAtAsync(cloudStore, "Photo", baby, response.NewUpdatedAt);
            setLastUpdatedAtProcess.AssertFinished();

            process.AssertFinished();
            return response.MaybeHasMore;
        }

        private async Task SyncBabiesFromCloudAsync(
            ICloudStore cloudStore, bool markAsRead, InstrumentedProcess process) {

            /*
             * Progress breakdown is like this:
             * 0.05 - Fetch all babies.
             * 0.90 - Fetching each baby.
             *        0.1 - UpdateFromCloud
             *        0.7 - Entries
             *        0.2 - Photos
             * 0.05 - Delete unseen babies.
             */

            var syncDate = DateTime.Now.ToString("O");

            var fetchBabiesProcess = process.SubProcess("FetchAllBabies", 0.05);
            var babies = await cloudStore.FetchAllBabiesAsync(fetchBabiesProcess.CancellationToken);
            fetchBabiesProcess.AssertFinished();

            await NotifyRemotelyChangedAsync();

            var syncBabiesProcess = process.SubProcess("Sync Babies", 0.9);
            foreach (var baby in babies) {
                var syncBabyProcess = syncBabiesProcess.SubProcess("Baby " + baby.Uuid, 1.0 / babies.Count);

                var updateProcess = syncBabyProcess.SubProcess("UpdateFromCloud", 0.1);
                await UpdateFromCloudAsync(baby, syncDate);
                updateProcess.AssertFinished();


                var photosProcess = syncBabyProcess.SubProcess("SyncPhotos", 0.2);
                // Since it could loop infinitely, let's progress model an infinite series: 0.5, 0.25, 0.125, ...
                var iteration = 0;
                var weight = 0.5;
                var photoProcess = photosProcess.SubProcess("SyncPhotos " + iteration, weight);
                while (await SyncPhotosForBaby(cloudStore, baby, photoProcess)) {
                    iteration++;
                    weight /= 2.0;
                    photoProcess = photosProcess.SubProcess("SyncPhotos " + iteration, weight);
                    await NotifyRemotelyChangedAsync();
                }
                photosProcess.AssertFinished();
                await NotifyRemotelyChangedAsync();

                var entriesProcess = syncBabyProcess.SubProcess("SyncEntries", 0.7);
                // Since it could loop infinitely, let's progress model an infinite series: 0.5, 0.25, 0.125, ...
                iteration = 0;
                weight = 0.5;
                var entryProcess = entriesProcess.SubProcess("SyncEntries " + iteration, weight);
                while (await SyncEntriesForBaby(cloudStore, baby, markAsRead, entryProcess)) {
                    iteration++;
                    weight /= 2.0;
                    entryProcess = entriesProcess.SubProcess("SyncEntries " + iteration, weight);
                    await NotifyRemotelyChangedAsync();
                }
                entriesProcess.AssertFinished();
                await NotifyRemotelyChangedAsync();

                syncBabyProcess.AssertFinished();

            }
            syncBabiesProcess.AssertFinished();

            var deleteProcess = process.SubProcess("DeleteBabiesNotSeenSince", 0.05);
            await DeleteBabiesNotSeenSinceAsync(cloudStore, syncDate);
            deleteProcess.AssertFinished();

            process.AssertFinished();

            await NotifyRemotelyChangedAsync();
        }

        public async Task SyncToCloudAsync(
            ICloudStore cloudStore,
            bool markNewAsRead,
            InstrumentedProcess process) {

            cloudStore.LogEvent("ParseStore.SyncToCloudAsync.Started");

            /*
             * The breakdown for progress is like this:
             * 0.15 - Save changes.
             * 0.80 - Sync babies.
             * 0.05 - Save sync status to the database.
             */

            process.Progress = 0.0;

            DateTime startTime = DateTime.Now;
            var saveProcess = process.SubProcess("SaveChangesToCloud", 0.15);
            await SaveChangesToCloudAsync(cloudStore, saveProcess);

            var syncProcess = process.SubProcess("SyncBabiesFromCloudAsync", 0.80);
            await SyncBabiesFromCloudAsync(cloudStore, markNewAsRead, syncProcess);

            DateTime finishedTime = DateTime.Now;

            await NotifyRemotelyChangedAsync();

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

            process.AssertFinished();

            // Signal listeners that the database has been updated.
            await NotifyRemotelyChangedAsync();

            try {
                // We just did a sync to the cloud, so saving a log file should usually work.
                var report = process.GenerateReport();
                await cloudStore.LogSyncReportAsync(report);
            } catch (Exception e) {
                // Meh.
                cloudStore.LogException("LogSyncReport", e);
            }

            cloudStore.LogEvent("ParseStore.SyncToCloudAsync.Finished");
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

