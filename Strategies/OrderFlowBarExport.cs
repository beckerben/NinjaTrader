#region Using declarations
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.BarsTypes;
#endregion

// OrderFlowBarExportV2 - export-only strategy (places NO orders) for the Strategy Analyzer.
// Same columns as OrderFlowBarExport (v1), in the same order, PLUS:
//   * large-trade order flow: a second 1-minute volumetric series whose delta counts only trades >= LargeTradeSize
//     contracts (lg_volume, lg_buy_volume, lg_sell_volume, lg_bar_delta, lg_trades);
//   * per-price imbalance summary computed from the bar's own price ladder (diagonal imbalances, ratio >= 3,
//     min ImbalanceMinVolume contracts on the dominant side): counts, longest stacked run, and how many sit in the
//     top / bottom quarter of the bar.
// Run exactly like v1: one Analyzer run per specific contract (e.g. "NQ 12-25"), 1 Minute, ETH, roll window + padding.
// Output: C:\temp\<instrument>_1min_of2_<yyyyMMdd-HHmmss>.csv
//
// LargeTradeSize is a CONSTANT on purpose: NinjaTrader requires AddVolumetric arguments to be hard-coded in
// State.Configure (user inputs are not reliable there). 10 is meant for NQ; change it to 20 for ES, recompile (F5),
// and the value is written into every row (lg_size_filter) so the files say what they contain.
namespace NinjaTrader.NinjaScript.Strategies
{
    public class OrderFlowBarExportV2 : Strategy
    {
        private const int LargeTradeSize = 10;
        private const double ImbalanceRatio = 3.0;
        private const long ImbalanceMinVolume = 5;
        private const string TradingHours = "CME US Index Futures ETH";

        private StreamWriter writer;
        private string outPath;
        private long rows, unmatched;
        private string pendingRow;          // v1 + imbalance fields of the last series-1 bar, waiting for series 2
        private DateTime pendingTime;
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Name = "OrderFlowBarExportV2";
                Description = "Exports 1-minute OHLCV + Order Flow Volumetric fields + large-trade delta + imbalance summary. Places no orders.";
                Calculate = Calculate.OnBarClose;
                EntriesPerDirection = 1;
                IsExitOnSessionCloseStrategy = false;
                BarsRequiredToTrade = 0;
                IsInstantiatedOnEachOptimizationIteration = true;
                OutputFolder = @"C:\temp";
            }
            else if (State == State.Configure)
            {
                // BarsInProgress 1: all trades.  BarsInProgress 2: delta counts only trades >= LargeTradeSize (NT8 R17+).
                AddVolumetric(null, BarsPeriodType.Minute, 1, VolumetricDeltaType.BidAsk, 1);
                AddVolumetric(null, BarsPeriodType.Minute, 1, VolumetricDeltaType.BidAsk, 1, LargeTradeSize, TradingHours, null);
            }
            else if (State == State.DataLoaded)
            {
                string dir = Path.IsPathRooted(OutputFolder) ? OutputFolder : Path.Combine(NinjaTrader.Core.Globals.UserDataDir, OutputFolder);
                Directory.CreateDirectory(dir);
                string inst = Instrument.FullName.Replace(" ", "_").Replace("/", "-");
                outPath = Path.Combine(dir, inst + "_1min_of2_" + DateTime.Now.ToString("yyyyMMdd-HHmmss", Inv) + ".csv");
                writer = new StreamWriter(outPath, false);
                writer.WriteLine(string.Join(",", new[] {
                    "barcount", "bar_start_date", "bar_end_date", "bar_duration_ms", "first_bar_of_session",
                    "open", "high", "low", "close", "volume",
                    "buy_volume", "sell_volume", "bar_delta", "delta_pct", "trades",
                    "max_seen_delta", "min_seen_delta", "delta_since_high", "delta_since_low",
                    "max_positive_delta", "max_negative_delta",
                    "poc_price", "poc_volume",
                    "ask_vol_at_high", "bid_vol_at_high", "ask_vol_at_low", "bid_vol_at_low",
                    "instrument",
                    "buy_imb_count", "sell_imb_count", "buy_imb_max_stack", "sell_imb_max_stack",
                    "buy_imb_top_quarter", "sell_imb_bottom_quarter", "price_levels",
                    "lg_size_filter", "lg_volume", "lg_buy_volume", "lg_sell_volume", "lg_bar_delta", "lg_trades" }));
                rows = unmatched = 0;
                pendingRow = null;
                Print("OrderFlowBarExportV2 writing " + outPath + " (large trades >= " + LargeTradeSize + ")");
            }
            else if (State == State.Terminated)
            {
                if (writer != null)
                {
                    FlushPending();
                    writer.Flush();
                    writer.Dispose();
                    writer = null;
                    Print("OrderFlowBarExportV2 wrote " + rows + " rows to " + outPath + "; rows without a large-trade bar: " + unmatched);
                }
            }
        }

        protected override void OnBarUpdate()
        {
            if (writer == null)
                return;
            // NT8 processes series of the same timestamp in BarsInProgress order (1 before 2): series 1 builds the row,
            // series 2 completes it. A series-1 bar with no matching series-2 bar is written with empty lg_ fields.
            if (BarsInProgress == 1 && CurrentBars[1] >= 0)
            {
                FlushPending();
                VolumetricBarsType vbt = BarsArray[1].BarsType as VolumetricBarsType;
                if (vbt == null)
                {
                    Print("OrderFlowBarExportV2: series 1 is not volumetric; nothing written");
                    return;
                }
                pendingTime = Times[1][0];
                pendingRow = BuildRow(vbt, CurrentBars[1]);
            }
            else if (BarsInProgress == 2 && CurrentBars[2] >= 0 && pendingRow != null && Times[2][0] == pendingTime)
            {
                VolumetricBarsType lg = BarsArray[2].BarsType as VolumetricBarsType;
                if (lg == null)
                    return;
                var v = lg.Volumes[CurrentBars[2]];
                Write(pendingRow + "," + string.Join(",", new[] {
                    LargeTradeSize.ToString(Inv), v.TotalVolume.ToString(Inv), v.TotalBuyingVolume.ToString(Inv),
                    v.TotalSellingVolume.ToString(Inv), v.BarDelta.ToString(Inv), v.Trades.ToString(Inv) }));
                pendingRow = null;
            }
        }

        private string BuildRow(VolumetricBarsType vbt, int idx)
        {
            var v = vbt.Volumes[idx];
            double hi = Highs[1][0], lo = Lows[1][0];
            DateTime end = Times[1][0];
            double pocPrice;
            long pocVol = v.GetMaximumVolume(null, out pocPrice);

            // ---- imbalance summary over the bar's price ladder (diagonal: ask at p vs bid one tick below)
            double tick = TickSize;
            int levels = (int)Math.Round((hi - lo) / tick) + 1;
            int buyN = 0, sellN = 0, buyRun = 0, sellRun = 0, buyMax = 0, sellMax = 0, buyTop = 0, sellBot = 0;
            for (int i = 0; i < levels; i++)
            {
                double p = Instrument.MasterInstrument.RoundToTickSize(lo + i * tick);
                long ask = v.GetAskVolumeForPrice(p);
                long bid = v.GetBidVolumeForPrice(p);
                long bidBelow = i > 0 ? v.GetBidVolumeForPrice(Instrument.MasterInstrument.RoundToTickSize(p - tick)) : 0;
                long askAbove = i < levels - 1 ? v.GetAskVolumeForPrice(Instrument.MasterInstrument.RoundToTickSize(p + tick)) : 0;
                bool buyImb = i > 0 && ask >= ImbalanceMinVolume && ask >= ImbalanceRatio * bidBelow;
                bool sellImb = i < levels - 1 && bid >= ImbalanceMinVolume && bid >= ImbalanceRatio * askAbove;
                double pos = levels > 1 ? (double)i / (levels - 1) : 0.5;
                if (buyImb) { buyN++; buyRun++; buyMax = Math.Max(buyMax, buyRun); if (pos >= 0.75) buyTop++; } else buyRun = 0;
                if (sellImb) { sellN++; sellRun++; sellMax = Math.Max(sellMax, sellRun); if (pos <= 0.25) sellBot++; } else sellRun = 0;
            }

            return string.Join(",", new[] {
                idx.ToString(Inv),
                end.AddMinutes(-1).ToString("yyyy-MM-dd HH:mm:ss.fff", Inv),
                end.ToString("yyyy-MM-dd HH:mm:ss.fff", Inv),
                "60000",
                BarsArray[1].IsFirstBarOfSession ? "True" : "False",
                P(Opens[1][0]), P(hi), P(lo), P(Closes[1][0]),
                v.TotalVolume.ToString(Inv),
                v.TotalBuyingVolume.ToString(Inv),
                v.TotalSellingVolume.ToString(Inv),
                v.BarDelta.ToString(Inv),
                v.GetDeltaPercent().ToString("0.####", Inv),
                v.Trades.ToString(Inv),
                v.MaxSeenDelta.ToString(Inv),
                v.MinSeenDelta.ToString(Inv),
                v.DeltaSh.ToString(Inv),
                v.DeltaSl.ToString(Inv),
                v.GetMaximumPositiveDelta().ToString(Inv),
                v.GetMaximumNegativeDelta().ToString(Inv),
                P(pocPrice),
                pocVol.ToString(Inv),
                v.GetAskVolumeForPrice(hi).ToString(Inv),
                v.GetBidVolumeForPrice(hi).ToString(Inv),
                v.GetAskVolumeForPrice(lo).ToString(Inv),
                v.GetBidVolumeForPrice(lo).ToString(Inv),
                Instrument.FullName,
                buyN.ToString(Inv), sellN.ToString(Inv), buyMax.ToString(Inv), sellMax.ToString(Inv),
                buyTop.ToString(Inv), sellBot.ToString(Inv), levels.ToString(Inv) });
        }

        private void FlushPending()
        {
            if (pendingRow == null)
                return;
            Write(pendingRow + "," + LargeTradeSize.ToString(Inv) + ",,,,,");
            unmatched++;
            pendingRow = null;
        }

        private void Write(string line)
        {
            writer.WriteLine(line);
            if (++rows % 5000 == 0)
                writer.Flush();
        }

        private static string P(double x)
        {
            return x.ToString("0.########", Inv);
        }

        #region Properties
        [NinjaScriptProperty]
        [Display(Name = "Output folder", Description = @"Absolute path (default C:\temp); created if missing", Order = 1, GroupName = "Export")]
        public string OutputFolder { get; set; }
        #endregion
    }
}
