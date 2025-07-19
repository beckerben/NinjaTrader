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
using NinjaTrader.Gui;
#endregion

// This namespace holds indicators in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators
{
	/// <summary>
	/// Directional Movement (DM). This is the same indicator as the ADX,
	/// with the addition of the two directional movement indicators +DI
	/// and -DI. +DI and -DI measure upward and downward momentum. A buy
	/// signal is generated when +DI crosses -DI to the upside.
	/// A sell signal is generated when -DI crosses +DI to the downside.
	/// </summary>
	public class DM : Indicator
	{
		private Series<double> dmPlus;
		private Series<double> dmMinus;
		private Series<double> sumDmPlus;
		private Series<double> sumDmMinus;
		private Series<double> sumTr;
		private Series<double> tr;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= Custom.Resource.NinjaScriptIndicatorDescriptionDM;
				Name						= Custom.Resource.NinjaScriptIndicatorNameDM;
				IsSuspendedWhileInactive	= true;
				Period						= 14;

				AddPlot(new Stroke(Brushes.DarkSeaGreen, 2),	PlotStyle.Line,	Custom.Resource.NinjaScriptIndicatorNameADX);
				AddPlot(Brushes.DodgerBlue,									Custom.Resource.DMPlusDI);
				AddPlot(Brushes.Crimson,										Custom.Resource.DMMinusDI);

				AddLine(Brushes.DarkCyan,						25,				Custom.Resource.NinjaScriptIndicatorLower);
				AddLine(Brushes.DarkCyan,						75,				Custom.Resource.NinjaScriptIndicatorUpper);
			}

			else if (State == State.DataLoaded)
			{
				dmPlus		= new Series<double>(this);
				dmMinus		= new Series<double>(this);
				sumDmPlus	= new Series<double>(this);
				sumDmMinus	= new Series<double>(this);
				sumTr		= new Series<double>(this);
				tr			= new Series<double>(this);
			}
		}

		protected override void OnBarUpdate()
		{
			double high0		= High[0];
			double low0			= Low[0];
			double trueRange	= high0 - low0;

			if (CurrentBar == 0)
			{
				tr[0]			= trueRange;
				dmPlus[0]		= 0;
				dmMinus[0]		= 0;
				sumTr[0]		= tr[0];
				sumDmPlus[0]	= dmPlus[0];
				sumDmMinus[0]	= dmMinus[0];
				ADXPlot[0]		= 50;
			}
			else
			{
				double low1			= Low[1];
				double high1		= High[1];
				double close1		= Close[1];

				tr[0]				= Math.Max(Math.Abs(low0 - close1), Math.Max(trueRange, Math.Abs(high0 - close1)));
				dmPlus[0]			= high0 - high1 > low1 - low0 ? Math.Max(high0 - high1, 0) : 0;
				dmMinus[0]			= low1 - low0 > high0 - high1 ? Math.Max(low1 - low0, 0) : 0;

				double sumDmPlus1	= sumDmPlus[1];
				double sumDmMinus1	= sumDmMinus[1];
				double sumTr1		= sumTr[1];

				if (CurrentBar < Period)
				{
					sumTr[0]		= sumTr1 + tr[0];
					sumDmPlus[0]	= sumDmPlus1 + dmPlus[0];
					sumDmMinus[0]	= sumDmMinus1 + dmMinus[0];
				}
				else
				{
					sumTr[0]		= sumTr1 - sumTr[1] / Period + tr[0];
					sumDmPlus[0]	= sumDmPlus1 - sumDmPlus1 / Period + dmPlus[0];
					sumDmMinus[0]	= sumDmMinus1 - sumDmMinus1 / Period + dmMinus[0];
				}

				double diPlus	= 100 * (sumTr[0] == 0 ? 0 : sumDmPlus[0] / sumTr[0]);
				double diMinus	= 100 * (sumTr[0] == 0 ? 0 : sumDmMinus[0] / sumTr[0]);
				double diff		= Math.Abs(diPlus - diMinus);
				double sum		= diPlus + diMinus;

				ADXPlot[0]		= sum == 0 ? 50 : ((Period - 1) * ADXPlot[1] + 100 * diff / sum) / Period;
				DiPlus[0]		= diPlus;
				DiMinus[0]		= diMinus;
			}
		}

		#region Properties
		[Browsable(false)]	// this line prevents the data series from being displayed in the indicator properties dialog, do not remove
		[XmlIgnore]		// this line ensures that the indicator can be saved/recovered as part of a chart template, do not remove
		public Series<double> ADXPlot => Values[0];

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> DiPlus => Values[1];

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> DiMinus => Values[2];

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
		private DM[] cacheDM;
		public DM DM(int period)
		{
			return DM(Input, period);
		}

		public DM DM(ISeries<double> input, int period)
		{
			if (cacheDM != null)
				for (int idx = 0; idx < cacheDM.Length; idx++)
					if (cacheDM[idx] != null && cacheDM[idx].Period == period && cacheDM[idx].EqualsInput(input))
						return cacheDM[idx];
			return CacheIndicator<DM>(new DM(){ Period = period }, input, ref cacheDM);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.DM DM(int period)
		{
			return indicator.DM(Input, period);
		}

		public Indicators.DM DM(ISeries<double> input , int period)
		{
			return indicator.DM(input, period);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.DM DM(int period)
		{
			return indicator.DM(Input, period);
		}

		public Indicators.DM DM(ISeries<double> input , int period)
		{
			return indicator.DM(input, period);
		}
	}
}

#endregion
