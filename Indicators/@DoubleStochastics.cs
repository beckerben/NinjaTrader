//
// Copyright (C) 2025, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
#region Using declarations
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Core.FloatingPoint;
#endregion

// This namespace holds indicators in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators
{
	/// <summary>
	/// Double stochastics
	/// </summary>
	public class DoubleStochastics : Indicator
	{
		private EMA				emaP1;
		private EMA				emaP3;
		private	MIN				minLow;
		private MIN				minP2;
		private	MAX				maxHigh;
		private MAX				maxP2;
		private Series<double>	p1;
		private Series<double>	p2;
		private Series<double>	p3;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= Custom.Resource.NinjaScriptIndicatorDescriptionDoubleStochastics;
				Name						= Custom.Resource.NinjaScriptIndicatorNameDoubleStochastics;
				IsSuspendedWhileInactive	= true;
				Period						= 10;

				AddPlot(Brushes.Crimson,													Custom.Resource.StochasticsK);
				AddLine(new Gui.Stroke(Brushes.DodgerBlue, Gui.DashStyleHelper.Dash, 1), 90,	Custom.Resource.NinjaScriptIndicatorUpper);
				AddLine(new Gui.Stroke(Brushes.DodgerBlue, Gui.DashStyleHelper.Dash, 1), 10,	Custom.Resource.NinjaScriptIndicatorLower);
			}
			else if (State == State.DataLoaded)
			{
				p1		= new Series<double>(this);
				p2		= new Series<double>(this);
				p3		= new Series<double>(this);
				emaP1	= EMA(p1, 3);
				emaP3	= EMA(p3, 3);
				maxHigh	= MAX(High, Period);
				maxP2	= MAX(p2, Period);
				minLow	= MIN(Low, Period);
				minP2	= MIN(p2, Period);
			}
		}

		protected override void OnBarUpdate()
		{
			double maxHigh0		= maxHigh[0];
			double minLow0		= minLow[0];
			double r			= maxHigh0 - minLow0;
			r					= r.ApproxCompare(0) == 0 ? 0 : r;

			if (r == 0)
				p1[0] = CurrentBar == 0 ? 50 : p1[1];
			else
				p1[0] = Math.Min(100, Math.Max(0, 100 * (Close[0] - minLow0) / r));

			p2[0]				= emaP1[0];
			double minP20		= minP2[0];
			double s			= maxP2[0] - minP20;
			s					= s.ApproxCompare(0) == 0 ? 0 : s;

			if (s == 0)
				p3[0] = CurrentBar == 0 ? 50 : p3[1];
			else
				p3[0] = Math.Min(100, Math.Max(0, 100 * (p2[0] - minP20) / s));

			K[0] = emaP3[0];
		}

		#region Properties
		[Browsable(false)]	// this line prevents the data series from being displayed in the indicator properties dialog, do not remove
		[XmlIgnore]		// this line ensures that the indicator can be saved/recovered as part of a chart template, do not remove
		public Series<double> K => Values[0];

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
		private DoubleStochastics[] cacheDoubleStochastics;
		public DoubleStochastics DoubleStochastics(int period)
		{
			return DoubleStochastics(Input, period);
		}

		public DoubleStochastics DoubleStochastics(ISeries<double> input, int period)
		{
			if (cacheDoubleStochastics != null)
				for (int idx = 0; idx < cacheDoubleStochastics.Length; idx++)
					if (cacheDoubleStochastics[idx] != null && cacheDoubleStochastics[idx].Period == period && cacheDoubleStochastics[idx].EqualsInput(input))
						return cacheDoubleStochastics[idx];
			return CacheIndicator<DoubleStochastics>(new DoubleStochastics(){ Period = period }, input, ref cacheDoubleStochastics);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.DoubleStochastics DoubleStochastics(int period)
		{
			return indicator.DoubleStochastics(Input, period);
		}

		public Indicators.DoubleStochastics DoubleStochastics(ISeries<double> input , int period)
		{
			return indicator.DoubleStochastics(input, period);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.DoubleStochastics DoubleStochastics(int period)
		{
			return indicator.DoubleStochastics(Input, period);
		}

		public Indicators.DoubleStochastics DoubleStochastics(ISeries<double> input , int period)
		{
			return indicator.DoubleStochastics(input, period);
		}
	}
}

#endregion
