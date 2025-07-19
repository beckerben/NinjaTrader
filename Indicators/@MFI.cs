//
// Copyright (C) 2025, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
#region Using declarations
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Cbi;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

// This namespace holds indicators in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators
{
	/// <summary>
	/// The MFI (Money Flow Index) is a momentum indicator that measures the strength of money flowing in and out of a security.
	/// </summary>
	public class MFI : Indicator
	{
		private	Series<double>		negative;
		private	Series<double>		positive;
		private	SUM					sumNegative;
		private SUM					sumPositive;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= Custom.Resource.NinjaScriptIndicatorDescriptionMFI;
				Name						= Custom.Resource.NinjaScriptIndicatorNameMFI;
				IsSuspendedWhileInactive	= true;
				Period						= 14;
				DrawOnPricePanel			= false;

				AddPlot(Brushes.DarkCyan,		Custom.Resource.NinjaScriptIndicatorNameMFI);
				AddLine(Brushes.SlateBlue,	20,	Custom.Resource.NinjaScriptIndicatorLower);
				AddLine(Brushes.Goldenrod,	80,	Custom.Resource.NinjaScriptIndicatorUpper);
			}
			else if (State == State.DataLoaded)
			{
				negative		= new Series<double>(this);
				positive		= new Series<double>(this);
				sumNegative		= SUM(negative, Period);
				sumPositive		= SUM(positive, Period);
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
			if (CurrentBar == 0)
				Value[0] = 50;
			else
			{
				double typical0		= Typical[0];
				double typical1		= Typical[1];
				double volume0		= Instrument.MasterInstrument.InstrumentType == InstrumentType.CryptoCurrency ? Core.Globals.ToCryptocurrencyVolume((long)Volume[0]) : Volume[0];
				negative[0]			= typical0 < typical1 ? typical0 * volume0 : 0;
				positive[0]			= typical0 > typical1 ? typical0 * volume0 : 0;

				double sumNegative0	= sumNegative[0];
				Value[0]			= sumNegative0 == 0 ? 50 : 100.0 - 100.0 / (1 + sumPositive[0] / sumNegative0);
			}
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
		private MFI[] cacheMFI;
		public MFI MFI(int period)
		{
			return MFI(Input, period);
		}

		public MFI MFI(ISeries<double> input, int period)
		{
			if (cacheMFI != null)
				for (int idx = 0; idx < cacheMFI.Length; idx++)
					if (cacheMFI[idx] != null && cacheMFI[idx].Period == period && cacheMFI[idx].EqualsInput(input))
						return cacheMFI[idx];
			return CacheIndicator<MFI>(new MFI(){ Period = period }, input, ref cacheMFI);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.MFI MFI(int period)
		{
			return indicator.MFI(Input, period);
		}

		public Indicators.MFI MFI(ISeries<double> input , int period)
		{
			return indicator.MFI(input, period);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.MFI MFI(int period)
		{
			return indicator.MFI(Input, period);
		}

		public Indicators.MFI MFI(ISeries<double> input , int period)
		{
			return indicator.MFI(input, period);
		}
	}
}

#endregion
