using System;

namespace BabbyJotz {
    public struct CloudFetchChangesRequest {
        public DateTime? LastEntryUpdatedAt;
        public DateTime? LastBabyUpdatedAt;
        public DateTime? LastPhotoUpdatedAt;
    }
}

