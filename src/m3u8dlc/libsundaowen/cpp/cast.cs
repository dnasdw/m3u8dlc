using System;
using System.Globalization;

namespace sdw
{
	public static partial class cpp
	{
		public static T? dynamic_cast<T>(object a_value) where T : class
		{
			return a_value as T;
		}

		public static T? reinterpret_cast<T>(object? a_value)
		{
			return (T?)a_value;
		}

		public static T? static_cast_slow<T>(object? a_value)
		{
			return (T?)Convert.ChangeType(a_value, typeof(T), CultureInfo.InvariantCulture);
		}
	}
}
