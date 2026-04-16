#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.NinjaScript.Strategies;
#endregion

namespace NinjaTrader.NinjaScript.Strategies
{
    /// <summary>
    /// RenkoReversal_01_Baseline — Single-bar mean-reversion strategy using a rolling
    /// frequency lookup table keyed on 4-feature bucket strings.
    ///
    /// Primary series: 40-tick Renko bars (BarsArray[0]).
    /// Secondary series: 10-second bars (BarsArray[1]).
    ///
    /// On each Renko brick close the strategy:
    ///   1. Scores the previous bar's bucket outcome (did the just-closed bar reverse?).
    ///   2. Builds the current bar's bucket string and looks up its reversal probability.
    ///   3. Enters opposite to the just-closed brick direction when P(reversal) >= ReversalThreshold.
    ///   4. Manages exits with fixed brick-multiple stop-loss and profit target.
    ///
    /// Multi-series note: Closes[n][idx] accesses bar idx on BarsArray[n].
    /// In NT8, Bars[idx] returns double (close price), Closes[n] is Series<double>.
    /// </summary>
    public class RenkoReversal_01_Baseline : Strategy
    {
        // ─────────────────────────────────────────────────────────────────────
        #region Inner Types

        /// <summary>Accumulates reversal counts for a single bucket key.</summary>
        private class ReversalStats
        {
            public int Reversals { get; set; }
            public int Total     { get; set; }

            /// <summary>Estimated probability of reversal; defaults to 0.5 when no data.</summary>
            public double P() => Total > 0 ? (double)Reversals / Total : 0.5;
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region Private Fields

        /// <summary>Rolling frequency lookup: bucket string → reversal statistics.</summary>
        private SortedDictionary<string, ReversalStats> _lookup;

        /// <summary>True while a position is open; prevents stacking entries.</summary>
        private bool _positionOpen;

        /// <summary>Brick size (in price) of the most recent completed brick.</summary>
        private double _brickSize;

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region Input Parameters

        [NinjaScriptProperty]
        [Range(0.01, 1.0)]
        [Display(Name = "Reversal Threshold",
                 Description = "Enter when P(reversal) >= this value (0.55 = 55%)",
                 Order = 1, GroupName = "Strategy Parameters")]
        public double ReversalThreshold { get; set; }

        [NinjaScriptProperty]
        [Range(1, 100)]
        [Display(Name = "Lookback Bricks",
                 Description = "How many prior Renko bricks to consider",
                 Order = 2, GroupName = "Strategy Parameters")]
        public int LookbackBricks { get; set; }

        [NinjaScriptProperty]
        [Range(1, 500)]
        [Display(Name = "Lookback 10s Bars",
                 Description = "How many 10-second bars for secondary indicators",
                 Order = 3, GroupName = "Strategy Parameters")]
        public int Lookback10sBars { get; set; }

        [NinjaScriptProperty]
        [Range(0.1, 20.0)]
        [Display(Name = "Stop Loss (bricks)",
                 Description = "Stop loss distance in brick-size multiples",
                 Order = 4, GroupName = "Strategy Parameters")]
        public double StopLossBricks { get; set; }

        [NinjaScriptProperty]
        [Range(0.1, 20.0)]
        [Display(Name = "Profit Target (bricks)",
                 Description = "Profit target distance in brick-size multiples",
                 Order = 5, GroupName = "Strategy Parameters")]
        public double ProfitTargetBricks { get; set; }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region OnStateChange

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description                  = "Renko reversal (Baseline): rolling frequency lookup, fixed brick SL/PT. "
                                             + "Apply to a 40-tick Renko chart in NT8.";
                Name                         = "RenkoReversal_01_Baseline";
                Calculate                    = Calculate.OnBarClose;
                EntriesPerDirection          = 1;
                EntryHandling                = EntryHandling.AllEntries;
                IsExitOnSessionCloseStrategy = true;
                ExitOnSessionCloseSeconds    = 30;
                IsFillLimitOnTouch           = false;
                MaximumBarsLookBack          = MaximumBarsLookBack.TwoHundredFiftySix;
                OrderFillResolution          = OrderFillResolution.Standard;
                Slippage                     = 0;
                StartBehavior                = StartBehavior.WaitUntilFlat;
                TimeInForce                  = TimeInForce.Gtc;
                TraceOrders                  = false;
                RealtimeErrorHandling        = RealtimeErrorHandling.StopCancelClose;
                StopTargetHandling           = StopTargetHandling.PerEntryExecution;
                BarsRequiredToTrade          = 20;

                ReversalThreshold  = 0.55;
                LookbackBricks     = 10;
                Lookback10sBars    = 20;
                StopLossBricks     = 2.0;
                ProfitTargetBricks = 3.0;
            }
            else if (State == State.Configure)
            {
                AddDataSeries(BarsPeriodType.Second, 10);
            }
            else if (State == State.DataLoaded)
            {
                _lookup       = new SortedDictionary<string, ReversalStats>();
                _positionOpen = false;
                _brickSize    = 0.0;
            }
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region OnBarUpdate

        protected override void OnBarUpdate()
        {
            // Skip secondary 10s series — all logic runs on primary Renko bars only
            if (BarsInProgress != 0) return;

            int b0 = Closes[0].Count;
            int b1 = Closes[1].Count;
            Print("=== ONBAR B0.Count=" + b0 + " B1.Count=" + b1 + " CurrentBar=" + CurrentBar);

            // Guard: primary needs ≥ 6 bars (deepest lookback is index 5)
            if (b0 < 6) { Print("GUARD: b0<6 return"); return; }
            // Guard: secondary needs ≥ 25 for EMASlope(20) warmup
            if (b1 < 25) { Print("GUARD: b1<25 return"); return; }

            // Flat-bar skip
            if (Closes[0][0] == Closes[0][1]) return;

            _positionOpen = Position.MarketPosition != MarketPosition.Flat;

            // ── Step 1: Score bar[1]'s bucket using bar[0]'s outcome ──
            string prevBucket = BuildBucket(1);
            UpdateLookup(prevBucket, WasReversalAt(0));

            // ── Step 2: Build signal bucket for bar[0] ──
            string bucket = BuildBucket(0);
            double prob   = P_Reversal(bucket);

            // ── Step 3: Entry — fade the just-closed brick ──
            int lastDir = BrickDirection(0);

            if (prob >= ReversalThreshold && !_positionOpen && lastDir != 0)
            {
                _brickSize = Math.Abs(Closes[0][0] - Opens[0][0]);
                if (_brickSize <= 0) return;

                double slTicks = Math.Round(StopLossBricks     * _brickSize / TickSize);
                double ptTicks = Math.Round(ProfitTargetBricks * _brickSize / TickSize);

                if (lastDir > 0)
                {
                    SetStopLoss    ("RR_Short", CalculationMode.Ticks, slTicks, false);
                    SetProfitTarget("RR_Short", CalculationMode.Ticks, ptTicks, false);
                    EnterShort(1, "RR_Short");
                }
                else
                {
                    SetStopLoss    ("RR_Long", CalculationMode.Ticks, slTicks, false);
                    SetProfitTarget("RR_Long", CalculationMode.Ticks, ptTicks, false);
                    EnterLong(1, "RR_Long");
                }
            }
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region OnExecutionUpdate

        protected override void OnExecutionUpdate(
            Execution     execution,
            string       executionId,
            double       price,
            int          quantity,
            MarketPosition marketPosition,
            string       orderId,
            DateTime     time)
        {
            if (Position.MarketPosition == MarketPosition.Flat)
                _positionOpen = false;
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region Brick Utilities

        /// <summary>
        /// Returns the direction of the Renko brick at <paramref name="index"/>.
        /// +1 = up brick, -1 = down brick, 0 = flat/undefined.
        /// </summary>
        private int BrickDirection(int index)
        {
            int bars = Closes[0].Count;
            if (index < 0 || index >= bars) return 0;
            if (Closes[0][index] > Opens[0][index]) return  1;
            if (Closes[0][index] < Opens[0][index]) return -1;
            return 0;
        }

        /// <summary>
        /// Counts the run of consecutive bricks sharing the same direction as the
        /// brick at <paramref name="startIndex"/>, searching backward.
        /// </summary>
        private int ConsecutiveCount(int startIndex, int maxLookback = 50)
        {
            int bars = Closes[0].Count;
            if (startIndex < 0 || startIndex >= bars) return 0;
            int dir = BrickDirection(startIndex);
            if (dir == 0) return 0;
            int count = 0;
            int maxIdx = Math.Min(bars, startIndex + maxLookback);
            for (int i = startIndex; i < maxIdx && BrickDirection(i) == dir; i++)
                count++;
            return count;
        }

        /// <summary>
        /// Returns true when the brick at <paramref name="idx"/> reversed relative to
        /// the immediately prior brick (<paramref name="idx"/> + 1).
        /// </summary>
        private bool WasReversalAt(int idx)
        {
            int bars = Closes[0].Count;
            if (idx < 0 || idx + 1 >= bars) return false;
            int d0 = BrickDirection(idx);
            int d1 = BrickDirection(idx + 1);
            return d0 != 0 && d1 != 0 && d0 != d1;
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region Secondary-Series Indicator Helpers (10-second bars)

        /// <summary>
        /// EMA value on the last completed 10-second bar (index [1]).
        /// Returns 0 when the secondary series has insufficient bars.
        /// </summary>
        private double EMA10s(int period)
        {
            int required = Math.Max(period, 1) + 2;
            if (Closes[1].Count < required) return 0;
            return EMA(BarsArray[1], period)[1];
        }

        /// <summary>
        /// RSI value on the last completed 10-second bar.
        /// Returns 50 when the secondary series has fewer than 2 bars.
        /// </summary>
        private double RSI10s(int period)
        {
            if (Closes[1].Count < 2) return 50;
            return RSI(BarsArray[1], period, 3)[1];
        }

        /// <summary>
        /// Slope direction of the EMA on the 10-second series.
        /// Compares bar[1] to bar[2] (both completed, no look-ahead).
        /// Returns +1 (rising), -1 (falling), or 0 (flat within threshold).
        /// </summary>
        private int EMASlope(int period, double threshold = 0.0)
        {
            int required = Math.Max(period, 1) + 3;
            if (Closes[1].Count < required) return 0;
            double emaNow  = EMA(BarsArray[1], period)[1];
            double emaPrev = EMA(BarsArray[1], period)[2];
            double delta    = emaNow - emaPrev;
            if (delta >  threshold) return  1;
            if (delta < -threshold) return -1;
            return 0;
        }

        /// <summary>
        /// RSI zone classification on the 10-second series.
        /// Returns +1 (overbought > obLevel), -1 (oversold &lt; osLevel), 0 (neutral).
        /// </summary>
        private int RSIZone(int period, double obLevel = 65.0, double osLevel = 35.0)
        {
            double rsi = RSI10s(period);
            if (rsi > obLevel) return  1;
            if (rsi < osLevel) return -1;
            return 0;
        }

        /// <summary>
        /// Volume of the just-closed Renko brick divided by the average volume of
        /// the previous <paramref name="lookback"/> bricks.
        /// Guard: caps lookback by available bars.
        /// </summary>
        private double VolumeRatio(int lookback = 10)
        {
            int bars = Volumes[0].Count;
            if (bars < 2) return 1.0;
            int available = Math.Min(bars - 1, lookback);
            if (available < 1) return 1.0;

            double avgVol = 0;
            for (int i = 1; i <= available; i++)
                avgVol += Volumes[0][i];
            avgVol /= available;
            double vol1 = (bars > 1) ? Volumes[0][1] : Volumes[0][0];
            return vol1 / (avgVol > 0 ? avgVol : 1);
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region Bucket Construction and Lookup Table

        /// <summary>
        /// Builds the 4-feature bucket string for the Renko brick at bar index
        /// <paramref name="idx"/> on the primary series.
        ///
        /// Format: "consec={N},ema={up|down|flat},rsi={ob|neutral|os},vol={high|normal|low}"
        ///
        /// Feature definitions:
        ///   consec — consecutive bricks in same direction, capped at "4+" for >= 4
        ///   ema    — EMASlope(20) on 10-second series
        ///   rsi    — RSIZone(14) on 10-second series (ob > 65, os &lt; 35)
        ///   vol    — VolumeRatio: high > 1.5, low &lt; 0.7, else normal
        ///
        /// Safe defaults are returned when insufficient bars exist on either series.
        /// </summary>
        private string BuildBucket(int idx)
        {
            int bars = Closes[0].Count;
            if (idx < 0 || idx >= bars)
                return "consec=0,ema=flat,rsi=neutral,vol=normal";

            int    consecutive = ConsecutiveCount(idx);
            string consecStr  = consecutive >= 4 ? "4+" : consecutive.ToString();

            // Guard: EMASlope(20) needs >= 23 bars on the 10s series
            if (Closes[1].Count < 25)
                return $"consec={consecStr},ema=flat,rsi=neutral,vol=normal";

            int    slope  = EMASlope(20);
            string emaStr = slope > 0 ? "up" : slope < 0 ? "down" : "flat";

            int    zone   = RSIZone(14);
            string rsiStr = zone > 0 ? "ob" : zone < 0 ? "os" : "neutral";

            double volRatio = VolumeRatio(10);
            string volStr   = volRatio > 1.5 ? "high" : volRatio < 0.7 ? "low" : "normal";

            return $"consec={consecStr},ema={emaStr},rsi={rsiStr},vol={volStr}";
        }

        /// <summary>
        /// Records one observation into the frequency lookup table for the given bucket.
        /// </summary>
        private void UpdateLookup(string bucket, bool wasReversal)
        {
            if (!_lookup.ContainsKey(bucket))
                _lookup[bucket] = new ReversalStats();

            ReversalStats s = _lookup[bucket];
            s.Total++;
            if (wasReversal) s.Reversals++;
        }

        /// <summary>
        /// Returns the estimated reversal probability for the given bucket.
        /// Falls back to 0.5 (no edge) when the bucket has not been seen.
        /// </summary>
        private double P_Reversal(string bucket)
        {
            if (_lookup.TryGetValue(bucket, out ReversalStats s))
                return s.P();
            return 0.5;
        }

        #endregion
    }
}
