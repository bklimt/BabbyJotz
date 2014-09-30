using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Parse;

namespace BabbyJotz.iOS {
	public class CloudStore : ICloudStore {
		public CloudStore() {
		}

		public async Task SaveAsync(LogEntry entry) {
			var obj = new ParseObject("LogEntry");
			obj["time"] = entry.DateTime;
			obj["text"] = entry.Text;
			obj["poop"] = entry.IsPoop;
			obj["asleep"] = entry.IsAsleep;
			obj["formula"] = (double)entry.FormulaEaten;
			await obj.SaveAsync();
		}

		public async Task<IEnumerable<LogEntry>> Fetch() {
			var query = new ParseQuery<ParseObject>("LogEntry");
			var objs = await query.FindAsync();
			return from obj in objs select new LogEntry {
				DateTime = obj.Get<DateTime>("time"),
				Text = obj.Get<string>("text"),
				IsPoop = obj.Get<bool>("poop"),
				IsAsleep = obj.Get<bool>("asleep"),
				FormulaEaten = (decimal)obj.Get<double>("formula")
			};
		}
	}
}

