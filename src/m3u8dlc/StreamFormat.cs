using System;

namespace m3u8dlc
{
	public class StreamFormat
	{
		public string Buffer { get; set; } = "";
		public n32 Index { get; set; } = 0;
		public n32 Index2 { get; set; } = 0;
		public n32 Id { get; set; } = 0;
		public string Language { get; set; } = "";
		public string CodecType { get; set; } = "";
		public string CodecName { get; set; } = "";

		public bool IsAAC()
		{
			return CodecType.Equals("audio", StringComparison.OrdinalIgnoreCase) && CodecName.Equals("aac", StringComparison.OrdinalIgnoreCase);
		}

		public override string ToString()
		{
			return Buffer;
		}
	}
}
