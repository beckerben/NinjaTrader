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
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators
{
	/// <summary>
	/// The Money Flow Oscillator measures the amount of money flow volume over a specific period. A move into positive territory indicates buying pressure while a move into negative territory indicates selling pressure.
	/// </summary>
	public class MoneyFlowOscillator : Indicator
	{
		private Series<double> mfv;
		private double dvs, mltp;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= Custom.Resource.NinjaScriptIndicatorDescriptionMoneyFlowOscillator;
				Name						= Custom.Resource.NinjaScriptIndicatorNameMoneyFlowOscillator;
				IsOverlay					= false;
				DrawOnPricePanel			= false;
				IsSuspendedWhileInactive	= true;
				Period						= 20;
				
				AddPlot(Brushes.DodgerBlue, Custom.Resource.NinjaScriptIndicatorMoneyFlowLine);
				AddLine(Brushes.DarkGray, 0, Custom.Resource.NinjaScriptIndicatorZeroLine);
			}
			else if (State == State.DataLoaded)
				mfv = new Series<double>(this);
		}

		protected override void OnBarUpdate()
		{
			if(CurrentBar > 0)
			{
				dvs 	= High[0] - Low[1] + (High[1] - Low[0]) == 0 ? 0.00001 : High[0] - Low[1] + (High[1] - Low[0]);
				mltp 	= Math.Round(High[0] < Low[1] ? -1: Low[0] > High[1] ? 1 : (High[0] - Low[1] - (High[1] - Low[0])) / dvs, 2);
				mfv[0] 	= mltp * (Instrument.MasterInstrument.InstrumentType == Cbi.InstrumentType.CryptoCurrency ? Core.Globals.ToCryptocurrencyVolume((long)Volume[0]) : Volume[0]);

				if (CurrentBar >= Period)
				{
					double sumVolume = SUM(Volume, Period)[0];
					if (Instrument.MasterInstrument.InstrumentType == Cbi.InstrumentType.CryptoCurrency)
						sumVolume = Core.Globals.ToCryptocurrencyVolume((long)sumVolume);
					Values[0][0] = Math.Round(SUM(mfv, Period)[0] / sumVolume, 3);
				}
			}
			else
				mfv[0] = 0;
		}

		#region Properties
		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Period", GroupName = "NinjaScriptParameters", Order = 0)]
		public int Period { get; set; }

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> MoneyFlow => Values[0];
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private MoneyFlowOscillator[] cacheMoneyFlowOscillator;
		public MoneyFlowOscillator MoneyFlowOscillator(int period)
		{
			return MoneyFlowOscillator(Input, period);
		}

		public MoneyFlowOscillator MoneyFlowOscillator(ISeries<double> input, int period)
		{
			if (cacheMoneyFlowOscillator != null)
				for (int idx = 0; idx < cacheMoneyFlowOscillator.Length; idx++)
					if (cacheMoneyFlowOscillator[idx] != null && cacheMoneyFlowOscillator[idx].Period == period && cacheMoneyFlowOscillator[idx].EqualsInput(input))
						return cacheMoneyFlowOscillator[idx];
			return CacheIndicator<MoneyFlowOscillator>(new MoneyFlowOscillator(){ Period = period }, input, ref cacheMoneyFlowOscillator);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.MoneyFlowOscillator MoneyFlowOscillator(int period)
		{
			return indicator.MoneyFlowOscillator(Input, period);
		}

		public Indicators.MoneyFlowOscillator MoneyFlowOscillator(ISeries<double> input , int period)
		{
			return indicator.MoneyFlowOscillator(input, period);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.MoneyFlowOscillator MoneyFlowOscillator(int period)
		{
			return indicator.MoneyFlowOscillator(Input, period);
		}

		public Indicators.MoneyFlowOscillator MoneyFlowOscillator(ISeries<double> input , int period)
		{
			return indicator.MoneyFlowOscillator(input, period);
		}
	}
}

#endregion
