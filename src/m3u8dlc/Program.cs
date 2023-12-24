using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;

using Spectre.Console;
using Spectre.Console.Cli;

namespace m3u8dlc
{
	internal class Program
	{
		public static async Task<n32> Main(string[] args)
		{
			CommandApp<AsyncMainCommand> app = new CommandApp<AsyncMainCommand>();
			app.Configure(configuration);
			n32 nResult = await app.RunAsync(args);
			return nResult;
		}

		private static void configuration(IConfigurator config)
		{
			_ = config.SetExceptionHandler(exceptionHandler);
			_ = config.SetApplicationName("m3u8dlc");
			_ = config.UseStrictParsing();
			_ = config.CaseSensitivity(CaseSensitivity.All);
		}

		private static n32 exceptionHandler(Exception ex)
		{
			AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
			return -1;
		}
	}

	public class AsyncMainCommand : AsyncCommand<AsyncMainCommand.Settings>
	{
		public class Settings : CommandSettings
		{
			[Description("链接或文件")]
			[CommandArgument(0, "<input>")]
			public string Input { get; set; } = "";

			[Description("设置临时文件存储目录")]
			[CommandOption("--tmp-dir <tmp-dir>")]
			public string TempDir { get; set; } = "";

			[Description("设置输出目录")]
			[CommandOption("--save-dir <save-dir>")]
			public string SaveDir { get; set; } = "";

			[Description("设置保存文件名")]
			[CommandOption("--save-name <save-name>")]
			public string SaveName { get; set; } = "";

			public override ValidationResult Validate()
			{
				if (string.IsNullOrEmpty(Input))
				{
					return ValidationResult.Error("链接或文件不能为空");
				}
				return base.Validate();
			}
		}

		private const string ManifestFileName = "manifest.json";

		private Settings? m_settings = null;
		private Manifest? m_manifest = null;

		public override async Task<n32> ExecuteAsync([NotNull] CommandContext context, [NotNull] Settings settings)
		{
			m_settings = settings;
			if (string.IsNullOrEmpty(m_settings.SaveName))
			{
				m_settings.SaveName = PathUtility.GetFileNameWithoutExtension(m_settings.Input);
			}
			if (string.IsNullOrEmpty(m_settings.TempDir))
			{
				m_settings.TempDir = Environment.CurrentDirectory;
			}
			m_settings.TempDir = Path.Combine(m_settings.TempDir, m_settings.SaveName);
			if (string.IsNullOrEmpty(m_settings.SaveDir))
			{
				m_settings.SaveDir = Environment.CurrentDirectory;
			}

			n32 nExitCode = 0;
			do
			{
				_ = Directory.CreateDirectory(m_settings.TempDir);
				string sManifestPath = Path.Combine(m_settings.TempDir, ManifestFileName);
				if (JsonUtility.DeserializeFile(sManifestPath, ref m_manifest))
				{
					AnsiConsole.MarkupLine("加载manifest: " + sManifestPath);
				}
				else
				{
					m_manifest = null;
				}
				if (m_manifest == null)
				{
					Parser parser = new Parser(m_settings.Input);
					if (!parser.Parse())
					{
						nExitCode = 1;
						break;
					}
					m_manifest = parser.Manifest;
					_ = JsonUtility.SerializeFile(m_manifest, sManifestPath);
					for (SortedDictionary<string, string>.Enumerator it = parser.TempFiles.GetEnumerator(); it.MoveNext(); /**/)
					{
						KeyValuePair<string, string> item = it.Current;
						string sTempFilePath = Path.Combine(m_settings.TempDir, item.Key);
						File.WriteAllText(sTempFilePath, item.Value);
					}
					parser.TempFiles.Clear();
				}
				if (m_manifest == null)
				{
					nExitCode = 1;
					break;
				}
				//nExitCode = 0;
			} while (false);

			return nExitCode;
		}
	}
}
