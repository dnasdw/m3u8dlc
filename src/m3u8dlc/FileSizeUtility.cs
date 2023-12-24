namespace m3u8dlc
{
	public static class FileSizeUtility
	{
		public static string GetString(n64 fileSize)
		{
			if (fileSize < 0)
			{
				fileSize = 0;
			}
			if (fileSize < 1024)
			{
				return $"{fileSize}B";
			}
			if (fileSize < 1024 * 1024)
			{
				return $"{fileSize / (static_cast_f64)(1024):F3}KiB";
			}
			if (fileSize < 1024 * 1024 * 1024)
			{
				return $"{fileSize / (static_cast_f64)(1024 * 1024):F3}MiB";
			}
			return $"{fileSize / (static_cast_f64)(1024 * 1024 * 1024):F3}GiB";
		}
	}
}
