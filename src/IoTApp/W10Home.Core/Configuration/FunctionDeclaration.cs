using W10Home.Interfaces.Configuration;

namespace W10Home.Core.Configuration
{
	public class FunctionDeclaration : IFunctionDeclaration
	{
		public FunctionTriggerType TriggerType { get; set; }
		public string Name { get; set; }
		public string Script { get; set; }
	    public string Language { get; set; }

		#region QeueFunction
		/// <summary>
		/// Name of the queue this function waits for messages on
		/// </summary>
		public string QueueName { get; set; }

		#endregion

		#region RecurringIntervalFunction

		/// <summary>
		/// Interval in miliseconds
		/// </summary>
		public int Interval { get; set; }

		#endregion
	}
}