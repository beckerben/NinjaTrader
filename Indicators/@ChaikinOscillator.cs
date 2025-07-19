//
// Copyright (C) 2025, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
#region Using declarations
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Cbi;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

// This namespace holds indicators in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators
{
	/// <summary>
	/// Chaikin Oscillator.
	/// </summary>
	public class ChaikinOscillator : Indicator
	{
		private Series<double>	cummulative;
		private EMA				emaFast;
		private EMA				emaSlow;
		private Series<double>	moneyFlow;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= Custom.Resource.NinjaScriptIndicatorDescriptionChaikinOscillator;
				Name						= Custom.Resource.NinjaScriptIndicatorNameChaikinOscillator;
				Fast						= 3;
				DrawOnPricePanel			= false;
				IsSuspendedWhileInactive	= true;
				Slow						= 10;

				AddPlot(Brushes.Goldenrod, Custom.Resource.NinjaScriptIndicatorNameChaikinOscillator);
			}
			else if (State == State.DataLoaded)
			{
				cummulative		= new Series<double>(this);
				moneyFlow		= new Series<double>(this);
				emaFast			= EMA(cummulative, Fast);
				emaSlow			= EMA(cummulative, Slow);
			}
			else if (State == State.Historical)
			{
				if (Calculate == Calculate.OnPriceChange)
				{
					Draw.TextFixed(this, "NinjaScriptInfo", string.Format(Custom.Resource.NinjaScriptOnPriceChangeError, Name), TextPosition.BottomRight);
					Log(string.Format(Custom.Resource.NinjaScriptOnPriceChangeError, Name), LogLevel.Error);
				}
			}
		}

		protected override void OnBarUpdate()
		{
			double close0	= Close[0];
			double low0		= Low[0];
			double high0	= High[0];
			double volume0	= Instrument.MasterInstrument.InstrumentType == InstrumentType.CryptoCurrency ? Core.Globals.ToCryptocurrencyVolume((long)Volume[0]) : Volume[0];

			moneyFlow[0]	= volume0 * (close0 - low0 - (high0 - close0)) / ((high0 - low0).ApproxCompare(0) == 0 ? 1 : high0 - low0);
			cummulative[0]	= moneyFlow[0] + (CurrentBar == 0 ? 0 : cummulative[1]);
			Value[0]		= emaFast[0] - emaSlow[0];
		}

		#region Properties
		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Fast", GroupName = "NinjaScriptParameters", Order = 0)]
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
		private ChaikinOscillator[] cacheChaikinOscillator;
		public ChaikinOscillator ChaikinOscillator(int fast, int slow)
		{
			return ChaikinOscillator(Input, fast, slow);
		}

		public ChaikinOscillator ChaikinOscillator(ISeries<double> input, int fast, int slow)
		{
			if (cacheChaikinOscillator != null)
				for (int idx = 0; idx < cacheChaikinOscillator.Length; idx++)
					if (cacheChaikinOscillator[idx] != null && cacheChaikinOscillator[idx].Fast == fast && cacheChaikinOscillator[idx].Slow == slow && cacheChaikinOscillator[idx].EqualsInput(input))
						return cacheChaikinOscillator[idx];
			return CacheIndicator<ChaikinOscillator>(new ChaikinOscillator(){ Fast = fast, Slow = slow }, input, ref cacheChaikinOscillator);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.ChaikinOscillator ChaikinOscillator(int fast, int slow)
		{
			return indicator.ChaikinOscillator(Input, fast, slow);
		}

		public Indicators.ChaikinOscillator ChaikinOscillator(ISeries<double> input , int fast, int slow)
		{
			return indicator.ChaikinOscillator(input, fast, slow);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.ChaikinOscillator ChaikinOscillator(int fast, int slow)
		{
			return indicator.ChaikinOscillator(Input, fast, slow);
		}

		public Indicators.ChaikinOscillator ChaikinOscillator(ISeries<double> input , int fast, int slow)
		{
			return indicator.ChaikinOscillator(input, fast, slow);
		}
	}
}

#endregion
