using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FluentStorage.Queue {
	/// <summary>
	/// Message processing interface used to register a callback that receives a message
	/// </summary>
	public interface IQueueProcessor {
		/// <summary>
		/// 
		/// </summary>
		/// <param name="messages"></param>
		/// <returns></returns>
		Task ProcessMessagesAsync(List<QueueMessage> messages);
	}
}
