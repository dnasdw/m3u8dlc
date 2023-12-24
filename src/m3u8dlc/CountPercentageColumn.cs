using System;

using Spectre.Console;
using Spectre.Console.Rendering;

namespace m3u8dlc
{
	/// <summary>
	/// A column showing task progress in percentage.
	/// </summary>
	public class CountPercentageColumn : ProgressColumn
	{
		/// <summary>
		/// Gets or sets the style for a non-complete task.
		/// </summary>
		public Style Style { get; set; } = Style.Plain;

		/// <summary>
		/// Gets or sets the style for a completed task.
		/// </summary>
		public Style CompletedStyle { get; set; } = Color.Green;

		// 列宽度,显示"100.00%"的宽度为7
		private readonly n32 m_nWidth = 7; // "100.00%".Length;

		// 要在百分数前面显示最大为"maxValue/maxValue "的字符串
		public CountPercentageColumn(n32 maxValue)
		{
			m_nWidth += ($"{maxValue}".Length + 1) * 2;
		}

		/// <inheritdoc/>
		public override IRenderable Render(RenderOptions options, ProgressTask task, TimeSpan deltaTime)
		{
			// 强制将task.MaxValue和task.Value转换为整数
			n32 nMaxValue = (static_cast_n32)(task.MaxValue);
			n32 nValue = (static_cast_n32)(task.Value);
			f64 fPercentage = task.Percentage;
			Style style = fPercentage == 100 ? CompletedStyle : Style ?? Style.Plain;
			// 百分号前保证宽度为6,"  0.00"~"100.00"
			return new Text($"{nValue}/{nMaxValue} {fPercentage,6:F2}%", style).RightJustified();
		}

		/// <inheritdoc/>
		public override int? GetColumnWidth(RenderOptions options)
		{
			return m_nWidth;
		}
	}
}
