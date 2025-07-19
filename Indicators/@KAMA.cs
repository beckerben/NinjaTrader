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
	/// Kaufman's Adaptive Moving Average. Developed by Perry Kaufman, this indicator is an
	/// EMA using an Efficiency Ratio to modify the smoothing constant, which ranges from
	/// a minimum of Fast Length to a maximum of Slow Length. Since this moving average is
	/// adaptive it tends to follow prices more closely than other MA's.
	/// </summary>
	public class KAMA : Indicator
	{
		private Series<double>	diffSeries;
		private double			fastCf;
		private double			slowCf;
		private SUM				sum;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= Custom.Resource.NinjaScriptIndicatorDescriptionKAMA;
				Name						= Custom.Resource.NinjaScriptIndicatorNameKAMA;
				Fast						= 2;
				IsSuspendedWhileInactive	= true;
				IsOverlay					= true;
				Period						= 10;
				Slow						= 30;

				AddPlot(Brushes.DodgerBlue, Custom.Resource.NinjaScriptIndicatorNameKAMA);
			}
			else if (State == State.Configure)
			{
				fastCf		= 2.0 / (Fast + 1);
				slowCf		= 2.0 / (Slow + 1);
			}
			else if (State == State.DataLoaded)
			{
				diffSeries = new Series<double>(this);
				sum = SUM(diffSeries, Period);
			}
		}

		protected override void OnBarUpdate()
		{
			double input0 = Input[0];
			diffSeries[0] = CurrentBar > 0 ? Math.Abs(input0 - Input[1]) : input0;

			if (CurrentBar < Period)
			{
				Value[0] = Input[0];
				return;
			}

			double signal = Math.Abs(input0 - Input[Period]);
			double noise  = sum[0];

			// Prevent div by zero
			if (noise == 0)
			{
				Value[0] = Value[1];
				return;
			}

			double value1   = Value[1];
			Value[0]		= value1 + Math.Pow(signal / noise * (fastCf - slowCf) + slowCf, 2) * (input0 - value1);
		}

		#region Properties
		[Range(1, 125), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Fast", GroupName = "NinjaScriptParameters", Order = 0)]
		public int Fast { get; set; }

		[Range(5, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Period", GroupName = "NinjaScriptParameters", Order = 1)]
		public int Period { get; set; }

		[Range(1, 125), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Slow", GroupName = "NinjaScriptParameters", Order = 2)]
		public int Slow { get; set; }
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private KAMA[] cacheKAMA;
		public KAMA KAMA(int fast, int period, int slow)
		{
			return KAMA(Input, fast, period, slow);
		}

		public KAMA KAMA(ISeries<double> input, int fast, int period, int slow)
		{
			if (cacheKAMA != null)
				for (int idx = 0; idx < cacheKAMA.Length; idx++)
					if (cacheKAMA[idx] != null && cacheKAMA[idx].Fast == fast && cacheKAMA[idx].Period == period && cacheKAMA[idx].Slow == slow && cacheKAMA[idx].EqualsInput(input))
						return cacheKAMA[idx];
			return CacheIndicator<KAMA>(new KAMA(){ Fast = fast, Period = period, Slow = slow }, input, ref cacheKAMA);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.KAMA KAMA(int fast, int period, int slow)
		{
			return indicator.KAMA(Input, fast, period, slow);
		}

		public Indicators.KAMA KAMA(ISeries<double> input , int fast, int period, int slow)
		{
			return indicator.KAMA(input, fast, period, slow);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.KAMA KAMA(int fast, int period, int slow)
		{
			return indicator.KAMA(Input, fast, period, slow);
		}

		public Indicators.KAMA KAMA(ISeries<double> input , int fast, int period, int slow)
		{
			return indicator.KAMA(input, fast, period, slow);
		}
	}
}

#endregion
