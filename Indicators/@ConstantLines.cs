//
// Copyright (C) 2025, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
#region Using declarations

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Gui;

#endregion

// This namespace holds indicators in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators
{
	/// <summary>
	/// Plots lines at user  defined values.
	/// </summary>
	public class ConstantLines : Indicator
	{
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= Custom.Resource.NinjaScriptIndicatorDescriptionConstantLines;
				Name						= Custom.Resource.NinjaScriptIndicatorNameConstantLines;
				IsSuspendedWhileInactive	= true;
				Line1Value					= 0;
				Line2Value					= 0;
				Line3Value					= 0;
				Line4Value					= 0;
				IsAutoScale					= false;
				IsOverlay					= true;

				IsChartOnly					= true;
				DisplayInDataBox			= false;

				AddPlot(new Stroke(Brushes.DodgerBlue),	PlotStyle.HLine, Custom.Resource.ConstantLines1);
				AddPlot(new Stroke(Brushes.DarkCyan),	PlotStyle.HLine, Custom.Resource.ConstantLines2);
				AddPlot(new Stroke(Brushes.SlateBlue),	PlotStyle.HLine, Custom.Resource.ConstantLines3);
				AddPlot(new Stroke(Brushes.Goldenrod),	PlotStyle.HLine, Custom.Resource.ConstantLines4);
			}
		}

		protected override void OnBarUpdate()
		{
			if (Line1Value != 0) Line1[0] = Line1Value;
			if (Line2Value != 0) Line2[0] = Line2Value;
			if (Line3Value != 0) Line3[0] = Line3Value;
			if (Line4Value != 0) Line4[0] = Line4Value;
		}

		#region Properties
		[Browsable(false)]	// This line prevents the data series from being displayed in the indicator properties dialog, do not remove
		[XmlIgnore]		// This line ensures that the indicator can be saved/recovered as part of a chart template, do not remove
		public Series<double> Line1 => Values[0];

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> Line2 => Values[1];

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> Line3 => Values[2];

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> Line4 => Values[3];

		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Line1Value", GroupName = "NinjaScriptParameters", Order = 0)]
		public double Line1Value { get; set; }

		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Line2Value", GroupName = "NinjaScriptParameters", Order = 1)]
		public double Line2Value { get; set; }

		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Line3Value", GroupName = "NinjaScriptParameters", Order = 2)]
		public double Line3Value { get; set; }

		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Line4Value", GroupName = "NinjaScriptParameters", Order = 3)]
		public double Line4Value { get; set; }
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private ConstantLines[] cacheConstantLines;
		public ConstantLines ConstantLines(double line1Value, double line2Value, double line3Value, double line4Value)
		{
			return ConstantLines(Input, line1Value, line2Value, line3Value, line4Value);
		}

		public ConstantLines ConstantLines(ISeries<double> input, double line1Value, double line2Value, double line3Value, double line4Value)
		{
			if (cacheConstantLines != null)
				for (int idx = 0; idx < cacheConstantLines.Length; idx++)
					if (cacheConstantLines[idx] != null && cacheConstantLines[idx].Line1Value == line1Value && cacheConstantLines[idx].Line2Value == line2Value && cacheConstantLines[idx].Line3Value == line3Value && cacheConstantLines[idx].Line4Value == line4Value && cacheConstantLines[idx].EqualsInput(input))
						return cacheConstantLines[idx];
			return CacheIndicator<ConstantLines>(new ConstantLines(){ Line1Value = line1Value, Line2Value = line2Value, Line3Value = line3Value, Line4Value = line4Value }, input, ref cacheConstantLines);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.ConstantLines ConstantLines(double line1Value, double line2Value, double line3Value, double line4Value)
		{
			return indicator.ConstantLines(Input, line1Value, line2Value, line3Value, line4Value);
		}

		public Indicators.ConstantLines ConstantLines(ISeries<double> input , double line1Value, double line2Value, double line3Value, double line4Value)
		{
			return indicator.ConstantLines(input, line1Value, line2Value, line3Value, line4Value);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.ConstantLines ConstantLines(double line1Value, double line2Value, double line3Value, double line4Value)
		{
			return indicator.ConstantLines(Input, line1Value, line2Value, line3Value, line4Value);
		}

		public Indicators.ConstantLines ConstantLines(ISeries<double> input , double line1Value, double line2Value, double line3Value, double line4Value)
		{
			return indicator.ConstantLines(input, line1Value, line2Value, line3Value, line4Value);
		}
	}
}

#endregion
