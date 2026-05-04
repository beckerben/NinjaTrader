#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.Indicators;
#endregion

//This namespace holds Strategies in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Strategies
{
	/// <summary>
	/// Exports bar data and indicator values to a CSV file for ML / statistical analysis.
	/// Price-level indicators are expressed in ATR units: (indicatorPrice - close) / ATR(14).
	/// Toggle export groups via the "Export Groups" property section.
	/// </summary>
	public class Exporter : Strategy
	{
		#region Variables

		private StreamWriter sw;
		private bool   priorCloseHigher  = false;
		private int    trendSequence     = 1;
		private bool   currentCloseHigher = false;
		private bool   currentReversal   = false;
		private OrderFlowCumulativeDelta cumulativeDelta;

		#endregion

		#region Private methods

		private bool IsTradingTime()
		{
			if (!Enable_Time) return true;
			int barMins   = Time[0].Hour * 60 + Time[0].Minute;
			int startMins = Start_Time.Hour * 60 + Start_Time.Minute;
			int endMins   = End_Time.Hour   * 60 + End_Time.Minute;
			return barMins >= startMins && barMins < endMins;
		}

		/// <summary>
		/// Safely evaluates a lambda returning double. Returns empty string on any exception,
		/// NaN, or Infinity so that a single bad indicator never drops an entire row.
		/// </summary>
		private string V(Func<double> getValue)
		{
			try
			{
				double val = getValue();
				if (double.IsNaN(val) || double.IsInfinity(val)) return "";
				return val.ToString(System.Globalization.CultureInfo.InvariantCulture);
			}
			catch { return ""; }
		}

		/// <summary>
		/// Normalizes a price-level indicator relative to the current close, expressed in ATR(14) units.
		/// Formula: (indicatorPrice - close) / ATR(14), rounded to 4 decimal places.
		/// A value of 1.0 means the indicator is exactly one ATR above close.
		/// </summary>
		private string ATRNorm(Func<double> getPrice)
		{
			return V(() => Math.Round((getPrice() - Close[0]) / ATR(14)[0], 4));
		}

		private List<string> BuildHeaderColumns()
		{
			var cols = new List<string>();

			// Always-on bar identifiers
			cols.Add("barcount");
			cols.Add("bar_start_date");
			cols.Add("bar_duration_ms");

			if (Export_OHLCV)
				cols.AddRange(new[] {
					"open", "high", "low", "close", "volume",
					"higherclose", "reversal", "trendsequence"
				});

			if (Export_OHLCV_Normalized)
				cols.AddRange(new[] {
					"open_atr", "high_atr", "low_atr",
					"close_return_atr", "volume_ratio"
				});

			if (Export_Trend)
				cols.AddRange(new[] {
					"adx", "adxr",
					"aroonoscillator", "aroon_up", "aroon_down",
					"dm_diplus", "dm_diminus", "dmi", "dmindex",
					"disparityindex",
					"linreg", "linregintercept", "linregslope", "stderror",
					"macd", "macd_avg", "macd_diff",
					"mama_default", "mama_fama",
					"trix", "trix_signal",
					"tsf", "tsi",
					"vortex_viplus", "vortex_viminus",
					"ichimoku_tenkan", "ichimoku_kijun", "ichimoku_spana", "ichimoku_spanb",
					"swing_high", "swing_low"
				});

			if (Export_Momentum)
				cols.AddRange(new[] {
					"cci", "cmo", "momentum", "mfi", "moneyflowoscillator",
					"pfe", "ppo", "priceoscillator", "psychologicalline",
					"rind", "roc",
					"rsi", "rsi_avg", "rss", "rvi",
					"stochrsi",
					"stochastics_d", "stochastics_k",
					"stochasticsfast_d", "stochasticsfast_k",
					"ultimateoscillator", "williamsr"
				});

			if (Export_Volatility)
				cols.AddRange(new[] {
					"atr",
					"apz_lower", "apz_upper",
					"bollinger_lower", "bollinger_middle", "bollinger_upper",
					"bop",
					"chaikinvolatility", "choppinessindex",
					"donchian_lower", "donchian_mean", "donchian_upper",
					"doublestochastics_k",
					"easeofmovement",
					"fisherstransform", "fosc",
					"kama",
					"keltner_lower", "keltner_mean", "keltner_upper",
					"parabolic_sar",
					"relativevigorindex", "rsquared", "stddev",
					"regchannel_upper", "regchannel_middle", "regchannel_lower"
				});

			if (Export_Volume)
				cols.AddRange(new[] {
					"adl", "obv",
					"chaikinmoneyflow", "chaikinoscillator",
					"volma", "volume_oscillator", "vroc",
					"buysell_buypressure", "buysell_sellpressure",
					"volumeupdown_up", "volumeupdown_down"
				});

			if (Export_Pivots)
				cols.AddRange(new[] {
					"camarilla_r1", "camarilla_r2", "camarilla_r3", "camarilla_r4",
					"camarilla_s1", "camarilla_s2", "camarilla_s3", "camarilla_s4",
					"fibonacci_pp",
					"fibonacci_r1", "fibonacci_r2", "fibonacci_r3",
					"fibonacci_s1", "fibonacci_s2", "fibonacci_s3",
					"pivot_pp",
					"pivot_r1", "pivot_r2", "pivot_r3",
					"pivot_s1", "pivot_s2", "pivot_s3",
					"woodiespivot_pp",
					"woodiespivot_r1", "woodiespivot_r2",
					"woodiespivot_s1", "woodiespivot_s2",
					"currentday_open", "currentday_low", "currentday_high",
					"priorday_open", "priorday_high", "priorday_low", "priorday_close"
				});

			if (Export_OrderFlow)
				cols.AddRange(new[] {
					"orderflowcumulativedelta_deltaopen",
					"orderflowcumulativedelta_deltaclose",
					"orderflowcumulativedelta_deltahigh",
					"orderflowcumulativedelta_deltalow",
					"orderflowvwap_vwap",
					"orderflowvwap_s1_lower", "orderflowvwap_s1_upper",
					"orderflowvwap_s2_lower", "orderflowvwap_s2_upper",
					"orderflowvwap_s3_lower", "orderflowvwap_s3_upper"
				});

			if (Export_FVG)
				cols.AddRange(new[] {
					"fvg_direction",
					"fvg_top", "fvg_bottom", "fvg_midpoint", "fvg_size",
					"fvg_top_atr", "fvg_bottom_atr", "fvg_midpoint_atr", "fvg_size_atr"
				});

			if (Export_Custom)
				cols.AddRange(new[] {
					"woodiescci", "woodiescci_turbo",
					"wisemanawesomeoscillator",
					"alligator_jaw", "alligator_teeth", "alligator_lips",
					"highestlowestlines_high", "highestlowestlines_mid", "highestlowestlines_low"
				});

			if (Export_MovingAverages)
			{
				int[] periods = { 9, 14, 21, 50, 100, 150, 200 };
				string[] types = { "ema", "sma", "wma", "hma", "dema", "tema", "t3", "tma", "vma", "vwma", "zlema" };
				foreach (string t in types)
					foreach (int p in periods)
						cols.Add($"{t}_{p}");
			}

			return cols;
		}

		private List<string> BuildDataColumns()
		{
			var cols = new List<string>();
			var ic   = System.Globalization.CultureInfo.InvariantCulture;

			// Always-on bar identifiers
			cols.Add(CurrentBar.ToString());
			cols.Add(Time[0].ToString("yyyy-MM-dd HH:mm:ss.fff"));
			cols.Add(((long)(Time[0] - Time[1]).TotalMilliseconds).ToString());

			if (Export_OHLCV)
			{
				cols.Add(Open[0].ToString(ic));
				cols.Add(High[0].ToString(ic));
				cols.Add(Low[0].ToString(ic));
				cols.Add(Close[0].ToString(ic));
				cols.Add(Volume[0].ToString(ic));
				cols.Add(currentCloseHigher.ToString());
				cols.Add(currentReversal.ToString());
				cols.Add(trendSequence.ToString());
			}

			if (Export_OHLCV_Normalized)
			{
				cols.Add(ATRNorm(() => Open[0]));
				cols.Add(ATRNorm(() => High[0]));
				cols.Add(ATRNorm(() => Low[0]));
				cols.Add(V(() => Math.Round((Close[0] - Close[1]) / ATR(14)[0], 4)));
				cols.Add(V(() => Math.Round(Volume[0] / VOLMA(14)[0], 4)));
			}

			if (Export_Trend)
			{
				cols.Add(V(() => Math.Round(ADX(14)[0], 0)));
				cols.Add(V(() => Math.Round(ADXR(10, 14)[0], 0)));
				cols.Add(V(() => Math.Round(AroonOscillator(14)[0], 0)));
				cols.Add(V(() => Math.Round(Aroon(14).Up[0], 0)));
				cols.Add(V(() => Math.Round(Aroon(14).Down[0], 0)));
				cols.Add(V(() => Math.Round(DM(14).DiPlus[0], 0)));
				cols.Add(V(() => Math.Round(DM(14).DiMinus[0], 0)));
				cols.Add(V(() => Math.Round(DMI(14)[0], 0)));
				cols.Add(V(() => Math.Round(DMIndex(3)[0], 0)));
				cols.Add(V(() => Math.Round(DisparityIndex(25)[0], 3)));
				cols.Add(ATRNorm(() => LinReg(14)[0]));
				cols.Add(ATRNorm(() => LinRegIntercept(14)[0]));
				cols.Add(V(() => Math.Round(LinRegSlope(14)[0], 1)));
				cols.Add(V(() => Math.Round(StdError(14)[0], 1)));
				cols.Add(V(() => Math.Round(MACD(12, 26, 9)[0], 1)));
				cols.Add(V(() => Math.Round(MACD(12, 26, 9).Avg[0], 1)));
				cols.Add(V(() => Math.Round(MACD(12, 26, 9).Diff[0], 1)));
				cols.Add(ATRNorm(() => MAMA(0.5, 0.05).Default[0]));
				cols.Add(ATRNorm(() => MAMA(0.5, 0.05).Fama[0]));
				cols.Add(V(() => Math.Round(TRIX(14, 3)[0], 4)));
				cols.Add(V(() => Math.Round(TRIX(14, 3).Signal[0], 4)));
				cols.Add(ATRNorm(() => TSF(3, 14)[0]));
				cols.Add(V(() => Math.Round(TSI(3, 14)[0], 0)));
				cols.Add(V(() => Math.Round(Vortex(14).VIPlus[0], 1)));
				cols.Add(V(() => Math.Round(Vortex(14).VIMinus[0], 1)));
				var ich = IchimokuCloud(9, 26, 52, -26, 26);
				cols.Add(ATRNorm(() => ich.Values[0][26]));
				cols.Add(ATRNorm(() => ich.Values[1][26]));
				cols.Add(ATRNorm(() => ich.Values[2][0]));
				cols.Add(ATRNorm(() => ich.Values[3][0]));
				var swg = Swing(5);
				cols.Add(swg.SwingHigh[0] > 0 ? ATRNorm(() => swg.SwingHigh[0]) : "");
				cols.Add(swg.SwingLow[0]  > 0 ? ATRNorm(() => swg.SwingLow[0])  : "");
			}

			if (Export_Momentum)
			{
				cols.Add(V(() => Math.Round(CCI(14)[0], 0)));
				cols.Add(V(() => Math.Round(CMO(14)[0], 0)));
				cols.Add(V(() => Math.Round(Momentum(14)[0], 0)));
				cols.Add(V(() => Math.Round(MFI(14)[0], 0)));
				cols.Add(V(() => Math.Round(MoneyFlowOscillator(20)[0], 2)));
				cols.Add(V(() => Math.Round(PFE(14, 10)[0], 2)));
				cols.Add(V(() => Math.Round(PPO(12, 26, 9).Smoothed[0], 3)));
				cols.Add(V(() => Math.Round(PriceOscillator(12, 26, 9)[0], 1)));
				cols.Add(V(() => PsychologicalLine(10)[0]));
				cols.Add(V(() => Math.Round(RIND(3, 10)[0], 0)));
				cols.Add(V(() => Math.Round(ROC(14)[0], 2)));
				cols.Add(V(() => Math.Round(RSI(14, 3)[0], 0)));
				cols.Add(V(() => Math.Round(RSI(14, 3).Avg[0], 0)));
				cols.Add(V(() => Math.Round(RSS(10, 40, 5)[0], 0)));
				cols.Add(V(() => Math.Round(RVI(14)[0], 0)));
				cols.Add(V(() => Math.Round(StochRSI(14)[0], 2)));
				cols.Add(V(() => Math.Round(Stochastics(7, 14, 3).D[0], 0)));
				cols.Add(V(() => Math.Round(Stochastics(7, 14, 3).K[0], 0)));
				cols.Add(V(() => Math.Round(StochasticsFast(3, 14).D[0], 0)));
				cols.Add(V(() => Math.Round(StochasticsFast(3, 14).K[0], 0)));
				cols.Add(V(() => Math.Round(UltimateOscillator(7, 14, 28)[0], 0)));
				cols.Add(V(() => Math.Round(WilliamsR(14)[0], 0)));
			}

			if (Export_Volatility)
			{
				cols.Add(V(() => Math.Round(ATR(14)[0], 4)));
				cols.Add(ATRNorm(() => APZ(2, 20).Lower[0]));
				cols.Add(ATRNorm(() => APZ(2, 20).Upper[0]));
				cols.Add(ATRNorm(() => Bollinger(2, 14).Lower[0]));
				cols.Add(ATRNorm(() => Bollinger(2, 14)[0]));
				cols.Add(ATRNorm(() => Bollinger(2, 14).Upper[0]));
				cols.Add(V(() => Math.Round(BOP(14)[0], 3)));
				cols.Add(V(() => Math.Round(ChaikinVolatility(10, 10)[0], 0)));
				cols.Add(V(() => Math.Round(ChoppinessIndex(14)[0], 0)));
				cols.Add(ATRNorm(() => DonchianChannel(14).Lower[0]));
				cols.Add(ATRNorm(() => DonchianChannel(14)[0]));
				cols.Add(ATRNorm(() => DonchianChannel(14).Upper[0]));
				cols.Add(V(() => Math.Round(DoubleStochastics(10).K[0], 0)));
				cols.Add(V(() => Math.Round(EaseOfMovement(10, 1000)[0], 0)));
				cols.Add(V(() => Math.Round(FisherTransform(10)[0], 1)));
				cols.Add(V(() => Math.Round(FOSC(14)[0], 2)));
				cols.Add(ATRNorm(() => KAMA(2, 10, 30)[0]));
				cols.Add(ATRNorm(() => KeltnerChannel(1.5, 10).Lower[0]));
				cols.Add(ATRNorm(() => KeltnerChannel(1.5, 10)[0]));
				cols.Add(ATRNorm(() => KeltnerChannel(1.5, 10).Upper[0]));
				cols.Add(ATRNorm(() => ParabolicSAR(0.02, 0.2, 0.02)[0]));
				cols.Add(V(() => Math.Round(RelativeVigorIndex(10)[0], 2)));
				cols.Add(V(() => Math.Round(RSquared(8)[0], 2)));
				cols.Add(V(() => Math.Round(StdDev(14)[0], 1)));
				var rc = RegressionChannel(35, 2);
				cols.Add(ATRNorm(() => rc.Upper[0]));
				cols.Add(ATRNorm(() => rc.Middle[0]));
				cols.Add(ATRNorm(() => rc.Lower[0]));
			}

			if (Export_Volume)
			{
				cols.Add(V(() => ADL().AD[0]));
				cols.Add(V(() => OBV()[0]));
				cols.Add(V(() => Math.Round(ChaikinMoneyFlow(21)[0], 0)));
				cols.Add(V(() => Math.Round(ChaikinOscillator(3, 10)[0], 0)));
				cols.Add(V(() => Math.Round(VOLMA(14)[0], 0)));
				cols.Add(V(() => Math.Round(VolumeOscillator(12, 26)[0], 0)));
				cols.Add(V(() => Math.Round(VROC(14, 3)[0], 0)));
				cols.Add(V(() => Math.Round(BuySellPressure().BuyPressure[0], 0)));
				cols.Add(V(() => Math.Round(BuySellPressure().SellPressure[0], 0)));
				var vud = VolumeUpDown();
				cols.Add(V(() => vud.UpVolume[0]));
				cols.Add(V(() => vud.DownVolume[0]));
			}

			if (Export_Pivots)
			{
				var cam = CamarillaPivots(PivotRange.Daily, HLCCalculationMode.CalcFromIntradayData, 0, 0, 0, 20);
				cols.Add(ATRNorm(() => cam.R1[0]));
				cols.Add(ATRNorm(() => cam.R2[0]));
				cols.Add(ATRNorm(() => cam.R3[0]));
				cols.Add(ATRNorm(() => cam.R4[0]));
				cols.Add(ATRNorm(() => cam.S1[0]));
				cols.Add(ATRNorm(() => cam.S2[0]));
				cols.Add(ATRNorm(() => cam.S3[0]));
				cols.Add(ATRNorm(() => cam.S4[0]));
				var fib = FibonacciPivots(PivotRange.Daily, HLCCalculationMode.CalcFromIntradayData, 0, 0, 0, 20);
				cols.Add(ATRNorm(() => fib.Pp[0]));
				cols.Add(ATRNorm(() => fib.R1[0]));
				cols.Add(ATRNorm(() => fib.R2[0]));
				cols.Add(ATRNorm(() => fib.R3[0]));
				cols.Add(ATRNorm(() => fib.S1[0]));
				cols.Add(ATRNorm(() => fib.S2[0]));
				cols.Add(ATRNorm(() => fib.S3[0]));
				var piv = Pivots(PivotRange.Daily, HLCCalculationMode.CalcFromIntradayData, 0, 0, 0, 20);
				cols.Add(ATRNorm(() => piv.Pp[0]));
				cols.Add(ATRNorm(() => piv.R1[0]));
				cols.Add(ATRNorm(() => piv.R2[0]));
				cols.Add(ATRNorm(() => piv.R3[0]));
				cols.Add(ATRNorm(() => piv.S1[0]));
				cols.Add(ATRNorm(() => piv.S2[0]));
				cols.Add(ATRNorm(() => piv.S3[0]));
				var woo = WoodiesPivots(HLCCalculationModeWoodie.CalcFromIntradayData, 20);
				cols.Add(ATRNorm(() => woo.PP[0]));
				cols.Add(ATRNorm(() => woo.R1[0]));
				cols.Add(ATRNorm(() => woo.R2[0]));
				cols.Add(ATRNorm(() => woo.S1[0]));
				cols.Add(ATRNorm(() => woo.S2[0]));
				var cdohlc = CurrentDayOHL();
				cols.Add(ATRNorm(() => cdohlc.CurrentOpen[0]));
				cols.Add(ATRNorm(() => cdohlc.CurrentLow[0]));
				cols.Add(ATRNorm(() => cdohlc.CurrentHigh[0]));
				cols.Add(ATRNorm(() => PriorDayOHLC().PriorOpen[0]));
				cols.Add(ATRNorm(() => PriorDayOHLC().PriorHigh[0]));
				cols.Add(ATRNorm(() => PriorDayOHLC().PriorLow[0]));
				cols.Add(ATRNorm(() => PriorDayOHLC().PriorClose[0]));
			}

			if (Export_OrderFlow)
			{
				cols.Add(V(() => cumulativeDelta.DeltaOpen[0]));
				cols.Add(V(() => cumulativeDelta.DeltaClose[0]));
				cols.Add(V(() => cumulativeDelta.DeltaHigh[0]));
				cols.Add(V(() => cumulativeDelta.DeltaLow[0]));
				var vwap = OrderFlowVWAP(VWAPResolution.Standard, Bars.TradingHours, VWAPStandardDeviations.Three, 1, 2, 3);
				cols.Add(ATRNorm(() => vwap.VWAP[0]));
				cols.Add(ATRNorm(() => vwap.StdDev1Lower[0]));
				cols.Add(ATRNorm(() => vwap.StdDev1Upper[0]));
				cols.Add(ATRNorm(() => vwap.StdDev2Lower[0]));
				cols.Add(ATRNorm(() => vwap.StdDev2Upper[0]));
				cols.Add(ATRNorm(() => vwap.StdDev3Lower[0]));
				cols.Add(ATRNorm(() => vwap.StdDev3Upper[0]));
			}

			if (Export_FVG)
			{
				if (CurrentBars[0] >= 2)
				{
					bool fvgBullish = High[2] < Low[0];
					bool fvgBearish = Low[2]  > High[0];
					bool hasFvg     = fvgBullish || fvgBearish;

					string direction = fvgBullish ? "bullish" : fvgBearish ? "bearish" : "";
					double fvgTop    = fvgBullish ? Low[0]  : fvgBearish ? Low[2]  : 0.0;
					double fvgBottom = fvgBullish ? High[2] : fvgBearish ? High[0] : 0.0;
					double fvgMid    = hasFvg ? (fvgTop + fvgBottom) / 2.0 : 0.0;
					double fvgSize   = hasFvg ? fvgTop - fvgBottom : 0.0;

					cols.Add(direction);
					cols.Add(hasFvg ? fvgTop.ToString(ic)                              : "");
					cols.Add(hasFvg ? fvgBottom.ToString(ic)                           : "");
					cols.Add(hasFvg ? fvgMid.ToString(ic)                              : "");
					cols.Add(hasFvg ? Math.Round(fvgSize, 4).ToString(ic)              : "");
					cols.Add(hasFvg ? V(() => Math.Round((fvgTop    - Close[0]) / ATR(14)[0], 4)) : "");
					cols.Add(hasFvg ? V(() => Math.Round((fvgBottom - Close[0]) / ATR(14)[0], 4)) : "");
					cols.Add(hasFvg ? V(() => Math.Round((fvgMid    - Close[0]) / ATR(14)[0], 4)) : "");
					cols.Add(hasFvg ? V(() => Math.Round(fvgSize / ATR(14)[0], 4))     : "");
				}
				else
				{
					for (int i = 0; i < 9; i++) cols.Add("");
				}
			}

			if (Export_Custom)
			{
				cols.Add(V(() => Math.Round(WoodiesCCI(2, 5, 14, 34, 25, 6, 60, 100, 2)[0], 0)));
				cols.Add(V(() => Math.Round(WoodiesCCI(2, 5, 14, 34, 25, 6, 60, 100, 2).Turbo[0], 0)));
				cols.Add(V(() => Math.Round(WisemanAwesomeOscillator()[0], 1)));
				cols.Add(ATRNorm(() => WisemanAlligator(13, 8, 8, 5, 5, 3).Jaw[0]));
				cols.Add(ATRNorm(() => WisemanAlligator(13, 8, 8, 5, 5, 3).Teeth[0]));
				cols.Add(ATRNorm(() => WisemanAlligator(13, 8, 8, 5, 5, 3).Lips[0]));
				cols.Add(ATRNorm(() => HighestLowestLines(21).HighestHigh[0]));
				cols.Add(ATRNorm(() => HighestLowestLines(21).Midline[0]));
				cols.Add(ATRNorm(() => HighestLowestLines(21).LowestLow[0]));
			}

			if (Export_MovingAverages)
			{
				int[] periods = { 9, 14, 21, 50, 100, 150, 200 };
				foreach (int p in periods) cols.Add(ATRNorm(() => EMA(p)[0]));
				foreach (int p in periods) cols.Add(ATRNorm(() => SMA(p)[0]));
				foreach (int p in periods) cols.Add(ATRNorm(() => WMA(p)[0]));
				foreach (int p in periods) cols.Add(ATRNorm(() => HMA(p)[0]));
				foreach (int p in periods) cols.Add(ATRNorm(() => DEMA(p)[0]));
				foreach (int p in periods) cols.Add(ATRNorm(() => TEMA(p)[0]));
				foreach (int p in periods) cols.Add(ATRNorm(() => T3(p, 3, 0.7)[0]));
				foreach (int p in periods) cols.Add(ATRNorm(() => TMA(p)[0]));
				foreach (int p in periods) cols.Add(ATRNorm(() => VMA(p, p)[0]));
				foreach (int p in periods) cols.Add(ATRNorm(() => VWMA(p)[0]));
				foreach (int p in periods) cols.Add(ATRNorm(() => ZLEMA(p)[0]));
			}

			return cols;
		}

		#endregion

		#region Main methods

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description								= @"Exports bar data and indicator values to CSV for ML / statistical analysis.";
				Name									= "Exporter";
				Calculate								= Calculate.OnBarClose;
				EntriesPerDirection						= 1;
				EntryHandling							= EntryHandling.AllEntries;
				IsExitOnSessionCloseStrategy			= true;
				ExitOnSessionCloseSeconds				= 30;
				IsFillLimitOnTouch						= false;
				MaximumBarsLookBack						= MaximumBarsLookBack.Infinite;
				OrderFillResolution						= OrderFillResolution.Standard;
				Slippage								= 0;
				StartBehavior							= StartBehavior.WaitUntilFlat;
				TimeInForce								= TimeInForce.Gtc;
				TraceOrders								= false;
				RealtimeErrorHandling					= RealtimeErrorHandling.StopCancelClose;
				StopTargetHandling						= StopTargetHandling.PerEntryExecution;
				BarsRequiredToTrade						= 1;
				IsInstantiatedOnEachOptimizationIteration = true;
				// Output
				outputPath  = "C:\\Temp";
				outputFile  = null;
				// Filters
				Enable_Time = false;
				// Export Groups
				Export_OHLCV            = true;
				Export_OHLCV_Normalized = true;
				Export_Trend            = true;
				Export_Momentum         = true;
				Export_Volatility       = true;
				Export_Volume           = true;
				Export_Pivots           = true;
				Export_OrderFlow        = true;
				Export_FVG              = true;
				Export_Custom           = true;
				Export_MovingAverages   = true;
			}
			else if (State == State.Terminated)
			{
				if (sw != null) { sw.Close(); sw.Dispose(); sw = null; }
			}
			else if (State == State.Configure)
			{
				cumulativeDelta = OrderFlowCumulativeDelta(CumulativeDeltaType.BidAsk, CumulativeDeltaPeriod.Bar, 0);
			}
			else if (State == State.DataLoaded)
			{
				if (string.IsNullOrEmpty(outputFile))
				{
					string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
					outputFile = $"{Instrument.FullName.Replace(" ", "").Replace("-", "")}_{timestamp}.csv";
				}
				string fullPath = Path.Combine(outputPath, outputFile);
				if (!File.Exists(fullPath))
				{
					using (StreamWriter writer = File.CreateText(fullPath))
						writer.WriteLine(string.Join(",", BuildHeaderColumns()));
				}
				else if (new FileInfo(fullPath).Length == 0)
				{
					using (StreamWriter writer = File.AppendText(fullPath))
						writer.WriteLine(string.Join(",", BuildHeaderColumns()));
				}
				sw = File.AppendText(fullPath);
				sw.AutoFlush = true;
			}
		}

		protected override void OnBarUpdate()
		{
			if (!IsTradingTime()) return;
			if (CurrentBars[0] < BarsRequiredToTrade) return;

			// Update trend state — always runs regardless of export group toggles
			currentCloseHigher = Close[0] > Close[1];
			currentReversal    = priorCloseHigher != currentCloseHigher;
			if (currentReversal)
				trendSequence = 1;
			else
				trendSequence++;
			priorCloseHigher = currentCloseHigher;

			sw.WriteLine(string.Join(",", BuildDataColumns()));
		}

		#endregion

		#region Properties

		[Display(Name = "Output Path", Description = "Directory to write the CSV file, e.g. C:\\Temp", Order = 1, GroupName = "Output")]
		public string outputPath { get; set; }

		[Display(Name = "Output File", Description = "File name, e.g. output.csv. Leave blank to auto-generate from instrument name and timestamp.", Order = 2, GroupName = "Output")]
		public string outputFile { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Enable Time Filter", Description = "When enabled, only bars within Start Time and End Time will be exported.", Order = 1, GroupName = "Filters")]
		public bool Enable_Time { get; set; }

		[Gui.PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name = "Start Time", Description = "Start of the export window (HH:mm:ss). Only bars at or after this time are exported when the filter is enabled.", Order = 2, GroupName = "Filters")]
		public DateTime Start_Time { get; set; }

		[Gui.PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name = "End Time", Description = "End of the export window (HH:mm:ss). Bars at or after this time are excluded when the filter is enabled.", Order = 3, GroupName = "Filters")]
		public DateTime End_Time { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "OHLCV (Raw)", Description = "Export raw open, high, low, close, volume plus bar meta (barcount, date, duration, higherclose, reversal, trendsequence).", Order = 1, GroupName = "Export Groups")]
		public bool Export_OHLCV { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "OHLCV (ATR Normalized)", Description = "Export ATR-normalized versions of open/high/low and close return, plus volume ratio.", Order = 2, GroupName = "Export Groups")]
		public bool Export_OHLCV_Normalized { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Trend Indicators", Description = "ADX, ADXR, Aroon, DM, DMI, LinReg family, MACD, MAMA, TRIX, TSF, TSI, Vortex, Ichimoku Cloud (Tenkan/Kijun/SpanA/SpanB), Swing (High/Low).", Order = 3, GroupName = "Export Groups")]
		public bool Export_Trend { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Momentum Indicators", Description = "CCI, CMO, MFI, Momentum, PFE, PPO, PriceOscillator, RSI, RSS, RVI, Stochastics, UltimateOscillator, WilliamsR.", Order = 4, GroupName = "Export Groups")]
		public bool Export_Momentum { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Volatility Indicators", Description = "ATR, APZ, Bollinger, BOP, ChaikinVolatility, DonchianChannel, DoubleStochastics, FisherTransform, KAMA, Keltner, ParabolicSAR, RSquared, RelativeVigor, StdDev, RegressionChannel (Upper/Middle/Lower).", Order = 5, GroupName = "Export Groups")]
		public bool Export_Volatility { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Volume Indicators", Description = "ADL, OBV, ChaikinMoneyFlow, ChaikinOscillator, VOLMA, VolumeOscillator, VROC, BuySellPressure, VolumeUpDown (Up/Down).", Order = 6, GroupName = "Export Groups")]
		public bool Export_Volume { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Pivot Levels", Description = "Camarilla, Fibonacci, Standard, and Woodies pivots plus Current Day and Prior Day OHL.", Order = 7, GroupName = "Export Groups")]
		public bool Export_Pivots { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Order Flow", Description = "Cumulative Delta (open/close/high/low) and VWAP with 3 standard deviation bands.", Order = 8, GroupName = "Export Groups")]
		public bool Export_OrderFlow { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Fair Value Gaps", Description = "FVG bullish/bearish flags, gap size in price, and gap size in ATR units.", Order = 9, GroupName = "Export Groups")]
		public bool Export_FVG { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Custom / Other", Description = "WoodiesCCI, Wiseman Awesome Oscillator, Alligator (Jaw/Teeth/Lips), HighestLowestLines.", Order = 10, GroupName = "Export Groups")]
		public bool Export_Custom { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Moving Averages", Description = "EMA, SMA, WMA, HMA, DEMA, TEMA, T3, TMA, VMA, VWMA, ZLEMA — each at periods 9, 14, 21, 50, 100, 150, 200 (77 columns, ATR-normalized).", Order = 11, GroupName = "Export Groups")]
		public bool Export_MovingAverages { get; set; }

		#endregion
	}
}
