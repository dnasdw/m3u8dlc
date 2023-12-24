using System;
using System.ComponentModel;
using System.Globalization;
using System.Net;

namespace m3u8dlc
{
	public class ProxyConverter : TypeConverter
	{
		public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
		{
			if (value is string sValue)
			{
				do
				{
					if (string.IsNullOrEmpty(sValue))
					{
						break;
					}
					Uri uri = new Uri(sValue);
					WebProxy proxy = new WebProxy(uri, true);
					if (!string.IsNullOrEmpty(uri.UserInfo))
					{
						string[] sUserInfo = uri.UserInfo.Split(':');
						if (sUserInfo.Length == 1)
						{
							proxy.Credentials = new NetworkCredential(sUserInfo[0], "");
						}
						else if (sUserInfo.Length == 2)
						{
							proxy.Credentials = new NetworkCredential(sUserInfo[0], sUserInfo[1]);
						}
						else
						{
							break;
						}
					}
					return proxy;
				} while (false);
			}

			return base.ConvertFrom(context, culture, value);
		}
	}
}
