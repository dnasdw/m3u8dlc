using System;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

using Spectre.Console;

namespace m3u8dlc
{
	public static class JsonUtility
	{
		private static readonly JsonSerializerOptions s_jsonSerializerOptions = new JsonSerializerOptions()
		{
			// 防止url被转义
			Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
			// 不输出null对象
			DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
			// 输出缩进
			WriteIndented = true,
		};

		public static bool SerializeFile<T>(T? obj, string path)
		{
			// 如果不判断,空对象会序列化为null这4个字母
			if (obj == null)
			{
				return false;
			}
			try
			{
				string sJson = "";
				if (!SerializeString(obj, ref sJson))
				{
					return false;
				}
				File.WriteAllText(path, sJson);
			}
			catch (Exception ex)
			{
				AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
				return false;
			}
			return true;
		}

		public static bool SerializeString<T>(T? obj, ref string json)
		{
			// 如果不判断,空对象会序列化为null这4个字母
			if (obj == null)
			{
				return false;
			}
			try
			{
				json = JsonSerializer.Serialize(obj, s_jsonSerializerOptions);
			}
			catch (Exception ex)
			{
				AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
				return false;
			}
			return true;
		}

		public static bool DeserializeFile<T>(string path, ref T? obj)
		{
			if (!File.Exists(path))
			{
				return false;
			}
			try
			{
				string sJson = File.ReadAllText(path);
				if (string.IsNullOrEmpty(sJson))
				{
					return false;
				}
				if (!DeserializeString(sJson, ref obj))
				{
					return false;
				}
			}
			catch (Exception ex)
			{
				AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
				return false;
			}
			return obj != null;
		}

		public static bool DeserializeString<T>(string json, ref T? obj)
		{
			if (string.IsNullOrEmpty(json))
			{
				return false;
			}
			try
			{
				obj = JsonSerializer.Deserialize<T>(json, s_jsonSerializerOptions);
			}
			catch (Exception ex)
			{
				AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
				return false;
			}
			return obj != null;
		}
	}
}
