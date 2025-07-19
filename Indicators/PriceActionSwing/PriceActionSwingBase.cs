// #############################################################
// #														   #
// #                   PriceActionSwingBase                    #
// #														   #
// #     05.08.2022 by dorschden, die.unendlichkeit@gmx.de     #
// #														   #
// #              Comments are highly appreciated.             #
// #														   #
// #                 License: CC BY-NC-SA 4.0                  #
// #    https://creativecommons.org/licenses/by-nc-sa/4.0/     #
// #														   #
// #############################################################

#region Using declarations
using System;
using System.Collections.Generic;
using System.Windows.Media;
#endregion

namespace NinjaTrader.NinjaScript.Indicators.PriceActionSwing
{
    #region Enums
    public enum VisualizationStyle { Off, Dots, Dots_ZigZag, ZigZag, ZigZagVolume, GannStyle, }
    public enum SwingStyle { Standard, Gann, Ticks, Percent }
    public enum SwingLengthStyle { Off, Ticks, Ticks_Price, Price_Ticks, Points, Points_Price, Price_Points, Price, Percent, }
    public enum SwingDurationStyle { Off, Bars, MMSS, HHMM, SecondsTotal, MinutesTotal, HoursTotal, Days, }
    public enum SwingTimeStyle { Off, Integer, HHMM, HHMMSS, DDMM, }
    public enum SwingVolumeStyle { Off, Absolute, Relative, }
    public enum AbcPatternMode { Off, Long_Short, Long, Short, }
    public enum DivergenceMode { Off, MACD, Stochastics, }
    public enum DivergenceDirection { Long_Short, Long, Short }
    public enum DivergenceType { Regular, Hidden }
    public enum DivergenceBias { Up, Down }
    public enum ShowIndicationStyle { Trend, Relation, Volume }
    public enum StatisticMode { Off, NinjaSciptOutput, Table }
    #endregion

    #region Class objects and DataSeries

    #region public class SwingValues
    public class Swings
    {
        #region Current values
        /// <summary>
        /// Represents the price of the current swing.
        /// </summary>
        public double CurPrice { get; set; }
        /// <summary>
        /// Represents the bar number of the highest/lowest bar of the current swing.
        /// </summary>
        public int CurBar { get; set; }
        /// <summary>
        /// Represents the duration as time values of the current swing.
        /// </summary>
        public DateTime CurDateTime { get; set; }
        /// <summary>
        /// Represents the duration in bars of the current swing.
        /// </summary>
        public int CurDuration { get; set; }
        /// <summary>
        /// Represents the swing length in ticks of the current swing.
        /// </summary>
        public int CurLength { get; set; }
        /// <summary>
        /// Represents the percentage in relation between the last swing and the current swing. 
        /// E. g. 61.8% fib retracement.
        /// </summary>
        public double CurPercent { get; set; }
        /// <summary>
        /// Represents the duration as integer in HHMMSS of the current swing.
        /// </summary>
        public int CurTime { get; set; }
        /// <summary>
        /// Represents the entire volume of the current swing.
        /// </summary>
        public long CurVolume { get; set; }
        /// <summary>
        /// Represents the relation to the previous swing.
        /// -1 = Lower High | 0 = Double Top | 1 = Higher High
        /// </summary>
        public int CurRelation { get; set; }
        public string Output { get; set; }
        public System.Windows.Media.Brush TextColor { get; set; }
        public string TimeOutput { get; set; }
        public string Label { get; set; }
        #endregion

        #region Last values
        /// <summary>
        /// Represents the price of the last swing.
        /// </summary>
        public double LastPrice { get; set; }
        /// <summary>
        /// Represents the bar number of the highest/lowest bar of the last swing.
        /// </summary>
        public int LastBar { get; set; }
        /// <summary>
        /// Represents the duration as time values of the last swing.
        /// </summary>
        public DateTime LastDateTime { get; set; }
        /// <summary>
        /// Represents the duration in bars of the last swing.
        /// </summary>
        public int LastDuration { get; set; }
        /// <summary>
        /// Represents the swing length in ticks of the last swing.
        /// </summary>
        public int LastLength { get; set; }
        /// <summary>
        /// Represents the percentage in relation between the previous swing and the last swing. 
        /// E. g. 61.8% fib retracement.
        /// </summary>
        public double LastPercent { get; set; }
        /// <summary>
        /// Represents the duration as integer in HHMMSS of the last swing.
        /// </summary>
        public int LastTime { get; set; }
        /// <summary>
        /// Represents the entire volume of the last swing.
        /// </summary>
        public long LastVolume { get; set; }
        /// <summary>
        /// Represents the relation to the previous swing.
        /// -1 = Lower High | 0 = Double Top | 1 = Higher High
        /// </summary>
        public int LastRelation { get; set; }
        #endregion

        #region Other values
        /// <summary>
        /// Represents the number of swings.
        /// </summary>
        public int Counter { get; set; }
        /// <summary>
        /// Indicates if a new swing is found.
        /// 0 = no new swing | 1 = new swing  | 2 = complete new swing is found | 3 = update a swing
        /// </summary>
        public int New { get; set; }
        /// <summary>
        /// Represents the volume of the signal bar for the swing.
        /// </summary>
        public double SignalBarVolume { get; set; }
        #endregion
    }
    #endregion

    #region public class CurrentSwing
    public class SwingCurrent
    {
        /// <summary>
        /// Represents the swing slope direction. -1 = down | 0 = init | 1 = up.
        /// </summary>
        public int SwingSlope { get; set; }
        /// <summary>
        /// Represents the bar number of the swing slope change bar.
        /// </summary>
        public int SwingSlopeChangeBar { get; set; }
        /// <summary>
        /// Indicates if a new swing is found. And whether it is a swing high or a swing low.
        /// Used to control, that either a swing high or a swing low is set for each bar.
        /// 0 = no swing | -1 = down swing | 1 = up swing
        /// </summary>
        public int NewSwing { get; set; }
        /// <summary>
        /// Represents the number of consecutives up/down bars.
        /// </summary>
        public int ConsecutiveBars { get; set; }
        /// <summary>
        /// Represents the bar number of the last bar which was counted to the 
        /// consecutives up/down bars.
        /// </summary>
        public int ConsecutiveBarNumber { get; set; }
        /// <summary>
        /// Represents the high/low of the last consecutive bar.
        /// </summary>
        public double ConsecutiveBarValue { get; set; }
        /// <summary>
        /// Indicates if the outside bar calculation is stopped. Used to avoid an up swing and 
        /// a down swing in one bar.
        /// </summary>
        public bool StopOutsideBarCalc { get; set; }
    }
    #endregion

    #region public struct SwingStruct
    public struct SwingStruct
    {
        /// <summary>
        /// Swing price.
        /// </summary>
        public double price;
        /// <summary>
        /// Swing bar number.
        /// </summary>
        public int barNumber;
        /// <summary>
        /// Swing time.
        /// </summary>
        public DateTime time;
        /// <summary>
        /// Swing duration in bars.
        /// </summary>
        public int duration;
        /// <summary>
        /// Swing length in ticks.
        /// </summary>
        public int length;
        /// <summary>
        /// Swing relation.
        /// -1 = Lower | 0 = Double | 1 = Higher
        /// </summary>
        public int relation;
        /// <summary>
        /// Swing volume.
        /// </summary>
        public long volume;
        public string output;
        public System.Windows.Media.Brush textColor;
        public string timeOutput;
        public string label;
        public double percent;
        public SwingStruct(double swingPrice, int swingBarNumber, DateTime swingTime, int swingDuration,
                            int swingLength, int swingRelation, long swingVolume, string swingOutput, Brush swingColor,
                            string swingTimeOutput, string swingLabel, double swingPct)
        {
            price = swingPrice;
            barNumber = swingBarNumber;
            time = swingTime;
            duration = swingDuration;
            length = swingLength;
            relation = swingRelation;
            volume = swingVolume;
            output = swingOutput;
            textColor = swingColor;
            timeOutput = swingTimeOutput;
            label = swingLabel;
            percent = swingPct;
        }
        public SwingStruct(double swingPrice, int swingBarNumber, DateTime swingTime, int swingDuration,
                            int swingLength, int swingRelation, long swingVolume)
        {
            price = swingPrice;
            barNumber = swingBarNumber;
            time = swingTime;
            duration = swingDuration;
            length = swingLength;
            relation = swingRelation;
            volume = swingVolume;
            output = "";
            textColor = Brushes.Transparent;
            timeOutput = "";
            label = "";
            percent = 0.0;
        }
    }
    #endregion

    #endregion
	
    public class PriceActionSwingBase
    {
        #region Fields
        private Indicators.Indicator indicator;

        public int decimalPlaces = 0;
        public double tickThreshold;
        public double percentThreshold;
        public int calculationStartBar = 0;
        public bool visualize = false;

        // user settings 
        private bool UseCloseValues;
        private SwingStyle SwingStyleType;
        private double SwingSize;
        private int DtbStrength;
        private bool IgnoreInsideBars;
        private bool UseBreakouts;

        private SwingLengthStyle SwingLengthType;
        private SwingDurationStyle SwingDurationType;
        private SwingTimeStyle SwingTimeType;
        private Brush TextColorHigherHigh;
        private Brush TextColorLowerHigh;
        private Brush TextColorDoubleTop;
        private Brush TextColorHigherLow;
        private Brush TextColorLowerLow;
        private Brush TextColorDoubleBottom;

        public SwingCurrent CurrentSwing = new SwingCurrent();
        public Swings SwingHigh = new Swings();
        public Swings SwingLow = new Swings();

        public List<SwingStruct> SwingHighs = new List<SwingStruct>();
        public List<SwingStruct> SwingLows = new List<SwingStruct>();

        public Series<bool> DnFlip = null;
        public Series<bool> UpFlip = null;
        #endregion
		
		#region PriceActionSwingBase
        public PriceActionSwingBase(Indicators.Indicator indicator)
        {
            this.indicator = indicator;

            // initialize all the series that depend on the indicator
            this.DnFlip = new Series<bool>(indicator);
            this.UpFlip = new Series<bool>(indicator);
        }
		#endregion

        #region SetUserParameters
        public void SetUserParameters(bool UseCloseValues, SwingStyle SwingStyleType, 
            double SwingSize, int DtbStrength, bool IgnoreInsideBars, bool UseBreakouts, 
            SwingLengthStyle SwingLengthType, SwingDurationStyle SwingDurationType, 
            SwingTimeStyle SwingTimeType,
            Brush TextColorHigherHigh, Brush TextColorLowerHigh, Brush TextColorDoubleTop,
            Brush TextColorHigherLow, Brush TextColorLowerLow, Brush TextColorDoubleBottom
            )
        {
            // initialize the user parameters
            this.UseCloseValues = UseCloseValues;
            this.SwingStyleType = SwingStyleType;
            this.SwingSize = SwingSize;
            this.DtbStrength = DtbStrength;
            this.IgnoreInsideBars = IgnoreInsideBars;
            this.UseBreakouts = UseBreakouts;

            this.SwingLengthType = SwingLengthType;
            this.SwingDurationType = SwingDurationType;
            this.SwingTimeType = SwingTimeType;
            this.TextColorHigherHigh = TextColorHigherHigh;
            this.TextColorLowerHigh = TextColorLowerHigh;
            this.TextColorDoubleTop = TextColorDoubleTop;
            this.TextColorHigherLow = TextColorHigherLow;
            this.TextColorLowerLow = TextColorLowerLow;
            this.TextColorDoubleBottom = TextColorDoubleBottom;
        }
        public void SetUserParameters(bool UseCloseValues, SwingStyle SwingStyleType, 
            double SwingSize, int DtbStrength, bool IgnoreInsideBars, bool UseBreakouts)
        {
            // initialize the user parameters
            this.UseCloseValues = UseCloseValues;
            this.SwingStyleType = SwingStyleType;
            this.SwingSize = SwingSize;
            this.DtbStrength = DtbStrength;
            this.IgnoreInsideBars = IgnoreInsideBars;
            this.UseBreakouts = UseBreakouts;
        }
        #endregion

        #region SetAdditionalValues
        public void SetAdditionalValues()
        {
            // Calculate decimal places
            decimal increment = Convert.ToDecimal(indicator.TickSize);

            if (increment.ToString().Length == 1)
            {
                decimalPlaces = 0;
            }
            else if (increment.ToString().Length > 2)
            {
                decimalPlaces = increment.ToString().Length - 2;
            }

            switch (SwingStyleType)
            {
                case SwingStyle.Standard:
                    SwingSize = SwingSize < 1 ? 1 : Math.Round(SwingSize, MidpointRounding.AwayFromZero);
                    calculationStartBar = (int)SwingSize + 1;
                    break;
                case SwingStyle.Gann:
                    SwingSize = SwingSize < 1 ? 1 : Math.Round(SwingSize, MidpointRounding.AwayFromZero);
                    calculationStartBar = 0;
                    break;
                case SwingStyle.Ticks:
                    SwingSize = SwingSize < 1 ? 1 : Math.Round(SwingSize, MidpointRounding.AwayFromZero);
                    calculationStartBar = 0;
                    tickThreshold = (int)SwingSize * indicator.TickSize;
                    break;
                case SwingStyle.Percent:
                    calculationStartBar = 0;
                    percentThreshold = SwingSize / 100;
                    break;
            }
        }
        #endregion

        #region InitAndResetSwingCalculation
        public void InitAndResetSwingCalculation()
        {
            if (indicator.IsFirstTickOfBar)
            {                
                CurrentSwing.StopOutsideBarCalc = false;

                // Used to control, that only one swing is set for each bar
                CurrentSwing.NewSwing = 0;

                // Initialize first swing
                if (SwingHighs.Count == 0)
                {
                    SwingHigh.CurBar = indicator.CurrentBars[indicator.BarsInProgress];
                    SwingHigh.CurPrice = indicator.Highs[indicator.BarsInProgress][indicator.CurrentBars[indicator.BarsInProgress]];
                    SwingHigh.CurDateTime = indicator.Times[indicator.BarsInProgress][indicator.CurrentBars[indicator.BarsInProgress]];
                    SwingStruct up = new SwingStruct(SwingHigh.CurPrice, SwingHigh.CurBar, indicator.Times[indicator.BarsInProgress][indicator.CurrentBars[indicator.BarsInProgress] - 1],
                                                      0, 0, -1, Convert.ToInt64(indicator.Volumes[indicator.BarsInProgress][0]), SwingHigh.Output, SwingHigh.TextColor,
                                                      SwingHigh.TimeOutput, SwingHigh.Label, SwingHigh.CurPercent);
                    SwingHighs.Add(up);
                }
                if (SwingLows.Count == 0)
                {
                    SwingLow.CurBar = indicator.CurrentBars[indicator.BarsInProgress];
                    SwingLow.CurPrice = indicator.Lows[indicator.BarsInProgress][indicator.CurrentBars[indicator.BarsInProgress]];
                    SwingLow.CurDateTime = indicator.Times[indicator.BarsInProgress][indicator.CurrentBars[indicator.BarsInProgress]];
                    SwingStruct dn = new SwingStruct(SwingLow.CurPrice, SwingLow.CurBar, indicator.Times[indicator.BarsInProgress][indicator.CurrentBars[indicator.BarsInProgress] - 1],
                                                      0, 0, -1, Convert.ToInt64(indicator.Volumes[indicator.BarsInProgress][0]), SwingLow.Output, SwingLow.TextColor,
                                                      SwingLow.TimeOutput, SwingLow.Label, SwingLow.CurPercent);
                    SwingLows.Add(dn);
                }

                DnFlip[0] = false;
                UpFlip[0] = false;
            }

            // Set new high/low back to false, to avoid function calls which depends on them
            SwingHigh.New = 0;
            SwingLow.New = 0;
        }
        #endregion

        #region CalculateSwings
        public void CalculateSwings()
        {
            ISeries<double>[] highs;
            ISeries<double>[] lows;

            // use close or high/low values
            if (UseCloseValues == true)
            {
                lows = indicator.Closes;
                highs = indicator.Closes;
            }
            else
            {
                lows = indicator.Lows;
                highs = indicator.Highs;
            }

            if (SwingStyleType == SwingStyle.Gann)
            {
                #region Set bar property
                // Represents the bar type. -1 = Down | 0 = Inside | 1 = Up | 2 = Outside
                int barType;
                if (indicator.Highs[0][0] > indicator.Highs[0][1])
                {
                    if (indicator.Lows[0][0] < indicator.Lows[0][1])
                        barType = 2;
                    else
                        barType = 1;
                }
                else
                {
                    if (indicator.Lows[0][0] < indicator.Lows[0][1])
                        barType = -1;
                    else
                        barType = 0;
                }
                #endregion

                #region Up swing
                if (CurrentSwing.SwingSlope == 1)
                {
                    switch (barType)
                    {
                        // Up bar
                        case 1:
                            CurrentSwing.ConsecutiveBars = 0;
                            CurrentSwing.ConsecutiveBarValue = 0.0;
                            if (indicator.Highs[0][0] > SwingHigh.CurPrice)
                            {
                                SwingHigh.New = 1;
                                if ((CurrentSwing.ConsecutiveBars + 1) == SwingSize)
                                {
                                    CurrentSwing.StopOutsideBarCalc = true;
                                }
                            }
                            break;
                        // Down bar
                        case -1:
                            if (CurrentSwing.ConsecutiveBarNumber != indicator.CurrentBars[0])
                            {
                                if (CurrentSwing.ConsecutiveBarValue == 0.0)
                                {
                                    CurrentSwing.ConsecutiveBars++;
                                    CurrentSwing.ConsecutiveBarNumber = indicator.CurrentBars[0];
                                    CurrentSwing.ConsecutiveBarValue = indicator.Lows[0][0];
                                }
                                else if (indicator.Lows[0][0] < CurrentSwing.ConsecutiveBarValue)
                                {
                                    CurrentSwing.ConsecutiveBars++;
                                    CurrentSwing.ConsecutiveBarNumber = indicator.CurrentBars[0];
                                    CurrentSwing.ConsecutiveBarValue = indicator.Lows[0][0];
                                }
                            }
                            else if (indicator.Lows[0][0] < CurrentSwing.ConsecutiveBarValue)
                            {
                                CurrentSwing.ConsecutiveBarValue = indicator.Lows[0][0];
                            }

                            if (CurrentSwing.ConsecutiveBars == SwingSize
                                || (UseBreakouts && indicator.Lows[0][0] < SwingLow.CurPrice))
                            {
                                CurrentSwing.ConsecutiveBars = 0;
                                CurrentSwing.ConsecutiveBarValue = 0.0;
                                SwingLow.New = 1;
                                int bar = indicator.CurrentBars[0] - NinjaScriptBase.LowestBar(indicator.Lows[0], indicator.CurrentBars[0] - SwingHigh.CurBar);
                                double price = indicator.Lows[0][NinjaScriptBase.LowestBar(indicator.Lows[0], indicator.CurrentBars[0] - SwingHigh.CurBar)];
                            }
                            break;
                        // Inside bar
                        case 0:
                            if (!IgnoreInsideBars)
                            {
                                CurrentSwing.ConsecutiveBars = 0;
                                CurrentSwing.ConsecutiveBarValue = 0.0;
                            }
                            break;
                        // Outside bar
                        case 2:
                            if (indicator.Highs[0][0] > SwingHigh.CurPrice)
                            {
                                SwingHigh.New = 1;
                            }
                            else if (!CurrentSwing.StopOutsideBarCalc)
                            {
                                if (CurrentSwing.ConsecutiveBarNumber != indicator.CurrentBars[0])
                                {
                                    if (CurrentSwing.ConsecutiveBarValue == 0.0)
                                    {
                                        CurrentSwing.ConsecutiveBars++;
                                        CurrentSwing.ConsecutiveBarNumber = indicator.CurrentBars[0];
                                        CurrentSwing.ConsecutiveBarValue = indicator.Lows[0][0];
                                    }
                                    else if (indicator.Lows[0][0] < CurrentSwing.ConsecutiveBarValue)
                                    {
                                        CurrentSwing.ConsecutiveBars++;
                                        CurrentSwing.ConsecutiveBarNumber = indicator.CurrentBars[0];
                                        CurrentSwing.ConsecutiveBarValue = indicator.Lows[0][0];
                                    }
                                }
                                else if (indicator.Lows[0][0] < CurrentSwing.ConsecutiveBarValue)
                                {
                                    CurrentSwing.ConsecutiveBarValue = indicator.Lows[0][0];
                                }

                                if (CurrentSwing.ConsecutiveBars == SwingSize
                                    || (UseBreakouts && indicator.Lows[0][0] < SwingLow.CurPrice))
                                {
                                    CurrentSwing.ConsecutiveBars = 0;
                                    CurrentSwing.ConsecutiveBarValue = 0.0;
                                    SwingLow.New = 1;
                                    int bar = indicator.CurrentBars[0] - NinjaScriptBase.LowestBar(indicator.Lows[0], indicator.CurrentBars[0] - SwingHigh.CurBar);
                                    double price = indicator.Lows[0][NinjaScriptBase.LowestBar(indicator.Lows[0], indicator.CurrentBars[0] - SwingHigh.CurBar)];
                                }
                            }
                            break;
                    }
                }
                #endregion

                #region Down swing
                else
                {
                    switch (barType)
                    {
                        // Down bar
                        case -1:
                            CurrentSwing.ConsecutiveBars = 0;
                            CurrentSwing.ConsecutiveBarValue = 0.0;
                            if (indicator.Lows[0][0] < SwingLow.CurPrice)
                            {
                                SwingLow.New = 1;
                                if ((CurrentSwing.ConsecutiveBars + 1) == SwingSize)
                                {
                                    CurrentSwing.StopOutsideBarCalc = true;
                                }
                            }
                            break;
                        // Up bar
                        case 1:
                            if (CurrentSwing.ConsecutiveBarNumber != indicator.CurrentBars[0])
                            {
                                if (CurrentSwing.ConsecutiveBarValue == 0.0)
                                {
                                    CurrentSwing.ConsecutiveBars++;
                                    CurrentSwing.ConsecutiveBarNumber = indicator.CurrentBars[0];
                                    CurrentSwing.ConsecutiveBarValue = indicator.Highs[0][0];
                                }
                                else if (indicator.Highs[0][0] > CurrentSwing.ConsecutiveBarValue)
                                {
                                    CurrentSwing.ConsecutiveBars++;
                                    CurrentSwing.ConsecutiveBarNumber = indicator.CurrentBars[0];
                                    CurrentSwing.ConsecutiveBarValue = indicator.Highs[0][0];
                                }
                            }
                            else if (indicator.Highs[0][0] > CurrentSwing.ConsecutiveBarValue)
                            {
                                CurrentSwing.ConsecutiveBarValue = indicator.Highs[0][0];
                            }

                            if (CurrentSwing.ConsecutiveBars == SwingSize
                                || (UseBreakouts && indicator.Highs[0][0] > SwingHigh.CurPrice))
                            {
                                CurrentSwing.ConsecutiveBars = 0;
                                CurrentSwing.ConsecutiveBarValue = 0.0;
                                SwingHigh.New = 1;
                                int bar = indicator.CurrentBars[0] - NinjaScriptBase.HighestBar(indicator.Highs[0], indicator.CurrentBars[0] - SwingLow.CurBar);
                                double price = indicator.Highs[0][NinjaScriptBase.HighestBar(indicator.Highs[0], indicator.CurrentBars[0] - SwingLow.CurBar)];
                            }
                            break;
                        // Inside bar
                        case 0:
                            if (!IgnoreInsideBars)
                            {
                                CurrentSwing.ConsecutiveBars = 0;
                                CurrentSwing.ConsecutiveBarValue = 0.0;
                            }
                            break;
                        // Outside bar
                        case 2:
                            if (indicator.Lows[0][0] < SwingLow.CurPrice)
                            {
                                SwingLow.New = 1;
                            }
                            else if (!CurrentSwing.StopOutsideBarCalc)
                            {
                                if (CurrentSwing.ConsecutiveBarNumber != indicator.CurrentBars[0])
                                {
                                    if (CurrentSwing.ConsecutiveBarValue == 0.0)
                                    {
                                        CurrentSwing.ConsecutiveBars++;
                                        CurrentSwing.ConsecutiveBarNumber = indicator.CurrentBars[0];
                                        CurrentSwing.ConsecutiveBarValue = indicator.Highs[0][0];
                                    }
                                    else if (indicator.Highs[0][0] >
                                        CurrentSwing.ConsecutiveBarValue)
                                    {
                                        CurrentSwing.ConsecutiveBars++;
                                        CurrentSwing.ConsecutiveBarNumber = indicator.CurrentBars[0];
                                        CurrentSwing.ConsecutiveBarValue = indicator.Highs[0][0];
                                    }
                                }
                                else if (indicator.Highs[0][0] > CurrentSwing.ConsecutiveBarValue)
                                {
                                    CurrentSwing.ConsecutiveBarValue = indicator.Highs[0][0];
                                }

                                if (CurrentSwing.ConsecutiveBars == SwingSize
                                    || (UseBreakouts && indicator.Highs[0][0] > SwingHigh.CurPrice))
                                {
                                    CurrentSwing.ConsecutiveBars = 0;
                                    CurrentSwing.ConsecutiveBarValue = 0.0;
                                    SwingHigh.New = 1;
                                    int bar = indicator.CurrentBars[0] - NinjaScriptBase.HighestBar(indicator.Highs[0], indicator.CurrentBars[0] - SwingLow.CurBar);
                                    double price = indicator.Highs[0][NinjaScriptBase.HighestBar(indicator.Highs[0], indicator.CurrentBars[0] - SwingLow.CurBar)];
                                }
                            }
                            break;
                    }
                }
                #endregion
            }
            else
            {
                #region Swing calculation
                // For a new swing high in an uptrend, High[0] must be 
                // greater than the current swing high
                if (CurrentSwing.SwingSlope != 1 || highs[indicator.BarsInProgress][0] > SwingHigh.CurPrice)
                {
                    SwingHigh.New = 1;

                    // No down swing is found
                    if (CurrentSwing.NewSwing != -1)
                    {
                        switch (SwingStyleType)
                        {
                            case SwingStyle.Standard:
                                // test if High[0] is higher than the last 
                                // calculationSize highs = new swing high
                                for (int i = 1; i <= SwingSize; i++)
                                {
                                    if (highs[indicator.BarsInProgress][0] <= highs[indicator.BarsInProgress][i])
                                    {
                                        SwingHigh.New = 0;
                                        break;
                                    }
                                }
                                break;
                            case SwingStyle.Ticks:
                                if (highs[indicator.BarsInProgress][0] < (SwingLow.CurPrice + tickThreshold))
                                {
                                    SwingHigh.New = 0;
                                }
                                break;
                            case SwingStyle.Percent:
                                if (highs[indicator.BarsInProgress][0] < SwingLow.CurPrice + (SwingLow.CurPrice * percentThreshold))
                                {
                                    SwingHigh.New = 0;
                                }
                                break;
                        }
                        // Found a swing high
                        if (SwingHigh.New == 1)
                            CurrentSwing.NewSwing = 1;
                    }
                }

                // For a new swing low in a downtrend, Low[0] must be 
                // smaller than the current swing low
                if (CurrentSwing.SwingSlope != -1 || lows[indicator.BarsInProgress][0] < SwingLow.CurPrice)
                {
                    SwingLow.New = 1;

                    // No up swing is found
                    if (CurrentSwing.NewSwing != 1)
                    {
                        switch (SwingStyleType)
                        {
                            case SwingStyle.Standard:
                                // test if Low[0] is lower than the last 
                                // calculationSize lows = new swing low
                                for (int i = 1; i <= SwingSize; i++)
                                {
                                    if (lows[indicator.BarsInProgress][0] >= lows[indicator.BarsInProgress][i])
                                    {
                                        SwingLow.New = 0;
                                        break;
                                    }
                                }
                                break;
                            case SwingStyle.Ticks:
                                if (lows[indicator.BarsInProgress][0] > (SwingHigh.CurPrice - tickThreshold))
                                {
                                    SwingLow.New = 0;
                                }
                                break;
                            case SwingStyle.Percent:
                                if (lows[indicator.BarsInProgress][0] > SwingHigh.CurPrice - (SwingLow.CurPrice * percentThreshold))
                                {
                                    SwingLow.New = 0;
                                }
                                break;
                        }
                        // Found a swing low
                        if (SwingLow.New == 1)
                            CurrentSwing.NewSwing = -1;
                    }
                }

                // Set newLow back to false
                if (CurrentSwing.NewSwing == 1)
                {
                    SwingLow.New = 0;
                }

                // Set newHigh back to false
                if (CurrentSwing.NewSwing == -1)
                {
                    SwingHigh.New = 0;
                }
                #endregion
            }

            #region New swing high
            // Calculate swing high
            if (SwingHigh.New == 1)
            {
                #region Set core values
                int bar = 0;
                double price = 0.0;
                int barsAgo = 0;
                // New swing high
                if (CurrentSwing.SwingSlope != 1)
                {
                    barsAgo = NinjaScriptBase.HighestBar(highs[indicator.BarsInProgress], indicator.CurrentBars[indicator.BarsInProgress] - SwingLow.CurBar);
                    bar = indicator.CurrentBars[indicator.BarsInProgress] - barsAgo;
                    price = highs[indicator.BarsInProgress][barsAgo];
                    SwingHigh.LastPrice = SwingHigh.CurPrice;
                    SwingHigh.LastBar = SwingHigh.CurBar;
                    SwingHigh.LastDateTime = SwingHigh.CurDateTime;
                    SwingHigh.LastDuration = SwingHigh.CurDuration;
                    SwingHigh.LastLength = SwingHigh.CurLength;
                    SwingHigh.LastTime = SwingHigh.CurTime;
                    SwingHigh.LastPercent = SwingHigh.CurPercent;
                    SwingHigh.LastRelation = SwingHigh.CurRelation;
                    SwingHigh.LastVolume = SwingHigh.CurVolume;
                    SwingHigh.New = 2;
                    SwingHigh.Counter++;
                    CurrentSwing.SwingSlope = 1;
                    CurrentSwing.SwingSlopeChangeBar = bar;
                    UpFlip[0] = true;
                }
                // Update swing high
                else
                {
                    bar = indicator.CurrentBars[indicator.BarsInProgress];
                    price = highs[indicator.BarsInProgress][0];
                    SwingHigh.New = 3;
                    SwingHighs.RemoveAt(SwingHighs.Count - 1);
                }
                #endregion

                #region Update swing values
                SwingHigh.CurBar = bar;
                SwingHigh.CurPrice = price;
                SwingHigh.CurDateTime = indicator.Time[barsAgo];
                SwingHigh.CurTime = NinjaScriptBase.ToTime(SwingHigh.CurDateTime);
                SwingHigh.CurLength = Convert.ToInt32(indicator.Instrument.MasterInstrument.RoundToTickSize(
                    (SwingHigh.CurPrice - SwingLow.CurPrice) / indicator.TickSize));
                if (SwingLow.CurLength != 0)
                    SwingHigh.CurPercent = Math.Round(100.0 / Math.Abs(SwingLow.CurLength) * SwingHigh.CurLength, 1);
                SwingHigh.CurDuration = SwingHigh.CurBar - SwingLow.CurBar;
                // TODO add ATR in OnStateChange
                double dtbOffset = indicator.ATR(indicator.BarsArray[indicator.BarsInProgress], 14)[indicator.CurrentBars[indicator.BarsInProgress] - SwingHigh.CurBar] * DtbStrength / 100;
                if (SwingHigh.CurPrice > SwingHigh.LastPrice - dtbOffset && SwingHigh.CurPrice < SwingHigh.LastPrice + dtbOffset)
                    SwingHigh.CurRelation = 0;
                else if (SwingHigh.CurPrice < SwingHigh.LastPrice)
                    SwingHigh.CurRelation = -1;
                else
                    SwingHigh.CurRelation = 1;
                if (indicator.Calculate != Calculate.OnBarClose)
                    SwingLow.SignalBarVolume = indicator.Volume[0];
                double swingVolume = 0.0;
                for (int i = 0; i < SwingHigh.CurDuration; i++)
                    swingVolume += indicator.Volume[i];
                if (indicator.Calculate != Calculate.OnBarClose)
                    swingVolume += indicator.Volume[indicator.CurrentBars[indicator.BarsInProgress] - SwingLow.CurBar] - SwingHigh.SignalBarVolume;
                SwingHigh.CurVolume = Convert.ToInt64(swingVolume);
                #endregion

                #region Swing value output
                if (visualize)
                {
                    switch (SwingLengthType)
                    {
                        case SwingLengthStyle.Off:
                            break;
                        case SwingLengthStyle.Ticks:
                            SwingHigh.Output = SwingHigh.CurLength.ToString();
                            break;
                        case SwingLengthStyle.Ticks_Price:
                            SwingHigh.Output = SwingHigh.CurLength.ToString() + " / " + SwingHigh.CurPrice.ToString();
                            break;
                        case SwingLengthStyle.Price_Ticks:
                            SwingHigh.Output = SwingHigh.CurPrice.ToString() + " / " + SwingHigh.CurLength.ToString();
                            break;
                        case SwingLengthStyle.Points:
                            SwingHigh.Output = (SwingHigh.CurLength * indicator.TickSize).ToString();
                            break;
                        case SwingLengthStyle.Points_Price:
                            SwingHigh.Output = (SwingHigh.CurLength * indicator.TickSize).ToString() + " / " + SwingHigh.CurPrice.ToString();
                            break;
                        case SwingLengthStyle.Price_Points:
                            SwingHigh.Output = SwingHigh.CurPrice.ToString() + " / " + (SwingHigh.CurLength * indicator.TickSize).ToString();
                            break;
                        case SwingLengthStyle.Price:
                            SwingHigh.Output = SwingHigh.CurPrice.ToString();
                            break;
                        case SwingLengthStyle.Percent:
                            SwingHigh.Output = (Math.Round((100.0 / SwingLow.CurPrice * (SwingHigh.CurLength * indicator.TickSize)), 2, MidpointRounding.AwayFromZero)).ToString();
                            break;
                    }
                    string outputDuration = "";
                    TimeSpan timeSpan;
                    int hours, minutes, seconds;

                    switch (SwingDurationType)
                    {
                        case SwingDurationStyle.Off:
                            break;
                        case SwingDurationStyle.Bars:
                            outputDuration = SwingHigh.CurDuration.ToString();
                            break;
                        case SwingDurationStyle.MMSS:
                            timeSpan = SwingHigh.CurDateTime.Subtract(SwingLow.CurDateTime);
                            minutes = timeSpan.Minutes;
                            seconds = timeSpan.Seconds;
                            if (minutes == 0)
                                outputDuration = "0:" + seconds.ToString();
                            else if (seconds == 0)
                                outputDuration = minutes + ":00";
                            else
                                outputDuration = minutes + ":" + seconds;
                            break;
                        case SwingDurationStyle.HHMM:
                            timeSpan = SwingHigh.CurDateTime.Subtract(SwingLow.CurDateTime);
                            hours = timeSpan.Hours;
                            minutes = timeSpan.Minutes;
                            if (hours == 0)
                                outputDuration = "0:" + minutes.ToString();
                            else if (minutes == 0)
                                outputDuration = hours + ":00";
                            else
                                outputDuration = hours + ":" + minutes;
                            break;
                        case SwingDurationStyle.SecondsTotal:
                            timeSpan = SwingHigh.CurDateTime.Subtract(SwingLow.CurDateTime);
                            outputDuration = Math.Round(timeSpan.TotalSeconds, 1, MidpointRounding.AwayFromZero).ToString();
                            break;
                        case SwingDurationStyle.MinutesTotal:
                            timeSpan = SwingHigh.CurDateTime.Subtract(SwingLow.CurDateTime);
                            outputDuration = Math.Round(timeSpan.TotalMinutes, 1, MidpointRounding.AwayFromZero).ToString();
                            break;
                        case SwingDurationStyle.HoursTotal:
                            timeSpan = SwingHigh.CurDateTime.Subtract(SwingLow.CurDateTime);
                            outputDuration = Math.Round(timeSpan.TotalHours, 1, MidpointRounding.AwayFromZero).ToString();
                            break;
                        case SwingDurationStyle.Days:
                            timeSpan = SwingHigh.CurDateTime.Subtract(SwingLow.CurDateTime);
                            outputDuration = Math.Round(timeSpan.TotalDays, 1, MidpointRounding.AwayFromZero).ToString();
                            break;
                    }
                    if (SwingLengthType != SwingLengthStyle.Off)
                    {
                        if (SwingDurationType != SwingDurationStyle.Off)
                        {
                            SwingHigh.Output = SwingHigh.Output + " / " + outputDuration;
                        }
                    }
                    else
                    {
                        SwingHigh.Output = outputDuration;
                    }

                    Brush textColor = Brushes.Transparent;

                    switch (SwingHigh.CurRelation)
                    {
                        case 1:
                            SwingHigh.Label = "HH";
                            SwingHigh.TextColor = TextColorHigherHigh;
                            break;
                        case -1:
                            SwingHigh.Label = "LH";
                            SwingHigh.TextColor = TextColorLowerHigh;
                            break;
                        case 0:
                            SwingHigh.Label = "DT";
                            SwingHigh.TextColor = TextColorDoubleTop;
                            break;
                    }

                    if (SwingTimeType != SwingTimeStyle.Off)
                    {
                        switch (SwingTimeType)
                        {
                            case SwingTimeStyle.Off:
                                break;
                            case SwingTimeStyle.Integer:
                                SwingHigh.TimeOutput = SwingHigh.CurTime.ToString();
                                break;
                            case SwingTimeStyle.HHMM:
                                SwingHigh.TimeOutput = string.Format("{0:t}", indicator.Times[indicator.BarsInProgress][indicator.CurrentBars[indicator.BarsInProgress] - SwingHigh.CurBar]);
                                break;
                            case SwingTimeStyle.HHMMSS:
                                SwingHigh.TimeOutput = string.Format("{0:T}", indicator.Times[indicator.BarsInProgress][indicator.CurrentBars[indicator.BarsInProgress] - SwingHigh.CurBar]);
                                break;
                            case SwingTimeStyle.DDMM:
                                SwingHigh.TimeOutput = string.Format("{0:dd.MM}", indicator.Times[indicator.BarsInProgress][indicator.CurrentBars[indicator.BarsInProgress] - SwingHigh.CurBar]);
                                break;
                        }
                    }

                    SwingStruct up = new SwingStruct(SwingHigh.CurPrice, SwingHigh.CurBar, SwingHigh.CurDateTime, SwingHigh.CurDuration,
                                                      SwingHigh.CurLength, SwingHigh.CurRelation, SwingHigh.CurVolume, SwingHigh.Output,
                                                      SwingHigh.TextColor, SwingHigh.TimeOutput, SwingHigh.Label, SwingHigh.CurPercent);
                    SwingHighs.Add(up);
                }
                else
                {

                    SwingStruct up = new SwingStruct(SwingHigh.CurPrice, SwingHigh.CurBar, SwingHigh.CurDateTime, SwingHigh.CurDuration,
                                                      SwingHigh.CurLength, SwingHigh.CurRelation, SwingHigh.CurVolume);

                    SwingHighs.Add(up);
                }
                #endregion
            }
            #endregion

            #region New swing low
            // Calculate swing low
            else if (SwingLow.New == 1)
            {
                #region Set core values
                int bar = 0;
                double price = 0.0;
                int barsAgo = 0;
                // New swing low
                if (CurrentSwing.SwingSlope != -1)
                {
                    barsAgo = NinjaScriptBase.LowestBar(lows[indicator.BarsInProgress], indicator.CurrentBars[indicator.BarsInProgress] - SwingHigh.CurBar);
                    bar = indicator.CurrentBars[indicator.BarsInProgress] - barsAgo;
                    price = lows[indicator.BarsInProgress][barsAgo];
                    SwingLow.LastPrice = SwingLow.CurPrice;
                    SwingLow.LastBar = SwingLow.CurBar;
                    SwingLow.LastDateTime = SwingLow.CurDateTime;
                    SwingLow.LastDuration = SwingLow.CurDuration;
                    SwingLow.LastLength = SwingLow.CurLength;
                    SwingLow.LastTime = SwingLow.CurTime;
                    SwingLow.LastPercent = SwingLow.CurPercent;
                    SwingLow.LastRelation = SwingLow.CurRelation;
                    SwingLow.LastVolume = SwingLow.CurVolume;
                    SwingLow.New = 2;
                    SwingLow.Counter++;
                    CurrentSwing.SwingSlope = -1;
                    CurrentSwing.SwingSlopeChangeBar = bar;
                    DnFlip[0] = true;
                }
                // Update swing low
                else
                {
                    bar = indicator.CurrentBars[indicator.BarsInProgress];
                    price = lows[indicator.BarsInProgress][0];
                    SwingLow.New = 3;
                    SwingLows.RemoveAt(SwingLows.Count - 1);
                }
                #endregion

                #region Update Swing values
                SwingLow.CurBar = bar;
                SwingLow.CurPrice = price;
                SwingLow.CurDateTime = indicator.Time[barsAgo];
                SwingLow.CurTime = NinjaScriptBase.ToTime(SwingLow.CurDateTime);
                SwingLow.CurLength = Convert.ToInt32(indicator.Instrument.MasterInstrument.RoundToTickSize(
                    (SwingLow.CurPrice - SwingHigh.CurPrice) / indicator.TickSize));
                if (SwingHigh.CurLength != 0)
                    SwingLow.CurPercent = Math.Round(100.0 / SwingHigh.CurLength * Math.Abs(SwingLow.CurLength), 1);
                SwingLow.CurDuration = SwingLow.CurBar - SwingHigh.CurBar;
                double dtbOffset = indicator.ATR(indicator.BarsArray[indicator.BarsInProgress], 14)[indicator.CurrentBars[indicator.BarsInProgress] - SwingLow.CurBar] * DtbStrength / 100;
                if (SwingLow.CurPrice > SwingLow.LastPrice - dtbOffset && SwingLow.CurPrice < SwingLow.LastPrice + dtbOffset)
                    SwingLow.CurRelation = 0;
                else if (SwingLow.CurPrice < SwingLow.LastPrice)
                    SwingLow.CurRelation = -1;
                else
                    SwingLow.CurRelation = 1;
                if (indicator.Calculate != Calculate.OnBarClose)
                    SwingHigh.SignalBarVolume = indicator.Volume[0];
                double swingVolume = 0.0;
                for (int i = 0; i < SwingLow.CurDuration; i++)
                    swingVolume += indicator.Volume[i];
                if (indicator.Calculate != Calculate.OnBarClose)
                    swingVolume += indicator.Volume[indicator.CurrentBars[indicator.BarsInProgress] - SwingHigh.CurBar] - SwingLow.SignalBarVolume;
                SwingLow.CurVolume = Convert.ToInt64(swingVolume);
                #endregion

                #region Swing value output
                if (visualize)
                {
                    switch (SwingLengthType)
                    {
                        case SwingLengthStyle.Off:
                            break;
                        case SwingLengthStyle.Ticks:
                            SwingLow.Output = SwingLow.CurLength.ToString();
                            break;
                        case SwingLengthStyle.Ticks_Price:
                            SwingLow.Output = SwingLow.CurLength.ToString() + " / " + SwingLow.CurPrice.ToString();
                            break;
                        case SwingLengthStyle.Price_Ticks:
                            SwingLow.Output = SwingLow.CurPrice.ToString() + " / " + SwingLow.CurLength.ToString();
                            break;
                        case SwingLengthStyle.Points:
                            SwingLow.Output = (SwingLow.CurLength * indicator.TickSize).ToString();
                            break;
                        case SwingLengthStyle.Points_Price:
                            SwingLow.Output = (SwingLow.CurLength * indicator.TickSize).ToString() + " / " + SwingLow.CurPrice.ToString();
                            break;
                        case SwingLengthStyle.Price_Points:
                            SwingLow.Output = SwingLow.CurPrice.ToString() + " / " + (SwingLow.CurLength * indicator.TickSize).ToString();
                            break;
                        case SwingLengthStyle.Price:
                            SwingLow.Output = SwingLow.CurPrice.ToString();
                            break;
                        case SwingLengthStyle.Percent:
                            SwingLow.Output = (Math.Round((100.0 / SwingHigh.CurPrice * (SwingLow.CurLength * indicator.TickSize)), 2, MidpointRounding.AwayFromZero)).ToString();
                            break;
                    }

                    string outputDuration = "";
                    TimeSpan timeSpan;
                    int hours, minutes, seconds;

                    switch (SwingDurationType)
                    {
                        case SwingDurationStyle.Off:
                            break;
                        case SwingDurationStyle.Bars:
                            outputDuration = SwingLow.CurDuration.ToString();
                            break;
                        case SwingDurationStyle.MMSS:
                            timeSpan = SwingLow.CurDateTime.Subtract(SwingHigh.CurDateTime);
                            minutes = timeSpan.Minutes;
                            seconds = timeSpan.Seconds;
                            if (minutes == 0)
                                outputDuration = "0:" + seconds.ToString();
                            else if (seconds == 0)
                                outputDuration = minutes + ":00";
                            else
                                outputDuration = minutes + ":" + seconds;
                            break;
                        case SwingDurationStyle.HHMM:
                            timeSpan = SwingLow.CurDateTime.Subtract(SwingHigh.CurDateTime);
                            hours = timeSpan.Hours;
                            minutes = timeSpan.Minutes;
                            if (hours == 0)
                                outputDuration = "0:" + minutes.ToString();
                            else if (minutes == 0)
                                outputDuration = hours + ":00";
                            else
                                outputDuration = hours + ":" + minutes;
                            break;
                        case SwingDurationStyle.SecondsTotal:
                            timeSpan = SwingLow.CurDateTime.Subtract(SwingHigh.CurDateTime);
                            outputDuration = Math.Round(timeSpan.TotalSeconds, 1, MidpointRounding.AwayFromZero).ToString();
                            break;
                        case SwingDurationStyle.MinutesTotal:
                            timeSpan = SwingLow.CurDateTime.Subtract(SwingHigh.CurDateTime);
                            outputDuration = Math.Round(timeSpan.TotalMinutes, 1, MidpointRounding.AwayFromZero).ToString();
                            break;
                        case SwingDurationStyle.HoursTotal:
                            timeSpan = SwingLow.CurDateTime.Subtract(SwingHigh.CurDateTime);
                            outputDuration = timeSpan.TotalHours.ToString();
                            break;
                        case SwingDurationStyle.Days:
                            timeSpan = SwingLow.CurDateTime.Subtract(SwingHigh.CurDateTime);
                            outputDuration = Math.Round(timeSpan.TotalDays, 1, MidpointRounding.AwayFromZero).ToString();
                            break;
                    }
                    if (SwingLengthType != SwingLengthStyle.Off)
                    {
                        if (SwingDurationType != SwingDurationStyle.Off)
                        {
                            SwingLow.Output += " / " + outputDuration;
                        }
                    }
                    else
                    {
                        SwingLow.Output = outputDuration;
                    }

                    switch (SwingLow.CurRelation)
                    {
                        case 1:
                            SwingLow.Label = "HL";
                            SwingLow.TextColor = TextColorHigherLow;
                            break;
                        case -1:
                            SwingLow.Label = "LL";
                            SwingLow.TextColor = TextColorLowerLow;
                            break;
                        case 0:
                            SwingLow.Label = "DB";
                            SwingLow.TextColor = TextColorDoubleBottom;
                            break;
                    }

                    if (SwingTimeType != SwingTimeStyle.Off)
                    {
                        switch (SwingTimeType)
                        {
                            case SwingTimeStyle.Off:
                                break;
                            case SwingTimeStyle.Integer:
                                SwingLow.TimeOutput = SwingLow.CurTime.ToString();
                                break;
                            case SwingTimeStyle.HHMM:
                                SwingLow.TimeOutput = string.Format("{0:t}", indicator.Times[indicator.BarsInProgress][indicator.CurrentBars[indicator.BarsInProgress] - SwingLow.CurBar]);
                                break;
                            case SwingTimeStyle.HHMMSS:
                                SwingLow.TimeOutput = string.Format("{0:T}", indicator.Times[indicator.BarsInProgress][indicator.CurrentBars[indicator.BarsInProgress] - SwingLow.CurBar]);
                                break;
                            case SwingTimeStyle.DDMM:
                                SwingLow.TimeOutput = string.Format("{0:dd.MM}", indicator.Times[indicator.BarsInProgress][indicator.CurrentBars[indicator.BarsInProgress] - SwingHigh.CurBar]);
                                break;
                        }
                    }

                    SwingStruct dn = new SwingStruct(SwingLow.CurPrice, SwingLow.CurBar, SwingLow.CurDateTime, SwingLow.CurDuration,
                                                  SwingLow.CurLength, SwingLow.CurRelation, SwingLow.CurVolume, SwingLow.Output,
                                                  SwingLow.TextColor, SwingLow.TimeOutput, SwingLow.Label, SwingLow.CurPercent);
                    SwingLows.Add(dn);
                }
                else
                {
                    SwingStruct dn = new SwingStruct(SwingLow.CurPrice, SwingLow.CurBar, SwingLow.CurDateTime, SwingLow.CurDuration,
                                                  SwingLow.CurLength, SwingLow.CurRelation, SwingLow.CurVolume);
                    SwingLows.Add(dn);
                }
                #endregion
            }
            #endregion
        }
        #endregion
    }
}
