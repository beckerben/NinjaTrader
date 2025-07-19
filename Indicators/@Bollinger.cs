//
// Copyright (C) 2025, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
#region Using declarations
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using System.Xml.Serialization;
#endregion

// This namespace holds indicators in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators
{
	/// <summary>
	/// Bollinger Bands are plotted at standard deviation levels above and below a moving average.
	/// Since standard deviation is a measure of volatility, the bands are self-adjusting:
	/// widening during volatile markets and contracting during calmer periods.
	/// </summary>
	public class Bollinger : Indicator
	{
		private SMA		sma;
		private StdDev	stdDev;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= Custom.Resource.NinjaScriptIndicatorDescriptionBollinger;
				Name						= Custom.Resource.NinjaScriptIndicatorNameBollinger;
				IsOverlay					= true;
				IsSuspendedWhileInactive	= true;
				NumStdDev					= 2;
				Period						= 14;

				AddPlot(Brushes.Goldenrod, Custom.Resource.BollingerUpperBand);
				AddPlot(Brushes.Goldenrod, Custom.Resource.BollingerMiddleBand);
				AddPlot(Brushes.Goldenrod, Custom.Resource.BollingerLowerBand);
			}
			else if (State == State.DataLoaded)
			{
				sma		= SMA(Period);
				stdDev	= StdDev(Period);
			}
		}

		protected override void OnBarUpdate()
		{
			double sma0		= sma[0];
			double stdDev0	= stdDev[0];

			Upper[0]		= sma0 + NumStdDev * stdDev0;
			Middle[0]		= sma0;
			Lower[0]		= sma0 - NumStdDev * stdDev0;
		}

		#region Properties
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> Lower => Values[2];

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> Middle => Values[1];

		[Range(0, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "NumStdDev", GroupName = "NinjaScriptParameters", Order = 0)]
		public double NumStdDev { get; set; }

		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Period", GroupName = "NinjaScriptParameters", Order = 1)]
		public int Period { get; set; }

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> Upper => Values[0];

		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private Bollinger[] cacheBollinger;
		public Bollinger Bollinger(double numStdDev, int period)
		{
			return Bollinger(Input, numStdDev, period);
		}

		public Bollinger Bollinger(ISeries<double> input, double numStdDev, int period)
		{
			if (cacheBollinger != null)
				for (int idx = 0; idx < cacheBollinger.Length; idx++)
					if (cacheBollinger[idx] != null && cacheBollinger[idx].NumStdDev == numStdDev && cacheBollinger[idx].Period == period && cacheBollinger[idx].EqualsInput(input))
						return cacheBollinger[idx];
			return CacheIndicator<Bollinger>(new Bollinger(){ NumStdDev = numStdDev, Period = period }, input, ref cacheBollinger);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.Bollinger Bollinger(double numStdDev, int period)
		{
			return indicator.Bollinger(Input, numStdDev, period);
		}

		public Indicators.Bollinger Bollinger(ISeries<double> input , double numStdDev, int period)
		{
			return indicator.Bollinger(input, numStdDev, period);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.Bollinger Bollinger(double numStdDev, int period)
		{
			return indicator.Bollinger(Input, numStdDev, period);
		}

		public Indicators.Bollinger Bollinger(ISeries<double> input , double numStdDev, int period)
		{
			return indicator.Bollinger(input, numStdDev, period);
		}
	}
}

#endregion
