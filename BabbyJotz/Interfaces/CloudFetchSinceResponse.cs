using System;
using System.Collections.Generic;

namespace BabbyJotz {
    public struct CloudFetchSinceResponse<T> {
        public List<T> Results;
        public DateTime? NewUpdatedAt;
        public bool MaybeHasMore;
    }
}

