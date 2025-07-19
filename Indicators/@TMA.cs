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
	/// The TMA (Triangular Moving Average) is a weighted moving average. Compared to the WMA which puts more weight on the latest price bar, the TMA puts more weight on the data in the middle of the specified period.
	/// </summary>
	public class TMA : Indicator
	{
		private int p1;
		private int p2;
		private SMA sma;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= Custom.Resource.NinjaScriptIndicatorDescriptionTMA;
				Name						= Custom.Resource.NinjaScriptIndicatorNameTMA;
				IsOverlay					= true;
				IsSuspendedWhileInactive	= true;
				Period						= 15;

				AddPlot(Brushes.DodgerBlue, Custom.Resource.NinjaScriptIndicatorNameTMA);
			}
			else if (State == State.Configure)
			{
				p1 = 0;
				p2 = 0;
				if ((Period & 1) == 0)
				{
					// Even period
					p1 = Period / 2;
					p2 = p1 + 1;
				}
				else
				{
					// Odd period
					p1 = (Period + 1) / 2;
					p2 = p1;
				}
			}
			else if (State == State.DataLoaded)
				sma = SMA(SMA(Inputs[0], p1), p2);
		}

		protected override void OnBarUpdate() => Value[0] = sma[0];

		#region Properties

		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof (Custom.Resource), Name = "Period", GroupName = "NinjaScriptParameters", Order = 0)]
		public int Period { get; set; }

		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private TMA[] cacheTMA;
		public TMA TMA(int period)
		{
			return TMA(Input, period);
		}

		public TMA TMA(ISeries<double> input, int period)
		{
			if (cacheTMA != null)
				for (int idx = 0; idx < cacheTMA.Length; idx++)
					if (cacheTMA[idx] != null && cacheTMA[idx].Period == period && cacheTMA[idx].EqualsInput(input))
						return cacheTMA[idx];
			return CacheIndicator<TMA>(new TMA(){ Period = period }, input, ref cacheTMA);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.TMA TMA(int period)
		{
			return indicator.TMA(Input, period);
		}

		public Indicators.TMA TMA(ISeries<double> input , int period)
		{
			return indicator.TMA(input, period);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.TMA TMA(int period)
		{
			return indicator.TMA(Input, period);
		}

		public Indicators.TMA TMA(ISeries<double> input , int period)
		{
			return indicator.TMA(input, period);
		}
	}
}

#endregion
