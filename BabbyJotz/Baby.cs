using System;
using System.IO;

using Xamarin.Forms;

namespace BabbyJotz {
    public class Baby : StorableObject {
        public static readonly BindableProperty NameProperty =
            BindableProperty.Create<Baby, string>(p => p.Name, null);
        public string Name {
            get { return (string)base.GetValue(NameProperty); }
            set { SetValue(NameProperty, value); }
        }

        public static readonly BindableProperty BirthdayProperty =
            BindableProperty.Create<Baby, DateTime>(p => p.Birthday, DateTime.Now.Date);
        public DateTime Birthday {
            get { return (DateTime)base.GetValue(BirthdayProperty); }
            set { SetValue(BirthdayProperty, value); }
        }

        public static readonly BindableProperty ShowBreastfeedingProperty =
            BindableProperty.Create<Baby, bool>(p => p.ShowBreastfeeding, true);
        public bool ShowBreastfeeding {
            get { return (bool)base.GetValue(ShowBreastfeedingProperty); }
            set { SetValue(ShowBreastfeedingProperty, value); }
        }

        public static readonly BindableProperty ShowPumpedProperty =
            BindableProperty.Create<Baby, bool>(p => p.ShowPumped, true);
        public bool ShowPumped {
            get { return (bool)base.GetValue(ShowPumpedProperty); }
            set { SetValue(ShowPumpedProperty, value); }
        }

        public static readonly BindableProperty ShowFormulaProperty =
            BindableProperty.Create<Baby, bool>(p => p.ShowFormula, true);
        public bool ShowFormula {
            get { return (bool)base.GetValue(ShowFormulaProperty); }
            set { SetValue(ShowFormulaProperty, value); }
        }

        public static readonly BindableProperty ProfilePhotoProperty =
            BindableProperty.Create<Baby, Photo>(p => p.ProfilePhoto, null);
        public Photo ProfilePhoto {
            get { return (Photo)base.GetValue(ProfilePhotoProperty); }
            set { SetValue(ProfilePhotoProperty, value); }
        }

        public Baby() {
            Uuid = Guid.NewGuid().ToString("D");
        }

        public Baby(string uuid) {
            Uuid = uuid;
        }

        public Baby(Baby other) {
            CopyFrom(other);
        }

        public void CopyFrom(Baby other) {
            Name = other.Name;
            Birthday = other.Birthday;
            ShowBreastfeeding = other.ShowBreastfeeding;
            ShowPumped = other.ShowPumped;
            ShowFormula = other.ShowFormula;
            ProfilePhoto = other.ProfilePhoto;
            Uuid = other.Uuid;
            ObjectId = other.ObjectId;
            Deleted = other.Deleted;
        }
    }
}