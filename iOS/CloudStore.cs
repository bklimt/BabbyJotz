using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Parse;

namespace BabbyJotz.iOS {
    public class CloudStore {
        private LogEntry CreateLogEntryFromParseObject(ParseObject obj) {
            DateTime? deleted = null;
            obj.TryGetValue<DateTime?>("deleted", out deleted);

            return new LogEntry(obj.Get<String>("uuid")) {
                DateTime = obj.Get<DateTime>("time"),
                Text = obj.Get<string>("text"),
                IsPoop = obj.Get<bool>("poop"),
                IsAsleep = obj.Get<bool>("asleep"),
                FormulaEaten = (decimal)obj.Get<double>("formula"),
                Deleted = deleted,
                ObjectId = obj.ObjectId as string
            };
        }

        public CloudStore() {
        }

        public string UserName {
            get {
                return ParseUser.CurrentUser != null ? ParseUser.CurrentUser.Username : null;
            }
        }

        // Returns the ObjectId of the object.
        public async Task SaveAsync(LogEntry entry) {
            if (ParseUser.CurrentUser == null) {
                throw new InvalidOperationException("Tried to sync without logging in.");
            }

            var obj = entry.ObjectId != null
                ? ParseObject.CreateWithoutData("LogEntry", entry.ObjectId)
                : ParseObject.Create("LogEntry");
            obj["uuid"] = entry.Uuid;
            obj["time"] = entry.DateTime;
            obj["text"] = entry.Text;
            obj["poop"] = entry.IsPoop;
            obj["asleep"] = entry.IsAsleep;
            obj["formula"] = (double)entry.FormulaEaten;
            obj["deleted"] = entry.Deleted;
            obj.ACL = new ParseACL(ParseUser.CurrentUser);
            await obj.SaveAsync();
            entry.ObjectId = obj.ObjectId;
        }

        public async Task<Tuple<IEnumerable<LogEntry>, DateTime?>> FetchChangesAsync(DateTime? lastUpdatedAt) {
            var query = from entry in ParseObject.GetQuery("LogEntry")
                        orderby entry.UpdatedAt ascending
                        select entry;
            query = query.Limit(1000);
            if (lastUpdatedAt != null) {
                query = query.WhereGreaterThanOrEqualTo("updatedAt", lastUpdatedAt.Value);
            }

            var results = (await query.FindAsync()).ToList();
            var objs = from result in results
                       select CreateLogEntryFromParseObject(result);
            if (results.Count > 0) {
                lastUpdatedAt = results[results.Count - 1].UpdatedAt;
            }
            return new Tuple<IEnumerable<LogEntry>, DateTime?>(objs.ToList(), lastUpdatedAt);
        }
    }
}

