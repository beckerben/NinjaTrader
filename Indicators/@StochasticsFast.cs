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
	/// The Stochastic Oscillator is made up of two lines that oscillate between
	/// a vertical scale of 0 to 100. The %K is the main line and it is drawn as
	/// a solid line. The second is the %D line and is a moving average of %K.
	/// The %D line is drawn as a dotted line. Use as a buy/sell signal generator,
	/// buying when fast moves above slow and selling when fast moves below slow.
	/// </summary>
	public class StochasticsFast : Indicator
	{
		private Series<double>			den;
		private MAX						max;
		private MIN						min;
		private Series<double>			nom;
		private SMA						smaK;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= Custom.Resource.NinjaScriptIndicatorDescriptionStochasticsFast;
				Name						= Custom.Resource.NinjaScriptIndicatorNameStochasticsFast;
				IsSuspendedWhileInactive	= true;
				PeriodD						= 3;
				PeriodK						= 14;

				AddPlot(Brushes.DodgerBlue,		Custom.Resource.StochasticsD);
				AddPlot(Brushes.Goldenrod,		Custom.Resource.StochasticsK);
				AddLine(Brushes.DarkCyan,		20,	Custom.Resource.NinjaScriptIndicatorLower);
				AddLine(Brushes.DarkCyan,		80,	Custom.Resource.NinjaScriptIndicatorUpper);
			}
			else if (State == State.DataLoaded)
			{
				den			= new Series<double>(this);
				nom			= new Series<double>(this);
				min			= MIN(Low, PeriodK);
				max			= MAX(High, PeriodK);
				smaK		= SMA(K, PeriodD);
			}
		}

		protected override void OnBarUpdate()
		{
			double min0	= min[0];
			nom[0]		= Close[0] - min0;
			den[0]		= max[0] - min0;

			K[0] = den[0].ApproxCompare(0) == 0 ? CurrentBar == 0 ? 50 : K[1] : Math.Min(100, Math.Max(0, 100 * nom[0] / den[0]));

			D[0] = smaK[0];
		}

		#region Properties
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> D => Values[0];

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> K => Values[1];

		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "PeriodD", GroupName = "NinjaScriptParameters", Order = 0)]
		public int PeriodD { get; set; }

		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "PeriodK", GroupName = "NinjaScriptParameters", Order = 1)]
		public int PeriodK { get; set; }
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private StochasticsFast[] cacheStochasticsFast;
		public StochasticsFast StochasticsFast(int periodD, int periodK)
		{
			return StochasticsFast(Input, periodD, periodK);
		}

		public StochasticsFast StochasticsFast(ISeries<double> input, int periodD, int periodK)
		{
			if (cacheStochasticsFast != null)
				for (int idx = 0; idx < cacheStochasticsFast.Length; idx++)
					if (cacheStochasticsFast[idx] != null && cacheStochasticsFast[idx].PeriodD == periodD && cacheStochasticsFast[idx].PeriodK == periodK && cacheStochasticsFast[idx].EqualsInput(input))
						return cacheStochasticsFast[idx];
			return CacheIndicator<StochasticsFast>(new StochasticsFast(){ PeriodD = periodD, PeriodK = periodK }, input, ref cacheStochasticsFast);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.StochasticsFast StochasticsFast(int periodD, int periodK)
		{
			return indicator.StochasticsFast(Input, periodD, periodK);
		}

		public Indicators.StochasticsFast StochasticsFast(ISeries<double> input , int periodD, int periodK)
		{
			return indicator.StochasticsFast(input, periodD, periodK);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.StochasticsFast StochasticsFast(int periodD, int periodK)
		{
			return indicator.StochasticsFast(Input, periodD, periodK);
		}

		public Indicators.StochasticsFast StochasticsFast(ISeries<double> input , int periodD, int periodK)
		{
			return indicator.StochasticsFast(input, periodD, periodK);
		}
	}
}

#endregion
