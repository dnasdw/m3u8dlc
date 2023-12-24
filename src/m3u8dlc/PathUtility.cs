using System;
using System.Collections.Generic;
using System.IO;

namespace m3u8dlc
{
	public static class PathUtility
	{
		public static string GetFileNameWithoutExtension(string url)
		{
			if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase) && !url.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
			{
				url = Path.GetFullPath(url);
			}
			Uri uri = new Uri(url);
			string sName = Path.GetFileNameWithoutExtension(uri.LocalPath);
			return sName;
		}

		public static string GetLocalPath(string path)
		{
			if (!path.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
			{
				path = Path.GetFullPath(path);
			}
			Uri uri = new Uri(path);
			path = uri.LocalPath;
			return path;
		}

		public static string? FindExePath(string fileNameWithoutExtension)
		{
			string sFileName = fileNameWithoutExtension;
			if (OperatingSystem.IsWindows())
			{
				sFileName += ".exe";
			}
			List<string> dirs = new List<string>() { Environment.CurrentDirectory };
			string? sPath = Path.GetDirectoryName(Environment.ProcessPath);
			if (sPath != null)
			{
				dirs.Add(sPath);
			}
			sPath = Environment.GetEnvironmentVariable("PATH");
			if (sPath != null)
			{
				string[] sDir = sPath.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
				for (n32 i = 0; i < sDir.Length; i++)
				{
					dirs.Add(sDir[i]);
				}
			}
			for (List<string>.Enumerator it = dirs.GetEnumerator(); it.MoveNext(); /**/)
			{
				string sDir = it.Current;
				string sFilePath = Path.Combine(sDir, sFileName);
				if (File.Exists(sFilePath))
				{
					return sFilePath;
				}
			}
			return null;
		}
	}
}
