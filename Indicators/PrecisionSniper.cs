#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

namespace NinjaTrader.NinjaScript
{
	public enum PrecisionSniperPresetType { Default, Conservative, Aggressive, Scalping }
}

namespace NinjaTrader.NinjaScript.Indicators
{
	/// <summary>
	/// Precision Sniper — 10-Factor Confluence Signal Engine.
	/// Ported from TradingView indicator. Fires buy/sell arrows when a
	/// composite confluence score (0-10) meets a configurable threshold.
	/// </summary>
	public class PrecisionSniper : Indicator
	{
		#region Private Variables

		private EMA emaFast;
		private EMA emaSlow;
		private EMA emaTrend;
		private RSI rsi;
		private MACD macd;
		private ADX adx;
		private DMI dmi;
		private ATR atr;
		private SMA atrSma42;
		private SMA volumeSMA;
		private double cumulativePV;
		private double cumulativeVolume;

		// HTF indicators
		private EMA htfEmaFast;
		private EMA htfEmaSlow;

		#endregion

		#region OnStateChange

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description	= "10-Factor Confluence Signal Engine with Progressive Trailing Stop";
				Name		= "PrecisionSniper";
				Calculate	= Calculate.OnBarClose;
				IsOverlay	= true;
				DisplayInDataBox = true;
				DrawOnPricePanel = true;
				PaintPriceMarkers = true;
				ScaleJustification = NinjaTrader.Gui.Chart.ScaleJustification.Right;
				// Don't let our plot values drive the price-panel autoscale (SignalCode is 1/-1
				// and would otherwise compress the price axis). Price bars still set the scale.
				IsAutoScale = false;

				// Entry Engine defaults
				EmaFastPeriod		= 9;
				EmaSlowPeriod		= 21;
				EmaTrendPeriod		= 55;
				MinConfluenceScore	= 5;
				RsiLength			= 13;

				// MACD defaults
				MacdFast			= 12;
				MacdSlow			= 26;
				MacdSmooth			= 9;

				// ADX defaults
				AdxPeriod			= 14;

				// ATR (used for arrow offset only)
				AtrPeriod			= 14;

				// HTF filter
				HtfBarsPeriod		= BarsPeriodType.Minute;
				HtfValue			= 0; // 0 = disabled

				// Preset
				Preset				= PrecisionSniperPresetType.Default;

				AddPlot(new Stroke(Brushes.LimeGreen, 2), PlotStyle.Line, "FastEMAPlot");
				AddPlot(new Stroke(Brushes.IndianRed, 2), PlotStyle.Line, "SlowEMAPlot");
				AddPlot(new Stroke(Brushes.DimGray, 3), PlotStyle.Dot, "TrendEMAPlot");
				// Entry-price plots on signal bars (NaN otherwise). Price-scale friendly.
				AddPlot(new Stroke(Brushes.LimeGreen, 3), PlotStyle.Dot, "LongEntryPrice");
				AddPlot(new Stroke(Brushes.IndianRed, 3), PlotStyle.Dot, "ShortEntryPrice");
				// SignalCode: 1 long, -1 short, 0 none. Hidden from Y-axis autoscale.
				AddPlot(new Stroke(Brushes.Goldenrod, 1), PlotStyle.Dot, "SignalCode");
			}
			else if (State == State.Configure)
			{
				ApplyPreset();

				if (HtfValue > 0)
				{
					AddDataSeries(HtfBarsPeriod, HtfValue);
				}
			}
			else if (State == State.DataLoaded)
			{
				emaFast		= EMA(Close, EmaFastPeriod);
				emaSlow		= EMA(Close, EmaSlowPeriod);
				emaTrend	= EMA(Close, EmaTrendPeriod);
				rsi			= RSI(Close, RsiLength, 3);
				macd		= MACD(Close, MacdFast, MacdSlow, MacdSmooth);
				adx			= ADX(AdxPeriod);
				dmi			= DMI(AdxPeriod);
				atr			= ATR(AtrPeriod);
				atrSma42	= SMA(atr, 42);
				volumeSMA	= SMA(Volume, 20);
			}
		}

		#endregion

		#region OnBarUpdate

		protected override void OnBarUpdate()
		{
			if (CurrentBars[0] < Math.Max(EmaTrendPeriod, 50))
				return; // Warmup period

			if (BarsInProgress != 0)
				return; // Only process primary series

			// --- Core Indicator Values ---
			double emaFastVal	= emaFast[0];
			double emaSlowVal	= emaSlow[0];
			double emaTrendVal	= emaTrend[0];
			double rsiVal		= rsi[0];
			double macdVal		= macd[0];
			double macdSignal	= macd.Avg[0];
			double macdHist		= macd.Diff[0];
			double adxVal		= adx[0];
			double dmiVal		= dmi[0];
			double atrVal		= atr[0];
			double atrSmaVal	= atrSma42[0];
			double volSMA		= volumeSMA[0];
			double close		= Close[0];
			double hlc3			= (High[0] + Low[0] + Close[0]) / 3.0;

			// Session VWAP approximation: cumulative(hlc3 * volume) / cumulative(volume)
			// Reset each trading session, which is closer to TradingView's ta.vwap behavior than plain hlc3.
			bool hasVol = Volume[0] > 0;
			if (Bars.IsFirstBarOfSession)
			{
				cumulativePV = 0;
				cumulativeVolume = 0;
			}
			if (hasVol)
			{
				cumulativePV += hlc3 * Volume[0];
				cumulativeVolume += Volume[0];
			}
			double vwap = cumulativeVolume > 0 ? cumulativePV / cumulativeVolume : hlc3;

			// --- 10-Factor Confluence Scoring ---
			double bullScore = 0;
			double bearScore = 0;

			// Factor 1: EMA Fast vs Slow direction
			if (emaFastVal > emaSlowVal) bullScore += 1.0;
			if (emaFastVal < emaSlowVal) bearScore += 1.0;

			// Factor 2: Close vs EMA Trend (macro direction)
			if (close > emaTrendVal) bullScore += 1.0;
			if (close < emaTrendVal) bearScore += 1.0;

			// Factor 3: RSI zone (not extreme)
			if (rsiVal > 50 && rsiVal < 75) bullScore += 1.0;
			if (rsiVal < 50 && rsiVal > 25) bearScore += 1.0;

			// Factor 4: MACD Histogram direction
			if (macdHist > 0) bullScore += 1.0;
			if (macdHist < 0) bearScore += 1.0;

			// Factor 5: MACD vs Signal line
			if (macdVal > macdSignal) bullScore += 1.0;
			if (macdVal < macdSignal) bearScore += 1.0;

			// Factor 6: Close vs VWAP
			// If no volume data is available, auto-pass to mirror TV behavior on volume-less instruments.
			if (hasVol && cumulativeVolume > 0)
			{
				if (close > vwap) bullScore += 1.0;
				if (close < vwap) bearScore += 1.0;
			}
			else
			{
				bullScore += 1.0;
				bearScore += 1.0;
			}

			// Factor 7: Volume confirmation
			if (hasVol && volSMA > 0 && Volume[0] > volSMA * 1.2)
			{
				bullScore += 1.0;
				bearScore += 1.0;
			}
			else if (!hasVol)
			{
				// No volume data: auto-pass
				bullScore += 1.0;
				bearScore += 1.0;
			}

			// Factor 8: ADX trending + DI alignment
			if (adxVal > 20 && dmiVal > 0) bullScore += 1.0;
			if (adxVal > 20 && dmiVal < 0) bearScore += 1.0;

			// Factor 9: HTF EMA bias (1.5x weight)
			if (HtfValue > 0 && BarsArray.Length >= 2)
			{
				double htfBias = GetHTFBias();
				if (htfBias > 0) bullScore += 1.5;
				if (htfBias < 0) bearScore += 1.5;
			}

			// Factor 10: Close vs EMA Fast (minor confirmation, 0.5x)
			if (close > emaFastVal) bullScore += 0.5;
			if (close < emaFastVal) bearScore += 0.5;

			// --- EMA Crossover Detection ---
			bool bullCross = emaFast[1] <= emaSlow[1] && emaFast[0] > emaSlow[0];
			bool bearCross = emaFast[1] >= emaSlow[1] && emaFast[0] < emaSlow[0];

			// --- Signal Trigger ---
			bool bullSignal = bullCross
				&& close > emaFastVal
				&& close > emaSlowVal
				&& rsiVal > 50 && rsiVal < 75
				&& bullScore >= MinConfluenceScore;

			bool bearSignal = bearCross
				&& close < emaFastVal
				&& close < emaSlowVal
				&& rsiVal < 50 && rsiVal > 25
				&& bearScore >= MinConfluenceScore;

			// --- Data Box plot values ---
			Values[0][0] = emaFastVal;
			Values[1][0] = emaSlowVal;
			Values[2][0] = emaTrendVal;
			Values[3][0] = bullSignal ? close : double.NaN;
			Values[4][0] = bearSignal ? close : double.NaN;
			Values[5][0] = bullSignal ? 1 : bearSignal ? -1 : 0;

			// --- Draw signal arrows ---
			if (bullSignal)
			{
				double arrowY = Low[0] - Math.Max(4 * TickSize, atrVal * 0.25);
				Draw.ArrowUp(this, "Signal" + CurrentBar, true, 0, arrowY, Brushes.LimeGreen);
			}
			else if (bearSignal)
			{
				double arrowY = High[0] + Math.Max(4 * TickSize, atrVal * 0.25);
				Draw.ArrowDown(this, "Signal" + CurrentBar, true, 0, arrowY, Brushes.Red);
			}
		}

		#endregion

		#region HTF Bias

		private double GetHTFBias()
		{
			if (HtfValue <= 0 || BarsArray.Length < 2)
				return 0;

			try
			{
				if (CurrentBars.Length < 2 || CurrentBars[1] < Math.Max(EmaFastPeriod, EmaSlowPeriod) + 1)
					return 0;

				if (htfEmaFast == null || htfEmaSlow == null)
				{
					htfEmaFast = EMA(BarsArray[1], EmaFastPeriod);
					htfEmaSlow = EMA(BarsArray[1], EmaSlowPeriod);
				}

				// Use [1] to avoid repainting (previous closed HTF bar)
				double htfFast = htfEmaFast[1];
				double htfSlow = htfEmaSlow[1];

				if (htfFast > htfSlow) return 1;
				if (htfFast < htfSlow) return -1;
				return 0;
			}
			catch
			{
				return 0;
			}
		}

		#endregion

		#region Helpers

		private void ApplyPreset()
		{
			switch (Preset)
			{
				case PrecisionSniperPresetType.Conservative:
					MinConfluenceScore = 7;
					break;
				case PrecisionSniperPresetType.Aggressive:
					MinConfluenceScore = 3;
					break;
				case PrecisionSniperPresetType.Scalping:
					MinConfluenceScore = 4;
					EmaFastPeriod = 5;
					EmaSlowPeriod = 13;
					EmaTrendPeriod = 34;
					break;
				case PrecisionSniperPresetType.Default:
				default:
					break;
			}
		}

		#endregion

		#region Properties

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "EMA Fast Period", Description = "Fast EMA period", Order = 1, GroupName = "Entry Engine")]
		public int EmaFastPeriod { get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "EMA Slow Period", Description = "Slow EMA period", Order = 2, GroupName = "Entry Engine")]
		public int EmaSlowPeriod { get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "EMA Trend Period", Description = "Macro trend EMA period", Order = 3, GroupName = "Entry Engine")]
		public int EmaTrendPeriod { get; set; }

		[NinjaScriptProperty]
		[Range(1, 10)]
		[Display(Name = "Min Confluence Score", Description = "Minimum score (out of 10) to trigger signal", Order = 4, GroupName = "Entry Engine")]
		public int MinConfluenceScore { get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "RSI Length", Description = "RSI period", Order = 5, GroupName = "Entry Engine")]
		public int RsiLength { get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "MACD Fast", Description = "MACD fast period", Order = 6, GroupName = "Entry Engine")]
		public int MacdFast { get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "MACD Slow", Description = "MACD slow period", Order = 7, GroupName = "Entry Engine")]
		public int MacdSlow { get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "MACD Smooth", Description = "MACD signal smoothing period", Order = 8, GroupName = "Entry Engine")]
		public int MacdSmooth { get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "ADX Period", Description = "ADX/DI period", Order = 9, GroupName = "Entry Engine")]
		public int AdxPeriod { get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "ATR Period", Description = "ATR period (used for arrow offset & volatility scoring)", Order = 1, GroupName = "Risk Management")]
		public int AtrPeriod { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "HTF Bars Period Type", Description = "Higher timeframe period type for trend bias", Order = 1, GroupName = "HTF Filter")]
		public BarsPeriodType HtfBarsPeriod { get; set; }

		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name = "HTF Value", Description = "Higher timeframe value (0 = disabled)", Order = 2, GroupName = "HTF Filter")]
		public int HtfValue { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Preset", Description = "Parameter preset", Order = 1, GroupName = "Preset")]
		public PrecisionSniperPresetType Preset { get; set; }

		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private PrecisionSniper[] cachePrecisionSniper;
		public PrecisionSniper PrecisionSniper(int emaFastPeriod, int emaSlowPeriod, int emaTrendPeriod, int minConfluenceScore, int rsiLength, int macdFast, int macdSlow, int macdSmooth, int adxPeriod, int atrPeriod, BarsPeriodType htfBarsPeriod, int htfValue, PrecisionSniperPresetType preset)
		{
			return PrecisionSniper(Input, emaFastPeriod, emaSlowPeriod, emaTrendPeriod, minConfluenceScore, rsiLength, macdFast, macdSlow, macdSmooth, adxPeriod, atrPeriod, htfBarsPeriod, htfValue, preset);
		}

		public PrecisionSniper PrecisionSniper(ISeries<double> input, int emaFastPeriod, int emaSlowPeriod, int emaTrendPeriod, int minConfluenceScore, int rsiLength, int macdFast, int macdSlow, int macdSmooth, int adxPeriod, int atrPeriod, BarsPeriodType htfBarsPeriod, int htfValue, PrecisionSniperPresetType preset)
		{
			if (cachePrecisionSniper != null)
				for (int idx = 0; idx < cachePrecisionSniper.Length; idx++)
					if (cachePrecisionSniper[idx] != null && cachePrecisionSniper[idx].EmaFastPeriod == emaFastPeriod && cachePrecisionSniper[idx].EmaSlowPeriod == emaSlowPeriod && cachePrecisionSniper[idx].EmaTrendPeriod == emaTrendPeriod && cachePrecisionSniper[idx].MinConfluenceScore == minConfluenceScore && cachePrecisionSniper[idx].RsiLength == rsiLength && cachePrecisionSniper[idx].MacdFast == macdFast && cachePrecisionSniper[idx].MacdSlow == macdSlow && cachePrecisionSniper[idx].MacdSmooth == macdSmooth && cachePrecisionSniper[idx].AdxPeriod == adxPeriod && cachePrecisionSniper[idx].AtrPeriod == atrPeriod && cachePrecisionSniper[idx].HtfBarsPeriod == htfBarsPeriod && cachePrecisionSniper[idx].HtfValue == htfValue && cachePrecisionSniper[idx].Preset == preset && cachePrecisionSniper[idx].EqualsInput(input))
						return cachePrecisionSniper[idx];
			return CacheIndicator<PrecisionSniper>(new PrecisionSniper(){ EmaFastPeriod = emaFastPeriod, EmaSlowPeriod = emaSlowPeriod, EmaTrendPeriod = emaTrendPeriod, MinConfluenceScore = minConfluenceScore, RsiLength = rsiLength, MacdFast = macdFast, MacdSlow = macdSlow, MacdSmooth = macdSmooth, AdxPeriod = adxPeriod, AtrPeriod = atrPeriod, HtfBarsPeriod = htfBarsPeriod, HtfValue = htfValue, Preset = preset }, input, ref cachePrecisionSniper);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.PrecisionSniper PrecisionSniper(int emaFastPeriod, int emaSlowPeriod, int emaTrendPeriod, int minConfluenceScore, int rsiLength, int macdFast, int macdSlow, int macdSmooth, int adxPeriod, int atrPeriod, BarsPeriodType htfBarsPeriod, int htfValue, PrecisionSniperPresetType preset)
		{
			return indicator.PrecisionSniper(Input, emaFastPeriod, emaSlowPeriod, emaTrendPeriod, minConfluenceScore, rsiLength, macdFast, macdSlow, macdSmooth, adxPeriod, atrPeriod, htfBarsPeriod, htfValue, preset);
		}

		public Indicators.PrecisionSniper PrecisionSniper(ISeries<double> input , int emaFastPeriod, int emaSlowPeriod, int emaTrendPeriod, int minConfluenceScore, int rsiLength, int macdFast, int macdSlow, int macdSmooth, int adxPeriod, int atrPeriod, BarsPeriodType htfBarsPeriod, int htfValue, PrecisionSniperPresetType preset)
		{
			return indicator.PrecisionSniper(input, emaFastPeriod, emaSlowPeriod, emaTrendPeriod, minConfluenceScore, rsiLength, macdFast, macdSlow, macdSmooth, adxPeriod, atrPeriod, htfBarsPeriod, htfValue, preset);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.PrecisionSniper PrecisionSniper(int emaFastPeriod, int emaSlowPeriod, int emaTrendPeriod, int minConfluenceScore, int rsiLength, int macdFast, int macdSlow, int macdSmooth, int adxPeriod, int atrPeriod, BarsPeriodType htfBarsPeriod, int htfValue, PrecisionSniperPresetType preset)
		{
			return indicator.PrecisionSniper(Input, emaFastPeriod, emaSlowPeriod, emaTrendPeriod, minConfluenceScore, rsiLength, macdFast, macdSlow, macdSmooth, adxPeriod, atrPeriod, htfBarsPeriod, htfValue, preset);
		}

		public Indicators.PrecisionSniper PrecisionSniper(ISeries<double> input , int emaFastPeriod, int emaSlowPeriod, int emaTrendPeriod, int minConfluenceScore, int rsiLength, int macdFast, int macdSlow, int macdSmooth, int adxPeriod, int atrPeriod, BarsPeriodType htfBarsPeriod, int htfValue, PrecisionSniperPresetType preset)
		{
			return indicator.PrecisionSniper(input, emaFastPeriod, emaSlowPeriod, emaTrendPeriod, minConfluenceScore, rsiLength, macdFast, macdSlow, macdSmooth, adxPeriod, atrPeriod, htfBarsPeriod, htfValue, preset);
		}
	}
}

#endregion
