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
	/// The ROC (Rate-of-Change) indicator displays the percent change between the current price and the price x-time periods ago.
	/// </summary>
	public class ROC : Indicator
	{
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= Custom.Resource.NinjaScriptIndicatorDescriptionROC;
				Name						= Custom.Resource.NinjaScriptIndicatorNameROC;
				IsSuspendedWhileInactive	= true;
				Period						= 14;

				AddLine(Brushes.DarkGray,	0,	Custom.Resource.NinjaScriptIndicatorZeroLine);
				AddPlot(Brushes.DodgerBlue,		Custom.Resource.NinjaScriptIndicatorNameROC);
			}
		}

		protected override void OnBarUpdate()
		{
			double inputPeriod = Input[Math.Min(CurrentBar, Period)];
			
			if (inputPeriod <= 0)
				return;
			
			Value[0] = (Input[0] - inputPeriod) / inputPeriod * 100;
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
		private ROC[] cacheROC;
		public ROC ROC(int period)
		{
			return ROC(Input, period);
		}

		public ROC ROC(ISeries<double> input, int period)
		{
			if (cacheROC != null)
				for (int idx = 0; idx < cacheROC.Length; idx++)
					if (cacheROC[idx] != null && cacheROC[idx].Period == period && cacheROC[idx].EqualsInput(input))
						return cacheROC[idx];
			return CacheIndicator<ROC>(new ROC(){ Period = period }, input, ref cacheROC);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.ROC ROC(int period)
		{
			return indicator.ROC(Input, period);
		}

		public Indicators.ROC ROC(ISeries<double> input , int period)
		{
			return indicator.ROC(input, period);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.ROC ROC(int period)
		{
			return indicator.ROC(Input, period);
		}

		public Indicators.ROC ROC(ISeries<double> input , int period)
		{
			return indicator.ROC(input, period);
		}
	}
}

#endregion
