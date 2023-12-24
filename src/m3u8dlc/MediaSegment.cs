namespace m3u8dlc
{
	public class MediaSegment
	{
		public u64? Index { get; set; }
		public f64? Duration { get; set; }
		public string? Title { get; set; }
		public string? Url { get; set; }

#if DEBUG
		public override string ToString()
		{
			string sString = $"i:{Index}, d:{Duration}, t:{Title}, u:{Url}";
			return sString;
		}
#endif
	}
}
