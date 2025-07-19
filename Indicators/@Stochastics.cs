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
	public class Stochastics : Indicator
	{
		private Series<double>		den;
		private Series<double>		fastK;
		private MIN					min;
		private MAX					max;
		private Series<double>		nom;
		private SMA					smaFastK;
		private SMA					smaK;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= Custom.Resource.NinjaScriptIndicatorDescriptionStochastics;
				Name						= Custom.Resource.NinjaScriptIndicatorNameStochastics;
				IsSuspendedWhileInactive	= true;
				PeriodD						= 7;
				PeriodK						= 14;
				Smooth						= 3;

				AddPlot(Brushes.DodgerBlue,		Custom.Resource.StochasticsD);
				AddPlot(Brushes.Goldenrod,		Custom.Resource.StochasticsK);

				AddLine(Brushes.DarkCyan,	20,	Custom.Resource.NinjaScriptIndicatorLower);
				AddLine(Brushes.DarkCyan,	80,	Custom.Resource.NinjaScriptIndicatorUpper);
			}
			else if (State == State.DataLoaded)
			{
				den			= new Series<double>(this);
				nom			= new Series<double>(this);
				fastK		= new Series<double>(this);
				min			= MIN(Low, PeriodK);
				max			= MAX(High, PeriodK);
				smaFastK	= SMA(fastK, Smooth);
				smaK		= SMA(K, PeriodD);
			}
		}

		protected override void OnBarUpdate()
		{
			double min0 = min[0];
			nom[0]		= Close[0] - min0;
			den[0]		= max[0] - min0;

			fastK[0] = den[0].ApproxCompare(0) == 0 ? CurrentBar == 0 ? 50 : fastK[1] : Math.Min(100, Math.Max(0, 100 * nom[0] / den[0]));

			// Slow %K == Fast %D
			K[0] = smaFastK[0];
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

		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Smooth", GroupName = "NinjaScriptParameters", Order = 2)]
		public int Smooth { get; set; }
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private Stochastics[] cacheStochastics;
		public Stochastics Stochastics(int periodD, int periodK, int smooth)
		{
			return Stochastics(Input, periodD, periodK, smooth);
		}

		public Stochastics Stochastics(ISeries<double> input, int periodD, int periodK, int smooth)
		{
			if (cacheStochastics != null)
				for (int idx = 0; idx < cacheStochastics.Length; idx++)
					if (cacheStochastics[idx] != null && cacheStochastics[idx].PeriodD == periodD && cacheStochastics[idx].PeriodK == periodK && cacheStochastics[idx].Smooth == smooth && cacheStochastics[idx].EqualsInput(input))
						return cacheStochastics[idx];
			return CacheIndicator<Stochastics>(new Stochastics(){ PeriodD = periodD, PeriodK = periodK, Smooth = smooth }, input, ref cacheStochastics);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.Stochastics Stochastics(int periodD, int periodK, int smooth)
		{
			return indicator.Stochastics(Input, periodD, periodK, smooth);
		}

		public Indicators.Stochastics Stochastics(ISeries<double> input , int periodD, int periodK, int smooth)
		{
			return indicator.Stochastics(input, periodD, periodK, smooth);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.Stochastics Stochastics(int periodD, int periodK, int smooth)
		{
			return indicator.Stochastics(Input, periodD, periodK, smooth);
		}

		public Indicators.Stochastics Stochastics(ISeries<double> input , int periodD, int periodK, int smooth)
		{
			return indicator.Stochastics(input, periodD, periodK, smooth);
		}
	}
}

#endregion
