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
	/// Average Directional Movement Rating quantifies momentum change in the ADX.
	/// It is calculated by adding two values of ADX (the current value and a value n periods back),
	/// then dividing by two. This additional smoothing makes the ADXR slightly less responsive than ADX.
	/// The interpretation is the same as the ADX; the higher the value, the stronger the trend.
	/// </summary>
	public class ADXR : Indicator
	{
		private ADX adx;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= Custom.Resource.NinjaScriptIndicatorDescriptionADXR;
				Name						= Custom.Resource.NinjaScriptIndicatorNameADXR;
				IsSuspendedWhileInactive	= true;
				Period						= 14;
				Interval					= 10;

				AddPlot(Brushes.DarkCyan,		Custom.Resource.NinjaScriptIndicatorNameADXR);
				AddLine(Brushes.SlateBlue,	25,	Custom.Resource.NinjaScriptIndicatorLower);
				AddLine(Brushes.Goldenrod,	75,	Custom.Resource.NinjaScriptIndicatorUpper);
			}
			else if (State == State.DataLoaded)
				adx = ADX(Period);
		}

		protected override void OnBarUpdate() => Value[0] = CurrentBar < Interval ? (adx[0] + adx[CurrentBar]) / 2 : (adx[0] + adx[Interval]) / 2;

		#region Properties
		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Interval", GroupName = "NinjaScriptParameters", Order = 0)]
		public int Interval { get; set; }

		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Period", GroupName = "NinjaScriptParameters", Order = 1)]
		public int Period { get; set; }
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private ADXR[] cacheADXR;
		public ADXR ADXR(int interval, int period)
		{
			return ADXR(Input, interval, period);
		}

		public ADXR ADXR(ISeries<double> input, int interval, int period)
		{
			if (cacheADXR != null)
				for (int idx = 0; idx < cacheADXR.Length; idx++)
					if (cacheADXR[idx] != null && cacheADXR[idx].Interval == interval && cacheADXR[idx].Period == period && cacheADXR[idx].EqualsInput(input))
						return cacheADXR[idx];
			return CacheIndicator<ADXR>(new ADXR(){ Interval = interval, Period = period }, input, ref cacheADXR);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.ADXR ADXR(int interval, int period)
		{
			return indicator.ADXR(Input, interval, period);
		}

		public Indicators.ADXR ADXR(ISeries<double> input , int interval, int period)
		{
			return indicator.ADXR(input, interval, period);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.ADXR ADXR(int interval, int period)
		{
			return indicator.ADXR(Input, interval, period);
		}

		public Indicators.ADXR ADXR(ISeries<double> input , int interval, int period)
		{
			return indicator.ADXR(input, interval, period);
		}
	}
}

#endregion
