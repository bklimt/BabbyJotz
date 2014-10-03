using System;
using System.Threading.Tasks;

namespace BabbyJotz {
	public class TaskQueue {
		private object mutex = new object();
		private Task<int> tail;

		public TaskQueue() {
			tail = Task.FromResult(0);
		}

		public Task<T> EnqueueAsync<T>(Func<Task<int>, Task<T>> func) {
			lock (mutex) {
				var thisTask = func(tail);
				var tailAndThisTask = tail.ContinueWith(_ => thisTask).Unwrap();
				tail = tailAndThisTask.ContinueWith(_ => 0);
				return tailAndThisTask;
			}
		}
	}
}

