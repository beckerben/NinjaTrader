//
// Copyright (C) 2025, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
#region Using declarations
using System;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
#endregion

// This namespace holds indicators in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators
{
	public class PsychologicalLine : Indicator
	{
		private double	prevUpBars;
		private int		saveCurrentBar;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description = Custom.Resource.NinjaScriptIndicatorDescriptionPsychologicalLine;
				Name		= Custom.Resource.NinjaScriptIndicatorNamePsychologicalLine;
				IsOverlay	= false;
				Period		= 10;

				AddPlot(Brushes.DodgerBlue,		Custom.Resource.NinjaScriptIndicatorNamePsychologicalLine);
				AddLine(Brushes.DarkCyan,		75,	Custom.Resource.NinjaScriptIndicatorOverBoughtLine);
				AddLine(Brushes.DarkCyan,		25,	Custom.Resource.NinjaScriptIndicatorOverSoldLine);
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar > saveCurrentBar)
				prevUpBars = prevUpBars + (Close[1] > Open[1] ? 1 : 0) - (CurrentBar <= Period - 1 ? 0 : Close[Period] > Open[Period] ? 1 : 0);
			else if (BarsArray[0].BarsType.IsRemoveLastBarSupported && saveCurrentBar < CurrentBar)
			{
				prevUpBars = 0;
				for (int barsBack = Math.Min(CurrentBar, Period - 1); barsBack > 0; barsBack--)
					if (Close[barsBack] > Open[barsBack])
						prevUpBars++;
			}

			Value[0]		= (prevUpBars + (Close[0] > Open[0] ? 1 : 0)) / Math.Min(CurrentBar + 1, Period) * 100;
			saveCurrentBar	= CurrentBar;
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
		private PsychologicalLine[] cachePsychologicalLine;
		public PsychologicalLine PsychologicalLine(int period)
		{
			return PsychologicalLine(Input, period);
		}

		public PsychologicalLine PsychologicalLine(ISeries<double> input, int period)
		{
			if (cachePsychologicalLine != null)
				for (int idx = 0; idx < cachePsychologicalLine.Length; idx++)
					if (cachePsychologicalLine[idx] != null && cachePsychologicalLine[idx].Period == period && cachePsychologicalLine[idx].EqualsInput(input))
						return cachePsychologicalLine[idx];
			return CacheIndicator<PsychologicalLine>(new PsychologicalLine(){ Period = period }, input, ref cachePsychologicalLine);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.PsychologicalLine PsychologicalLine(int period)
		{
			return indicator.PsychologicalLine(Input, period);
		}

		public Indicators.PsychologicalLine PsychologicalLine(ISeries<double> input , int period)
		{
			return indicator.PsychologicalLine(input, period);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.PsychologicalLine PsychologicalLine(int period)
		{
			return indicator.PsychologicalLine(Input, period);
		}

		public Indicators.PsychologicalLine PsychologicalLine(ISeries<double> input , int period)
		{
			return indicator.PsychologicalLine(input, period);
		}
	}
}

#endregion

