using System;

using Xamarin.Forms;

namespace BabbyJotz {
    public abstract class StorableObject : BindableObject {
        public string Uuid { get; protected set; }
        public string ObjectId { get; set; }
        public DateTime? Deleted { get; set; }
    }
}

