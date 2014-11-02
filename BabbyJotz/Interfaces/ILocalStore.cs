using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BabbyJotz {
    public interface ILocalStore {
        // Fired whenever the contents of the database change.
        event EventHandler LocallyChanged;
        event EventHandler RemotelyChanged;

        Task RecreateDatabaseAsync();

        // Basic CRUD
        Task SaveAsync(LogEntry entry);
        Task DeleteAsync(LogEntry entry);
        Task<List<LogEntry>> FetchEntriesAsync(Baby baby, DateTime day);

        Task SaveAsync(Baby baby);
        Task DeleteAsync(Baby baby);
        Task<List<Baby>> FetchBabiesAsync();

        Task SaveAsync(Photo photo);
        Task DeleteAsync(Photo photo);

        // Statistics
        Task<List<LogEntry>> GetEntriesForStatisticsAsync(Baby baby);

        // Cloud Syncing
        Task SyncToCloudAsync(ICloudStore cloudStore, bool markNewAsRead);
        Task MarkAllAsReadAsync();
        Task<IEnumerable<LogEntry>> FetchUnreadAsync();
    }
}

