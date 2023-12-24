using System;
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
	}
}
