using W10Home.Interfaces.Configuration;

namespace W10Home.Core.Configuration
{
	public class FunctionDeclaration : IFunctionDeclaration
	{
		public FunctionTriggerType TriggerType { get; set; }
		public string Name { get; set; }
		public string Code { get; set; }
	}
}