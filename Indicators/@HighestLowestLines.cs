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

//This namespace holds Indicators in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators
{
	/// <summary>
	/// Plots the highest high and lowest low over a specified period as two lines.
	/// </summary>
	public class HighestLowestLines : Indicator
	{
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= @"Plots the highest high and lowest low over a specified period as two lines.";
				Name						= "Highest Lowest Lines";
				IsOverlay					= true;
				IsSuspendedWhileInactive	= true;
				Period						= 21;
				HighestHighColor			= Brushes.Green;
				LowestLowColor				= Brushes.Red;

				AddPlot(new Stroke(HighestHighColor, 1), PlotStyle.Line, "Highest High");
				AddPlot(new Stroke(LowestLowColor, 1), PlotStyle.Line, "Lowest Low");
			}
		}

		protected override void OnBarUpdate()
		{
			// Update plot colors to match properties
			Plots[0].Brush = HighestHighColor;
			Plots[1].Brush = LowestLowColor;

			if (CurrentBar < Period)
				return;

			Values[0][0] = MAX(High, Period)[0];
			Values[1][0] = MIN(Low, Period)[0];
		}

		#region Properties
		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(Name = "Period", Description = "Number of bars to look back", GroupName = "NinjaScriptParameters", Order = 0)]
		public int Period { get; set; }

		[Display(Name = "Highest High Color", Description = "Color for the highest high line", GroupName = "NinjaScriptParameters", Order = 1)]
		[XmlIgnore]
		public Brush HighestHighColor { get; set; }

		[Browsable(false)]
		public string HighestHighColorSerialize
		{
			get { return Serialize.BrushToString(HighestHighColor); }
			set { HighestHighColor = Serialize.StringToBrush(value); }
		}

		[Display(Name = "Lowest Low Color", Description = "Color for the lowest low line", GroupName = "NinjaScriptParameters", Order = 2)]
		[XmlIgnore]
		public Brush LowestLowColor { get; set; }

		[Browsable(false)]
		public string LowestLowColorSerialize
		{
			get { return Serialize.BrushToString(LowestLowColor); }
			set { LowestLowColor = Serialize.StringToBrush(value); }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> HighestHigh => Values[0];

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> LowestLow => Values[1];
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private HighestLowestLines[] cacheHighestLowestLines;
		public HighestLowestLines HighestLowestLines(int period)
		{
			return HighestLowestLines(Input, period);
		}

		public HighestLowestLines HighestLowestLines(ISeries<double> input, int period)
		{
			if (cacheHighestLowestLines != null)
				for (int idx = 0; idx < cacheHighestLowestLines.Length; idx++)
					if (cacheHighestLowestLines[idx] != null && cacheHighestLowestLines[idx].Period == period && cacheHighestLowestLines[idx].EqualsInput(input))
						return cacheHighestLowestLines[idx];
			return CacheIndicator<HighestLowestLines>(new HighestLowestLines(){ Period = period }, input, ref cacheHighestLowestLines);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.HighestLowestLines HighestLowestLines(int period)
		{
			return indicator.HighestLowestLines(Input, period);
		}

		public Indicators.HighestLowestLines HighestLowestLines(ISeries<double> input , int period)
		{
			return indicator.HighestLowestLines(input, period);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.HighestLowestLines HighestLowestLines(int period)
		{
			return indicator.HighestLowestLines(Input, period);
		}

		public Indicators.HighestLowestLines HighestLowestLines(ISeries<double> input , int period)
		{
			return indicator.HighestLowestLines(input, period);
		}
	}
}

#endregion
