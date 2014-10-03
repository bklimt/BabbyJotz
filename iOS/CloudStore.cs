﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Parse;

namespace BabbyJotz.iOS {
	public class CloudStore {
		private LogEntry CreateLogEntryFromParseObject(ParseObject obj) {
			return new LogEntry(obj.Get<String>("uuid")) {
				DateTime = obj.Get<DateTime>("time"),
				Text = obj.Get<string>("text"),
				IsPoop = obj.Get<bool>("poop"),
				IsAsleep = obj.Get<bool>("asleep"),
				FormulaEaten = (decimal)obj.Get<double>("formula"),
				ObjectId = obj.ObjectId as string
			};
		}

		public CloudStore() {
		}

		public bool IsLoggedIn {
			get {
				return ParseUser.CurrentUser != null;
			}
		}

		// Returns the ObjectId of the object.
		public async Task SaveAsync(LogEntry entry) {
			var obj = entry.ObjectId != null
				? ParseObject.CreateWithoutData("LogEntry", entry.ObjectId)
				: ParseObject.Create("LogEntry");
			obj["uuid"] = entry.Uuid;
			obj["time"] = entry.DateTime;
			obj["text"] = entry.Text;
			obj["poop"] = entry.IsPoop;
			obj["asleep"] = entry.IsAsleep;
			obj["formula"] = (double)entry.FormulaEaten;
			await obj.SaveAsync();
			entry.ObjectId = obj.ObjectId;
		}

		public async Task<IEnumerable<LogEntry>> FetchChangesAsync() {
			var query = from entry in ParseObject.GetQuery("LogEntry")
			            orderby entry.UpdatedAt ascending
			            select entry;
			query = query.Limit(1000);

			var objs = await query.FindAsync();
			return from obj in objs
			       select CreateLogEntryFromParseObject(obj);
		}
	}
}

