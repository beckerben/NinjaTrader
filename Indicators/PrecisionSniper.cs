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
	/// Precision Sniper — 10-Factor Confluence Signal Engine with Progressive Trailing Stop
	/// Ported from TradingView indicator.
	/// 
	/// Fires buy/sell signals only when a composite confluence score (0-10) 
	/// meets a configurable threshold. Each signal includes auto-placed TP/SL
	/// with structure-based stops and a progressive trailing stop system.
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
		private SimpleFont signalFont;
		private double cumulativePV;
		private double cumulativeVolume;

		// HTF indicators
		private EMA htfEmaFast;
		private EMA htfEmaSlow;

		// Trade state
		private enum TradeState { None, Active, TP1Hit, TP2Hit, TP3Hit, SLHit }
		private TradeState currentTradeState = TradeState.None;
		private TradeDirection currentDirection = TradeDirection.None;
		private double entryPrice = 0;
		private double stopLoss = 0;
		private double tp1 = 0;
		private double tp2 = 0;
		private double tp3 = 0;
		private double trailingStop = 0;
		private int entryBar = -1;
		private double confluenceScore = 0;
		private double prevTrailingStop = 0;

		// Swing detection
		private double recentSwingHigh = 0;
		private double recentSwingLow = 0;

		private enum TradeDirection { None, Long, Short }

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

				// Risk Management defaults
				AtrPeriod			= 14;
				SlAtrMultiplier		= 1.5;
				Tp1RR				= 1.0;
				Tp2RR				= 2.0;
				Tp3RR				= 3.0;
				TrailingStopEnabled	= true;
				StructureBasedSL	= true;
				SwingLookback		= 10;
				SlMinATRDistance	= 0.5;

				// HTF filter
				HtfBarsPeriod		= BarsPeriodType.Minute;
				HtfValue			= 0; // 0 = disabled

				// Preset
				Preset				= PrecisionSniperPresetType.Default;

				// Visual
				ShowRibbon			= true;
				ShowTPSLLines		= true;
				ShowTrailingStop	= true;
				ShowDashboard		= true;
				ShowBackgroundTint	= false;

				AddPlot(new Stroke(Brushes.LimeGreen, 2), PlotStyle.Line, "FastEMAPlot");
				AddPlot(new Stroke(Brushes.IndianRed, 2), PlotStyle.Line, "SlowEMAPlot");
				AddPlot(new Stroke(Brushes.DimGray, 3), PlotStyle.Dot, "TrendEMAPlot");
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
				signalFont	= new SimpleFont { Size = 12 };
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

			// Direction lock + mutual exclusion (buy takes priority)
			if (bullSignal && currentDirection != TradeDirection.Long)
			{
				OpenTrade(TradeDirection.Long, close, bullScore);
			}
			else if (bearSignal && currentDirection != TradeDirection.Short)
			{
				OpenTrade(TradeDirection.Short, close, bearScore);
			}

			// --- Volatility Regime ---
			double baselineAtr = atrSmaVal > 0 ? atrSmaVal : atrVal;
			double volRatio = atrVal / baselineAtr;
			string volRegime = volRatio > 1.3 ? "High" : volRatio < 0.7 ? "Low" : "Normal";

			// --- Trade Monitoring ---
			if (currentTradeState != TradeState.None && currentTradeState != TradeState.SLHit)
			{
				MonitorTrade(close, atrVal);
			}

			// --- Visual: EMA Ribbon ---
			if (ShowRibbon)
			{
				Values[0][0] = emaFastVal;
				Values[1][0] = emaSlowVal;
				Values[2][0] = emaTrendVal;
			}
			else
			{
				Values[0][0] = double.NaN;
				Values[1][0] = double.NaN;
				Values[2][0] = double.NaN;
			}

			// --- Visual: TP/SL Lines ---
			if (ShowTPSLLines && currentTradeState != TradeState.None && currentTradeState != TradeState.SLHit)
			{
				int barsSinceEntry = Math.Max(1, CurrentBar - entryBar);
				Draw.Line(this, "Entry", false, barsSinceEntry, entryPrice, 0, entryPrice, Brushes.DodgerBlue, DashStyleHelper.Solid, 1);
				Draw.Line(this, "SL", false, barsSinceEntry, stopLoss, 0, stopLoss, Brushes.Red, DashStyleHelper.Solid, 1);
				Draw.Line(this, "TP1", false, barsSinceEntry, tp1, 0, tp1, Brushes.LimeGreen, DashStyleHelper.Dash, 1);
				Draw.Line(this, "TP2", false, barsSinceEntry, tp2, 0, tp2, Brushes.LimeGreen, DashStyleHelper.Dash, 1);
				Draw.Line(this, "TP3", false, barsSinceEntry, tp3, 0, tp3, Brushes.LimeGreen, DashStyleHelper.Dash, 1);
			}

			// --- Visual: Trailing Stop Line ---
			if (ShowTrailingStop && (currentTradeState == TradeState.TP1Hit 
				|| currentTradeState == TradeState.TP2Hit || currentTradeState == TradeState.TP3Hit))
			{
				int barsSinceEntry = Math.Max(1, CurrentBar - entryBar);
				Draw.Line(this, "Trail", false, barsSinceEntry, trailingStop, 0, trailingStop, Brushes.Orange, DashStyleHelper.Dot, 2);
			}

			// --- Visual: Background Tint on Signal ---
			if (ShowBackgroundTint)
			{
				if (bullSignal)
					Draw.Rectangle(this, "BG" + CurrentBar, false, 0, High[0] + atrVal, -1, Low[0] - atrVal, Brushes.Transparent, Brushes.LimeGreen, 20);
				else if (bearSignal)
					Draw.Rectangle(this, "BG" + CurrentBar, false, 0, High[0] + atrVal, -1, Low[0] - atrVal, Brushes.Transparent, Brushes.Red, 20);
			}

			// --- Dashboard ---
			if (ShowDashboard)
			{
				string trendStr = emaFastVal > emaSlowVal ? "Bullish" : emaFastVal < emaSlowVal ? "Bearish" : "Neutral";
				string statusStr = currentTradeState.ToString();
				string htfStr = "N/A";
				if (HtfValue > 0)
				{
					double htfBias = GetHTFBias();
					htfStr = htfBias > 0 ? "Bullish" : htfBias < 0 ? "Bearish" : "Neutral";
				}

				double displayScore = Math.Max(bullScore, bearScore);
				string dashText = string.Format(
					"Trend: {0}  |  Score: {1:F1}/10  |  Status: {2}\n" +
					"HTF Bias: {3}  |  Vol: {4}  |  RSI: {5:F0}  |  ADX: {6:F0}",
					trendStr, displayScore, statusStr, htfStr, volRegime, rsiVal, adxVal);

				Draw.Text(this, "Dashboard", dashText, 0, High[0] + 10 * TickSize, Brushes.White);
			}
		}

		#endregion

		#region Trade Management

		private void OpenTrade(TradeDirection direction, double price, double score)
		{
			currentDirection = direction;
			entryPrice = price;
			entryBar = CurrentBar;
			confluenceScore = score;
			double risk = 0;

			if (direction == TradeDirection.Long)
			{
				if (StructureBasedSL)
				{
					recentSwingLow = FindSwingLow();
					double structureStop = recentSwingLow - 0.2 * atr[0];
					double atrStop = price - atr[0] * SlAtrMultiplier;
					// Pick the HIGHER (tighter) stop
					stopLoss = Math.Max(structureStop, atrStop);
					// Enforce minimum distance
					stopLoss = Math.Min(stopLoss, price - SlMinATRDistance * atr[0]);
				}
				else
				{
					stopLoss = price - atr[0] * SlAtrMultiplier;
				}

				risk = price - stopLoss;
				tp1 = price + risk * Tp1RR;
				tp2 = price + risk * Tp2RR;
				tp3 = price + risk * Tp3RR;
				trailingStop = stopLoss;
				prevTrailingStop = stopLoss;

				Draw.ArrowUp(this, "Signal" + CurrentBar, true, 0, Low[0] - 2 * TickSize, Brushes.LimeGreen);
				Draw.Text(this, "SignalLabel" + CurrentBar, false, string.Format("Long {0}", GetSignalGrade(score)),
					0, Low[0] - 5 * TickSize, 0, Brushes.White, signalFont,
					TextAlignment.Center, Brushes.Transparent, Brushes.LimeGreen, 90);
			}
			else // Short
			{
				if (StructureBasedSL)
				{
					recentSwingHigh = FindSwingHigh();
					double structureStop = recentSwingHigh + 0.2 * atr[0];
					double atrStop = price + atr[0] * SlAtrMultiplier;
					// Pick the LOWER (tighter) stop
					stopLoss = Math.Min(structureStop, atrStop);
					// Enforce minimum distance
					stopLoss = Math.Max(stopLoss, price + SlMinATRDistance * atr[0]);
				}
				else
				{
					stopLoss = price + atr[0] * SlAtrMultiplier;
				}

				risk = stopLoss - price;
				tp1 = price - risk * Tp1RR;
				tp2 = price - risk * Tp2RR;
				tp3 = price - risk * Tp3RR;
				trailingStop = stopLoss;
				prevTrailingStop = stopLoss;

				Draw.ArrowDown(this, "Signal" + CurrentBar, true, 0, High[0] + 2 * TickSize, Brushes.Red);
				Draw.Text(this, "SignalLabel" + CurrentBar, false, string.Format("Short {0}", GetSignalGrade(score)),
					0, High[0] + 5 * TickSize, 0, Brushes.White, signalFont,
					TextAlignment.Center, Brushes.Transparent, Brushes.IndianRed, 90);
			}

			currentTradeState = TradeState.Active;
		}

		private void MonitorTrade(double close, double atrVal)
		{
			if (CurrentBar <= entryBar)
				return;

			prevTrailingStop = trailingStop;
			bool isLong = currentDirection == TradeDirection.Long;

			// Check TPs
			if (currentTradeState == TradeState.Active)
			{
				if ((isLong && close >= tp1) || (!isLong && close <= tp1))
				{
					currentTradeState = TradeState.TP1Hit;
					if (TrailingStopEnabled)
						trailingStop = entryPrice; // Breakeven

					Draw.Diamond(this, "TP1" + CurrentBar, true, 0, tp1, Brushes.LimeGreen);
					Draw.Text(this, "TP1Label" + CurrentBar, "TP1 Hit", 0, 
						isLong ? tp1 + 2 * TickSize : tp1 - 2 * TickSize, Brushes.LimeGreen);
				}
			}

			if (currentTradeState == TradeState.TP1Hit)
			{
				if ((isLong && close >= tp2) || (!isLong && close <= tp2))
				{
					currentTradeState = TradeState.TP2Hit;
					if (TrailingStopEnabled)
						trailingStop = tp1; // Lock TP1 profit

					Draw.Diamond(this, "TP2" + CurrentBar, true, 0, tp2, Brushes.LimeGreen);
					Draw.Text(this, "TP2Label" + CurrentBar, "TP2 Hit", 0, 
						isLong ? tp2 + 2 * TickSize : tp2 - 2 * TickSize, Brushes.LimeGreen);
				}
			}

			if (currentTradeState == TradeState.TP2Hit)
			{
				if ((isLong && close >= tp3) || (!isLong && close <= tp3))
				{
					currentTradeState = TradeState.TP3Hit;
					if (TrailingStopEnabled)
						trailingStop = tp2; // Lock TP2 profit

					Draw.Diamond(this, "TP3" + CurrentBar, true, 0, tp3, Brushes.Gold);
					Draw.Text(this, "TP3Label" + CurrentBar, "TP3 Hit!", 0, 
						isLong ? tp3 + 2 * TickSize : tp3 - 2 * TickSize, Brushes.Gold);
				}
			}

			// Check SL using PRE-UPDATE trail value
			double slCheck = TrailingStopEnabled ? prevTrailingStop : stopLoss;

			if (isLong && close <= slCheck)
			{
				currentTradeState = TradeState.SLHit;
				Draw.Text(this, "SLHit" + CurrentBar, "SL HIT", 0, stopLoss - 5 * TickSize, Brushes.Red);
				ResetTrade();
			}
			else if (!isLong && close >= slCheck)
			{
				currentTradeState = TradeState.SLHit;
				Draw.Text(this, "SLHit" + CurrentBar, "SL HIT", 0, stopLoss + 5 * TickSize, Brushes.Red);
				ResetTrade();
			}
		}

		private void ResetTrade()
		{
			currentDirection = TradeDirection.None;
			currentTradeState = TradeState.None;
			entryPrice = 0;
			stopLoss = 0;
			tp1 = tp2 = tp3 = 0;
			trailingStop = 0;
			entryBar = -1;
		}

		private string GetSignalGrade(double score)
		{
			if (score >= 8.0)
				return "A+";
			if (score >= 6.5)
				return "A";
			if (score >= 5.0)
				return "B";
			return "C";
		}

		#endregion

		#region Swing Detection

		private double FindSwingLow()
		{
			double swingLow = Low[0];
			int lookback = Math.Min(SwingLookback, CurrentBar);
			for (int i = 0; i <= lookback; i++)
			{
				if (Low[i] < swingLow)
					swingLow = Low[i];
			}
			return swingLow;
		}

		private double FindSwingHigh()
		{
			double swingHigh = High[0];
			int lookback = Math.Min(SwingLookback, CurrentBar);
			for (int i = 0; i <= lookback; i++)
			{
				if (High[i] > swingHigh)
					swingHigh = High[i];
			}
			return swingHigh;
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
					SlAtrMultiplier = 2.0;
					break;
				case PrecisionSniperPresetType.Aggressive:
					MinConfluenceScore = 3;
					break;
				case PrecisionSniperPresetType.Scalping:
					MinConfluenceScore = 4;
					SlAtrMultiplier = 1.0;
					EmaFastPeriod = 5;
					EmaSlowPeriod = 13;
					EmaTrendPeriod = 34;
					Tp1RR = 0.75;
					Tp2RR = 1.5;
					Tp3RR = 2.5;
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
		[Range(0.1, double.MaxValue)]
		[Display(Name = "SL ATR Multiplier", Description = "ATR multiplier for stop loss distance", Order = 1, GroupName = "Risk Management")]
		public double SlAtrMultiplier { get; set; }

		[NinjaScriptProperty]
		[Range(0.1, double.MaxValue)]
		[Display(Name = "TP1 R:R", Description = "Risk:Reward ratio for TP1", Order = 2, GroupName = "Risk Management")]
		public double Tp1RR { get; set; }

		[NinjaScriptProperty]
		[Range(0.1, double.MaxValue)]
		[Display(Name = "TP2 R:R", Description = "Risk:Reward ratio for TP2", Order = 3, GroupName = "Risk Management")]
		public double Tp2RR { get; set; }

		[NinjaScriptProperty]
		[Range(0.1, double.MaxValue)]
		[Display(Name = "TP3 R:R", Description = "Risk:Reward ratio for TP3", Order = 4, GroupName = "Risk Management")]
		public double Tp3RR { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Trailing Stop Enabled", Description = "Ratchet SL to BE after TP1, TP1 after TP2, etc.", Order = 5, GroupName = "Risk Management")]
		public bool TrailingStopEnabled { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Structure-Based SL", Description = "Use swing low/high as SL anchor instead of pure ATR", Order = 6, GroupName = "Risk Management")]
		public bool StructureBasedSL { get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Swing Lookback", Description = "Bars to look back for structure SL", Order = 7, GroupName = "Risk Management")]
		public int SwingLookback { get; set; }

		[NinjaScriptProperty]
		[Range(0.1, double.MaxValue)]
		[Display(Name = "SL Min ATR Distance", Description = "Minimum SL distance as fraction of ATR", Order = 8, GroupName = "Risk Management")]
		public double SlMinATRDistance { get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "ATR Period", Description = "ATR period for SL calculation", Order = 9, GroupName = "Risk Management")]
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

		[NinjaScriptProperty]
		[Display(Name = "Show EMA Ribbon", Description = "Draw EMA ribbon fill", Order = 1, GroupName = "Visual")]
		public bool ShowRibbon { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show TP/SL Lines", Description = "Draw entry, SL, and TP lines", Order = 2, GroupName = "Visual")]
		public bool ShowTPSLLines { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Trailing Stop", Description = "Draw trailing stop line", Order = 3, GroupName = "Visual")]
		public bool ShowTrailingStop { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Dashboard", Description = "Show on-chart dashboard text", Order = 4, GroupName = "Visual")]
		public bool ShowDashboard { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Background Tint", Description = "Tint background on signals", Order = 5, GroupName = "Visual")]
		public bool ShowBackgroundTint { get; set; }

		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private PrecisionSniper[] cachePrecisionSniper;
		public PrecisionSniper PrecisionSniper(int emaFastPeriod, int emaSlowPeriod, int emaTrendPeriod, int minConfluenceScore, int rsiLength, int macdFast, int macdSlow, int macdSmooth, int adxPeriod, double slAtrMultiplier, double tp1RR, double tp2RR, double tp3RR, bool trailingStopEnabled, bool structureBasedSL, int swingLookback, double slMinATRDistance, int atrPeriod, BarsPeriodType htfBarsPeriod, int htfValue, PrecisionSniperPresetType preset, bool showRibbon, bool showTPSLLines, bool showTrailingStop, bool showDashboard, bool showBackgroundTint)
		{
			return PrecisionSniper(Input, emaFastPeriod, emaSlowPeriod, emaTrendPeriod, minConfluenceScore, rsiLength, macdFast, macdSlow, macdSmooth, adxPeriod, slAtrMultiplier, tp1RR, tp2RR, tp3RR, trailingStopEnabled, structureBasedSL, swingLookback, slMinATRDistance, atrPeriod, htfBarsPeriod, htfValue, preset, showRibbon, showTPSLLines, showTrailingStop, showDashboard, showBackgroundTint);
		}

		public PrecisionSniper PrecisionSniper(ISeries<double> input, int emaFastPeriod, int emaSlowPeriod, int emaTrendPeriod, int minConfluenceScore, int rsiLength, int macdFast, int macdSlow, int macdSmooth, int adxPeriod, double slAtrMultiplier, double tp1RR, double tp2RR, double tp3RR, bool trailingStopEnabled, bool structureBasedSL, int swingLookback, double slMinATRDistance, int atrPeriod, BarsPeriodType htfBarsPeriod, int htfValue, PrecisionSniperPresetType preset, bool showRibbon, bool showTPSLLines, bool showTrailingStop, bool showDashboard, bool showBackgroundTint)
		{
			if (cachePrecisionSniper != null)
				for (int idx = 0; idx < cachePrecisionSniper.Length; idx++)
					if (cachePrecisionSniper[idx] != null && cachePrecisionSniper[idx].EmaFastPeriod == emaFastPeriod && cachePrecisionSniper[idx].EmaSlowPeriod == emaSlowPeriod && cachePrecisionSniper[idx].EmaTrendPeriod == emaTrendPeriod && cachePrecisionSniper[idx].MinConfluenceScore == minConfluenceScore && cachePrecisionSniper[idx].RsiLength == rsiLength && cachePrecisionSniper[idx].MacdFast == macdFast && cachePrecisionSniper[idx].MacdSlow == macdSlow && cachePrecisionSniper[idx].MacdSmooth == macdSmooth && cachePrecisionSniper[idx].AdxPeriod == adxPeriod && cachePrecisionSniper[idx].SlAtrMultiplier == slAtrMultiplier && cachePrecisionSniper[idx].Tp1RR == tp1RR && cachePrecisionSniper[idx].Tp2RR == tp2RR && cachePrecisionSniper[idx].Tp3RR == tp3RR && cachePrecisionSniper[idx].TrailingStopEnabled == trailingStopEnabled && cachePrecisionSniper[idx].StructureBasedSL == structureBasedSL && cachePrecisionSniper[idx].SwingLookback == swingLookback && cachePrecisionSniper[idx].SlMinATRDistance == slMinATRDistance && cachePrecisionSniper[idx].AtrPeriod == atrPeriod && cachePrecisionSniper[idx].HtfBarsPeriod == htfBarsPeriod && cachePrecisionSniper[idx].HtfValue == htfValue && cachePrecisionSniper[idx].Preset == preset && cachePrecisionSniper[idx].ShowRibbon == showRibbon && cachePrecisionSniper[idx].ShowTPSLLines == showTPSLLines && cachePrecisionSniper[idx].ShowTrailingStop == showTrailingStop && cachePrecisionSniper[idx].ShowDashboard == showDashboard && cachePrecisionSniper[idx].ShowBackgroundTint == showBackgroundTint && cachePrecisionSniper[idx].EqualsInput(input))
						return cachePrecisionSniper[idx];
			return CacheIndicator<PrecisionSniper>(new PrecisionSniper(){ EmaFastPeriod = emaFastPeriod, EmaSlowPeriod = emaSlowPeriod, EmaTrendPeriod = emaTrendPeriod, MinConfluenceScore = minConfluenceScore, RsiLength = rsiLength, MacdFast = macdFast, MacdSlow = macdSlow, MacdSmooth = macdSmooth, AdxPeriod = adxPeriod, SlAtrMultiplier = slAtrMultiplier, Tp1RR = tp1RR, Tp2RR = tp2RR, Tp3RR = tp3RR, TrailingStopEnabled = trailingStopEnabled, StructureBasedSL = structureBasedSL, SwingLookback = swingLookback, SlMinATRDistance = slMinATRDistance, AtrPeriod = atrPeriod, HtfBarsPeriod = htfBarsPeriod, HtfValue = htfValue, Preset = preset, ShowRibbon = showRibbon, ShowTPSLLines = showTPSLLines, ShowTrailingStop = showTrailingStop, ShowDashboard = showDashboard, ShowBackgroundTint = showBackgroundTint }, input, ref cachePrecisionSniper);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.PrecisionSniper PrecisionSniper(int emaFastPeriod, int emaSlowPeriod, int emaTrendPeriod, int minConfluenceScore, int rsiLength, int macdFast, int macdSlow, int macdSmooth, int adxPeriod, double slAtrMultiplier, double tp1RR, double tp2RR, double tp3RR, bool trailingStopEnabled, bool structureBasedSL, int swingLookback, double slMinATRDistance, int atrPeriod, BarsPeriodType htfBarsPeriod, int htfValue, PrecisionSniperPresetType preset, bool showRibbon, bool showTPSLLines, bool showTrailingStop, bool showDashboard, bool showBackgroundTint)
		{
			return indicator.PrecisionSniper(Input, emaFastPeriod, emaSlowPeriod, emaTrendPeriod, minConfluenceScore, rsiLength, macdFast, macdSlow, macdSmooth, adxPeriod, slAtrMultiplier, tp1RR, tp2RR, tp3RR, trailingStopEnabled, structureBasedSL, swingLookback, slMinATRDistance, atrPeriod, htfBarsPeriod, htfValue, preset, showRibbon, showTPSLLines, showTrailingStop, showDashboard, showBackgroundTint);
		}

		public Indicators.PrecisionSniper PrecisionSniper(ISeries<double> input , int emaFastPeriod, int emaSlowPeriod, int emaTrendPeriod, int minConfluenceScore, int rsiLength, int macdFast, int macdSlow, int macdSmooth, int adxPeriod, double slAtrMultiplier, double tp1RR, double tp2RR, double tp3RR, bool trailingStopEnabled, bool structureBasedSL, int swingLookback, double slMinATRDistance, int atrPeriod, BarsPeriodType htfBarsPeriod, int htfValue, PrecisionSniperPresetType preset, bool showRibbon, bool showTPSLLines, bool showTrailingStop, bool showDashboard, bool showBackgroundTint)
		{
			return indicator.PrecisionSniper(input, emaFastPeriod, emaSlowPeriod, emaTrendPeriod, minConfluenceScore, rsiLength, macdFast, macdSlow, macdSmooth, adxPeriod, slAtrMultiplier, tp1RR, tp2RR, tp3RR, trailingStopEnabled, structureBasedSL, swingLookback, slMinATRDistance, atrPeriod, htfBarsPeriod, htfValue, preset, showRibbon, showTPSLLines, showTrailingStop, showDashboard, showBackgroundTint);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.PrecisionSniper PrecisionSniper(int emaFastPeriod, int emaSlowPeriod, int emaTrendPeriod, int minConfluenceScore, int rsiLength, int macdFast, int macdSlow, int macdSmooth, int adxPeriod, double slAtrMultiplier, double tp1RR, double tp2RR, double tp3RR, bool trailingStopEnabled, bool structureBasedSL, int swingLookback, double slMinATRDistance, int atrPeriod, BarsPeriodType htfBarsPeriod, int htfValue, PrecisionSniperPresetType preset, bool showRibbon, bool showTPSLLines, bool showTrailingStop, bool showDashboard, bool showBackgroundTint)
		{
			return indicator.PrecisionSniper(Input, emaFastPeriod, emaSlowPeriod, emaTrendPeriod, minConfluenceScore, rsiLength, macdFast, macdSlow, macdSmooth, adxPeriod, slAtrMultiplier, tp1RR, tp2RR, tp3RR, trailingStopEnabled, structureBasedSL, swingLookback, slMinATRDistance, atrPeriod, htfBarsPeriod, htfValue, preset, showRibbon, showTPSLLines, showTrailingStop, showDashboard, showBackgroundTint);
		}

		public Indicators.PrecisionSniper PrecisionSniper(ISeries<double> input , int emaFastPeriod, int emaSlowPeriod, int emaTrendPeriod, int minConfluenceScore, int rsiLength, int macdFast, int macdSlow, int macdSmooth, int adxPeriod, double slAtrMultiplier, double tp1RR, double tp2RR, double tp3RR, bool trailingStopEnabled, bool structureBasedSL, int swingLookback, double slMinATRDistance, int atrPeriod, BarsPeriodType htfBarsPeriod, int htfValue, PrecisionSniperPresetType preset, bool showRibbon, bool showTPSLLines, bool showTrailingStop, bool showDashboard, bool showBackgroundTint)
		{
			return indicator.PrecisionSniper(input, emaFastPeriod, emaSlowPeriod, emaTrendPeriod, minConfluenceScore, rsiLength, macdFast, macdSlow, macdSmooth, adxPeriod, slAtrMultiplier, tp1RR, tp2RR, tp3RR, trailingStopEnabled, structureBasedSL, swingLookback, slMinATRDistance, atrPeriod, htfBarsPeriod, htfValue, preset, showRibbon, showTPSLLines, showTrailingStop, showDashboard, showBackgroundTint);
		}
	}
}

#endregion
