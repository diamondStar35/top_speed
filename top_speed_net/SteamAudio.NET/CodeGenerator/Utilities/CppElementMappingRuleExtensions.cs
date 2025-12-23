using CppAst.CodeGen.CSharp;

namespace CodeGenerator.Utilities
{
	public static class CppElementMappingRuleExtensions
	{
		public static CppElementMappingRule CSharpName(this CppElementMappingRule rule, string name)
			=> rule.CSharpAction((_, element) => ((ICSharpMember)element).Name = name);
	}
}
