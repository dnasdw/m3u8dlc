using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace m3u8dlc
{
	public class DownloadRecorder
	{
		public static n64? SpeedLimit { get; set; } = null;
		public n64 TotalSize { get { return m_nTotalSize; } }
		public n64 DownloadSize { get { return m_nDownloadSize; } }

		private static n64 s_nLastTime = DateTime.Now.ToBinary();
		private static n64 s_nCurrentSpeed = 0;
		private readonly ConcurrentDictionary<u64, n64?> m_contentLength = new ConcurrentDictionary<u64, n64?>();
		private readonly ConcurrentDictionary<u64, n64> m_downloadSize = new ConcurrentDictionary<u64, n64>();
		private n64 m_nTotalSize = 0;
		private n64 m_nDownloadSize = 0;
		private n64 m_nDeltaDownloadSize = 0;

		public static bool HasReachedSpeedLimit()
		{
			if (SpeedLimit == null)
			{
				return false;
			}
			DateTime time = DateTime.Now;
			TimeSpan deltaTime = time - DateTime.FromBinary(s_nLastTime);
			if (deltaTime.TotalSeconds >= 1)
			{
				_ = Interlocked.Exchange(ref s_nLastTime, time.ToBinary());
				_ = Interlocked.Exchange(ref s_nCurrentSpeed, 0);
				return false;
			}
			return s_nCurrentSpeed >= SpeedLimit;
		}

		public static void AddCurrentSpeed(n64 currentSpeed)
		{
			_ = Interlocked.Add(ref s_nCurrentSpeed, currentSpeed);
		}

		public void SetContentLength(u64 index, n64? contentLength)
		{
			m_contentLength[index] = contentLength;
			n64 nTotalSize = 0;
			for (IEnumerator<KeyValuePair<u64, n64?>> it = m_contentLength.GetEnumerator(); it.MoveNext(); /**/)
			{
				KeyValuePair<u64, n64?> item = it.Current;
				if (item.Value != null)
				{
					nTotalSize += item.Value.Value;
				}
			}
			_ = Interlocked.Exchange(ref m_nTotalSize, nTotalSize);
		}

		public void SetDonwloadSize(u64 index, n64 downloadSize)
		{
			m_downloadSize[index] = downloadSize;
			n64 nDownloadSize = 0;
			for (IEnumerator<KeyValuePair<u64, n64>> it = m_downloadSize.GetEnumerator(); it.MoveNext(); /**/)
			{
				KeyValuePair<u64, n64> item = it.Current;
				nDownloadSize += item.Value;
			}
			_ = Interlocked.Exchange(ref m_nDownloadSize, nDownloadSize);
		}

		public void AddDeltaDownloadSize(n64 deltaDownloadSize)
		{
			_ = Interlocked.Add(ref m_nDeltaDownloadSize, deltaDownloadSize);
		}

		public n64 GetAndResetDeltaDownloadSize()
		{
			n64 nDeltaDownloadSize = Interlocked.Exchange(ref m_nDeltaDownloadSize, 0);
			return nDeltaDownloadSize;
		}
	}
}
