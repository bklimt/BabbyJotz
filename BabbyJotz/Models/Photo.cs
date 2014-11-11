using System;
using System.IO;

using Xamarin.Forms;

namespace BabbyJotz {
    public class Photo : StorableObject {
        public Baby Baby { get; private set; }

        private byte[] bytes;
        public byte[] Bytes {
            get {
                return bytes;
            }
            set {
                if (bytes != null && value == null) {
                    throw new InvalidOperationException("Cannot set a image to null after loading it.");
                }
                if (value != null && Uuid == null) {
                    throw new InvalidOperationException("Cannot set bytes for a placeholder image.");
                }
                bytes = value;
            }
        }

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
            if (Uuid == null || other.Uuid == null) {
                throw new InvalidOperationException("Cannot copy the placeholder image.");
            }
            Baby = other.Baby;
            Uuid = other.Uuid;
            Bytes = other.Bytes;
            ObjectId = other.ObjectId;
            Deleted = other.Deleted;
        }
    }
}