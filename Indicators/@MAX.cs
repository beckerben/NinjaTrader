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
	/// The Maximum shows the maximum of the last n bars.
	/// </summary>
	public class MAX : Indicator
	{
		private int		lastBar;
		private double	lastMax;
		private double	runningMax;
		private int		runningBar;
		private int		thisBar;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= Custom.Resource.NinjaScriptIndicatorDescriptionMAX;
				Name						= Custom.Resource.NinjaScriptIndicatorNameMAX;
				IsOverlay					= true;
				IsSuspendedWhileInactive	= true;
				Period						= 14;

				AddPlot(Brushes.DarkCyan, Custom.Resource.NinjaScriptIndicatorNameMAX);
			}
			else if (State == State.Configure)
			{
				lastBar		= 0;
				lastMax		= 0;
				runningMax	= 0;
				runningBar	= 0;
				thisBar		= 0;
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar == 0)
			{
				runningMax	= Input[0];
				lastMax		= Input[0];
				runningBar	= 0;
				lastBar		= 0;
				thisBar		= 0;
				Value[0]	= Input[0];
				return;
			}

			if (CurrentBar - runningBar >= Period || CurrentBar < thisBar)
			{
				runningMax = double.MinValue;
				for (int barsBack = Math.Min(CurrentBar, Period - 1); barsBack > 0; barsBack--)
					if (Input[barsBack] >= runningMax)
					{
						runningMax = Input[barsBack];
						runningBar = CurrentBar - barsBack;
					}
			}

			if (thisBar != CurrentBar)
			{
				lastMax = runningMax;
				lastBar = runningBar;
				thisBar = CurrentBar;
			}

			if (Input[0] >= lastMax)
			{
				runningMax = Input[0];
				runningBar = CurrentBar;
			}
			else
			{
				runningMax = lastMax;
				runningBar = lastBar;
			}

			Value[0] = runningMax;
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
		private MAX[] cacheMAX;
		public MAX MAX(int period)
		{
			return MAX(Input, period);
		}

		public MAX MAX(ISeries<double> input, int period)
		{
			if (cacheMAX != null)
				for (int idx = 0; idx < cacheMAX.Length; idx++)
					if (cacheMAX[idx] != null && cacheMAX[idx].Period == period && cacheMAX[idx].EqualsInput(input))
						return cacheMAX[idx];
			return CacheIndicator<MAX>(new MAX(){ Period = period }, input, ref cacheMAX);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.MAX MAX(int period)
		{
			return indicator.MAX(Input, period);
		}

		public Indicators.MAX MAX(ISeries<double> input , int period)
		{
			return indicator.MAX(input, period);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.MAX MAX(int period)
		{
			return indicator.MAX(Input, period);
		}

		public Indicators.MAX MAX(ISeries<double> input , int period)
		{
			return indicator.MAX(input, period);
		}
	}
}

#endregion
