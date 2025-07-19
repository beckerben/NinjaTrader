//
// Copyright (C) 2025, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
#region Using declarations
using System.Windows.Media;
using NinjaTrader.Gui;
#endregion

// This namespace holds indicators in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators
{
	/// <summary>
	/// Calculates the range of a bar.
	/// </summary>
	public class Range : Indicator
	{
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= Custom.Resource.NinjaScriptIndicatorDescriptionRange;
				Name						= Custom.Resource.NinjaScriptIndicatorNameRange;
				BarsRequiredToPlot			= 0;
				IsSuspendedWhileInactive	= true;

				AddPlot(new Stroke(Brushes.Goldenrod, 2), PlotStyle.Bar, Custom.Resource.RangeValue);
			}
		}

		protected override void OnBarUpdate() => Value[0] = High[0] - Low[0];
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private Range[] cacheRange;
		public Range Range()
		{
			return Range(Input);
		}

		public Range Range(ISeries<double> input)
		{
			if (cacheRange != null)
				for (int idx = 0; idx < cacheRange.Length; idx++)
					if (cacheRange[idx] != null &&  cacheRange[idx].EqualsInput(input))
						return cacheRange[idx];
			return CacheIndicator<Range>(new Range(), input, ref cacheRange);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.Range Range()
		{
			return indicator.Range(Input);
		}

		public Indicators.Range Range(ISeries<double> input )
		{
			return indicator.Range(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.Range Range()
		{
			return indicator.Range(Input);
		}

		public Indicators.Range Range(ISeries<double> input )
		{
			return indicator.Range(input);
		}
	}
}

#endregion
