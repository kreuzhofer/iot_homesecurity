using System;
using System.Runtime.CompilerServices;

namespace W10Home.Core.Configuration
{
	public class RecurringIntervalTriggeredFunctionDeclaration : FunctionDeclaration
	{
		/// <summary>
		/// Interval in miliseconds
		/// </summary>
		public int Interval { get; set; }
	}
}