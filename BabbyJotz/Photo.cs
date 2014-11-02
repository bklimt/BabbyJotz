using System;
using System.IO;

using Xamarin.Forms;

namespace BabbyJotz {
    public class Photo : StorableObject {
        public Baby Baby { get; private set; }
        public byte[] Bytes { get; set; }

        public Photo(Baby baby) {
            Baby = baby;
            Uuid = Guid.NewGuid().ToString("D");
            Bytes = null;
        }

        public Photo(Baby baby, string uuid) {
            Baby = baby;
            Uuid = uuid;
            Bytes = null;
        }

        public Photo(Photo other) {
            CopyFrom(other);
        }

        public void CopyFrom(Photo other) {
            Baby = other.Baby;
            Uuid = other.Uuid;
            Bytes = other.Bytes;
            ObjectId = other.ObjectId;
            Deleted = other.Deleted;
        }
    }
}