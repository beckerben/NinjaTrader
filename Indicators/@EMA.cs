//
// Copyright (C) 2025, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
#region Using declarations
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
#endregion

// This namespace holds indicators in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators
{
	/// <summary>
	/// Exponential Moving Average. The Exponential Moving Average is an indicator that
	/// shows the average value of a security's price over a period of time. When calculating
	/// a moving average. The EMA applies more weight to recent prices than the SMA.
	/// </summary>
	public class EMA : Indicator
	{
		private double constant1;
		private double constant2;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= Custom.Resource.NinjaScriptIndicatorDescriptionEMA;
				Name						= Custom.Resource.NinjaScriptIndicatorNameEMA;
				IsOverlay					= true;
				IsSuspendedWhileInactive	= true;
				Period						= 14;

				AddPlot(Brushes.Goldenrod, Custom.Resource.NinjaScriptIndicatorNameEMA);
			}
			else if (State == State.Configure)
			{
				constant1 = 2.0 / (1 + Period);
				constant2 = 1 - 2.0 / (1 + Period);
			}
		}

		protected override void OnBarUpdate() => Value[0] = CurrentBar == 0 ? Input[0] : Input[0] * constant1 + constant2 * Value[1];

		#region Properties
		[Range(1, int.MaxValue), NinjaScriptProperty]
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
		private EMA[] cacheEMA;
		public EMA EMA(int period)
		{
			return EMA(Input, period);
		}

		public EMA EMA(ISeries<double> input, int period)
		{
			if (cacheEMA != null)
				for (int idx = 0; idx < cacheEMA.Length; idx++)
					if (cacheEMA[idx] != null && cacheEMA[idx].Period == period && cacheEMA[idx].EqualsInput(input))
						return cacheEMA[idx];
			return CacheIndicator<EMA>(new EMA(){ Period = period }, input, ref cacheEMA);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.EMA EMA(int period)
		{
			return indicator.EMA(Input, period);
		}

		public Indicators.EMA EMA(ISeries<double> input , int period)
		{
			return indicator.EMA(input, period);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.EMA EMA(int period)
		{
			return indicator.EMA(Input, period);
		}

		public Indicators.EMA EMA(ISeries<double> input , int period)
		{
			return indicator.EMA(input, period);
		}
	}
}

#endregion
