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

// This namespace holds indicators in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators
{
	/// <summary>
	/// Fisher Transform. The Fisher Transform has sharp and distinct turning points
	/// that occur in a timely fashion. The resulting peak swings are used to identify price reversals.
	/// </summary>
	public class FisherTransform : Indicator
	{
		private	MAX				max;
		private	MIN				min;
		private Series<double>	tmpSeries;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= Custom.Resource.NinjaScriptIndicatorDescriptionFisherTransform;
				Name						= Custom.Resource.NinjaScriptIndicatorNameFisherTransform;
				IsSuspendedWhileInactive	= true;
				Period						= 10;

				AddPlot(new Stroke(Brushes.DodgerBlue, 2), PlotStyle.Bar, Custom.Resource.NinjaScriptIndicatorNameFisherTransform);
			}
			else if (State == State.DataLoaded)
			{
				max			= MAX(Input, Period);
				min			= MIN(Input, Period);
				tmpSeries	= new Series<double>(this);
			}
		}

		protected override void OnBarUpdate()
		{
			double fishPrev		= 0;
			double tmpValuePrev	= 0;

			if (CurrentBar > 0)
			{
				fishPrev		= Value[1];
				tmpValuePrev	= tmpSeries[1];
			}

			double minLo	= min[0];
			double num1		= max[0] - minLo;

			// Guard against infinite numbers and div by zero
			num1			= num1 < TickSize / 10 ? TickSize / 10 : num1;
			double tmpValue = 0.66 * ((Input[0] - minLo) / num1 - 0.5) + 0.67 * tmpValuePrev;

			if (tmpValue > 0.99)
				tmpValue = 0.999;
			else if (tmpValue < -0.99)
				tmpValue = -0.999;

			tmpSeries[0]	= tmpValue;
			Value[0]		= 0.5 * Math.Log((1 + tmpValue) / (1 - tmpValue)) + 0.5 * fishPrev;
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
		private FisherTransform[] cacheFisherTransform;
		public FisherTransform FisherTransform(int period)
		{
			return FisherTransform(Input, period);
		}

		public FisherTransform FisherTransform(ISeries<double> input, int period)
		{
			if (cacheFisherTransform != null)
				for (int idx = 0; idx < cacheFisherTransform.Length; idx++)
					if (cacheFisherTransform[idx] != null && cacheFisherTransform[idx].Period == period && cacheFisherTransform[idx].EqualsInput(input))
						return cacheFisherTransform[idx];
			return CacheIndicator<FisherTransform>(new FisherTransform(){ Period = period }, input, ref cacheFisherTransform);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.FisherTransform FisherTransform(int period)
		{
			return indicator.FisherTransform(Input, period);
		}

		public Indicators.FisherTransform FisherTransform(ISeries<double> input , int period)
		{
			return indicator.FisherTransform(input, period);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.FisherTransform FisherTransform(int period)
		{
			return indicator.FisherTransform(Input, period);
		}

		public Indicators.FisherTransform FisherTransform(ISeries<double> input , int period)
		{
			return indicator.FisherTransform(input, period);
		}
	}
}

#endregion
