﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BabbyJotz {
	public interface IDataStore {
		// Fired whenever the contents of the database change.
		event EventHandler Changed;

		// Basic CRUD
		Task SaveAsync(LogEntry entry);
		Task DeleteAsync(LogEntry entry);
		Task<IEnumerable<LogEntry>> FetchAsync(DateTime day);

		// Statistics
		Task GetStatisticsAsync(Statistics stats);

		// Cloud Syncing
		string CloudUserName { get; }
        string CloudUserId { get; }
		Task LogInAsync(string username, string password);
		Task SignUpAsync(string username, string password);
		void LogOut();
		Task SyncToCloudAsync();
	}
}

