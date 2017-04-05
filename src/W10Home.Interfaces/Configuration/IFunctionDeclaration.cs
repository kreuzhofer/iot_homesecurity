namespace W10Home.Interfaces.Configuration
{
	public interface IFunctionDeclaration
	{
		FunctionTriggerType TriggerType { get; set; }
		string Name { get; set; }
		string Code { get; set; }
	}
}