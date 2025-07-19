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
	/// <summary>
	/// The TSI (True Strength Index) is a momentum-based indicator, developed by William Blau.
	/// Designed to determine both trend and overbought/oversold conditions, the TSI is
	/// applicable to intraday time frames as well as long term trading.
	/// </summary>
	public class TSI : Indicator
	{
		private double				constant1;
		private	double				constant2;
		private double				constant3;
		private double				constant4;
		private Series<double>		fastEma;
		private Series<double>		fastAbsEma;
		private Series<double>		slowEma;
		private Series<double>		slowAbsEma;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= Custom.Resource.NinjaScriptIndicatorDescriptionTSI;
				Name						= Custom.Resource.NinjaScriptIndicatorNameTSI;
				Fast						= 3;
				IsSuspendedWhileInactive	= true;
				Slow						= 14;

				AddPlot(Brushes.DarkCyan, Custom.Resource.NinjaScriptIndicatorNameTSI);
			}
			else if (State == State.Configure)
			{
				constant1	= 2.0 / (1 + Slow);
				constant2	= 1 - 2.0 / (1 + Slow);
				constant3	= 2.0 / (1 + Fast);
				constant4	= 1 - 2.0 / (1 + Fast);
			}
			else if (State == State.DataLoaded)
			{
				fastAbsEma	= new Series<double>(this);
				fastEma		= new Series<double>(this);
				slowAbsEma	= new Series<double>(this);
				slowEma		= new Series<double>(this);
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar == 0)
			{
				fastAbsEma[0]	= 0;
				fastEma[0]		= 0;
				slowAbsEma[0]	= 0;
				slowEma[0]		= 0;
				Value[0]		= 0;
			}
			else
			{
				double momentum	= Input[0] - Input[1];
				slowEma[0]		= momentum * constant1 + constant2 * slowEma[1];
				fastEma[0]		= slowEma[0] * constant3 + constant4 * fastEma[1];
				slowAbsEma[0]	= Math.Abs(momentum) * constant1 + constant2 * slowAbsEma[1];
				fastAbsEma[0]	= slowAbsEma[0] * constant3 + constant4 * fastAbsEma[1];
				Value[0]		= fastAbsEma[0] == 0 ? 0 : 100 * fastEma[0] / fastAbsEma[0];
			}
		}

		#region Properties

		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof (Custom.Resource), Name = "Fast", GroupName = "NinjaScriptParameters", Order = 0)]
		public int Fast { get; set; }

		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Slow", GroupName = "NinjaScriptParameters", Order = 1)]
		public int Slow { get; set; }
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private TSI[] cacheTSI;
		public TSI TSI(int fast, int slow)
		{
			return TSI(Input, fast, slow);
		}

		public TSI TSI(ISeries<double> input, int fast, int slow)
		{
			if (cacheTSI != null)
				for (int idx = 0; idx < cacheTSI.Length; idx++)
					if (cacheTSI[idx] != null && cacheTSI[idx].Fast == fast && cacheTSI[idx].Slow == slow && cacheTSI[idx].EqualsInput(input))
						return cacheTSI[idx];
			return CacheIndicator<TSI>(new TSI(){ Fast = fast, Slow = slow }, input, ref cacheTSI);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.TSI TSI(int fast, int slow)
		{
			return indicator.TSI(Input, fast, slow);
		}

		public Indicators.TSI TSI(ISeries<double> input , int fast, int slow)
		{
			return indicator.TSI(input, fast, slow);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.TSI TSI(int fast, int slow)
		{
			return indicator.TSI(Input, fast, slow);
		}

		public Indicators.TSI TSI(ISeries<double> input , int fast, int slow)
		{
			return indicator.TSI(input, fast, slow);
		}
	}
}

#endregion
