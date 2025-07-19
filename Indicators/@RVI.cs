//
// Copyright (C) 2025, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
#region Using declarations
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
#endregion

//This namespace holds indicators in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators
{
	/// <summary>
	/// The RVI (Relative Volatility Index) was developed by Donald Dorsey as a compliment to and a confirmation of momentum based indicators. When used to confirm other signals, only buy when the RVI is over 50 and only sell when the RVI is under 50.
	/// </summary>
	public class RVI : Indicator
	{
		private double			dnAvgH;
		private double			dnAvgL;
		private double			upAvgH;
		private double			upAvgL;
		private double			lastDnAvgH;
		private double			lastDnAvgL;
		private double			lastUpAvgH;
		private double			lastUpAvgL;
		private Series<double>	dnAvgHSeries;
		private Series<double>	dnAvgLSeries;
		private Series<double>	upAvgHSeries;
		private Series<double>	upAvgLSeries;
		private int				savedCurrentBar;
		private StdDev			stdDevHigh;
		private StdDev			stdDevLow;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= Custom.Resource.NinjaScriptIndicatorDescriptionRVI;
				Name						= Custom.Resource.NinjaScriptIndicatorNameRVI;
				IsSuspendedWhileInactive	= true;
				Period						= 14;
				IsOverlay					= false;

				AddPlot(Brushes.Goldenrod,		Custom.Resource.NinjaScriptIndicatorNameRVI);
				AddLine(Brushes.DarkGray,		50,	Custom.Resource.RVISignalLine);
			}

			else if (State == State.Configure)
			{
				savedCurrentBar	= -1;
				dnAvgH			= dnAvgL = upAvgH = upAvgL = lastDnAvgH = lastDnAvgL = lastUpAvgH = lastUpAvgL = 0;
			}
			else if (State == State.DataLoaded)
			{
				stdDevHigh	= StdDev(High, 10);
				stdDevLow	= StdDev(Low, 10);

				if (BarsArray[0].BarsType.IsRemoveLastBarSupported)
				{
					dnAvgHSeries = new Series<double>(this);
					dnAvgLSeries = new Series<double>(this);
					upAvgHSeries = new Series<double>(this);
					upAvgLSeries = new Series<double>(this);
				}
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar == 0)
			{
				Value[0] = 50;
				return;
			}

			if (CurrentBar != savedCurrentBar)
			{
				dnAvgH			= BarsArray[0].BarsType.IsRemoveLastBarSupported ? dnAvgHSeries[1] : lastDnAvgH;
				dnAvgL			= BarsArray[0].BarsType.IsRemoveLastBarSupported ? dnAvgLSeries[1] : lastDnAvgL;
				upAvgH			= BarsArray[0].BarsType.IsRemoveLastBarSupported ? upAvgHSeries[1] : lastUpAvgH;
				upAvgL			= BarsArray[0].BarsType.IsRemoveLastBarSupported ? upAvgLSeries[1] : lastUpAvgL;
				savedCurrentBar	= CurrentBar;
			}

			double high0		= High[0];
			double high1		= High[1];
			double low0			= Low[0];
			double low1			= Low[1];
			double up			= 0;
			double dn			= 0;

			// RVI(High)
			if (high0 > high1)
				up = stdDevHigh[0];
			else if (high0 < high1)
				dn = stdDevHigh[0];

			double actUpAvgH	= lastUpAvgH = (upAvgH * (Period - 1) + up) / Period;
			double actDnAvgH	= lastDnAvgH = (dnAvgH * (Period - 1) + dn) / Period;
			double rviH			= 100 * (actUpAvgH / (actUpAvgH + actDnAvgH));

			// RVI(Low)
			up = 0;
			dn = 0;

			if (low0 > low1)
				up = stdDevLow[0];
			else if (low0 < low1)
				dn = stdDevLow[0];

			double actUpAvgL	= lastUpAvgL = (upAvgL * (Period - 1) + up) / Period;
			double actDnAvgL 	= lastDnAvgL = (dnAvgL * (Period - 1) + dn) / Period;
			double rviL 		= 100 * (actUpAvgL / (actUpAvgL + actDnAvgL));

			if (BarsArray[0].BarsType.IsRemoveLastBarSupported)
			{
				dnAvgHSeries[0] = actDnAvgH;
				dnAvgLSeries[0] = actDnAvgL;
				upAvgHSeries[0] = actUpAvgH;
				upAvgLSeries[0] = actUpAvgL;
			}

			Value[0] = CurrentBar == 1 ? 50 : (rviH + rviL) / 2;
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
		private RVI[] cacheRVI;
		public RVI RVI(int period)
		{
			return RVI(Input, period);
		}

		public RVI RVI(ISeries<double> input, int period)
		{
			if (cacheRVI != null)
				for (int idx = 0; idx < cacheRVI.Length; idx++)
					if (cacheRVI[idx] != null && cacheRVI[idx].Period == period && cacheRVI[idx].EqualsInput(input))
						return cacheRVI[idx];
			return CacheIndicator<RVI>(new RVI(){ Period = period }, input, ref cacheRVI);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.RVI RVI(int period)
		{
			return indicator.RVI(Input, period);
		}

		public Indicators.RVI RVI(ISeries<double> input , int period)
		{
			return indicator.RVI(input, period);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.RVI RVI(int period)
		{
			return indicator.RVI(Input, period);
		}

		public Indicators.RVI RVI(ISeries<double> input , int period)
		{
			return indicator.RVI(input, period);
		}
	}
}

#endregion
