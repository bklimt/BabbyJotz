using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BabbyJotz {
    public interface ICloudStore {
        // Fired whenever the user logs in or logs out.
        event EventHandler UserChanged;

        string UserName { get; }
        string UserId { get; }
        Task LogInAsync(string username, string password);
        Task SignUpAsync(string username, string password);
        void LogOut();
        Task SendPasswordResetEmailAsync(string username);

        Task InviteAsync(string username, Baby baby);
        Task<List<Invite>> GetInvitesAsync();
        Task AcceptInviteAsync(Invite invite);
        Task UnlinkAsync(Baby baby);

        Task<List<Baby>> FetchAllBabiesAsync(CancellationToken cancellationToken);
        Task<CloudFetchSinceResponse<LogEntry>> FetchEntriesSinceAsync(
            Baby baby, DateTime? lastUpdatedAt, CancellationToken cancellationToken);
        Task<CloudFetchSinceResponse<Photo>> FetchPhotosSinceAsync(
            Baby baby, DateTime? lastUpdatedAt, CancellationToken cancellationToken);

        Task SaveAsync(LogEntry entry, CancellationToken cancellationToken);
        Task SaveAsync(Baby baby, CancellationToken cancellationToken);
        Task SaveAsync(Photo photo, CancellationToken cancellationToken);

        Task RegisterForPushAsync(string deviceToken);

        Task LogSyncReportAsync(string report);
        void LogException(string tag, Exception e);
        void LogEvent(string name);
    }
}

