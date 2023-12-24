using System.Collections.Generic;

namespace m3u8dlc
{
	public class Manifest
	{
		public f64? TargetDuration { get; set; }
		public List<MediaSegment> MediaSegments { get; init; } = new List<MediaSegment>();
		public bool? EndList { get; set; }
	}
}
