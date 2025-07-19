//
// Copyright (C) 2025, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
#region Using declarations
using System;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
#endregion

//This namespace holds indicators in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators
{
	/// <summary>
	/// The Hull Moving Average (HMA) employs weighted MA calculations to offer superior
	/// smoothing, and much less lag, over traditional SMA indicators.
	/// This indicator is based on the reference article found here:
	/// http://www.justdata.com.au/Journals/AlanHull/hull_ma.htm
	/// </summary>
	public class HMA : Indicator
	{
		private Series<double> diffSeries;
		private WMA	wma1;
		private WMA wma2;
		private WMA wmaDiffSeries;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= Custom.Resource.NinjaScriptIndicatorDescriptionHMA;
				Name						= Custom.Resource.NinjaScriptIndicatorNameHMA;
				IsSuspendedWhileInactive	= true;
				Period						= 14;
				IsOverlay 					= true;

				AddPlot(Brushes.Goldenrod, Custom.Resource.NinjaScriptIndicatorNameHMA);
			}
			else if (State == State.DataLoaded)
			{
				diffSeries		= new Series<double>(this);
				wma1			= WMA(Inputs[0], Period / 2);
				wma2			= WMA(Inputs[0], Period);
				wmaDiffSeries	= WMA(diffSeries, (int) Math.Sqrt(Period));
			}
		}

		protected override void OnBarUpdate()
		{
			diffSeries[0]	= 2 * wma1[0] - wma2[0];
			Value[0]		= wmaDiffSeries[0];
		}

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
		private HMA[] cacheHMA;
		public HMA HMA(int period)
		{
			return HMA(Input, period);
		}

		public HMA HMA(ISeries<double> input, int period)
		{
			if (cacheHMA != null)
				for (int idx = 0; idx < cacheHMA.Length; idx++)
					if (cacheHMA[idx] != null && cacheHMA[idx].Period == period && cacheHMA[idx].EqualsInput(input))
						return cacheHMA[idx];
			return CacheIndicator<HMA>(new HMA(){ Period = period }, input, ref cacheHMA);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.HMA HMA(int period)
		{
			return indicator.HMA(Input, period);
		}

		public Indicators.HMA HMA(ISeries<double> input , int period)
		{
			return indicator.HMA(input, period);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.HMA HMA(int period)
		{
			return indicator.HMA(Input, period);
		}

		public Indicators.HMA HMA(ISeries<double> input , int period)
		{
			return indicator.HMA(input, period);
		}
	}
}

#endregion
