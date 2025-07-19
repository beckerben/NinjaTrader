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
	/// The VMA (Variable Moving Average, also known as VIDYA or Variable Index Dynamic Average)
	///  is an exponential moving average that automatically adjusts the smoothing weight based
	/// on the volatility of the data series. VMA solves a problem with most moving averages.
	/// In times of low volatility, such as when the price is trending, the moving average time
	///  period should be shorter to be sensitive to the inevitable break in the trend. Whereas,
	/// in more volatile non-trending times, the moving average time period should be longer to
	/// filter out the choppiness. VIDYA uses the CMO indicator for it's internal volatility calculations.
	/// Both the VMA and the CMO period are adjustable.
	/// </summary>
	public class VMA : Indicator
	{
		private CMO		cmo;
		private double	sc;	//Smoothing Constant

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= Custom.Resource.NinjaScriptIndicatorDescriptionVMA;
				Name						= Custom.Resource.NinjaScriptIndicatorNameVMA;
				IsSuspendedWhileInactive	= true;
				IsOverlay					= true;
				Period						= 9;
				VolatilityPeriod			= 9;

				AddPlot(Brushes.DodgerBlue, Custom.Resource.NinjaScriptIndicatorNameVMA);
			}
			else if (State == State.Configure)
				sc = 2 / (double)(Period + 1);
			else if (State == State.DataLoaded)
				cmo = CMO(Inputs[0], VolatilityPeriod);
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar == 0)
			{
				Value[0] = Input[0];
				return;
			}

			// Volatility Index
			double vi	= Math.Abs(cmo[0]) / 100;
			Value[0]	= sc * vi * Input[0] + (1 - sc * vi) * Value[1];
		}

		#region Properties
		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Period", GroupName = "NinjaScriptParameters", Order = 0)]
		public int Period { get; set; }

		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "VolatilityPeriod", GroupName = "NinjaScriptParameters", Order = 1)]
		public int VolatilityPeriod { get; set; }
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private VMA[] cacheVMA;
		public VMA VMA(int period, int volatilityPeriod)
		{
			return VMA(Input, period, volatilityPeriod);
		}

		public VMA VMA(ISeries<double> input, int period, int volatilityPeriod)
		{
			if (cacheVMA != null)
				for (int idx = 0; idx < cacheVMA.Length; idx++)
					if (cacheVMA[idx] != null && cacheVMA[idx].Period == period && cacheVMA[idx].VolatilityPeriod == volatilityPeriod && cacheVMA[idx].EqualsInput(input))
						return cacheVMA[idx];
			return CacheIndicator<VMA>(new VMA(){ Period = period, VolatilityPeriod = volatilityPeriod }, input, ref cacheVMA);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.VMA VMA(int period, int volatilityPeriod)
		{
			return indicator.VMA(Input, period, volatilityPeriod);
		}

		public Indicators.VMA VMA(ISeries<double> input , int period, int volatilityPeriod)
		{
			return indicator.VMA(input, period, volatilityPeriod);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.VMA VMA(int period, int volatilityPeriod)
		{
			return indicator.VMA(Input, period, volatilityPeriod);
		}

		public Indicators.VMA VMA(ISeries<double> input , int period, int volatilityPeriod)
		{
			return indicator.VMA(input, period, volatilityPeriod);
		}
	}
}

#endregion
