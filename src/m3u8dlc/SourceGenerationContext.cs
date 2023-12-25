using System.Text.Json.Serialization;

namespace m3u8dlc
{
	[JsonSerializable(typeof(Manifest))]
	public partial class SourceGenerationContext : JsonSerializerContext
	{
	}
}
