using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BabbyJotz {
	public interface IDataStore {
		bool IsLoggedIn { get; }
		Task SaveAsync(LogEntry entry);
		Task<IEnumerable<LogEntry>> FetchAsync(DateTime day);
		Task SyncToCloudAsync();
		event EventHandler Changed;
	}
}

