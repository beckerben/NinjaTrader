//
// Copyright (C) 2025, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
#region Using declarations
using System;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
#endregion

// This namespace holds indicators in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators
{
	/// <summary>
	/// The Momentum indicator measures the amount that a security's price has changed over a given time span.
	/// </summary>
	public class Momentum : Indicator
	{
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= Custom.Resource.NinjaScriptIndicatorDescriptionMomentum;
				Name						= Custom.Resource.NinjaScriptIndicatorNameMomentum;
				IsSuspendedWhileInactive	= true;
				Period						= 14;

				AddPlot(Brushes.DarkCyan,		Custom.Resource.NinjaScriptIndicatorNameMomentum);
				AddLine(Brushes.SlateBlue,	0,	Custom.Resource.NinjaScriptIndicatorZeroLine);
			}
		}

		protected override void OnBarUpdate() => Value[0] = CurrentBar == 0 ? 0 : Input[0] - Input[Math.Min(CurrentBar, Period)];

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
		private Momentum[] cacheMomentum;
		public Momentum Momentum(int period)
		{
			return Momentum(Input, period);
		}

		public Momentum Momentum(ISeries<double> input, int period)
		{
			if (cacheMomentum != null)
				for (int idx = 0; idx < cacheMomentum.Length; idx++)
					if (cacheMomentum[idx] != null && cacheMomentum[idx].Period == period && cacheMomentum[idx].EqualsInput(input))
						return cacheMomentum[idx];
			return CacheIndicator<Momentum>(new Momentum(){ Period = period }, input, ref cacheMomentum);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.Momentum Momentum(int period)
		{
			return indicator.Momentum(Input, period);
		}

		public Indicators.Momentum Momentum(ISeries<double> input , int period)
		{
			return indicator.Momentum(input, period);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.Momentum Momentum(int period)
		{
			return indicator.Momentum(Input, period);
		}

		public Indicators.Momentum Momentum(ISeries<double> input , int period)
		{
			return indicator.Momentum(input, period);
		}
	}
}

#endregion
