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
	/// R-squared indicator
	/// </summary>
	public class RSquared : Indicator
	{
		private double myPeriod;
		private double priorSumXy;
		private	double priorSumY;
		private double priorSumY2;
		private double sumX;
		private double sumX2;
		private double sumXy;
		private double sumY;
		private double sumY2;
		private double denominator;
		private double r;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= Custom.Resource.NinjaScriptIndicatorDescriptionRSquared;
				Name						= Custom.Resource.NinjaScriptIndicatorNameRSquared;
				IsSuspendedWhileInactive	= true;
				Period						= 8;
				IsOverlay					= false;

				AddPlot(Brushes.Crimson,				Custom.Resource.NinjaScriptIndicatorNameRSquared);
				AddLine(Brushes.SlateBlue,	0.2,	Custom.Resource.NinjaScriptIndicatorLower);
				AddLine(Brushes.Goldenrod,	0.75,	Custom.Resource.NinjaScriptIndicatorUpper);
			}
			else if (State == State.Configure)
				priorSumXy = priorSumY = priorSumY2 = sumX = sumXy = sumX2 = sumY2 = denominator = 0;
		}

		protected override void OnBarUpdate()
		{
			if (BarsArray[0].BarsType.IsRemoveLastBarSupported)
			{
				sumX	= (double)Period * (Period - 1) * 0.5;
				sumXy	= 0;
				sumX2	= 0;
				sumY2	= 0;

				for (int count = 0; count < Period && CurrentBar - count >= 0; count++)
				{
					sumXy += count * Input[count];
					sumX2 += count * count;
					sumY2 += Input[count] * Input[count];
				}

				double numerator	= Period * sumXy - sumX * SUM(Inputs[0], Period)[0];
				denominator			= (Period * sumX2 - sumX * sumX) * (Period * sumY2 - SUM(Inputs[0], Period)[0] * SUM(Inputs[0], Period)[0]);

				r					= denominator > 0 ? Math.Pow(numerator / Math.Sqrt(denominator), 2) : 0;
				Value[0]			= r;
			}
			else
			{
				if (IsFirstTickOfBar)
				{
					priorSumXy	= sumXy;
					priorSumY	= sumY;
					priorSumY2	= sumY2;
					myPeriod	= Math.Min(CurrentBar + 1, Period);
					sumX		= myPeriod * (myPeriod + 1) * 0.5;
					sumX2		= sumX * (2 * myPeriod + 1) / 3;
				}

				double input0		= Input[0];
				double inputPeriod	= Input[Math.Min(Period, CurrentBar)];

				sumXy				= priorSumXy - (CurrentBar >= Period ? priorSumY : 0) + myPeriod * input0;
				sumY				= priorSumY + input0 - (CurrentBar >= Period ? inputPeriod : 0);
				sumY2				= priorSumY2 + input0 * input0 - (CurrentBar >= Period ? inputPeriod * inputPeriod : 0);
				denominator			= (myPeriod * sumX2 - sumX * sumX) * (myPeriod * sumY2 - sumY * sumY);
				r					= denominator > 0 ? (myPeriod * sumXy - sumX * sumY) / Math.Sqrt(denominator) : 0;
				Value[0] = r * r;
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
		private RSquared[] cacheRSquared;
		public RSquared RSquared(int period)
		{
			return RSquared(Input, period);
		}

		public RSquared RSquared(ISeries<double> input, int period)
		{
			if (cacheRSquared != null)
				for (int idx = 0; idx < cacheRSquared.Length; idx++)
					if (cacheRSquared[idx] != null && cacheRSquared[idx].Period == period && cacheRSquared[idx].EqualsInput(input))
						return cacheRSquared[idx];
			return CacheIndicator<RSquared>(new RSquared(){ Period = period }, input, ref cacheRSquared);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.RSquared RSquared(int period)
		{
			return indicator.RSquared(Input, period);
		}

		public Indicators.RSquared RSquared(ISeries<double> input , int period)
		{
			return indicator.RSquared(input, period);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.RSquared RSquared(int period)
		{
			return indicator.RSquared(Input, period);
		}

		public Indicators.RSquared RSquared(ISeries<double> input , int period)
		{
			return indicator.RSquared(input, period);
		}
	}
}

#endregion
