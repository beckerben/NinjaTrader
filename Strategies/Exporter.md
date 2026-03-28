# Exporter — Column Reference

Strategy that writes bar data and indicator values to a CSV file for machine learning and statistical analysis.

## Normalization Approach

### Price-level indicators
Any indicator that returns a price (Bollinger bands, pivot levels, VWAP, etc.) is expressed in **ATR units**:

```
(indicator_price - close) / ATR(14)
```

A value of `1.0` means the indicator is exactly one ATR above the current close. A value of `-0.5` means it is half an ATR below close. This approach is **volatility-adjusted** and regime-independent — the same number means the same thing whether the instrument is trading at 5,000 or 20,000 or whether it is a quiet or volatile session.

### Non-price indicators
Oscillators and indicators that are already on their own scale (RSI 0–100, ADX, Stochastics, etc.) are exported as-is with light rounding.

---

## Always-On Columns (never toggled off)

| Column | Type | Description |
|---|---|---|
| `barcount` | int | NinjaTrader `CurrentBar` index (0-based) |
| `bar_start_date` | datetime | Timestamp of bar open: `yyyy-MM-dd HH:mm:ss.fff` |
| `bar_duration_ms` | long | Milliseconds elapsed from prior bar start to this bar start. Constant for time-based bars; varies for range/renko/tick bars. |

---

## Export Groups

Each group is independently toggled via the **Export Groups** property section in the strategy UI.

---

### OHLCV (Raw) — `Export_OHLCV`

Raw price and volume data plus bar classification fields.

| Column | Type | Description |
|---|---|---|
| `open` | double | Bar open price |
| `high` | double | Bar high price |
| `low` | double | Bar low price |
| `close` | double | Bar close price |
| `volume` | double | Bar volume (contracts/shares) |
| `higherclose` | bool | `True` if `close > close[1]` |
| `reversal` | bool | `True` if `higherclose` changed direction from prior bar |
| `trendsequence` | int | Count of consecutive bars in the same close direction; resets to 1 on reversal |

---

### OHLCV (ATR Normalized) — `Export_OHLCV_Normalized`

Price and volume values expressed relative to volatility. More useful for ML than raw prices.

| Column | Type | Formula | Description |
|---|---|---|---|
| `open_atr` | double | `(open - close) / ATR(14)` | Bar open relative to close in ATR units |
| `high_atr` | double | `(high - close) / ATR(14)` | Bar high relative to close in ATR units (always ≥ 0) |
| `low_atr` | double | `(low - close) / ATR(14)` | Bar low relative to close in ATR units (always ≤ 0) |
| `close_return_atr` | double | `(close - close[1]) / ATR(14)` | Bar return expressed in ATR units |
| `volume_ratio` | double | `volume / VOLMA(14)` | Volume relative to its 14-bar moving average; 1.0 = average, 2.0 = double average |

---

### Trend Indicators — `Export_Trend`

Indicators that measure trend direction, strength, and momentum of the primary trend.

| Column | Type | Formula / Parameters | Description |
|---|---|---|---|
| `adx` | int | `ADX(14)` | Average Directional Index — trend strength 0–100; above 25 = trending |
| `adxr` | int | `ADXR(10,14)` | ADX Rating — smoothed ADX |
| `aroonoscillator` | int | `AroonOscillator(14)` | Aroon Up minus Aroon Down; range −100 to +100 |
| `aroon_up` | int | `Aroon(14).Up` | Aroon Up — 0–100, measures bars since highest high |
| `aroon_down` | int | `Aroon(14).Down` | Aroon Down — 0–100, measures bars since lowest low |
| `dm_diplus` | int | `DM(14).DiPlus` | Positive Directional Indicator (+DI) |
| `dm_diminus` | int | `DM(14).DiMinus` | Negative Directional Indicator (−DI) |
| `dmi` | int | `DMI(14)` | Directional Movement Index |
| `dmindex` | int | `DMIndex(3)` | Dynamic Momentum Index |
| `disparityindex` | double | `DisparityIndex(25)` | % difference between close and a 25-period MA |
| `linreg` | double | `(LinReg(14) - close) / ATR(14)` | 14-period linear regression value, ATR-normalized |
| `linregintercept` | double | `(LinRegIntercept(14) - close) / ATR(14)` | Linear regression y-intercept, ATR-normalized |
| `linregslope` | double | `LinRegSlope(14)` | Slope of the 14-period linear regression line |
| `stderror` | double | `StdError(14)` | Standard error of the 14-period linear regression |
| `macd` | double | `MACD(12,26,9)` | MACD line (12 EMA − 26 EMA) |
| `macd_avg` | double | `MACD(12,26,9).Avg` | MACD signal line (9 EMA of MACD) |
| `macd_diff` | double | `MACD(12,26,9).Diff` | MACD histogram (MACD − signal) |
| `mama_default` | double | `(MAMA(0.5,0.05).Default - close) / ATR(14)` | MESA Adaptive Moving Average, ATR-normalized |
| `mama_fama` | double | `(MAMA(0.5,0.05).Fama - close) / ATR(14)` | Following Adaptive Moving Average, ATR-normalized |
| `trix` | double | `TRIX(14,3)` | Triple-smoothed EMA rate of change |
| `trix_signal` | double | `TRIX(14,3).Signal` | TRIX signal line |
| `tsf` | double | `(TSF(3,14) - close) / ATR(14)` | Time Series Forecast, ATR-normalized |
| `tsi` | int | `TSI(3,14)` | True Strength Index |
| `vortex_viplus` | double | `Vortex(14).VIPlus` | Vortex +VI (bullish movement) |
| `vortex_viminus` | double | `Vortex(14).VIMinus` | Vortex −VI (bearish movement) |

---

### Momentum Indicators — `Export_Momentum`

Oscillators that measure the rate and magnitude of price changes.

| Column | Type | Parameters | Description |
|---|---|---|---|
| `cci` | int | `CCI(14)` | Commodity Channel Index; ±100 = normal range |
| `cmo` | int | `CMO(14)` | Chande Momentum Oscillator; range −100 to +100 |
| `momentum` | int | `Momentum(14)` | Raw price momentum (close − close[14]) |
| `mfi` | int | `MFI(14)` | Money Flow Index; volume-weighted RSI |
| `moneyflowoscillator` | double | `MoneyFlowOscillator(20)` | Money Flow Oscillator |
| `pfe` | double | `PFE(14,10)` | Polarized Fractal Efficiency; measures trend efficiency |
| `ppo` | double | `PPO(12,26,9).Smoothed` | Percentage Price Oscillator (smoothed) |
| `priceoscillator` | double | `PriceOscillator(12,26,9)` | Price difference between two MAs |
| `psychologicalline` | double | `PsychologicalLine(10)` | % of bars that closed higher over last 10 bars |
| `rind` | int | `RIND(3,10)` | Range Indicator |
| `roc` | double | `ROC(14)` | Rate of Change (% price change over 14 bars) |
| `rsi` | int | `RSI(14,3)` | Relative Strength Index; 30 = oversold, 70 = overbought |
| `rsi_avg` | int | `RSI(14,3).Avg` | RSI signal line (3-period smoothing) |
| `rss` | int | `RSS(10,40,5)` | Relative Spread Strength |
| `rvi` | int | `RVI(14)` | Relative Volatility Index |
| `stochrsi` | double | `StochRSI(14)` | Stochastic RSI; applies stochastics formula to RSI |
| `stochastics_d` | int | `Stochastics(7,14,3).D` | Stochastic %D (3-period MA of %K) |
| `stochastics_k` | int | `Stochastics(7,14,3).K` | Stochastic %K |
| `stochasticsfast_d` | int | `StochasticsFast(3,14).D` | Fast Stochastic %D |
| `stochasticsfast_k` | int | `StochasticsFast(3,14).K` | Fast Stochastic %K |
| `ultimateoscillator` | int | `UltimateOscillator(7,14,28)` | Combines 3 timeframes; range 0–100 |
| `williamsr` | int | `WilliamsR(14)` | Williams %R; range −100 to 0; −80 to −100 = oversold |

---

### Volatility Indicators — `Export_Volatility`

Indicators that measure price bands, ranges, and volatility levels. Price-level outputs are ATR-normalized.

| Column | Type | Formula | Description |
|---|---|---|---|
| `atr` | double | `ATR(14)` | Average True Range — raw value in price units (the normalization divisor) |
| `apz_lower` | double | `(APZ(2,20).Lower - close) / ATR(14)` | Adaptive Price Zone lower band, ATR-normalized |
| `apz_upper` | double | `(APZ(2,20).Upper - close) / ATR(14)` | Adaptive Price Zone upper band, ATR-normalized |
| `bollinger_lower` | double | `(Bollinger(2,14).Lower - close) / ATR(14)` | Bollinger lower band (2 std devs), ATR-normalized |
| `bollinger_middle` | double | `(Bollinger(2,14).Middle - close) / ATR(14)` | Bollinger middle band (20 SMA), ATR-normalized |
| `bollinger_upper` | double | `(Bollinger(2,14).Upper - close) / ATR(14)` | Bollinger upper band (2 std devs), ATR-normalized |
| `bop` | double | `BOP(14)` | Balance of Power; range −1 to +1 |
| `chaikinvolatility` | int | `ChaikinVolatility(10,10)` | Chaikin Volatility; % change in high−low range |
| `choppinessindex` | int | `ChoppinessIndex(14)` | 100 = max chop, lower = trending; threshold ~61.8 |
| `donchian_lower` | double | `(DonchianChannel(14).Lower - close) / ATR(14)` | Donchian lower band (14-bar lowest low), ATR-normalized |
| `donchian_mean` | double | `(DonchianChannel(14).Mean - close) / ATR(14)` | Donchian midline, ATR-normalized |
| `donchian_upper` | double | `(DonchianChannel(14).Upper - close) / ATR(14)` | Donchian upper band (14-bar highest high), ATR-normalized |
| `doublestochastics_k` | int | `DoubleStochastics(10).K` | Double-smoothed Stochastic %K |
| `easeofmovement` | int | `EaseOfMovement(10,1000)` | Ease of Movement; positive = rising on low volume |
| `fisherstransform` | double | `FisherTransform(10)` | Fisher Transform; extreme values signal reversals |
| `fosc` | double | `FOSC(14)` | Forecast Oscillator; % deviation of close from linear regression |
| `kama` | double | `(KAMA(2,10,30) - close) / ATR(14)` | Kaufman Adaptive Moving Average, ATR-normalized |
| `keltner_lower` | double | `(KeltnerChannel(1.5,10).Lower - close) / ATR(14)` | Keltner lower band, ATR-normalized |
| `keltner_mean` | double | `(KeltnerChannel(1.5,10).Mean - close) / ATR(14)` | Keltner midline (EMA), ATR-normalized |
| `keltner_upper` | double | `(KeltnerChannel(1.5,10).Upper - close) / ATR(14)` | Keltner upper band, ATR-normalized |
| `parabolic_sar` | double | `(ParabolicSAR(0.02,0.2,0.02) - close) / ATR(14)` | Parabolic SAR level, ATR-normalized; negative = above price (bearish) |
| `relativevigorindex` | double | `RelativeVigorIndex(10)` | Relative Vigor Index |
| `rsquared` | double | `RSquared(8)` | R² of the 8-bar linear regression; 1.0 = perfect trend |
| `stddev` | double | `StdDev(14)` | Standard deviation of close over 14 bars (raw price units) |

---

### Volume Indicators — `Export_Volume`

Cumulative and flow-based volume measures.

| Column | Type | Parameters | Description |
|---|---|---|---|
| `adl` | double | `ADL().AD` | Accumulation/Distribution Line; cumulative money flow |
| `obv` | double | `OBV()` | On Balance Volume; cumulative volume by direction |
| `chaikinmoneyflow` | int | `ChaikinMoneyFlow(21)` | Chaikin Money Flow; range −1 to +1 (scaled ×100 by NT) |
| `chaikinoscillator` | int | `ChaikinOscillator(3,10)` | Chaikin Oscillator; MACD applied to ADL |
| `volma` | int | `VOLMA(14)` | Volume Moving Average (14-bar simple MA of volume) |
| `volume_oscillator` | int | `VolumeOscillator(12,26)` | % difference between two volume MAs |
| `vroc` | int | `VROC(14,3)` | Volume Rate of Change |
| `buysell_buypressure` | int | `BuySellPressure().BuyPressure` | Buy-side pressure |
| `buysell_sellpressure` | int | `BuySellPressure().SellPressure` | Sell-side pressure |

---

### Pivot Levels — `Export_Pivots`

Support/resistance price levels, all ATR-normalized: `(level - close) / ATR(14)`.

| Column | Level |
|---|---|
| `camarilla_r1` … `camarilla_r4` | Camarilla resistance levels R1–R4 |
| `camarilla_s1` … `camarilla_s4` | Camarilla support levels S1–S4 |
| `fibonacci_pp` | Fibonacci pivot point |
| `fibonacci_r1` … `fibonacci_r3` | Fibonacci resistance levels R1–R3 |
| `fibonacci_s1` … `fibonacci_s3` | Fibonacci support levels S1–S3 |
| `pivot_pp` | Classic/standard pivot point |
| `pivot_r1` … `pivot_r3` | Classic resistance levels R1–R3 |
| `pivot_s1` … `pivot_s3` | Classic support levels S1–S3 |
| `woodiespivot_pp` | Woodies pivot point |
| `woodiespivot_r1`, `woodiespivot_r2` | Woodies resistance R1–R2 |
| `woodiespivot_s1`, `woodiespivot_s2` | Woodies support S1–S2 |
| `currentday_open` | Current session open, ATR-normalized |
| `currentday_low` | Current session low so far, ATR-normalized |
| `currentday_high` | Current session high so far, ATR-normalized |
| `priorday_open` | Prior session open, ATR-normalized |
| `priorday_high` | Prior session high, ATR-normalized |
| `priorday_low` | Prior session low, ATR-normalized |
| `priorday_close` | Prior session close, ATR-normalized |

All pivot calculations use `PivotRange.Daily` with `HLCCalculationMode.CalcFromIntradayData`.

---

### Order Flow — `Export_OrderFlow`

Tick-level bid/ask data aggregated to bar. **Requires tick data replayed.**

| Column | Type | Description |
|---|---|---|
| `orderflowcumulativedelta_deltaopen` | double | Cumulative delta at bar open |
| `orderflowcumulativedelta_deltaclose` | double | Cumulative delta at bar close |
| `orderflowcumulativedelta_deltahigh` | double | Maximum cumulative delta during bar |
| `orderflowcumulativedelta_deltalow` | double | Minimum cumulative delta during bar |
| `orderflowvwap_vwap` | double | Session VWAP, ATR-normalized |
| `orderflowvwap_s1_lower` | double | VWAP −1 std dev band, ATR-normalized |
| `orderflowvwap_s1_upper` | double | VWAP +1 std dev band, ATR-normalized |
| `orderflowvwap_s2_lower` | double | VWAP −2 std dev band, ATR-normalized |
| `orderflowvwap_s2_upper` | double | VWAP +2 std dev band, ATR-normalized |
| `orderflowvwap_s3_lower` | double | VWAP −3 std dev band, ATR-normalized |
| `orderflowvwap_s3_upper` | double | VWAP +3 std dev band, ATR-normalized |

---

### Fair Value Gaps — `Export_FVG`

A Fair Value Gap (FVG) is a 3-bar price pattern where the market moves so quickly that a gap is left between the first bar's extreme and the third bar's extreme — a zone where no trading occurred.

**Bullish FVG:** `High[2] < Low[0]` — the high of the bar 2 bars ago is below the low of the current bar, with bar 1 being an impulsive up move that skipped price.

**Bearish FVG:** `Low[2] > High[0]` — the low of the bar 2 bars ago is above the high of the current bar, with bar 1 being an impulsive down move.

| Column | Type | Formula | Description |
|---|---|---|---|
| `fvg_direction` | string | — | `"bullish"`, `"bearish"`, or empty if no FVG on this bar |
| `fvg_top` | double | Bullish: `Low[0]`; Bearish: `Low[2]` | Upper price boundary of the gap zone (empty if no FVG) |
| `fvg_bottom` | double | Bullish: `High[2]`; Bearish: `High[0]` | Lower price boundary of the gap zone (empty if no FVG) |
| `fvg_midpoint` | double | `(fvg_top + fvg_bottom) / 2` | Midpoint of the gap zone — useful for partial fill analysis (empty if no FVG) |
| `fvg_size` | double | `fvg_top - fvg_bottom` | Raw gap size in price units (empty if no FVG) |
| `fvg_top_atr` | double | `(fvg_top - close) / ATR(14)` | Gap top relative to close in ATR units (empty if no FVG) |
| `fvg_bottom_atr` | double | `(fvg_bottom - close) / ATR(14)` | Gap bottom relative to close in ATR units (empty if no FVG) |
| `fvg_midpoint_atr` | double | `(fvg_midpoint - close) / ATR(14)` | Gap midpoint relative to close in ATR units (empty if no FVG) |
| `fvg_size_atr` | double | `fvg_size / ATR(14)` | Gap size in ATR units — regime-independent measure of significance (empty if no FVG) |

All FVG columns are empty on the first 2 bars (insufficient history) and empty on bars where no FVG was formed.

**Closure detection in analysis:** A bullish FVG (zone `[fvg_bottom, fvg_top]`) is closed when a future bar's `low <= fvg_bottom`. A bearish FVG is closed when a future bar's `high >= fvg_top`. Use `fvg_top` and `fvg_bottom` directly from the row where the FVG was created — no row lookback required.

```python
# Example: bars to close a bullish FVG
fvgs = df[df['fvg_direction'] == 'bullish'][['barcount', 'fvg_bottom']].copy()
results = []
for _, fvg in fvgs.iterrows():
    future = df[df['barcount'] > fvg['barcount']]
    closed = future[future['low'] <= fvg['fvg_bottom']]
    if not closed.empty:
        results.append(closed.iloc[0]['barcount'] - fvg['barcount'])
print(f"Average bars to close: {sum(results) / len(results):.1f}")
```

---

### Custom / Other — `Export_Custom`

| Column | Type | Parameters | Description |
|---|---|---|---|
| `woodiescci` | int | `WoodiesCCI(2,5,14,34,25,6,60,100,2)` | Woodies CCI main line |
| `woodiescci_turbo` | int | `WoodiesCCI(...).Turbo` | Woodies CCI turbo (faster) line |
| `wisemanawesomeoscillator` | double | `WisemanAwesomeOscillator()` | Bill Williams Awesome Oscillator (5 SMA − 34 SMA of midpoints) |
| `alligator_jaw` | double | `(WisemanAlligator(13,8,8,5,5,3).Jaw - close) / ATR(14)` | Wiseman Alligator Jaw (13-period SMMA offset 8), ATR-normalized |
| `alligator_teeth` | double | `(WisemanAlligator(13,8,8,5,5,3).Teeth - close) / ATR(14)` | Wiseman Alligator Teeth (8-period SMMA offset 5), ATR-normalized |
| `alligator_lips` | double | `(WisemanAlligator(13,8,8,5,5,3).Lips - close) / ATR(14)` | Wiseman Alligator Lips (5-period SMMA offset 3), ATR-normalized |
| `highestlowestlines_high` | double | `(HighestLowestLines(21).HighestHigh - close) / ATR(14)` | 21-bar highest high, ATR-normalized |
| `highestlowestlines_mid` | double | `(HighestLowestLines(21).Midline - close) / ATR(14)` | Midpoint of 21-bar high/low range, ATR-normalized |
| `highestlowestlines_low` | double | `(HighestLowestLines(21).LowestLow - close) / ATR(14)` | 21-bar lowest low, ATR-normalized |

---

## Time Filter

When **Enable Time Filter** is `true`, only bars whose timestamp falls within `[Start Time, End Time)` are exported. The end time is exclusive (a bar at exactly End Time is not exported). The date portion of Start/End Time is ignored — only the hour and minute are used.

## File Naming

If **Output File** is left blank, the file is auto-named:
```
{InstrumentName}_{yyyyMMddHHmmss}.csv
```

The file is opened once at strategy load and appended to on each bar close. If the file already exists and is non-empty, no header is written and new rows are appended.

## Missing Values

Individual cells are written as empty string when an indicator is not yet ready (insufficient bars) or produces NaN/Infinity. The row is always written — a bad indicator never drops the entire row.
