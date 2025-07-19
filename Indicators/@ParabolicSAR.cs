//
// Copyright (C) 2025, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
#region Using declarations
using System;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Gui;
#endregion

//This namespace holds indicators in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators
{
	/// <summary>
	/// Parabolic SAR according to Stocks and Commodities magazine V 11:11 (477-479).
	/// </summary>
	public class ParabolicSAR : Indicator
	{
		private double			af;				// Acceleration factor
		private bool			afIncreased;
		private bool			longPosition;
		private int				prevBar;
		private double			prevSar;
		private int				reverseBar;
		private double			reverseValue;
		private double			todaySar;		// SAR value
		private double			xp;				// Extreme Price

		private Series<double>	afSeries;
		private Series<bool>	afIncreasedSeries;
		private Series<bool>	longPositionSeries;
		private Series<int>		prevBarSeries;
		private Series<double>	prevSarSeries;
		private Series<int>		reverseBarSeries;
		private Series<double>	reverseValueSeries;
		private Series<double>	todaySarSeries;
		private Series<double>	xpSeries;

		private ISeries<double>	high;
		private ISeries<double>	low;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= Custom.Resource.NinjaScriptIndicatorDescriptionParabolicSAR;
				Name						= Custom.Resource.NinjaScriptIndicatorNameParabolicSAR;
				Acceleration				= 0.02;
				AccelerationStep			= 0.02;
				AccelerationMax				= 0.2;
				Calculate 					= Calculate.OnPriceChange;
				IsSuspendedWhileInactive	= true;
				IsOverlay					= true;

				AddPlot(new Stroke(Brushes.Goldenrod, 2), PlotStyle.Dot, Custom.Resource.NinjaScriptIndicatorNameParabolicSAR);
			}

			else if (State == State.Configure)
			{
				xp				= 0.0;
				af				= 0;
				todaySar		= 0;
				prevSar			= 0;
				reverseBar		= 0;
				reverseValue	= 0;
				prevBar			= 0;
				afIncreased		= false;
			}
			else if (State == State.DataLoaded)
			{
				if (BarsArray[0].BarsType.IsRemoveLastBarSupported)
				{
					afSeries			= new Series<double>(this);
					afIncreasedSeries	= new Series<bool>(this);
					longPositionSeries	= new Series<bool>(this);
					prevBarSeries		= new Series<int>(this);
					prevSarSeries		= new Series<double>(this);
					reverseBarSeries	= new Series<int>(this);
					reverseValueSeries	= new Series<double>(this);
					todaySarSeries		= new Series<double>(this);
					xpSeries			= new Series<double>(this);
				}

				high	= Input is NinjaScriptBase ? Input : High;
				low		= Input is NinjaScriptBase ? Input : Low;
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar < 3)
				return;

			if (CurrentBar == 3)
			{
				// Determine initial position
				longPosition	= high[0] > high[1];
				xp				= longPosition ? MAX(high, CurrentBar)[0] : MIN(low, CurrentBar)[0];
				af				= Acceleration;
				Value[0]		= xp + (longPosition ? -1 : 1) * ((MAX(high, CurrentBar)[0] - MIN(low, CurrentBar)[0]) * af);
				return;
			}
			if (BarsArray[0].BarsType.IsRemoveLastBarSupported && CurrentBar < prevBar)
			{
				af				= afSeries[0];
				afIncreased		= afIncreasedSeries[0];
				longPosition	= longPositionSeries[0];
				prevBar			= prevBarSeries[0];
				prevSar			= prevSarSeries[0];
				reverseBar		= reverseBarSeries[0];
				reverseValue	= reverseValueSeries[0];
				todaySar		= todaySarSeries[0];
				xp				= xpSeries[0];
			}

			// Reset accelerator increase limiter on new bars
			if (afIncreased && prevBar != CurrentBar)
				afIncreased = false;

			// Current event is on a bar not marked as a reversal bar yet
			if (reverseBar != CurrentBar)
			{
				// SAR = SAR[1] + af * (xp - SAR[1])
				todaySar = TodaySar(Value[1] + af * (xp - Value[1]));
				for (int x = 1; x <= 2; x++)
				{
					if (longPosition)
					{
						if (todaySar > low[x])
							todaySar = low[x];
					}
					else
					{
						if (todaySar < high[x])
							todaySar = high[x];
					}
				}

				// Holding long position
				if (longPosition)
				{
					// Process a new SAR value only on a new bar or if SAR value was penetrated.
					if (prevBar != CurrentBar || low[0] < prevSar)
					{
						Value[0] = todaySar;
						prevSar = todaySar;
					}
					else
						Value[0] = prevSar;

					if (high[0] > xp)
					{
						xp = high[0];
						AfIncrease();
					}
				}

				// Holding short position
				else if (!longPosition)
				{
					// Process a new SAR value only on a new bar or if SAR value was penetrated.
					if (prevBar != CurrentBar || high[0] > prevSar)
					{
						Value[0] = todaySar;
						prevSar = todaySar;
					}
					else
						Value[0] = prevSar;

					if (low[0] < xp)
					{
						xp = low[0];
						AfIncrease();
					}
				}
			}

			// Current event is on the same bar as the reversal bar
			else
			{
				// Only set new xp values. No increasing af since this is the first bar.
				if (longPosition && high[0] > xp)
					xp = high[0];
				else if (!longPosition && low[0] < xp)
					xp = low[0];

				Value[0] = prevSar;

				todaySar = TodaySar(longPosition ? Math.Min(reverseValue, low[0]) : Math.Max(reverseValue, high[0]));
			}

			prevBar = CurrentBar;

			// Reverse position
			if ((longPosition && (low[0] < todaySar || low[1] < todaySar))
				|| (!longPosition && (high[0] > todaySar || high[1] > todaySar)))
				Value[0] = Reverse();

			if (BarsArray[0].BarsType.IsRemoveLastBarSupported)
			{
				afSeries[0]				= af;
				afIncreasedSeries[0]	= afIncreased;
				longPositionSeries[0]	= longPosition;
				prevBarSeries[0]		= prevBar;
				prevSarSeries[0]		= prevSar;
				reverseBarSeries[0]		= reverseBar;
				reverseValueSeries[0]	= reverseValue;
				todaySarSeries[0]		= todaySar;
				xpSeries[0]				= xp;
			}
		}

		#region Miscellaneous
		// Only raise accelerator if not raised for current bar yet
		private void AfIncrease()
		{
			if (!afIncreased)
			{
				af			= Math.Min(AccelerationMax, af + AccelerationStep);
				afIncreased	= true;
			}
		}

		// Additional rule. SAR for today can't be placed inside the bar of day - 1 or day - 2.
		private double TodaySar(double tSar)
		{
			if (longPosition)
			{
				double lowestSar = Math.Min(Math.Min(tSar, low[0]), low[1]);
				if (low[0] > lowestSar)
					tSar = lowestSar;
			}
			else
			{
				double highestSar = Math.Max(Math.Max(tSar, high[0]), high[1]);
				if (high[0] < highestSar)
					tSar = highestSar;
			}
			return tSar;
		}

		private double Reverse()
		{
			double tSar = xp;

			if ((longPosition && prevSar > low[0]) || (!longPosition && prevSar < high[0]) || prevBar != CurrentBar)
			{
				longPosition	= !longPosition;
				reverseBar		= CurrentBar;
				reverseValue	= xp;
				af				= Acceleration;
				xp				= longPosition ? high[0] : low[0];
				prevSar			= tSar;
			}
			else
				tSar = prevSar;
			return tSar;
		}
		#endregion

		#region Properties
		[Range(0.00, double.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Acceleration", GroupName = "NinjaScriptParameters", Order = 0)]
		public double Acceleration { get; set; }

		[Range(0.001, double.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "AccelerationMax", GroupName = "NinjaScriptParameters", Order = 1)]
		public double AccelerationMax { get; set; }

		[Range(0.001, double.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "AccelerationStep", GroupName = "NinjaScriptParameters", Order = 2)]
		public double AccelerationStep { get; set; }
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private ParabolicSAR[] cacheParabolicSAR;
		public ParabolicSAR ParabolicSAR(double acceleration, double accelerationMax, double accelerationStep)
		{
			return ParabolicSAR(Input, acceleration, accelerationMax, accelerationStep);
		}

		public ParabolicSAR ParabolicSAR(ISeries<double> input, double acceleration, double accelerationMax, double accelerationStep)
		{
			if (cacheParabolicSAR != null)
				for (int idx = 0; idx < cacheParabolicSAR.Length; idx++)
					if (cacheParabolicSAR[idx] != null && cacheParabolicSAR[idx].Acceleration == acceleration && cacheParabolicSAR[idx].AccelerationMax == accelerationMax && cacheParabolicSAR[idx].AccelerationStep == accelerationStep && cacheParabolicSAR[idx].EqualsInput(input))
						return cacheParabolicSAR[idx];
			return CacheIndicator<ParabolicSAR>(new ParabolicSAR(){ Acceleration = acceleration, AccelerationMax = accelerationMax, AccelerationStep = accelerationStep }, input, ref cacheParabolicSAR);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.ParabolicSAR ParabolicSAR(double acceleration, double accelerationMax, double accelerationStep)
		{
			return indicator.ParabolicSAR(Input, acceleration, accelerationMax, accelerationStep);
		}

		public Indicators.ParabolicSAR ParabolicSAR(ISeries<double> input , double acceleration, double accelerationMax, double accelerationStep)
		{
			return indicator.ParabolicSAR(input, acceleration, accelerationMax, accelerationStep);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.ParabolicSAR ParabolicSAR(double acceleration, double accelerationMax, double accelerationStep)
		{
			return indicator.ParabolicSAR(Input, acceleration, accelerationMax, accelerationStep);
		}

		public Indicators.ParabolicSAR ParabolicSAR(ISeries<double> input , double acceleration, double accelerationMax, double accelerationStep)
		{
			return indicator.ParabolicSAR(input, acceleration, accelerationMax, accelerationStep);
		}
	}
}

#endregion
