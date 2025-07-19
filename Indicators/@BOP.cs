//
// Copyright (C) 2025, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
#region Using declarations
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Gui;
using NinjaTrader.Core.FloatingPoint;
#endregion

// This namespace holds indicators in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators
{
	/// <summary>
	/// The balance of power indicator measures the strength of the bulls vs. bears by
	///  assessing the ability of each to push price to an extreme level.
	/// </summary>
	public class BOP : Indicator
	{
		private Series<double>	bop;
		private SMA				sma;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= Custom.Resource.NinjaScriptIndicatorDescriptionBOP;
				Name						= Custom.Resource.NinjaScriptIndicatorNameBOP;
				IsSuspendedWhileInactive	= true;
				Smooth 						= 14;
				IsOverlay					= false;

				AddPlot(new Stroke(Brushes.DodgerBlue, 2), PlotStyle.Bar, Custom.Resource.NinjaScriptIndicatorNameBOP);
				AddLine(Brushes.DarkGray, 0, Custom.Resource.NinjaScriptIndicatorZeroLine);
			}
			else if (State == State.DataLoaded)
			{
				bop = new Series<double>(this);
				sma	= SMA(bop, Smooth);
			}
		}

		protected override void OnBarUpdate()
		{
			double high0	= High[0];
			double low0		= Low[0];

			if ((high0 - low0).ApproxCompare(0) == 0)
				bop[0] = 0;
			else
				bop[0] = (Close[0] - Open[0]) / (high0 - low0);

			Value[0] = sma[0];
		}

		#region Properties
		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Smooth", GroupName = "NinjaScriptParameters", Order = 0)]
		public int Smooth { get; set; }
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private BOP[] cacheBOP;
		public BOP BOP(int smooth)
		{
			return BOP(Input, smooth);
		}

		public BOP BOP(ISeries<double> input, int smooth)
		{
			if (cacheBOP != null)
				for (int idx = 0; idx < cacheBOP.Length; idx++)
					if (cacheBOP[idx] != null && cacheBOP[idx].Smooth == smooth && cacheBOP[idx].EqualsInput(input))
						return cacheBOP[idx];
			return CacheIndicator<BOP>(new BOP(){ Smooth = smooth }, input, ref cacheBOP);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.BOP BOP(int smooth)
		{
			return indicator.BOP(Input, smooth);
		}

		public Indicators.BOP BOP(ISeries<double> input , int smooth)
		{
			return indicator.BOP(input, smooth);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.BOP BOP(int smooth)
		{
			return indicator.BOP(Input, smooth);
		}

		public Indicators.BOP BOP(ISeries<double> input , int smooth)
		{
			return indicator.BOP(input, smooth);
		}
	}
}

#endregion
