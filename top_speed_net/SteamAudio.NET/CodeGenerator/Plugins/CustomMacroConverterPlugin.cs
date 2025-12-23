using System.Collections.Generic;
using System.Text.RegularExpressions;
using CppAst.CodeGen.CSharp;

namespace CodeGenerator.Plugins
{
	public sealed class CustomMacroConverterPlugin : ICSharpConverterPlugin
	{
		public List<CustomMacroRule> Rules { get; } = new();

		public void Register(CSharpConverter converter, CSharpConverterPipeline pipeline)
		{
			pipeline.ConvertEnd.Add(ConvertEnd);
		}

		private void ConvertEnd(CSharpConverter converter)
		{
			var cpp = converter.CurrentCppCompilation;

			foreach (var macro in cpp.Macros) {
				if (string.IsNullOrWhiteSpace(macro.Value)) {
					continue;
				}

				bool processed = false;

				foreach (var rule in Rules) {
					var match = Regex.Match(macro.Name, rule.MacroNameRegex);

					if (match.Success && match.Length == macro.Name.Length && (!processed || !rule.OnlyNonProcessedMacros)) {
						rule.Process(converter, macro, match);

						processed = true;
					}
				}
			}
		}
	}
}
