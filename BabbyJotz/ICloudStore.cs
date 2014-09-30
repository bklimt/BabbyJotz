using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BabbyJotz {
	public interface ICloudStore {
		Task SaveAsync(LogEntry entry);
		Task<IEnumerable<LogEntry>> Fetch();
	}
}

