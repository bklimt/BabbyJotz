using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BabbyJotz {
	public interface IDataStore {
		// Fired whenever the contents of the database change.
		event EventHandler Changed;

		// Basic CRUD.
		Task SaveAsync(LogEntry entry);
		Task<IEnumerable<LogEntry>> FetchAsync(DateTime day);

		// Cloud Syncing.
		string CloudUserName { get; }
		Task SyncToCloudAsync();
	}
}

