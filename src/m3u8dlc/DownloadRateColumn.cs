using System;

using Spectre.Console;
using Spectre.Console.Rendering;

namespace m3u8dlc
{
	/// <summary>
	/// A column showing download progress.
	/// </summary>
	public class DownloadRateColumn : ProgressColumn
	{
		private readonly DownloadRecorder? m_downloadRecorder = null;
		private DateTime m_lastTime = DateTime.Now;
		private string m_sDownloadSize = FileSizeUtility.GetString(0);
		private string m_sTotalSize = "?";

		public DownloadRateColumn(DownloadRecorder? downloadRecorder = null)
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
			if (task.IsFinished)
			{
				n64 nDownloadSize = m_downloadRecorder.DownloadSize;
				m_sDownloadSize = FileSizeUtility.GetString(nDownloadSize);
				return new Markup($"[green]{m_sDownloadSize}[/]");
			}
			DateTime time = DateTime.Now;
			TimeSpan delta = time - m_lastTime;
			if (delta.TotalSeconds >= 1)
			{
				m_lastTime = time;
				n64 nDownloadSize = m_downloadRecorder.DownloadSize;
				m_sDownloadSize = FileSizeUtility.GetString(nDownloadSize);
				n64 nTotalSize = m_downloadRecorder.TotalSize;
				m_sTotalSize = nTotalSize == 0 ? "?" : FileSizeUtility.GetString(nTotalSize);
			}
			return new Markup($"[darkcyan]{m_sDownloadSize}[/][grey]/{m_sTotalSize}[/]");
		}
	}
}
