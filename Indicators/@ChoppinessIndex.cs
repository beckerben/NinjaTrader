//
// Copyright (C) 2025, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
#region Using declarations
using System;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Core.FloatingPoint;
#endregion

// This namespace holds indicators in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators
{
	public class ChoppinessIndex : Indicator
	{
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description = Custom.Resource.NinjaScriptIndicatorDescriptionChoppinessIndex;
				Name		= Custom.Resource.NinjaScriptIndicatorNameChoppinessIndex;
				IsOverlay	= false;
				Period		= 14;

				AddPlot(Brushes.DodgerBlue,		Custom.Resource.NinjaScriptIndicatorNameChoppinessIndex);
				AddLine(Brushes.DarkCyan, 38.2,	Custom.Resource.NinjaScriptIndicatorLower);
				AddLine(Brushes.DarkCyan, 62.8,	Custom.Resource.NinjaScriptIndicatorUpper);
			}
		}

		protected override void OnBarUpdate() => Value[0] = (MAX(High, Period)[0] - MIN(Low, Period)[0]).ApproxCompare(0) == 0 || SUM(ATR(1), Period)[0].ApproxCompare(0) == 0 ? 0 : 100 * Math.Log10(SUM(ATR(1), Period)[0] / (MAX(High, Period)[0] - MIN(Low, Period)[0])) / Math.Log10(Period);

		#region Properties
		[Range(2, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Period", GroupName = "NinjaScriptParameters", Order = 0)]
		public int Period { get; set; }
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private ChoppinessIndex[] cacheChoppinessIndex;
		public ChoppinessIndex ChoppinessIndex(int period)
		{
			return ChoppinessIndex(Input, period);
		}

		public ChoppinessIndex ChoppinessIndex(ISeries<double> input, int period)
		{
			if (cacheChoppinessIndex != null)
				for (int idx = 0; idx < cacheChoppinessIndex.Length; idx++)
					if (cacheChoppinessIndex[idx] != null && cacheChoppinessIndex[idx].Period == period && cacheChoppinessIndex[idx].EqualsInput(input))
						return cacheChoppinessIndex[idx];
			return CacheIndicator<ChoppinessIndex>(new ChoppinessIndex(){ Period = period }, input, ref cacheChoppinessIndex);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.ChoppinessIndex ChoppinessIndex(int period)
		{
			return indicator.ChoppinessIndex(Input, period);
		}

		public Indicators.ChoppinessIndex ChoppinessIndex(ISeries<double> input , int period)
		{
			return indicator.ChoppinessIndex(input, period);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.ChoppinessIndex ChoppinessIndex(int period)
		{
			return indicator.ChoppinessIndex(Input, period);
		}

		public Indicators.ChoppinessIndex ChoppinessIndex(ISeries<double> input , int period)
		{
			return indicator.ChoppinessIndex(input, period);
		}
	}
}

#endregion