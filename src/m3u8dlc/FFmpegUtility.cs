using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Spectre.Console;

using static sdw.zzz;

namespace m3u8dlc
{
	public static partial class FFmpegUtility
	{
		[GeneratedRegex(@"  (Stream #(\d+):(\d+)(\[0x([0-9A-Fa-f]+)\])?(\(([^\)]*)\))?: (\w+): (\w+).*)")]
		private static partial Regex StreamFormatRegex();

		public static string FFmpegPath { get; set; } = "ffmpeg";

		public static async Task<bool> ReadStreamFormat(string path, List<StreamFormat> streamFormats)
		{
			string sArguments = "-hide_banner -nostdin -i \"" + path + "\" -c copy -t 0 -f null -";
			StringBuilder stderrString = new StringBuilder();
			bool bResult = await runFFmpeg(sArguments, stderrString);
			if (!bResult)
			{
				return false;
			}
			string sError = stderrString.ToString();
			List<string> lines = new List<string>(sError.Split(["\r\n", "\r", "\n"], StringSplitOptions.RemoveEmptyEntries));
			for (n32 i = 0; i < lines.Count; i++)
			{
				string sLine = lines[i];
				if (sLine.StartsWith("Output", StringComparison.OrdinalIgnoreCase))
				{
					break;
				}
				Match match = StreamFormatRegex().Match(sLine);
				if (!match.Success)
				{
					continue;
				}
				string sBuffer = match.Groups[1].Value;
				n32 nIndex = SToN32(match.Groups[2].Value);
				n32 nIndex2 = SToN32(match.Groups[3].Value);
				string sId = match.Groups[5].Value;
				n32 nId = 0;
				if (!string.IsNullOrEmpty(sId))
				{
					nId = SToN32(sId, 16);
				}
				string sLanguage = match.Groups[7].Value;
				string sCodecType = match.Groups[8].Value.ToLower(CultureInfo.InvariantCulture);
				string sCodecName = match.Groups[9].Value.ToLower(CultureInfo.InvariantCulture);
				StreamFormat streamFormat = new StreamFormat()
				{
					Buffer = sBuffer,
					Index = nIndex,
					Index2 = nIndex2,
					Id = nId,
					Language = sLanguage,
					CodecType = sCodecType,
					CodecName = sCodecName,
				};
				streamFormats.Add(streamFormat);
			}
			return true;
		}

		public static async Task<bool> Convert(string inputFilePath, string outputFilePath, bool isAAC)
		{
			string sArguments = "-hide_banner -nostdin -loglevel warning -y -i \"" + inputFilePath + "\" -c copy" + (isAAC ? " -bsf:a aac_adtstoasc" : "") + " \"" + outputFilePath + "\"";
			StringBuilder stderrString = new StringBuilder();
			bool bResult = await runFFmpeg(sArguments, stderrString);
			string sError = stderrString.ToString();
			if (!string.IsNullOrEmpty(sError))
			{
				AnsiConsole.MarkupLine("[grey]" + sError.EscapeMarkup() + "[/]");
			}
			return bResult;
		}

		private static async Task<bool> runFFmpeg(string arguments, StringBuilder stderrString)
		{
			ProcessStartInfo processStartInfo = new ProcessStartInfo()
			{
				FileName = FFmpegPath,
				Arguments = arguments,
				UseShellExecute = false,
				RedirectStandardInput = true,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				StandardInputEncoding = Encoding.UTF8,
				StandardOutputEncoding = Encoding.UTF8,
				StandardErrorEncoding = Encoding.UTF8,
			};
			using Process process = new Process() { StartInfo = processStartInfo };
			bool bResult = process.Start();
			if (!bResult)
			{
				return false;
			}
			string sString = await process.StandardError.ReadToEndAsync();
			_ = stderrString.Append(sString);
			return process.ExitCode == 0;
		}
	}
}
