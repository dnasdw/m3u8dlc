using System;
using System.ComponentModel;
using System.Globalization;
using System.Text.RegularExpressions;

using static sdw.zzz;

namespace m3u8dlc
{
	public partial class DownloadSpeedConverter : TypeConverter
	{
		[GeneratedRegex(@"([\.\d]+)(K|M)", RegexOptions.IgnoreCase)]
		private static partial Regex SpeedRegex();

		public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
		{
			if (value is string sValue)
			{
				Match match = SpeedRegex().Match(sValue);
				if (match.Success)
				{
					f64 fValue = SToF64(match.Groups[1].Value);
					string sUnit = match.Groups[2].Value;
					if (sUnit == "K" || sUnit == "k")
					{
						n64 nValue = Math.Max((static_cast_n64)(Math.Ceiling(fValue * 1024)), 1);
						return nValue;
					}
					if (sUnit == "M" || sUnit == "m")
					{
						n64 nValue = Math.Max((static_cast_n64)(Math.Ceiling(fValue * (1024 * 1024))), 1);
						return nValue;
					}
				}
			}

			return base.ConvertFrom(context, culture, value);
		}
	}
}
