using System;
using System.IO;
using System.Threading.Tasks;
// using Mono.Data.Sqlite;

namespace BabbyJotz {
	public class Database {
		/*
		private static object instanceLock = new object();
		private static Database instance;

		public static Database DefaultInstance {
			get {
				lock (instanceLock) {
					string path = Path.Combine(
						Environment.GetFolderPath(Environment.SpecialFolder.Personal),
						"database.db3");

					if (instance == null) {
						instance = new Database(path);
					}
				}
				return instance;
			}
		}

		private TaskQueue queue = new TaskQueue();

		private Database(string path) {
			Enqueue(() => {
				if (!File.Exists(path)) {
					// SqliteConnection.CreateFile(path);
				}
				return true;
			});
		}

		private Task<T> Enqueue<T>(Func<T> func) {
			return queue.Enqueue(Task.Run(func));
		}*/
	}
}

