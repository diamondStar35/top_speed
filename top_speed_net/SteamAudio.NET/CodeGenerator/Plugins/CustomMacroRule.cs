using System.Text.RegularExpressions;
using CppAst;
using CppAst.CodeGen.CSharp;

namespace CodeGenerator.Plugins
{
	public abstract class CustomMacroRule
	{
		public string MacroNameRegex { get; set; }
		public bool OnlyNonProcessedMacros { get; set; }

		protected CustomMacroRule(string cppRegexName)
		{
			MacroNameRegex = cppRegexName;
		}

		public abstract void Process(CSharpConverter converter, CppMacro macro, Match regexMatch);
	}
}
