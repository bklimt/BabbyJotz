using System;
using System.IO;

using Xamarin.Forms;

namespace BabbyJotz {
    public class Baby : BindableObject {
        public string Uuid { get; private set; }

        public string ObjectId { get; set; }

        public DateTime? Deleted { get; set; }

        public static readonly BindableProperty NameProperty =
            BindableProperty.Create<Baby, string>(p => p.Name, null);

        public string Name {
            get { return (string)base.GetValue(NameProperty); }
            set { SetValue(NameProperty, value); }
        }

        public static readonly BindableProperty ProfilePhotoSourceProperty =
            BindableProperty.Create<Baby, ImageSource>(p => p.ProfilePhotoSource, null);

        public ImageSource ProfilePhotoSource {
            get { return (ImageSource)base.GetValue(ProfilePhotoSourceProperty); }
            private set { SetValue(ProfilePhotoSourceProperty, value); }
        }

        private Func<Stream> profilePhotoStream;
        public Func<Stream> ProfilePhotoStream {
            get {
                return profilePhotoStream;
            }
            set {
                profilePhotoStream = value;
                // TODO: Make sure this is actually a PNG.
                ProfilePhotoSource = ImageSource.FromStream(profilePhotoStream);
            }
        }

        public Baby() {
            ProfilePhotoSource = ImageSource.FromFile("Icon-76.png");
            Uuid = Guid.NewGuid().ToString("D");
        }

        public Baby(string uuid) {
            ProfilePhotoSource = ImageSource.FromFile("Icon-76.png");
            Uuid = uuid;
        }

        public Baby(Baby other) {
            Name = other.Name;
            ProfilePhotoStream = other.ProfilePhotoStream;
            Uuid = other.Uuid;
            ObjectId = other.ObjectId;
            Deleted = other.Deleted;
        }
    }
}

