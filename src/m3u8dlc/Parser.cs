using System;
using System.Collections.Generic;
using System.IO;

using Spectre.Console;

namespace m3u8dlc
{
	public class Parser
	{
		public Manifest? Manifest { get; private set; } = null;
		public SortedDictionary<string, string> TempFiles { get; init; } = new SortedDictionary<string, string>();

		private readonly string m_sUrl = "";
		private string m_sText = "";

		public Parser(string url)
		{
			m_sUrl = url;
		}

		public bool Parse()
		{
			if (m_sUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
			{
				// TODO: implement
				return false;
			}
			else
			{
				AnsiConsole.MarkupLine("加载URL: " + m_sUrl);
				string sPath = PathUtility.GetLocalPath(m_sUrl);
				m_sText = File.ReadAllText(sPath);
			}
			return parseText();
		}

		private bool parseText()
		{
			if (string.IsNullOrEmpty(m_sText))
			{
				return false;
			}
			if (m_sText.StartsWith(HLSTags.EXTM3U, StringComparison.Ordinal))
			{
				AnsiConsole.MarkupLine("内容匹配: [white on deepskyblue1]HTTP Live Streaming[/]");
				M3U8Parser m3u8Parser = new M3U8Parser(m_sText);
				AnsiConsole.MarkupLine("正在解析媒体信息...");
				if (!m3u8Parser.Parse())
				{
					return false;
				}
				Manifest = m3u8Parser.Manifest;
				TempFiles["orig.m3u8"] = m_sText;
				TempFiles["local.m3u8"] = m3u8Parser.LocalFile;
			}
			else
			{
				return false;
			}
			return true;
		}
	}
}
