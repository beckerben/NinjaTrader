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
	/// Linear Regression Slope
	/// </summary>
	public class LinRegSlope : Indicator
	{
		private double	avg;
		private double	divisor;
		private double	myPeriod;
		private double	priorSumXy;
		private	double	priorSumY;
		private double	sumX2;
		private double	sumXy;
		private double	sumY;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= Custom.Resource.NinjaScriptIndicatorDescriptionLinRegSlope;
				Name						= Custom.Resource.NinjaScriptIndicatorNameLinRegSlope;
				IsSuspendedWhileInactive	= true;
				Period						= 14;

				AddPlot(Brushes.Goldenrod, Custom.Resource.NinjaScriptIndicatorNameLinRegSlope);
			}
			else if (State == State.Configure)
				avg	= divisor = myPeriod = priorSumXy = priorSumY = sumX2 = sumY = sumXy = 0;
		}

		protected override void OnBarUpdate()
		{
			if (BarsArray[0].BarsType.IsRemoveLastBarSupported)
			{
				double sumX		= (double)Period * (Period - 1) * 0.5;
				double d		= sumX * sumX - (double)Period * Period * (Period - 1) * (2 * Period - 1) / 6;
				double xy		= 0;

				for (int count = 0; count < Period && CurrentBar - count >= 0; count++)
					xy += count * Input[count];

				Value[0] = (Period * xy - sumX * SUM(Inputs[0], Period)[0]) / d;
			}
			else
			{
				if (IsFirstTickOfBar)
				{
					priorSumY	= sumY;
					priorSumXy	= sumXy;
					myPeriod	= Math.Min(CurrentBar + 1, Period);
					sumX2		= myPeriod * (myPeriod + 1) * 0.5;
					divisor		= myPeriod * (myPeriod + 1) * (2 * myPeriod + 1) / 6 - sumX2 * sumX2 / myPeriod;
				}

				double input0 = Input[0];
				sumXy = priorSumXy - (CurrentBar >= Period ? priorSumY : 0) + myPeriod * input0;
				sumY = priorSumY + input0 - (CurrentBar >= Period ? Input[Period] : 0);
				avg = sumY / myPeriod;
				Value[0] = CurrentBar <= Period ? 0 : (sumXy - sumX2 * avg) / divisor;
			}
		}

		#region Properties
		[Range(2, int.MaxValue), NinjaScriptProperty]
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
		private LinRegSlope[] cacheLinRegSlope;
		public LinRegSlope LinRegSlope(int period)
		{
			return LinRegSlope(Input, period);
		}

		public LinRegSlope LinRegSlope(ISeries<double> input, int period)
		{
			if (cacheLinRegSlope != null)
				for (int idx = 0; idx < cacheLinRegSlope.Length; idx++)
					if (cacheLinRegSlope[idx] != null && cacheLinRegSlope[idx].Period == period && cacheLinRegSlope[idx].EqualsInput(input))
						return cacheLinRegSlope[idx];
			return CacheIndicator<LinRegSlope>(new LinRegSlope(){ Period = period }, input, ref cacheLinRegSlope);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.LinRegSlope LinRegSlope(int period)
		{
			return indicator.LinRegSlope(Input, period);
		}

		public Indicators.LinRegSlope LinRegSlope(ISeries<double> input , int period)
		{
			return indicator.LinRegSlope(input, period);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.LinRegSlope LinRegSlope(int period)
		{
			return indicator.LinRegSlope(Input, period);
		}

		public Indicators.LinRegSlope LinRegSlope(ISeries<double> input , int period)
		{
			return indicator.LinRegSlope(input, period);
		}
	}
}

#endregion
