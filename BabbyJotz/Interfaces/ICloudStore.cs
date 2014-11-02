using System;
using System.Collections.Generic;
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

        Task InviteAsync(string username, Baby baby);
        Task<List<Invite>> GetInvitesAsync();
        Task AcceptInviteAsync(Invite invite);
        Task UnlinkAsync(Baby baby);

        Task<CloudFetchChangesResponse> FetchChangesAsync(CloudFetchChangesRequest req);
        Task SaveAsync(LogEntry entry);
        Task SaveAsync(Baby baby);
        Task SaveAsync(Photo photo);

        Task RegisterForPushAsync(string deviceToken);
    }
}

