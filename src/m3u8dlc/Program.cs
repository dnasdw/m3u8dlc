using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Spectre.Console;
using Spectre.Console.Cli;

namespace m3u8dlc
{
	internal class Program
	{
		public static async Task<n32> Main(string[] args)
		{
			Console.CancelKeyPress += console_CancelKeyPress;
			CommandApp<AsyncMainCommand> app = new CommandApp<AsyncMainCommand>();
			app.Configure(configuration);
			n32 nResult = await app.RunAsync(args);
			return nResult;
		}

		private static void console_CancelKeyPress(object? sender, ConsoleCancelEventArgs e)
		{
			// 显示进度条的时候会自动隐藏光标,强制中断时应恢复显示
			AnsiConsole.Cursor.Show();
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

			//private class EnvironmentProcessorCountDefaultValueAttribute : DefaultValueAttribute
			//{
			//	public EnvironmentProcessorCountDefaultValueAttribute(n32 _) : base(Environment.ProcessorCount)
			//	{
			//	}
			//}

			[Description("设置下载线程数")]
			[CommandOption("--thread-count <number>")]
			//[EnvironmentProcessorCountDefaultValue(0)]
			[DefaultValue(16)]
			public n32 ThreadCount { get; set; } = 16; // Environment.ProcessorCount;

			[Description("每个分片下载异常时的重试次数")]
			[CommandOption("--download-retry-count <number>")]
			[DefaultValue(3)]
			public n32 DownloadRetryCount { get; set; } = 3;

			[Description("跳过合并分片")]
			[CommandOption("--skip-merge")]
			[DefaultValue(false)]
			public bool SkipMerge { get; set; } = false;

			[Description("跳过下载")]
			[CommandOption("--skip-download")]
			[DefaultValue(false)]
			public bool SkipDownload { get; set; } = false;

			[Description("设置限速, 单位支持 MiB/s 或 KiB/s, 如: 15M 100K")]
			[CommandOption("-R|--max-speed <SPEED>")]
			[TypeConverter(typeof(DownloadSpeedConverter))]
			public n64? MaxSpeed { get; set; } = null;

			[Description("使用系统默认代理")]
			[CommandOption("--use-system-proxy")]
			[DefaultValue(true)]
			public bool UseSystemProxy { get; set; } = true;

			[Description("设置请求代理, 如: http://127.0.0.1:8888")]
			[CommandOption("--custom-proxy <URL>")]
			[TypeConverter(typeof(ProxyConverter))]
			public WebProxy? CustomProxy { get; set; } = null;

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
		private const string DownloadDirName = "0";
		private const string ConcatFileName = "concat.ts";

		private Settings? m_settings = null;
		private Manifest? m_manifest = null;
		private DownloadRecorder? m_downloadRecorder = null;
		private string? m_sDownloadDir = null;
		private string m_sDownloadFileNameFormat = "0";
		private ProgressTask? m_task = null;
		private string? m_sTempConcatFilePath = null;

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
			m_settings.ThreadCount = Math.Max(m_settings.ThreadCount, 1);
			m_settings.DownloadRetryCount = Math.Max(m_settings.DownloadRetryCount, 0);
			if (m_settings.MaxSpeed != null)
			{
				DownloadRecorder.SpeedLimit = m_settings.MaxSpeed;
			}
			DownloadUtility.HttpClientHandler.UseProxy = m_settings.UseSystemProxy;
			if (m_settings.CustomProxy != null)
			{
				DownloadUtility.HttpClientHandler.Proxy = m_settings.CustomProxy;
				DownloadUtility.HttpClientHandler.UseProxy = true;
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

				if (m_settings.SkipDownload)
				{
					nExitCode = 0;
					break;
				}

				if (m_manifest.MediaSegments.Count == 0)
				{
					nExitCode = 0;
					break;
				}
				AnsiConsole.MarkupLine("保存文件名: " + $"[deepskyblue1]{m_settings.SaveName.EscapeMarkup()}[/]");
				AnsiConsole.MarkupLine("开始下载..." + "manifest");
				bool bResult = await downloadManifestAsync();
				if (!bResult)
				{
					nExitCode = 1;
					break;
				}

				if (m_settings.SkipMerge)
				{
					nExitCode = 0;
					break;
				}

				bResult = await concatFileAsync();
				if (!bResult)
				{
					if (m_sTempConcatFilePath != null)
					{
						File.Delete(m_sTempConcatFilePath);
					}
					nExitCode = 1;
					break;
				}
				Debug.Assert(m_sTempConcatFilePath != null);
				File.Delete(m_sTempConcatFilePath);
				//nExitCode = 0;
			} while (false);

			return nExitCode;
		}

		private async Task<bool> downloadManifestAsync()
		{
			Debug.Assert(m_manifest != null);

			m_downloadRecorder = new DownloadRecorder();
			Progress progress = AnsiConsole.Progress();
			_ = progress.AutoClear(false);
			_ = progress.Columns(new ProgressColumn[]
			{
				new TaskDescriptionColumn(),
				new ElapsedTimeColumn(),
				new ProgressBarColumn() { Width = 20 },
				new CountPercentageColumn(m_manifest.MediaSegments.Count),
				new DownloadRateColumn(m_downloadRecorder),
				new DownloadSpeedColumn(m_downloadRecorder),
				new RemainingTimeColumn(),
				new SpinnerColumn(),
			});
			bool bResult = await progress.StartAsync(downloadManifestAsync);
			return bResult;
		}

		private async Task<bool> downloadManifestAsync(ProgressContext context)
		{
			Debug.Assert(m_settings != null);
			Debug.Assert(m_manifest != null);

			m_sDownloadDir = Path.Combine(m_settings.TempDir, DownloadDirName);
			_ = Directory.CreateDirectory(m_sDownloadDir);

			List<MediaSegment> segments = new List<MediaSegment>(m_manifest.MediaSegments);
			n32 nWidth = $"{segments.Count}".Length;
			m_sDownloadFileNameFormat = new string('0', nWidth);

			m_task = context.AddTask("manifest", autoStart: false);
			m_task.MaxValue = segments.Count;
			m_task.StartTask();

			MediaSegment segment = segments[0];
			segments.RemoveAt(0);
			Debug.Assert(segment.Index != null);
			await downloadMediaSegmentAsync(segment, CancellationToken.None);
			bool bResult = false;
			do
			{
				string sDownloadFileName = segment.Index.Value.ToString(m_sDownloadFileNameFormat, CultureInfo.InvariantCulture) + ".ts";
				string sDownloadPath = Path.Combine(m_sDownloadDir, sDownloadFileName);
				if (!File.Exists(sDownloadPath))
				{
					break;
				}
				m_task.Increment(1);
				ParallelOptions parallelOptions = new ParallelOptions() { MaxDegreeOfParallelism = m_settings.ThreadCount };
				await Parallel.ForEachAsync(segments, parallelOptions, downloadMediaSegmentAsync);
				bResult = m_task.IsFinished;
			} while (false);
			m_task.StopTask();
			return bResult;
		}

		private async ValueTask downloadMediaSegmentAsync(MediaSegment mediaSegment, CancellationToken cancellationToken)
		{
			Debug.Assert(m_settings != null);
			Debug.Assert(m_sDownloadDir != null);
			Debug.Assert(m_task != null);
			Debug.Assert(mediaSegment.Index != null);
			Debug.Assert(mediaSegment.Url != null);

			string sDownloadFileName = mediaSegment.Index.Value.ToString(m_sDownloadFileNameFormat, CultureInfo.InvariantCulture) + ".ts";
			string sDownloadFileTempName = sDownloadFileName + ".tmp";
			string sDownloadPath = Path.Combine(m_sDownloadDir, sDownloadFileName);
			string sDownloadTempPath = Path.Combine(m_sDownloadDir, sDownloadFileTempName);
			if (!File.Exists(sDownloadPath))
			{
				n32 nRetryCount = m_settings.DownloadRetryCount;
				do
				{
					n32 nResult = -1;
					try
					{
						nResult = await DownloadUtility.DownloadFileAsync(mediaSegment.Url, sDownloadTempPath, true, m_downloadRecorder, mediaSegment.Index);
					}
					catch (Exception ex)
					{
						AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
					}
					if (nResult == 0 || nResult == 200)
					{
						File.Move(sDownloadTempPath, sDownloadPath);
						break;
					}
					else if (nResult == 405)
					{
						return;
					}
					else
					{
						if (nRetryCount > 0)
						{
							nRetryCount--;
						}
						else
						{
							return;
						}
					}
				} while (true);
			}
			else
			{
				File.Delete(sDownloadTempPath);
			}
			m_task.Increment(1);
		}

		private async Task<bool> concatFileAsync()
		{
			Debug.Assert(m_manifest != null);

			Progress progress = AnsiConsole.Progress();
			_ = progress.AutoClear(false);
			_ = progress.Columns(new ProgressColumn[]
			{
				new TaskDescriptionColumn(),
				new ElapsedTimeColumn(),
				new ProgressBarColumn() { Width = 20 },
				new CountPercentageColumn(m_manifest.MediaSegments.Count),
				new RemainingTimeColumn(),
				new SpinnerColumn(),
			});
			bool bResult = await progress.StartAsync(concatFileAsync);
			return bResult;
		}

		private async Task<bool> concatFileAsync(ProgressContext context)
		{
			Debug.Assert(m_settings != null);
			Debug.Assert(m_manifest != null);
			Debug.Assert(m_sDownloadDir != null);

			m_sTempConcatFilePath = Path.Combine(m_settings.TempDir, ConcatFileName);

			List<MediaSegment> segments = m_manifest.MediaSegments;

			m_task = context.AddTask(ConcatFileName, autoStart: false);
			m_task.MaxValue = segments.Count;
			m_task.StartTask();

			using FileStream outputStream = new FileStream(m_sTempConcatFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
			for (n32 i = 0; i < segments.Count; i++)
			{
				MediaSegment segment = segments[i];
				Debug.Assert(segment.Index != null);
				string sDownloadFileName = segment.Index.Value.ToString(m_sDownloadFileNameFormat, CultureInfo.InvariantCulture) + ".ts";
				string sDownloadPath = Path.Combine(m_sDownloadDir, sDownloadFileName);
				using FileStream inputStream = File.OpenRead(sDownloadPath);
				await inputStream.CopyToAsync(outputStream);
				m_task.Increment(1);
			}
			bool bResult = m_task.IsFinished;
			m_task.StopTask();
			return bResult;
		}
	}
}
