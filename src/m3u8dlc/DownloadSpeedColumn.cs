using System;

using Spectre.Console;
using Spectre.Console.Rendering;

namespace m3u8dlc
{
	/// <summary>
	/// A column showing transfer speed.
	/// </summary>
	public class DownloadSpeedColumn : ProgressColumn
	{
		private readonly DownloadRecorder? m_downloadRecorder = null;
		private DateTime m_lastTime = DateTime.Now;
		private string m_sDownloadSpeed = FileSizeUtility.GetString(0) + "/s";

		public DownloadSpeedColumn(DownloadRecorder? downloadRecorder = null)
		{
			m_downloadRecorder = downloadRecorder;
		}

		/// <inheritdoc/>
		public override IRenderable Render(RenderOptions options, ProgressTask task, TimeSpan deltaTime)
		{
			if (m_downloadRecorder == null)
			{
				return Text.Empty;
			}
			DateTime time = DateTime.Now;
			TimeSpan delta = time - m_lastTime;
			if (delta.TotalSeconds >= 1)
			{
				m_lastTime = time;
				n64 nDeltaDownloadSize = m_downloadRecorder.GetAndResetDeltaDownloadSize();
				n64 nDownloadSpeed = (static_cast_n64)(nDeltaDownloadSize / delta.TotalSeconds);
				m_sDownloadSpeed = FileSizeUtility.GetString(nDownloadSpeed) + "/s";
			}
			return new Text(m_sDownloadSpeed, Color.Green);
		}
	}
}
