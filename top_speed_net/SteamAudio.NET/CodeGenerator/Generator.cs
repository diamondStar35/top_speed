using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using CodeGenerator.Plugins;
using CppAst;
using CppAst.CodeGen.Common;
using CppAst.CodeGen.CSharp;
using Zio.FileSystems;

namespace CodeGenerator
{
	public static class Generator
	{
		private static readonly Dictionary<string, string> NameReplacements = new() {
			// C# naming convention "violations" fixes.

			{ "HRTF", "Hrtf" },
			{ "SIMD", "Simd" },

			// Case conversion mistake fixes.
			// This could theoretically be made automatic through crazy dictionary-based algorithms, but that's overkill.

			{ "Outofmemory", "OutOfMemory" },
			{ "Tailremaining", "TailRemaining" },
			{ "Tailcomplete", "TailComplete" },
			{ "Staticsource", "StaticSource" },
			{ "Staticlistener", "StaticListener" },
			{ "Inversedistance", "InverseDistance" },
			{ "Radeonrays", "RadeonRays" },
			{ "Uniformfloor", "UniformFloor" },
			{ "N3d", "N3D" },
			{ "Sn3D", "SN3D" },
			{ "Applydistanceattenuation", "ApplyDistanceAttenuation" },
			{ "Applyairabsorption", "ApplyAirAbsorption" },
			{ "Applydirectivity", "ApplyDirectivity" },
			{ "Applyocclusion", "ApplyOcclusion" },
			{ "Applytransmission", "ApplyTransmission" },
			{ "Bakeconvolution", "BakeConvolution" },
			{ "Bakeparametric", "BakeParametric" },
			{ "Distanceattenuation", "DistanceAttenuation" },
			{ "Airabsorption", "AirAbsorption" },

			// Special
			// Kind of doing actual renames for the sake of the explicitness C# naming conventions have.

			{ "Freqindependent", "FrequencyIndependent" },
			{ "Freqdependent", "FrequencyDependent" },
		};

		internal static void Main(string[] args)
		{
			Console.WriteLine($"NOTE: To run this generator, you may need a Windows machine and Visual Studio with C++ Development packages installed.");

			try {
				if (args.Length == 0) {
					throw new ArgumentException("An output path must be provided in command line arguments.");
				}

				ProcessFile("../Natives/steamaudio/include/phonon.h", Path.GetFullPath(args[0]), "IPL.Generated.cs", "SteamAudio", "IPL");

				Console.WriteLine("Success.");
				Thread.Sleep(500);
			}
			catch (Exception e) {
				Console.WriteLine($"{e.GetType().Name}: {e.Message}");
				Console.WriteLine();
				Console.WriteLine("Press any key to close the application...");
				Console.ReadKey();
			}
		}

		public static void ProcessFile(string inputFile, string outputPath, string outputFile, string defaultNamespace, string defaultClass)
		{
			inputFile = Path.GetFullPath(inputFile);

			Console.WriteLine($"Processing file '{Path.GetFileName(inputFile)}'...");

			var converterOptions = new CSharpConverterOptions() {
				DefaultNamespace = defaultNamespace,
				DefaultClassLib = defaultClass,
				DefaultOutputFilePath = outputFile,
				DefaultDllImportNameAndArguments = "Library",
				DispatchOutputPerInclude = false,
				GenerateEnumItemAsFields = false,
				ParseMacros = true,
				TypedefCodeGenKind = CppTypedefCodeGenKind.NoWrap,

				Plugins = {
					new CustomMacroConverterPlugin {
						Rules = {
							// There's A LOT of code related to this, but all this does is just convert ~4 macros to constants.......
							new CustomMacroToConstantRule("STEAMAUDIO_VERSION", CSharpPrimitiveType.UInt(), outputFile, defaultClass) {
								NameChanger = n => StringUtils.SnakeCaseToCamelCase(StringUtils.RemovePrefixWithSeparator(n, '_')),
								ValueChanger = v => v.Replace("IPLuint32", "uint"),
							},
							new CustomMacroToConstantRule("STEAMAUDIO_(VERSION_.+)", CSharpPrimitiveType.UInt(), outputFile, defaultClass) {
								NameChanger = n => StringUtils.SnakeCaseToCamelCase(StringUtils.RemovePrefixWithSeparator(n, '_')),
							},
						}
					}
				},

				MappingRules = {
					// Remove prefixes from elements' names.
					e => e.MapAll<CppElement>().CSharpAction((converter, element) => {
						if (element is not ICSharpMember csMember) {
							return;
						}

						string prefix = element switch {
							CSharpNamedType => "IPL",
							CSharpEnumItem => "IPL_",
							CSharpMethod => "ipl",
							_ => null,
						};

						if (prefix != null) {
							string newName = StringUtils.Capitalize(StringUtils.RemovePrefix(csMember.Name, prefix));

							// Add an EntryPoint parameter to the DllImportAttribute, so that this rename doesn't break anything.
							if (csMember is CSharpMethod csMethod && csMethod.Attributes.FirstOrDefault(attrib => attrib is CSharpDllImportAttribute) is CSharpDllImportAttribute dllImportAttribute) {
								dllImportAttribute.EntryPoint = $@"""{csMember.Name}""";
							}

							csMember.Name = newName;
						}
					}),

					// Replace the bool enum with an actual bool.
					e => e.Map<CppEnum>("IPLbool").Discard(),
					e => e.MapAll<CppDeclaration>().CSharpAction((converter, element) => {
						CSharpType type;
						Action<CSharpType> setType;

						if (element is CSharpField field) {
							type = field.FieldType;
							setType = value => field.FieldType = value;
						} else if (element is CSharpParameter parameter) {
							type = parameter.ParameterType;
							setType = value => parameter.ParameterType = value;
						} else {
							return;
						}

						if (type is CSharpFreeType freeType && freeType.Text == "unsupported_type /* enum IPLbool {...} */") {
							var boolean = converter.GetCSharpType(CppPrimitiveType.Bool, element);

							setType(boolean);

							if (boolean is CSharpTypeWithAttributes typeWithAttributes) {
								foreach(var attribute in typeWithAttributes.Attributes.Where(a => a is CSharpMarshalAttribute).Cast<CSharpMarshalAttribute>()) {
									// The enum's size depends on the used compilers.
									attribute.UnmanagedType = CSharpUnmanagedKind.U1; //CSharpUnmanagedKind.U4;
								}
							}
						}
					}),

					// Rename enum elements from SCREAMING_SNAKECASE to LameupperCamelcase. There are manual fixes below, for cases where words aren't separated.
					e => e.MapAll<CppEnumItem>().CSharpAction((converter, element) => {
						var csEnumItem = (CSharpEnumItem)element;

						csEnumItem.Name = StringUtils.RemovePrefixWithSeparator(csEnumItem.Name, '_');
						csEnumItem.Name = StringUtils.SnakeCaseToCamelCase(csEnumItem.Name);
					}),

					// Capitalize C# fields
					e => e.MapAll<CppField>().CSharpAction((_, csElement) => {
						var csField = (CSharpField)csElement;

						if (csField.Visibility == CSharpVisibility.Public) {
							csField.Name = StringUtils.Capitalize(csField.Name);
						}
					}),

					// Turn some 'ref' parameters to 'out' or 'in' based on \param documentation.
					e => e.MapAll<CppParameter>().CSharpAction((converter, element) => {
						var parameter = (CSharpParameter)element;

						if (parameter.ParameterType is not CSharpRefType refParameterType) {
							return;
						}

						if (element.Parent is not CSharpMethod method || method.CppElement is not CppFunction function) {
							return;
						}

						if (function.Comment?.Children?.FirstOrDefault(c => c is CppCommentParamCommand pc && pc.ParamName == parameter.Name) is not CppCommentParamCommand parameterComment) {
							return;
						}

						if (parameterComment?.Children?.FirstOrDefault() is not CppCommentParagraph paragraph) {
							return;
						}

						string paragraphText = paragraph.ToString().Trim();

						if (paragraphText.StartsWith("[out]")) {
							refParameterType.Kind = CSharpRefKind.Out;
							return;
						}
						
						if (!paragraphText.StartsWith("[in, out]")) {
							// Unfortunately, the above checks are still not enough.

							if (paragraphText.Contains("to allocate") || paragraphText.Contains("to free")) {
								return;
							}

							if (function.Name.Contains("Release")) {
								return;
							}

							refParameterType.Kind = CSharpRefKind.In;
						}
					}),
					
					// Handle _IPL*_t types. This has to be done at Cpp level due to the generated C# methods' code using raw text and therefore not being linked to the methods' types.
					e => e.MapAll<CppClass>().CppAction((converter, cppElement) => {
						var cppClass = (CppClass)cppElement;
						string newName = cppClass.Name;

						newName = StringUtils.RemovePrefix(newName, "_IPL");
						newName = StringUtils.RemoveSuffix(newName, "_t");
						newName = StringUtils.Capitalize(newName);

						foreach (var pair in NameReplacements) {
							newName = newName.Replace(pair.Key, pair.Value);
						}

						cppClass.Name = newName;
					}),

					// Execute replacements from the NameReplacements dictionaries for all C# types.
					e => e.MapAll<CppElement>().CSharpAction((converter, csElement) => {
						if (csElement is not ICSharpMember csMember) {
							return;
						}

						string oldName = csMember.Name;
						string newName = oldName;

						foreach (var pair in NameReplacements) {
							newName = newName.Replace(pair.Key, pair.Value);
						}

						csMember.Name = newName;
					}),

					// Lazy fixes for conversion mistakes.
					e => e.Map<CppField>("IPLMatrix4x4::elements").Type("float", 16),
					e => e.Map<CppEnumItem>("IPLSIMDLevel::IPL_SIMDLEVEL_NEON").CSharpAction((_, element) => ((CSharpEnumItem)element).Value = "Sse2"),
				}
			};

			converterOptions.IncludeFolders.Add(Path.GetDirectoryName(inputFile));

			var compilation = CSharpConverter.Convert(new List<string> { inputFile }, converterOptions);

			if (compilation.HasErrors) {
				foreach (var message in compilation.Diagnostics.Messages) {
					if (message.Type == CppLogMessageType.Error) {
						Console.WriteLine(message);
					}
				}

				Console.ReadKey();

				return;
			}

			using var fileSystem = new PhysicalFileSystem();
			using var subFileSystem = new SubFileSystem(fileSystem, fileSystem.ConvertPathFromInternal(outputPath));

			var codeWriterOptions = new CodeWriterOptions(subFileSystem);
			var codeWriter = new CodeWriter(codeWriterOptions);

			compilation.DumpTo(codeWriter);
		}
	}
}
