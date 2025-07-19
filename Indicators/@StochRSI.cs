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
	/// The StochRSI is an oscillator similar in computation to the stochastic measure,
	/// except instead of price values as input, the StochRSI uses RSI values.
	/// The StochRSI computes the current position of the RSI relative to the high and
	/// low RSI values over a specified number of days. The intent of this measure,
	/// designed by Tushard Chande and Stanley Kroll, is to provide further information
	/// about the overbought/oversold nature of the RSI. The StochRSI ranges between 0.0 and 1.0.
	/// Values above 0.8 are generally seen to identify overbought levels and values below 0.2 are
	/// considered to indicate oversold conditions.
	/// </summary>
	public class StochRSI : Indicator
	{
		private MAX max;
		private MIN min;
		private RSI rsi;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= Custom.Resource.NinjaScriptIndicatorDescriptionStochRSI;
				Name						= Custom.Resource.NinjaScriptIndicatorNameStochRSI;
				IsSuspendedWhileInactive	= true;
				IsOverlay					= false;
				Period						= 14;

				AddPlot(Brushes.DarkCyan,				Custom.Resource.NinjaScriptIndicatorNameStochRSI);

				AddLine(Brushes.Crimson,		0.8,	Custom.Resource.NinjaScriptIndicatorOverbought);
				AddLine(Brushes.DodgerBlue,	0.5,	Custom.Resource.NinjaScriptIndicatorNeutral);
				AddLine(Brushes.Crimson,		0.2,	Custom.Resource.NinjaScriptIndicatorOversold);
			}
			else if (State == State.DataLoaded)
			{
				rsi = RSI(Inputs[0], Period, 1);
				min	= MIN(rsi, Period);
				max = MAX(rsi, Period);
			}
		}

		protected override void OnBarUpdate()
		{
			double rsi0 = rsi[0];
			double rsiL = min[0];
			double rsiH = max[0];

			Value[0] = Math.Abs(rsi0 - rsiL) > double.Epsilon && Math.Abs(rsiH - rsiL) > double.Epsilon ? (rsi0 - rsiL) / (rsiH - rsiL) : 0;
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
		private StochRSI[] cacheStochRSI;
		public StochRSI StochRSI(int period)
		{
			return StochRSI(Input, period);
		}

		public StochRSI StochRSI(ISeries<double> input, int period)
		{
			if (cacheStochRSI != null)
				for (int idx = 0; idx < cacheStochRSI.Length; idx++)
					if (cacheStochRSI[idx] != null && cacheStochRSI[idx].Period == period && cacheStochRSI[idx].EqualsInput(input))
						return cacheStochRSI[idx];
			return CacheIndicator<StochRSI>(new StochRSI(){ Period = period }, input, ref cacheStochRSI);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.StochRSI StochRSI(int period)
		{
			return indicator.StochRSI(Input, period);
		}

		public Indicators.StochRSI StochRSI(ISeries<double> input , int period)
		{
			return indicator.StochRSI(input, period);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.StochRSI StochRSI(int period)
		{
			return indicator.StochRSI(Input, period);
		}

		public Indicators.StochRSI StochRSI(ISeries<double> input , int period)
		{
			return indicator.StochRSI(input, period);
		}
	}
}

#endregion
