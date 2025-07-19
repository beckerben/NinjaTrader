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
	/// The SMA (Simple Moving Average) is an indicator that shows the average value of a security's price over a period of time.
	/// </summary>
	public class SMA : Indicator
	{
		private double priorSum;
		private double sum;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= Custom.Resource.NinjaScriptIndicatorDescriptionSMA;
				Name						= Custom.Resource.NinjaScriptIndicatorNameSMA;
				IsOverlay					= true;
				IsSuspendedWhileInactive	= true;
				Period						= 14;

				AddPlot(Brushes.Goldenrod, Custom.Resource.NinjaScriptIndicatorNameSMA);
			}
			else if (State == State.Configure)
			{
				priorSum	= 0;
				sum			= 0;
			}
		}

		protected override void OnBarUpdate()
		{
			if (BarsArray[0].BarsType.IsRemoveLastBarSupported)
			{
				if (CurrentBar == 0)
					Value[0] = Input[0];
				else
				{
					double last = Value[1] * Math.Min(CurrentBar, Period);

					Value[0] = CurrentBar >= Period
						? (last + Input[0] - Input[Period]) / Math.Min(CurrentBar, Period)
						: (last + Input[0]) / (Math.Min(CurrentBar, Period) + 1);
				}
			}
			else
			{
				if (IsFirstTickOfBar)
					priorSum = sum;

				sum = priorSum + Input[0] - (CurrentBar >= Period ? Input[Period] : 0);
				Value[0] = sum / (CurrentBar < Period ? CurrentBar + 1 : Period);
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
		private SMA[] cacheSMA;
		public SMA SMA(int period)
		{
			return SMA(Input, period);
		}

		public SMA SMA(ISeries<double> input, int period)
		{
			if (cacheSMA != null)
				for (int idx = 0; idx < cacheSMA.Length; idx++)
					if (cacheSMA[idx] != null && cacheSMA[idx].Period == period && cacheSMA[idx].EqualsInput(input))
						return cacheSMA[idx];
			return CacheIndicator<SMA>(new SMA(){ Period = period }, input, ref cacheSMA);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.SMA SMA(int period)
		{
			return indicator.SMA(Input, period);
		}

		public Indicators.SMA SMA(ISeries<double> input , int period)
		{
			return indicator.SMA(input, period);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.SMA SMA(int period)
		{
			return indicator.SMA(Input, period);
		}

		public Indicators.SMA SMA(ISeries<double> input , int period)
		{
			return indicator.SMA(input, period);
		}
	}
}

#endregion
