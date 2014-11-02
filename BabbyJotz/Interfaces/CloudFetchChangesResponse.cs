using System;
using System.Collections.Generic;

namespace BabbyJotz {
    public struct CloudFetchChangesResponse {
        public List<LogEntry> Entries;
        public List<Photo> Photos;
        public List<Baby> Babies;
        public DateTime? LastEntryUpdatedAt;
        public DateTime? LastPhotoUpdatedAt;
        public DateTime? LastBabyUpdatedAt;
        public bool MaybeHasMore;
    }
}

