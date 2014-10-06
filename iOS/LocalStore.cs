using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Mono.Data.Sqlite;
using Parse;

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

		public event EventHandler Changed;

		public LocalStore(CloudStore store) {
			cloudStore = store;

			path = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.Personal),
				"database5.db3");

			EnqueueAsync(async () => {
				if (!File.Exists(path)) {
					SqliteConnection.CreateFile(path);

					var connection = new SqliteConnection("Data Source=" + path);
					await connection.OpenAsync();
					using (var command = connection.CreateCommand()) {
						command.CommandText = "CREATE TABLE [LogEntry] (" +
						"[Uuid] varchar(255) PRIMARY KEY, [Time] string, [Text] varchar(255), " +
						"[Asleep] bool, [Poop] bool, [Formula] double, [ObjectId] varchar(255), " +
						"[Deleted] varchar(255), [Synced] bool, [LocalVersion] integer);";
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

		public async Task LogInAsync(string username, string password) {
			ParseUser.LogOut();
			await ParseUser.LogInAsync(username, password);
		}

		public async Task SignUpAsync(string username, string password) {
			ParseUser.LogOut();
			var user = new ParseUser();
			user.Username = username;
			user.Password = password;
			user.ACL = new ParseACL();
			await user.SignUpAsync();
		}

		public void LogOut() {
			ParseUser.LogOut();
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

			// Now, read everything from the cloud and add it to the local store.
			// TODO: Use UpdatedAt to only fetch changes since the last sync.
			// TODO: Add a writer ID so that we don't fetch the changes we just made.
			var objs = await cloudStore.FetchChangesAsync();
			foreach (var obj in objs) {
				await UpdateFromCloudAsync(obj);
			}

			// Signal listeners that the database has been updated.
			if (Changed != null) {
				Changed(this, EventArgs.Empty);
			}
		}
	}
}

