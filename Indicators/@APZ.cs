//
// Copyright (C) 2025, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
// Reference to "Trading with Adaptive Price Zone" article in S&C, September 2006, p. 28 by Lee Leibfarth.
//
#region Using declarations
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using System.Xml.Serialization;
#endregion

// This namespace holds indicators in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators
{
	/// <summary>
	/// The APZ (Adaptive Prize Zone) forms a steady channel based on double smoothed
	/// exponential moving averages around the average price. See S/C, September 2006, p.28.
	/// </summary>
	public class APZ : Indicator
	{
		private EMA		emaEma;
		private EMA		emaRange;
		private int		newPeriod;
		private int		period;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= Custom.Resource.NinjaScriptIndicatorDescriptionAPZ;
				Name						= Custom.Resource.NinjaScriptIndicatorNameAPZ;
				IsSuspendedWhileInactive	= true;
				IsOverlay					= true;
				BandPct						= 2;
				Period						= 20;

				AddPlot(Brushes.Crimson, Custom.Resource.NinjaScriptIndicatorLower);
				AddPlot(Brushes.Crimson, Custom.Resource.NinjaScriptIndicatorUpper);
			}
			else if (State == State.DataLoaded)
			{
				emaEma		= EMA(EMA(newPeriod), newPeriod);
				emaRange	= EMA(Range(), Period);
				newPeriod	= 0;
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar < Period)
			{
				Lower[0] = Input[0];
				Upper[0] = Input[0];
				return;
			}

			double rangeOffset	= BandPct * emaRange[0];
			double emaEma0		= emaEma[0];

			Lower[0] = emaEma0 - rangeOffset;
			Upper[0] = emaEma0 + rangeOffset;
		}

		#region Properties
		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "BandPct", GroupName = "NinjaScriptParameters", Order = 0)]
		public double BandPct { get; set; }

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> Lower => Values[0];

		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Period", GroupName = "NinjaScriptParameters", Order = 1)]
		public int Period
		{
			get => period;
			set
			{
				period = value;
				newPeriod = Convert.ToInt32(Math.Sqrt(Convert.ToDouble(value)));
			}
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> Upper => Values[1];

		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private APZ[] cacheAPZ;
		public APZ APZ(double bandPct, int period)
		{
			return APZ(Input, bandPct, period);
		}

		public APZ APZ(ISeries<double> input, double bandPct, int period)
		{
			if (cacheAPZ != null)
				for (int idx = 0; idx < cacheAPZ.Length; idx++)
					if (cacheAPZ[idx] != null && cacheAPZ[idx].BandPct == bandPct && cacheAPZ[idx].Period == period && cacheAPZ[idx].EqualsInput(input))
						return cacheAPZ[idx];
			return CacheIndicator<APZ>(new APZ(){ BandPct = bandPct, Period = period }, input, ref cacheAPZ);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.APZ APZ(double bandPct, int period)
		{
			return indicator.APZ(Input, bandPct, period);
		}

		public Indicators.APZ APZ(ISeries<double> input , double bandPct, int period)
		{
			return indicator.APZ(input, bandPct, period);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.APZ APZ(double bandPct, int period)
		{
			return indicator.APZ(Input, bandPct, period);
		}

		public Indicators.APZ APZ(ISeries<double> input , double bandPct, int period)
		{
			return indicator.APZ(input, bandPct, period);
		}
	}
}

#endregion
