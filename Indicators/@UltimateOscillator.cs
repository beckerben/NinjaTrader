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
	/// The Ultimate Oscillator is the weighted sum of three oscillators of different time periods.
	/// The typical time periods are 7, 14 and 28. The values of the Ultimate Oscillator range
	/// from zero to 100. Values over 70 indicate overbought conditions, and values under 30 indicate
	/// oversold conditions. Also look for agreement/divergence with the price to confirm a trend or signal the end of a trend.
	/// </summary>
	public class UltimateOscillator : Indicator
	{
		private Series<double>	buyingPressure;
		private double			constant1;
		private double			constant2;
		private double			constant3;
		private SUM				sumBpFast;
		private SUM				sumBpIntermediate;
		private SUM				sumBpSlow;
		private SUM				sumTrFast;
		private SUM				sumTrIntermediate;
		private SUM				sumTrSlow;
		private Series<double>	trueRange;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= Custom.Resource.NinjaScriptIndicatorDescriptionUltimateOscillator;
				Name						= Custom.Resource.NinjaScriptIndicatorNameUltimateOscillator;
				IsSuspendedWhileInactive	= true;
				Fast						= 7;
				Intermediate				= 14;
				Slow						= 28;

				AddPlot(Brushes.DodgerBlue,		Custom.Resource.NinjaScriptIndicatorNameUltimateOscillator);

				AddLine(Brushes.DarkGray,	30,	Custom.Resource.NinjaScriptIndicatorOversold);
				AddLine(Brushes.DarkGray,	50,	Custom.Resource.NinjaScriptIndicatorNeutral);
				AddLine(Brushes.DarkGray,	70,	Custom.Resource.NinjaScriptIndicatorOverbought);
			}
			else if (State == State.Configure)
			{
				constant1			= Slow / Fast ;
				constant2			= Slow / Intermediate;
				constant3			= constant1 + constant2 + 1;
			}
			else if (State == State.DataLoaded)
			{
				buyingPressure		= new Series<double>(this);
				trueRange			= new Series<double>(this);
				sumBpFast			= SUM(buyingPressure, Fast);
				sumBpIntermediate	= SUM(buyingPressure, Intermediate);
				sumBpSlow			= SUM(buyingPressure, Slow);
				sumTrFast			= SUM(trueRange, Fast);
				sumTrIntermediate	= SUM(trueRange, Intermediate);
				sumTrSlow			= SUM(trueRange, Slow);
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar == 0)
				Value[0] =  0;

			else
			{
				double high0	= High[0];
				double low0		= Low[0];
				double close0	= Close[0];
				double close1	= Close[1];

				buyingPressure[0] 	= close0 - Math.Min(low0, close1);
				trueRange[0] 		= Math.Max(Math.Max(high0 - low0, high0 - close1), close1 - low0);

				// Use previous value if we get into trouble
				if (sumTrFast[0] == 0 || sumTrIntermediate[0] == 0 || sumTrSlow[0] == 0)
				{
					Value[0] = Value[1];
					return;
				}

				Value[0] = (sumBpFast[0] / sumTrFast[0] * constant1 + sumBpIntermediate[0] / sumTrIntermediate[0] * constant2 + sumBpSlow[0] / sumTrSlow[0])
					/ constant3 * 100;
			}
		}

		#region Properties
		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Fast", GroupName = "NinjaScriptParameters", Order = 0)]
		public int Fast { get; set; }

		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Intermediate", GroupName = "NinjaScriptParameters", Order = 1)]
		public int Intermediate { get; set; }

		[Range(1, int.MaxValue), NinjaScriptProperty]
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
		private UltimateOscillator[] cacheUltimateOscillator;
		public UltimateOscillator UltimateOscillator(int fast, int intermediate, int slow)
		{
			return UltimateOscillator(Input, fast, intermediate, slow);
		}

		public UltimateOscillator UltimateOscillator(ISeries<double> input, int fast, int intermediate, int slow)
		{
			if (cacheUltimateOscillator != null)
				for (int idx = 0; idx < cacheUltimateOscillator.Length; idx++)
					if (cacheUltimateOscillator[idx] != null && cacheUltimateOscillator[idx].Fast == fast && cacheUltimateOscillator[idx].Intermediate == intermediate && cacheUltimateOscillator[idx].Slow == slow && cacheUltimateOscillator[idx].EqualsInput(input))
						return cacheUltimateOscillator[idx];
			return CacheIndicator<UltimateOscillator>(new UltimateOscillator(){ Fast = fast, Intermediate = intermediate, Slow = slow }, input, ref cacheUltimateOscillator);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.UltimateOscillator UltimateOscillator(int fast, int intermediate, int slow)
		{
			return indicator.UltimateOscillator(Input, fast, intermediate, slow);
		}

		public Indicators.UltimateOscillator UltimateOscillator(ISeries<double> input , int fast, int intermediate, int slow)
		{
			return indicator.UltimateOscillator(input, fast, intermediate, slow);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.UltimateOscillator UltimateOscillator(int fast, int intermediate, int slow)
		{
			return indicator.UltimateOscillator(Input, fast, intermediate, slow);
		}

		public Indicators.UltimateOscillator UltimateOscillator(ISeries<double> input , int fast, int intermediate, int slow)
		{
			return indicator.UltimateOscillator(input, fast, intermediate, slow);
		}
	}
}

#endregion
