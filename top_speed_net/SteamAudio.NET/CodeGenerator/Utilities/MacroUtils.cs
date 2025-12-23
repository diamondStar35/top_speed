using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CppAst;

namespace CodeGenerator.Utilities
{
	public static class MacroUtils
	{
		public static Regex MacroRegex = new(@"(?<=^|[()|&\-+])[A-Za-z]\w+(?=$|[()|&\-+])", RegexOptions.Compiled);

		public static string RenameMacrosInExpression(string expression, List<CppMacro> macros, Func<string, string> renamer)
		{
			int stringOffset = 0;

			foreach (Match macroMatch in MacroRegex.Matches(expression)) {
				string macroName = macroMatch.Value;
				var matchingMacro = macros.FirstOrDefault(m => m.Name == macroName);

				if (matchingMacro != null) {
					string replacement = renamer(macroName);

					expression = expression
						.Remove(macroMatch.Index + stringOffset, macroMatch.Length)
						.Insert(macroMatch.Index + stringOffset, replacement);

					stringOffset += replacement.Length - macroMatch.Length;
				}
			}

			return expression;
		}
	}
}
