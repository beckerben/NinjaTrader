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
	/// The Price Oscillator indicator shows the variation among two moving averages for the price of a security.
	/// </summary>
	public class PriceOscillator : Indicator
	{
		private	EMA					emaFast;
		private EMA					emaSlow;
		private EMA					emaSmooth;
		private	Series<double>		smoothEma;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= Custom.Resource.NinjaScriptIndicatorDescriptionPriceOscillator;
				Name						= Custom.Resource.NinjaScriptIndicatorNamePriceOscillator;
				Fast						= 12;
				IsSuspendedWhileInactive	= true;
				Slow						= 26;
				Smooth						= 9;

				AddLine(Brushes.DarkGray,	0,	Custom.Resource.NinjaScriptIndicatorZeroLine);
				AddPlot(Brushes.Goldenrod,	Custom.Resource.NinjaScriptIndicatorNamePriceOscillator);
			}
			else if (State == State.DataLoaded)
			{
				smoothEma	= new Series<double>(this);
				emaFast		= EMA(Fast);
				emaSlow		= EMA(Slow);
				emaSmooth	= EMA(smoothEma, Smooth);
			}
		}

		protected override void OnBarUpdate()
		{
			smoothEma[0]	= emaFast[0] - emaSlow[0];
			Value[0]		= emaSmooth[0];
		}

		#region Properties
		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Fast", GroupName = "NinjaScriptParameters", Order = 0)]
		public int Fast { get; set; }

		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Slow", GroupName = "NinjaScriptParameters", Order = 1)]
		public int Slow { get; set; }

		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Smooth", GroupName = "NinjaScriptParameters", Order = 2)]
		public int Smooth { get; set; }
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private PriceOscillator[] cachePriceOscillator;
		public PriceOscillator PriceOscillator(int fast, int slow, int smooth)
		{
			return PriceOscillator(Input, fast, slow, smooth);
		}

		public PriceOscillator PriceOscillator(ISeries<double> input, int fast, int slow, int smooth)
		{
			if (cachePriceOscillator != null)
				for (int idx = 0; idx < cachePriceOscillator.Length; idx++)
					if (cachePriceOscillator[idx] != null && cachePriceOscillator[idx].Fast == fast && cachePriceOscillator[idx].Slow == slow && cachePriceOscillator[idx].Smooth == smooth && cachePriceOscillator[idx].EqualsInput(input))
						return cachePriceOscillator[idx];
			return CacheIndicator<PriceOscillator>(new PriceOscillator(){ Fast = fast, Slow = slow, Smooth = smooth }, input, ref cachePriceOscillator);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.PriceOscillator PriceOscillator(int fast, int slow, int smooth)
		{
			return indicator.PriceOscillator(Input, fast, slow, smooth);
		}

		public Indicators.PriceOscillator PriceOscillator(ISeries<double> input , int fast, int slow, int smooth)
		{
			return indicator.PriceOscillator(input, fast, slow, smooth);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.PriceOscillator PriceOscillator(int fast, int slow, int smooth)
		{
			return indicator.PriceOscillator(Input, fast, slow, smooth);
		}

		public Indicators.PriceOscillator PriceOscillator(ISeries<double> input , int fast, int slow, int smooth)
		{
			return indicator.PriceOscillator(input, fast, slow, smooth);
		}
	}
}

#endregion
