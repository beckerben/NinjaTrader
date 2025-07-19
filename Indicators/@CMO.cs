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
	/// The CMO differs from other momentum oscillators such as Relative Strength Index (RSI) and Stochastics.
	/// It uses both up and down days data in the numerator of the calculation to measure momentum directly.
	/// Primarily used to look for extreme overbought and oversold conditions, CMO can also be used to look for trends.
	/// </summary>
	public class CMO : Indicator
	{
		private Series<double>	down;
		private Series<double>	up;
		private SUM				sumDown;
		private SUM				sumUp;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= Custom.Resource.NinjaScriptIndicatorDescriptionCMO;
				Name						= Custom.Resource.NinjaScriptIndicatorNameCMO;
				IsSuspendedWhileInactive	= true;
				IsOverlay					= false;
				Period						= 14;

				AddPlot(Brushes.DodgerBlue, Custom.Resource.NinjaScriptIndicatorNameCMO);
			}
			else if (State == State.DataLoaded)
			{
				down	= new Series<double>(this);
				up		= new Series<double>(this);
				sumDown	= SUM(down, Period);
				sumUp	= SUM(up, Period);
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar == 0)
			{
				down[0]		= 0;
				up[0]		= 0;
				return;
			}

			double input0	= Input[0];
			double input1	= Input[1];
			down[0]			= Math.Max(input1 - input0, 0);
			up[0]			= Math.Max(input0 - input1, 0);

			double sumDown0 = sumDown[0];
			double sumUp0	= sumUp[0];

			if (sumUp0.ApproxCompare(sumDown0) == 0)
				Value[0] = 0;
			else
				Value[0] = 100 * ((sumUp0 - sumDown0) / (sumUp0 + sumDown0));
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
		private CMO[] cacheCMO;
		public CMO CMO(int period)
		{
			return CMO(Input, period);
		}

		public CMO CMO(ISeries<double> input, int period)
		{
			if (cacheCMO != null)
				for (int idx = 0; idx < cacheCMO.Length; idx++)
					if (cacheCMO[idx] != null && cacheCMO[idx].Period == period && cacheCMO[idx].EqualsInput(input))
						return cacheCMO[idx];
			return CacheIndicator<CMO>(new CMO(){ Period = period }, input, ref cacheCMO);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.CMO CMO(int period)
		{
			return indicator.CMO(Input, period);
		}

		public Indicators.CMO CMO(ISeries<double> input , int period)
		{
			return indicator.CMO(input, period);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.CMO CMO(int period)
		{
			return indicator.CMO(Input, period);
		}

		public Indicators.CMO CMO(ISeries<double> input , int period)
		{
			return indicator.CMO(input, period);
		}
	}
}

#endregion
