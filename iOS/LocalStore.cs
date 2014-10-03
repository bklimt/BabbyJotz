using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
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
			return new LogEntry((string)rec["Uuid"]) {
				DateTime = DateTime.Parse((string)rec["Time"]),
				Text = (string)rec["Text"],
				IsPoop = (bool)rec["Poop"],
				IsAsleep = (bool)rec["Asleep"],
				FormulaEaten = (decimal)((double)rec["Formula"]),
				ObjectId = rec["ObjectId"] as string
			};
		}

		public event EventHandler Changed;

		public LocalStore(CloudStore store) {
			cloudStore = store;

			path = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.Personal),
				"database2.db3");

			EnqueueAsync(async () => {
				if (!File.Exists(path)) {
					SqliteConnection.CreateFile(path);

					var connection = new SqliteConnection("Data Source=" + path);
					await connection.OpenAsync();
					using (var command = connection.CreateCommand()) {
						command.CommandText = "CREATE TABLE [LogEntry] (" +
							"[Uuid] varchar(255) PRIMARY KEY, [Time] string, [Text] varchar(255), " +
							"[Asleep] bool, [Poop] bool, [Formula] double, " +
							"[ObjectId] varchar(255), [Synced] bool);";
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

				var parameters = new SqliteParameter[] {
					new SqliteParameter("Uuid", entry.Uuid),
					new SqliteParameter("Time", entry.DateTime.ToString("O")),
					new SqliteParameter("Text", entry.Text),
					new SqliteParameter("Asleep", entry.IsAsleep),
					new SqliteParameter("Poop", entry.IsPoop),
					new SqliteParameter("Formula", entry.FormulaEaten),
					new SqliteParameter("Synced", false)
				};
				int rowsChanged = 0;

				using (var command = connection.CreateCommand()) {
					command.CommandText = "UPDATE LogEntry SET " +
						"Time=:Time, Text=:Text, Asleep=:Asleep, Poop=:Poop, Formula=:Formula, " +
						"Synced=:Synced WHERE Uuid=:Uuid";
					command.Parameters.AddRange(parameters);
					rowsChanged = await command.ExecuteNonQueryAsync();
				}

				if (rowsChanged < 1) {
					using (var command = connection.CreateCommand()) {
						command.CommandText = "INSERT INTO LogEntry(Uuid, Time, Text, Asleep, Poop, Formula, Synced) " +
							"VALUES (:Uuid, :Time, :Text, :Asleep, :Poop, :Formula, :Synced);";
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
						"SELECT * FROM LogEntry WHERE Time>=:Date AND Time<=:NextDate ORDER BY Time";
					command.Parameters.AddRange(parameters);
					var reader = await command.ExecuteReaderAsync();

					results = from obj in reader.Cast<IDataRecord>() select CreateEntryFromDataRecord(obj);
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
			await user.SignUpAsync();
		}

		public void LogOut() {
			ParseUser.LogOut();
		}

		public async Task UpdateFromCloudAsync(SqliteConnection conn, LogEntry entry) {
			var parameters = new SqliteParameter[] {
				new SqliteParameter("Uuid", entry.Uuid),
				new SqliteParameter("ObjectId", entry.ObjectId),
				new SqliteParameter("Time", entry.DateTime.ToString("O")),
				new SqliteParameter("Text", entry.Text),
				new SqliteParameter("Asleep", entry.IsAsleep),
				new SqliteParameter("Poop", entry.IsPoop),
				new SqliteParameter("Formula", entry.FormulaEaten),
				new SqliteParameter("Synced", true)
			};
			int rowsChanged = 0;

			using (var command = conn.CreateCommand()) {
				command.CommandText = "UPDATE LogEntry SET " +
					"ObjectId=:ObjectId, Time=:Time, Text=:Text, Asleep=:Asleep, Poop=:Poop, Formula=:Formula, " +
					"Synced=:Synced WHERE Uuid=:Uuid";
				command.Parameters.AddRange(parameters);
				rowsChanged = await command.ExecuteNonQueryAsync();
			}

			if (rowsChanged < 1) {
				using (var command = conn.CreateCommand()) {
					command.CommandText = "INSERT INTO LogEntry " +
						"(Uuid, ObjectId, Time, Text, Asleep, Poop, Formula, Synced) " +
						"VALUES (:Uuid, :ObjectId, :Time, :Text, :Asleep, :Poop, :Formula, :Synced);";
					command.Parameters.AddRange(parameters);
					rowsChanged = await command.ExecuteNonQueryAsync();
				}
			}
		}

		public async Task SyncToCloudAsync() {
			await EnqueueAsync<bool>(async () => {
				var connection = new SqliteConnection("Data Source=" + path);
				await connection.OpenAsync();

				// First, save all of the local objects that need to be saved to the cloud.
				using (var selectCommand = connection.CreateCommand()) {
					selectCommand.CommandText = "SELECT * FROM LogEntry WHERE (NOT Synced) OR ObjectId IS NULL";
					var reader = await selectCommand.ExecuteReaderAsync();

					var results = from obj in reader.Cast<IDataRecord>() select CreateEntryFromDataRecord(obj);

					foreach (var entry in results) {
						await cloudStore.SaveAsync(entry);
						using (var updateCommand = connection.CreateCommand()) {
							var parameters = new SqliteParameter[] {
								new SqliteParameter("Uuid", entry.Uuid),
								new SqliteParameter("ObjectId", entry.ObjectId),
								new SqliteParameter("Synced", true)
							};
							updateCommand.CommandText =
								"Update LogEntry SET ObjectId=:ObjectId, Synced=:Synced WHERE Uuid=:Uuid";
							updateCommand.Parameters.AddRange(parameters);
							await updateCommand.ExecuteNonQueryAsync();
						}
					}

					reader.Close();
				}

				// Now, read everything from the cloud and add it to the local store.
				// TODO: Use UpdatedAt to only fetch changes since the last sync.
				// TODO: Add a writer ID so that we don't fetch the changes we just made.
				var objs = await cloudStore.FetchChangesAsync();
				foreach (var obj in objs) {
					await UpdateFromCloudAsync(connection, obj);
				}

				connection.Close();
				return true;
			});

			// Signal listeners that the database has been updated.
			if (Changed != null) {
				Changed(this, EventArgs.Empty);
			}
		}
	}
}

