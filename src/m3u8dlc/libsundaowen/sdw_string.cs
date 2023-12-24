using System;
using System.Globalization;

namespace sdw
{
	public static partial class zzz
	{
		public static n8 SToN8(string a_sString, int a_nRadix = 10)
		{
			return (static_cast_n8)(Convert.ToInt32(a_sString, a_nRadix));
		}

		public static n16 SToN16(string a_sString, int a_nRadix = 10)
		{
			return (static_cast_n16)(Convert.ToInt32(a_sString, a_nRadix));
		}

		public static n32 SToN32(string a_sString, int a_nRadix = 10)
		{
			return (static_cast_n32)(Convert.ToInt32(a_sString, a_nRadix));
		}

		public static n64 SToN64(string a_sString, int a_nRadix = 10)
		{
			return Convert.ToInt64(a_sString, a_nRadix);
		}

		public static u8 SToU8(string a_sString, int a_nRadix = 10)
		{
			return (static_cast_u8)(Convert.ToUInt32(a_sString, a_nRadix));
		}

		public static u16 SToU16(string a_sString, int a_nRadix = 10)
		{
			return (static_cast_u16)(Convert.ToUInt32(a_sString, a_nRadix));
		}

		public static u32 SToU32(string a_sString, int a_nRadix = 10)
		{
			return (static_cast_u32)(Convert.ToUInt32(a_sString, a_nRadix));
		}

		public static u64 SToU64(string a_sString, int a_nRadix = 10)
		{
			return Convert.ToUInt64(a_sString, a_nRadix);
		}

		public static f32 SToF32(string a_sString)
		{
			return (static_cast_f32)(Convert.ToDouble(a_sString, CultureInfo.InvariantCulture));
		}

		public static f64 SToF64(string a_sString)
		{
			return Convert.ToDouble(a_sString, CultureInfo.InvariantCulture);
		}
	}
}
