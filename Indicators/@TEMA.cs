//
// Copyright (C) 2025, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
#region Using declarations
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
#endregion

//This namespace holds indicators in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators
{
	/// <summary>
	/// Triple Exponential Moving Average
	/// </summary>
	public class TEMA : Indicator
	{
		private EMA ema1;
		private EMA ema2;
		private EMA ema3;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= Custom.Resource.NinjaScriptIndicatorDescriptionTEMA;
				Name						= Custom.Resource.NinjaScriptIndicatorNameTEMA;
				IsSuspendedWhileInactive	= true;
				IsOverlay					= true;
				Period						= 14;

				AddPlot(Brushes.Goldenrod, Custom.Resource.NinjaScriptIndicatorNameTEMA);
			}
			else if (State == State.DataLoaded)
			{
				ema1 = EMA(Inputs[0], Period);
				ema2 = EMA(ema1, Period);
				ema3 = EMA(ema2, Period);
			}
		}

		protected override void OnBarUpdate() => Value[0] = 3 * ema1[0] - 3 * ema2[0] + ema3[0];

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
		private TEMA[] cacheTEMA;
		public TEMA TEMA(int period)
		{
			return TEMA(Input, period);
		}

		public TEMA TEMA(ISeries<double> input, int period)
		{
			if (cacheTEMA != null)
				for (int idx = 0; idx < cacheTEMA.Length; idx++)
					if (cacheTEMA[idx] != null && cacheTEMA[idx].Period == period && cacheTEMA[idx].EqualsInput(input))
						return cacheTEMA[idx];
			return CacheIndicator<TEMA>(new TEMA(){ Period = period }, input, ref cacheTEMA);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.TEMA TEMA(int period)
		{
			return indicator.TEMA(Input, period);
		}

		public Indicators.TEMA TEMA(ISeries<double> input , int period)
		{
			return indicator.TEMA(input, period);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.TEMA TEMA(int period)
		{
			return indicator.TEMA(Input, period);
		}

		public Indicators.TEMA TEMA(ISeries<double> input , int period)
		{
			return indicator.TEMA(input, period);
		}
	}
}

#endregion
