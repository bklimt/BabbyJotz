using System;

namespace BabbyJotz {
    public class CloudException : Exception {
        public int Code { get; set; }
        public string Error { get; set; }
    }
}

