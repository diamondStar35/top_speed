using System;

namespace CodeGenerator
{
	internal static class StringUtils
	{
		public static string Capitalize(string text)
		{
			if (text.Length > 0 && char.IsLower(text[0])) {
				char[] chars = text.ToCharArray();

				chars[0] = char.ToUpper(chars[0]);

				return new string(chars);
			}

			return text;
		}

		public static string RemovePrefixWithSeparator(string text, char separator)
		{
			int index = text.IndexOf(separator);

			if (index >= 0 && index + 1 < text.Length) {
				text = text.Substring(index + 1);
			}

			return text;
		}

		public static string RemovePrefix(string text, string prefix)
		{
			if (text.StartsWith(prefix)) {
				text = text.Substring(prefix.Length);
			}

			return text;
		}

		public static string RemoveSuffix(string text, string suffix)
		{
			if (text.EndsWith(suffix)) {
				text = text.Substring(0, text.Length - suffix.Length);
			}

			return text;
		}

		public static string SnakeCaseToCamelCase(string input)
		{
			string[] splits = input.Split('_');

			// Capitalize each part
			for (int i = 0; i < splits.Length; i++) {
				string split = splits[i];
				char[] chars = split.ToCharArray();

				for (int j = 0; j < chars.Length; j++) {
					chars[j] = j == 0 ? char.ToUpper(chars[j]) : char.ToLower(chars[j]);
				}

				splits[i] = new string(chars);
			}

			string result = string.Join(string.Empty, splits);

			return result;
		}
	}
}
