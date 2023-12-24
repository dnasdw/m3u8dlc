using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

using Spectre.Console;

namespace m3u8dlc
{
	public static class DownloadUtility
	{
		public static readonly HttpClientHandler HttpClientHandler = new HttpClientHandler()
		{
			// 自动解压缩
			AutomaticDecompression = DecompressionMethods.All,
			// 不应跟随重定向,方便每次重定向修改headers
			AllowAutoRedirect = false,
		};

		private static readonly HttpClient s_httpClient = new HttpClient(HttpClientHandler);

		public static async Task<n32> DownloadFileAsync(string url, string path, bool tempFile = false, DownloadRecorder? downloadRecorder = null, u64? recorderIndex = null)
		{
			u64 uRecorderIndex = recorderIndex != null ? recorderIndex.Value : 0;
			try
			{
				if (!tempFile && File.Exists(path))
				{
					return 0;
				}
				string sUrl = url;
				// 用循环代替递归
				do
				{
					Uri uri = new Uri(sUrl);
					using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);
					_ = request.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36 Edg/120.0.0.0");
					HttpResponseMessage response = await s_httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
					n32 nStatusCode = (static_cast_n32)(response.StatusCode);
					if (nStatusCode == 302)
					{
						HttpResponseHeaders responseHeaders = response.Headers;
						if (responseHeaders.Location != null)
						{
							Uri redirectUri = responseHeaders.Location;
							if (!redirectUri.IsAbsoluteUri)
							{
								redirectUri = new Uri(uri, redirectUri);
							}
							sUrl = redirectUri.AbsoluteUri;
							// 用循环代替递归
							continue;
						}
					}
					_ = response.EnsureSuccessStatusCode();
					if (nStatusCode != 200)
					{
						throw new HttpRequestException($"Response status code does not indicate success: {nStatusCode}", null, response.StatusCode);
					}
					n64? nContentLength = response.Content.Headers.ContentLength;
					if (downloadRecorder != null)
					{
						downloadRecorder.SetContentLength(uRecorderIndex, nContentLength);
					}
					n64 nReadLength = 0;
					using Stream inputStream = await response.Content.ReadAsStreamAsync();
					using FileStream outputStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
					bool bHasSpeedLimit = DownloadRecorder.SpeedLimit != null;
					// 缓冲默认大小16K
					n64 nBufferSize = 16 * 1024;
					if (bHasSpeedLimit)
					{
						Debug.Assert(DownloadRecorder.SpeedLimit != null);
						// 如果限速小于16K/s,则减小缓冲大小,否则可能无法限速
						nBufferSize = Math.Min(nBufferSize, DownloadRecorder.SpeedLimit.Value);
					}
					byte[] uBuffer = new byte[nBufferSize];
					n32 nSize = 0;
					do
					{
						while (DownloadRecorder.HasReachedSpeedLimit())
						{
							await Task.Delay(1);
						}
						nSize = inputStream.Read(uBuffer);
						if (nSize <= 0)
						{
							break;
						}
						nReadLength += nSize;
						if (downloadRecorder != null)
						{
							if (nContentLength == null)
							{
								// 如果header中没有ContentLength,则将现在已下载大小设置为ContentLength,保证下载进度列显示的总大小不比已下载大小要小
								downloadRecorder.SetContentLength(uRecorderIndex, nReadLength);
							}
							downloadRecorder.SetDonwloadSize(uRecorderIndex, nReadLength);
							downloadRecorder.AddDeltaDownloadSize(nSize);
						}
						if (bHasSpeedLimit)
						{
							DownloadRecorder.AddCurrentSpeed(nSize);
						}
						await outputStream.WriteAsync(uBuffer.AsMemory(0, nSize));
					} while (true);
					if (nContentLength == null)
					{
						return nStatusCode;
					}
					else
					{
						return nReadLength == nContentLength ? 0 : 1;
					}
				} while (true);
			}
			catch (Exception ex)
			{
				AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
				if (downloadRecorder != null)
				{
					// 如果出错,清除本文件相关的大小,防止统计出错,重试时会重新设置这些值
					downloadRecorder.SetContentLength(uRecorderIndex, null);
					downloadRecorder.SetDonwloadSize(uRecorderIndex, 0);
				}
				if (ex.Message.Contains(": 405"))
				{
					return 405;
				}
				return -1;
			}
		}
	}
}
