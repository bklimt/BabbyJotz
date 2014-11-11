using System;

namespace BabbyJotz {
    public class Invite {
        public string Id { get; private set; }
        public string BabyName { get; private set; }
        public string BabyUuid { get; private set; }

        public Invite(string id, string babyName, string babyUuid) {
            Id = id;
            BabyName = babyName;
            BabyUuid = babyUuid;
        }
    }
}

