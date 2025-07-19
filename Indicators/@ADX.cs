//
// Copyright (C) 2025, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
#region Using declarations
using System;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Core.FloatingPoint;
#endregion

// This namespace holds indicators in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators
{
	/// <summary>
	/// The Average Directional Index measures the strength of a prevailing trend as well as whether movement
	/// exists in the market. The ADX is measured on a scale of 0  100. A low ADX value (generally less than 20)
	/// can indicate a non-trending market with low volumes whereas a cross above 20 may indicate the start of
	///  a trend (either up or down). If the ADX is over 40 and begins to fall, it can indicate the slowdown of a current trend.
	/// </summary>
	public class ADX : Indicator
	{
		private Series<double>		dmPlus;
		private Series<double>		dmMinus;
		private Series<double>		sumDmPlus;
		private Series<double>		sumDmMinus;
		private Series<double>		sumTr;
		private Series<double>		tr;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= Custom.Resource.NinjaScriptIndicatorDescriptionADX;
				Name						= Custom.Resource.NinjaScriptIndicatorNameADX;
				IsSuspendedWhileInactive	= true;
				Period						= 14;

				AddPlot(Brushes.DarkCyan,		Custom.Resource.NinjaScriptIndicatorNameADX);
				AddLine(Brushes.SlateBlue,	25,	Custom.Resource.NinjaScriptIndicatorLower);
				AddLine(Brushes.Goldenrod,	75,	Custom.Resource.NinjaScriptIndicatorUpper);
			}
			else if (State == State.DataLoaded)
			{
				dmPlus		= new Series<double>(this);
				dmMinus		= new Series<double>(this);
				sumDmPlus	= new Series<double>(this);
				sumDmMinus	= new Series<double>(this);
				sumTr		= new Series<double>(this);
				tr			= new Series<double>(this);
			}
		}

		protected override void OnBarUpdate()
		{
			double high0	= High[0];
			double low0		= Low[0];

			if (CurrentBar == 0)
			{
				tr[0]				= high0 - low0;
				dmPlus[0]			= 0;
				dmMinus[0]			= 0;
				sumTr[0]			= tr[0];
				sumDmPlus[0]		= dmPlus[0];
				sumDmMinus[0]		= dmMinus[0];
				Value[0]			= 50;
			}
			else
			{
				double high1		= High[1];
				double low1			= Low[1];
				double close1		= Close[1];

				tr[0]				= Math.Max(Math.Abs(low0 - close1), Math.Max(high0 - low0, Math.Abs(high0 - close1)));
				dmPlus[0]			= high0 - high1 > low1 - low0 ? Math.Max(high0 - high1, 0) : 0;
				dmMinus[0]			= low1 - low0 > high0 - high1 ? Math.Max(low1 - low0, 0) : 0;

				if (CurrentBar < Period)
				{
					sumTr[0]		= sumTr[1] + tr[0];
					sumDmPlus[0]	= sumDmPlus[1] + dmPlus[0];
					sumDmMinus[0]	= sumDmMinus[1] + dmMinus[0];
				}
				else
				{
					double sumTr1		= sumTr[1];
					double sumDmPlus1	= sumDmPlus[1];
					double sumDmMinus1	= sumDmMinus[1];

					sumTr[0]			= sumTr1 - sumTr1 / Period + tr[0];
					sumDmPlus[0]		= sumDmPlus1 - sumDmPlus1 / Period + dmPlus[0];
					sumDmMinus[0]		= sumDmMinus1 - sumDmMinus1 / Period + dmMinus[0];
				}

				double sumTr0		= sumTr[0];
				double diPlus		= 100 * (sumTr0.ApproxCompare(0) == 0 ? 0 : sumDmPlus[0] / sumTr[0]);
				double diMinus		= 100 * (sumTr0.ApproxCompare(0) == 0 ? 0 : sumDmMinus[0] / sumTr[0]);
				double diff			= Math.Abs(diPlus - diMinus);
				double sum			= diPlus + diMinus;

				Value[0]			= sum.ApproxCompare(0) == 0 ? 50 : ((Period - 1) * Value[1] + 100 * diff / sum) / Period;
			}
		}

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
		private ADX[] cacheADX;
		public ADX ADX(int period)
		{
			return ADX(Input, period);
		}

		public ADX ADX(ISeries<double> input, int period)
		{
			if (cacheADX != null)
				for (int idx = 0; idx < cacheADX.Length; idx++)
					if (cacheADX[idx] != null && cacheADX[idx].Period == period && cacheADX[idx].EqualsInput(input))
						return cacheADX[idx];
			return CacheIndicator<ADX>(new ADX(){ Period = period }, input, ref cacheADX);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.ADX ADX(int period)
		{
			return indicator.ADX(Input, period);
		}

		public Indicators.ADX ADX(ISeries<double> input , int period)
		{
			return indicator.ADX(input, period);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.ADX ADX(int period)
		{
			return indicator.ADX(Input, period);
		}

		public Indicators.ADX ADX(ISeries<double> input , int period)
		{
			return indicator.ADX(input, period);
		}
	}
}

#endregion
