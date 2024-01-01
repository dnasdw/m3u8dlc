using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

using static sdw.zzz;

namespace m3u8dlc
{
	// https://datatracker.ietf.org/doc/html/rfc8216
	public static class HLSTags
	{
		// Basic Tags
		public const string EXTM3U = "#EXTM3U";
		// Media Segment Tags
		public const string EXTINF = "#EXTINF";
		public const string EXT_X_DISCONTINUITY = "#EXT-X-DISCONTINUITY";
		// Media Playlist Tags
		public const string EXT_X_TARGETDURATION = "#EXT-X-TARGETDURATION";
		public const string EXT_X_ENDLIST = "#EXT-X-ENDLIST";
	}

	public partial class M3U8Parser
	{
		[GeneratedRegex(@"^#EXTINF:([\.0-9]+),(.*)?$")]
		private static partial Regex ExtInfRegex();
		[GeneratedRegex(@"^#EXT-X-TARGETDURATION:([\.0-9]+)$")]
		private static partial Regex ExtXTargetDurationRegex();

		public Manifest Manifest { get; init; } = new Manifest();
		public string LocalFile { get; set; } = "";

		private const string DownloadDirName = "0";

		private string m_sText;

		public M3U8Parser(string text)
		{
			m_sText = text;
		}

		public bool Parse()
		{
			List<string> lines = new List<string>(m_sText.Split(["\r\n", "\r", "\n"], StringSplitOptions.None));
			if (lines.Count == 0)
			{
				return false;
			}
			StringBuilder localFileBuilder = new StringBuilder();
			// 第一行必须是#EXTM3U
			if (lines[0] != HLSTags.EXTM3U)
			{
				return false;
			}
			_ = localFileBuilder.Append(HLSTags.EXTM3U);
			u64 uIndex = 0;
			MediaSegment tempMediaSegment = new MediaSegment();
			bool bEndList = false;
			for (n32 i = 1; i < lines.Count; i++)
			{
				_ = localFileBuilder.Append("\r\n");
				string sLine = lines[i];
				if (sLine.Length == 0)
				{
					continue;
				}
				if (sLine.StartsWith('#'))
				{
					_ = localFileBuilder.Append(sLine);
					if (!sLine.StartsWith("#EXT", StringComparison.Ordinal))
					{
						// 忽略注释
						continue;
					}
					else if (sLine.StartsWith(HLSTags.EXTINF, StringComparison.Ordinal))
					{
						if (bEndList)
						{
							return false;
						}
						Match match = ExtInfRegex().Match(sLine);
						if (!match.Success)
						{
							return false;
						}
						tempMediaSegment.Index = uIndex;
						tempMediaSegment.Duration = SToF64(match.Groups[1].Value);
						tempMediaSegment.Title = match.Groups[2].Value;
						Manifest.MediaSegments.Add(tempMediaSegment);
						uIndex++;
					}
					else if (sLine == HLSTags.EXTM3U)
					{
						// 非第一行必须不是#EXTM3U
						return false;
					}
					else if (sLine == HLSTags.EXT_X_DISCONTINUITY)
					{
						if (bEndList)
						{
							return false;
						}
						// #EXTINF下没有url
						if (tempMediaSegment.Index != null && tempMediaSegment.Url == null)
						{
							return false;
						}
						Manifest.DiscontinuityStarts.Add(uIndex);
					}
					else if (sLine.StartsWith(HLSTags.EXT_X_TARGETDURATION, StringComparison.Ordinal))
					{
						if (bEndList)
						{
							return false;
						}
						Match match = ExtXTargetDurationRegex().Match(sLine);
						if (!match.Success)
						{
							return false;
						}
						Manifest.TargetDuration = SToF64(match.Groups[1].Value);
					}
					else if (sLine == HLSTags.EXT_X_ENDLIST)
					{
						if (bEndList)
						{
							return false;
						}
						bEndList = true;
					}
					else
					{
						return false;
					}
				}
				else
				{
					if (bEndList)
					{
						return false;
					}
					// 非#行上没有#EXTINF
					if (tempMediaSegment.Index == null)
					{
						return false;
					}
					// 连续出现非#行
					if (tempMediaSegment.Url != null)
					{
						return false;
					}
					string sIndexPlaceHolder = $$"""{{{tempMediaSegment.Index}}}""";
					_ = localFileBuilder.Append(sIndexPlaceHolder);
					tempMediaSegment.Url = sLine;
					tempMediaSegment = new MediaSegment();
				}
			}
			Manifest.EndList = bEndList;
			// #EXTINF下没有url
			if (tempMediaSegment.Index != null && tempMediaSegment.Url == null)
			{
				return false;
			}
			n32 nCount = Manifest.MediaSegments.Count;
			n32 nWidth = $"{nCount}".Length;
			string sDownloadFileNameFormat = new string('0', nWidth);
			for (n32 i = 0; i < nCount; i++)
			{
				string sIndexPlaceHolder = $$"""{{{i}}}""";
				string sRelativePath = $"{DownloadDirName}/{i.ToString(sDownloadFileNameFormat, CultureInfo.InvariantCulture)}.ts";
				_ = localFileBuilder.Replace(sIndexPlaceHolder, sRelativePath);
			}
			LocalFile = localFileBuilder.ToString();
			return true;
		}
	}
}
