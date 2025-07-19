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
	/// Double Exponential Moving Average
	/// </summary>
	public class DEMA : Indicator
	{
		private EMA ema;
		private EMA emaEma;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= Custom.Resource.NinjaScriptIndicatorDescriptionDEMA;
				Name						= Custom.Resource.NinjaScriptIndicatorNameDEMA;
				IsSuspendedWhileInactive	= true;
				IsOverlay					= true;
				Period						= 14;

				AddPlot(Brushes.Goldenrod, Custom.Resource.NinjaScriptIndicatorNameDEMA);
			}
			else if (State == State.DataLoaded)
			{
				ema		= EMA(Inputs[0], Period);
				emaEma	= EMA(ema, Period);
			}
		}

		protected override void OnBarUpdate() => Value[0] = 2 * ema[0] -  emaEma[0];

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
		private DEMA[] cacheDEMA;
		public DEMA DEMA(int period)
		{
			return DEMA(Input, period);
		}

		public DEMA DEMA(ISeries<double> input, int period)
		{
			if (cacheDEMA != null)
				for (int idx = 0; idx < cacheDEMA.Length; idx++)
					if (cacheDEMA[idx] != null && cacheDEMA[idx].Period == period && cacheDEMA[idx].EqualsInput(input))
						return cacheDEMA[idx];
			return CacheIndicator<DEMA>(new DEMA(){ Period = period }, input, ref cacheDEMA);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.DEMA DEMA(int period)
		{
			return indicator.DEMA(Input, period);
		}

		public Indicators.DEMA DEMA(ISeries<double> input , int period)
		{
			return indicator.DEMA(input, period);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.DEMA DEMA(int period)
		{
			return indicator.DEMA(Input, period);
		}

		public Indicators.DEMA DEMA(ISeries<double> input , int period)
		{
			return indicator.DEMA(input, period);
		}
	}
}

#endregion
