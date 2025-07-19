#region Using declarations
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.NinjaScript.Indicators.Gemify;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using System.Xml.Serialization;
using Brush = System.Windows.Media.Brush;
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators.Gemify
{
    // FVG fill type
    public enum FVGFillType
    {
        CLOSE_THROUGH,
        PIERCE_THROUGH
    }

    // Supported period types for FVG detection
    public enum FVGPeriodTypes
    {
        Tick,
        Volume,
        Second,
        Minute,
        Day,
        Week,
        Month,
        Year
    }

    [Gui.CategoryOrder("FVG Data Series", 1)]
    [Gui.CategoryOrder("Parameters", 2)]
    [Gui.CategoryOrder("FVG Data Series Label", 3)]
    [Gui.CategoryOrder("FVG Colors", 4)]
    public class ICTFVG : Indicator
    {

        // Support or Resistance FVG
        public enum FVGType
        {
            R, S
        }

        public class FVG
        {
            public double upperPrice;
            public double lowerPrice;
            public double consequentEncroachmentPrice;
            public string tag;
            public FVGType type;
            public bool filled;
            public DateTime gapStartTime;
            public DateTime fillTime;

            public FVG(string tag, FVGType type, double lowerPrice, double uppperPrice, DateTime gapStartTime)
            {
                this.tag = tag;
                this.type = type;
                this.lowerPrice = lowerPrice;
                this.upperPrice = uppperPrice;
                this.consequentEncroachmentPrice = (this.lowerPrice + this.upperPrice) / 2.0;
                this.filled = false;
                this.gapStartTime = gapStartTime;
            }
        }

        private List<FVG> fvgList = new List<FVG>();
        private ATR atr;
        private Brush FillBrush;
        private int MIN_BARS_REQUIRED = 3;
        private DateTime future;
        private String InstanceId;
        private String sDataSeries;
        private int iDataSeries = 1;

        private bool IsDebug = false;

        private void Debug(String str)
        {
            if (IsDebug) Print(this.Name + " :: " + str);
        }

        protected override void OnStateChange()
        {
            Debug(">>>>> " + State);

            if (State == State.SetDefaults)
            {
                Description = @"Fair Value Gap (ICT)";
                Name = "ICTFVG v0.0.2.2";
                Calculate = Calculate.OnBarClose;
                IsOverlay = true;
                DisplayInDataBox = true;
                DrawOnPricePanel = true;
                DrawHorizontalGridLines = true;
                DrawVerticalGridLines = true;
                PaintPriceMarkers = true;
                ScaleJustification = NinjaTrader.Gui.Chart.ScaleJustification.Right;
                //Disable this property if your indicator requires custom values that cumulate with each new market data event. 
                //See Help Guide for additional information.
                IsSuspendedWhileInactive = true;
                MaxBars = 1000;

                UpBrush = Brushes.DarkGreen;
                DownBrush = Brushes.Maroon;
                UpAreaBrush = Brushes.DarkGreen;
                DownAreaBrush = Brushes.Maroon;
                FillBrush = Brushes.DimGray;
                ActiveAreaOpacity = 13;
                FilledAreaOpacity = 4;
                UseATR = true;
                ATRPeriod = 10;
                ImpulseFactor = 1.1;
                HideFilledGaps = true;
                FillType = FVGFillType.CLOSE_THROUGH;
                DisplayCE = false;
                MinimumFVGSize = 0.01;

                UseFVGDataSeries = true;
                FVGSeriesPeriod = 1;
                FVGBarsPeriodType = FVGPeriodTypes.Minute;

                LabelPosition = TextPosition.TopRight;
                DrawLabel = true;
                LabelFont = new SimpleFont("Verdana", 12);
                LabelTextBrush = Brushes.WhiteSmoke;
                LabelBorderBrush = Brushes.DimGray;
                LabelFillBrush = Brushes.Blue;
                LabelFillOpacity = 50;

            }
            else if (State == State.Configure)
            {
                Debug("Adding " + FVGBarsPeriodType + " [" + FVGSeriesPeriod + "] Series");


                // Add additional data series
                if (UseFVGDataSeries)
                {
                    AddDataSeries((BarsPeriodType)FVGBarsPeriodType, FVGSeriesPeriod);
                    iDataSeries = 1;
                }
                else
                {
                    iDataSeries = 0;
                }
                // Helps keep track of draw object tags
                // if multiple instances are present on the same chart
                InstanceId = Guid.NewGuid().ToString();
            }
            else if (State == State.DataLoaded)
            {
                // Add ATR
                atr = ATR(Closes[iDataSeries], ATRPeriod);
                //Set series name value for Label
                sDataSeries = BarsArray[iDataSeries].BarsPeriod.ToString() + " FVG";
            }
        }

        protected override void OnBarUpdate()
        {
            // Only operate on selected data series type
            if (BarsInProgress != iDataSeries) return;

            // Nothing to do if current bar is earlier than lookback max
            if (CurrentBars[iDataSeries] <= (Bars.Count - Math.Min(Bars.Count, MaxBars)) + MIN_BARS_REQUIRED) return;

            if (DrawLabel)
            {
                Draw.TextFixed(this, "FVG_PERIOD_" + InstanceId, sDataSeries, LabelPosition, LabelTextBrush, LabelFont, LabelBorderBrush, LabelFillBrush, LabelFillOpacity);
            }

            Debug("Checking for FVGs that are filled.");

            // Mark FVGs that have been filled
            CheckFilledFVGs();

            Debug("Checking if there have been any impluse moves.");

            // FVG only applies if there's been an impulse move
            if ((UseATR && Math.Abs(Highs[iDataSeries][1] - Lows[iDataSeries][1]) >= ImpulseFactor * atr.Value[0]) || !UseATR)
            {
                Debug("Impulse move detected.");

                future = Times[iDataSeries][0].AddDays(ChartBars.Properties.DaysBack);

                // Fair value gap while going UP
                // Low[0] > High[2]
                if (Lows[iDataSeries][0] > Highs[iDataSeries][2] && (Math.Abs(Lows[iDataSeries][0] - Highs[iDataSeries][2]) >= MinimumFVGSize))
                // IDEA: Potential FVG filtering based on ATR: && (Math.Abs(Lows[iDataSeries][0] - Highs[iDataSeries][2]) >= ImpulseFactor * atr.Value[0]))
                {
                    Debug("Up FVG Found.");

                    string tag = "FVGUP_" + InstanceId + "_" + CurrentBars[iDataSeries];
                    FVG fvg = new FVG(tag, FVGType.S, Highs[iDataSeries][2], Lows[iDataSeries][0], Times[iDataSeries][2]);
                    Debug("Drawing Up FVG [" + fvg.gapStartTime + ", " + fvg.lowerPrice + ", " + fvg.upperPrice + "]");
                    Draw.Rectangle(this, tag, false, fvg.gapStartTime, fvg.lowerPrice, future, fvg.upperPrice, UpBrush, UpAreaBrush, ActiveAreaOpacity, true);
                    if (DisplayCE) Draw.Line(this, tag + "_CE", false, fvg.gapStartTime, fvg.consequentEncroachmentPrice, future, fvg.consequentEncroachmentPrice, UpBrush, DashStyleHelper.DashDotDot, 1);
                    fvgList.Add(fvg);
                }
                // Fair value gap while going DOWN
                // High[0] < Low[2]
                if (Highs[iDataSeries][0] < Lows[iDataSeries][2] && (Math.Abs(Highs[iDataSeries][0] - Lows[iDataSeries][2]) >= MinimumFVGSize))
                // IDEA: Potential FVG filtering based on ATR : && (Math.Abs(Highs[iDataSeries][0] - Lows[iDataSeries][2]) >= ImpulseFactor * atr.Value[0]))
                {
                    Debug("Down FVG Found.");

                    string tag = "FVGDOWN_" + InstanceId + "_" + CurrentBars[iDataSeries];
                    FVG fvg = new FVG(tag, FVGType.R, Highs[iDataSeries][0], Lows[iDataSeries][2], Times[iDataSeries][2]);
                    Debug("Drawing Down FVG [" + fvg.gapStartTime + ", " + fvg.upperPrice + ", " + fvg.lowerPrice + "]");
                    Draw.Rectangle(this, tag, false, fvg.gapStartTime, fvg.upperPrice, future, fvg.lowerPrice, DownBrush, DownAreaBrush, ActiveAreaOpacity, true);
                    if (DisplayCE) Draw.Line(this, tag + "_CE", false, fvg.gapStartTime, fvg.consequentEncroachmentPrice, future, fvg.consequentEncroachmentPrice, DownBrush, DashStyleHelper.DashDotDot, 1);
                    fvgList.Add(fvg);
                }
            }
        }

        private void CheckFilledFVGs()
        {
            List<FVG> filled = new List<FVG>();

            foreach (FVG fvg in fvgList)
            {
                if (fvg.filled) continue;

                if (DrawObjects[fvg.tag] != null && DrawObjects[fvg.tag] is DrawingTools.Rectangle)
                {
                    //Update EndAnchor of Gap to Expand into future.
                    Rectangle gapRect = (Rectangle)DrawObjects[fvg.tag];
                    gapRect.EndAnchor.Time = Times[iDataSeries][0].AddDays(ChartBars.Properties.DaysBack);

                    if (DisplayCE && DrawObjects[fvg.tag + "_CE"] != null && DrawObjects[fvg.tag + "_CE"] is DrawingTools.Line)
                    {
                        DrawingTools.Line gapLine = (DrawingTools.Line)DrawObjects[fvg.tag + "_CE"];
                        gapLine.EndAnchor.Time = Times[iDataSeries][0].AddDays(ChartBars.Properties.DaysBack);
                    }

                }

                if (fvg.type == FVGType.R && (FillType == FVGFillType.CLOSE_THROUGH ? (Closes[iDataSeries][0] >= fvg.upperPrice) : (Highs[iDataSeries][0] >= fvg.upperPrice)))
                {
                    if (DrawObjects[fvg.tag] != null)
                    {
                        fvg.filled = true;
                        fvg.fillTime = Times[iDataSeries][0];
                        filled.Add(fvg);
                    }
                }
                else if (fvg.type == FVGType.S && (FillType == FVGFillType.CLOSE_THROUGH ? (Closes[iDataSeries][0] <= fvg.lowerPrice) : (Lows[iDataSeries][0] <= fvg.lowerPrice)))
                {
                    if (DrawObjects[fvg.tag] != null)
                    {
                        fvg.filled = true;
                        fvg.fillTime = Times[iDataSeries][0];
                        filled.Add(fvg);
                    }
                }
            }

            foreach (FVG fvg in filled)
            {
                if (DrawObjects[fvg.tag] != null)
                {
                    var drawObject = DrawObjects[fvg.tag];
                    Rectangle rect = (Rectangle)drawObject;

                    RemoveDrawObject(fvg.tag);
                    RemoveDrawObject(fvg.tag + "_CE");

                    if (!HideFilledGaps)
                    {
                        Brush BorderBrush = fvg.type == FVGType.R ? DownBrush : UpBrush;
                        rect = Draw.Rectangle(this, "FILLEDFVG" + fvg.tag, false, fvg.gapStartTime, fvg.lowerPrice, Times[iDataSeries][0], fvg.upperPrice, BorderBrush, FillBrush, FilledAreaOpacity, true);
                        rect.OutlineStroke.Opacity = Math.Min(100, FilledAreaOpacity * 4);
                    }
                }
                if (HideFilledGaps)
                {
                    fvgList.Remove(fvg);
                }
            }
        }

        #region FVG Data
        [Browsable(false)]
        [XmlIgnore()]
        public List<FVG> FVGList
        {
            get { return new List<FVG>(fvgList); }
        }
        #endregion

        #region Properties

        [NinjaScriptProperty]
        [Display(Name = "Use FVG Data Series", Description = "If enabled, a secondary data series will be used to calculate FVGs.", Order = 90, GroupName = "FVG Data Series")]
        public bool UseFVGDataSeries
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "FVG Data Series Type", Order = 100, GroupName = "FVG Data Series")]
        public FVGPeriodTypes FVGBarsPeriodType
        { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "FVG Data Series Period", Order = 200, GroupName = "FVG Data Series")]
        public int FVGSeriesPeriod
        { get; set; }


        [NinjaScriptProperty]
        [Range(3, int.MaxValue)]
        [Display(Name = "Max Lookback Bars", Order = 100, GroupName = "Parameters")]
        public int MaxBars
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Use ATR", Description = "If enabled, ATR settings will be used to filter FVGs.", Order = 190, GroupName = "Parameters")]
        public bool UseATR
        { get; set; }

        [NinjaScriptProperty]
        [Range(3, int.MaxValue)]
        [Display(Name = "ATR Period (To Detect Impulse Moves)", Order = 200, GroupName = "Parameters")]
        public int ATRPeriod
        { get; set; }

        [NinjaScriptProperty]
        [Range(0.1, double.MaxValue)]
        [Display(Name = "ATRs in Impulse Move", Order = 300, GroupName = "Parameters")]
        public double ImpulseFactor
        { get; set; }

        [NinjaScriptProperty]
        [Range(0.0000000000000000000000001, double.MaxValue)]
        [Display(Name = "Minimum FVG Size (Points)", Order = 310, GroupName = "Parameters")]
        public double MinimumFVGSize
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Gap Fill Condition", Order = 325, GroupName = "Parameters")]
        public FVGFillType FillType
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Hide Filled Gaps", Order = 350, GroupName = "Parameters")]
        public bool HideFilledGaps
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Show Consequent Encroachment", Order = 400, GroupName = "Parameters")]
        public bool DisplayCE
        { get; set; }

        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name = "Bearish FVG (Border) Color", Order = 100, GroupName = "FVG Colors")]
        public Brush DownBrush
        { get; set; }

        [Browsable(false)]
        public string DownBrushSerializable
        {
            get { return Serialize.BrushToString(DownBrush); }
            set { DownBrush = Serialize.StringToBrush(value); }
        }

        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name = "Bearish FVG (Area) Color", Order = 110, GroupName = "FVG Colors")]
        public Brush DownAreaBrush
        { get; set; }

        [Browsable(false)]
        public string DownBrushAreaSerializable
        {
            get { return Serialize.BrushToString(DownAreaBrush); }
            set { DownAreaBrush = Serialize.StringToBrush(value); }
        }

        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name = "Bullish FVG (Border) Color", Order = 200, GroupName = "FVG Colors")]
        public Brush UpBrush
        { get; set; }

        [Browsable(false)]
        public string UpBrushSerializable
        {
            get { return Serialize.BrushToString(UpBrush); }
            set { UpBrush = Serialize.StringToBrush(value); }
        }

        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name = "Bullish FVG (Area) Color", Order = 210, GroupName = "FVG Colors")]
        public Brush UpAreaBrush
        { get; set; }

        [Browsable(false)]
        public string UpAreaBrushSerializable
        {
            get { return Serialize.BrushToString(UpAreaBrush); }
            set { UpAreaBrush = Serialize.StringToBrush(value); }
        }

        [NinjaScriptProperty]
        [Range(1, 100)]
        [Display(Name = "Active Gap Opacity", Order = 300, GroupName = "FVG Colors")]
        public int ActiveAreaOpacity
        { get; set; }

        [NinjaScriptProperty]
        [Range(1, 100)]
        [Display(Name = "Filled Gap Opacity", Order = 400, GroupName = "FVG Colors")]
        public int FilledAreaOpacity
        { get; set; }


        [NinjaScriptProperty]
        [Display(Name = "Display Label", Description = "Display the FVG Data Series Label", Order = 100, GroupName = "FVG Data Series Label")]
        public bool DrawLabel
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Label Position", Description = "FVG Data Series Label Position on Chart", Order = 200, GroupName = "FVG Data Series Label")]
        public TextPosition LabelPosition
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Label Font", Description = "FVG Data Series Label Font", Order = 300, GroupName = "FVG Data Series Label")]
        public SimpleFont LabelFont
        { get; set; }

        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name = "Label Text Color", Description = "FVG Data Series Label Text Color", Order = 400, GroupName = "FVG Data Series Label")]
        public Brush LabelTextBrush
        { get; set; }

        [Browsable(false)]
        public string LabelTextBrushSerializable
        {
            get { return Serialize.BrushToString(LabelTextBrush); }
            set { LabelTextBrush = Serialize.StringToBrush(value); }
        }

        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name = "Label Border Color", Description = "FVG Data Series Label Border Color", Order = 500, GroupName = "FVG Data Series Label")]
        public Brush LabelBorderBrush
        { get; set; }

        [Browsable(false)]
        public string LabelBorderBrushSerializable
        {
            get { return Serialize.BrushToString(LabelBorderBrush); }
            set { LabelBorderBrush = Serialize.StringToBrush(value); }
        }

        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name = "Label Fill Color", Description = "FVG Data Series Label Fill Color", Order = 600, GroupName = "FVG Data Series Label")]
        public Brush LabelFillBrush
        { get; set; }

        [Browsable(false)]
        public string LabelFillBrushSerializable
        {
            get { return Serialize.BrushToString(LabelFillBrush); }
            set { LabelFillBrush = Serialize.StringToBrush(value); }
        }

        [NinjaScriptProperty]
        [Range(1, 100)]
        [Display(Name = "Label Fill Opacity", Description = "FVG Data Series Label Fill Opacity", Order = 700, GroupName = "FVG Data Series Label")]
        public int LabelFillOpacity
        { get; set; }

        #endregion

    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
    public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
    {
        private Gemify.ICTFVG[] cacheICTFVG;
        public Gemify.ICTFVG ICTFVG(bool useFVGDataSeries, FVGPeriodTypes fVGBarsPeriodType, int fVGSeriesPeriod, int maxBars, bool useATR, int aTRPeriod, double impulseFactor, double minimumFVGSize, FVGFillType fillType, bool hideFilledGaps, bool displayCE, Brush downBrush, Brush downAreaBrush, Brush upBrush, Brush upAreaBrush, int activeAreaOpacity, int filledAreaOpacity, bool drawLabel, TextPosition labelPosition, SimpleFont labelFont, Brush labelTextBrush, Brush labelBorderBrush, Brush labelFillBrush, int labelFillOpacity)
        {
            return ICTFVG(Input, useFVGDataSeries, fVGBarsPeriodType, fVGSeriesPeriod, maxBars, useATR, aTRPeriod, impulseFactor, minimumFVGSize, fillType, hideFilledGaps, displayCE, downBrush, downAreaBrush, upBrush, upAreaBrush, activeAreaOpacity, filledAreaOpacity, drawLabel, labelPosition, labelFont, labelTextBrush, labelBorderBrush, labelFillBrush, labelFillOpacity);
        }

        public Gemify.ICTFVG ICTFVG(ISeries<double> input, bool useFVGDataSeries, FVGPeriodTypes fVGBarsPeriodType, int fVGSeriesPeriod, int maxBars, bool useATR, int aTRPeriod, double impulseFactor, double minimumFVGSize, FVGFillType fillType, bool hideFilledGaps, bool displayCE, Brush downBrush, Brush downAreaBrush, Brush upBrush, Brush upAreaBrush, int activeAreaOpacity, int filledAreaOpacity, bool drawLabel, TextPosition labelPosition, SimpleFont labelFont, Brush labelTextBrush, Brush labelBorderBrush, Brush labelFillBrush, int labelFillOpacity)
        {
            if (cacheICTFVG != null)
                for (int idx = 0; idx < cacheICTFVG.Length; idx++)
                    if (cacheICTFVG[idx] != null && cacheICTFVG[idx].UseFVGDataSeries == useFVGDataSeries && cacheICTFVG[idx].FVGBarsPeriodType == fVGBarsPeriodType && cacheICTFVG[idx].FVGSeriesPeriod == fVGSeriesPeriod && cacheICTFVG[idx].MaxBars == maxBars && cacheICTFVG[idx].UseATR == useATR && cacheICTFVG[idx].ATRPeriod == aTRPeriod && cacheICTFVG[idx].ImpulseFactor == impulseFactor && cacheICTFVG[idx].MinimumFVGSize == minimumFVGSize && cacheICTFVG[idx].FillType == fillType && cacheICTFVG[idx].HideFilledGaps == hideFilledGaps && cacheICTFVG[idx].DisplayCE == displayCE && cacheICTFVG[idx].DownBrush == downBrush && cacheICTFVG[idx].DownAreaBrush == downAreaBrush && cacheICTFVG[idx].UpBrush == upBrush && cacheICTFVG[idx].UpAreaBrush == upAreaBrush && cacheICTFVG[idx].ActiveAreaOpacity == activeAreaOpacity && cacheICTFVG[idx].FilledAreaOpacity == filledAreaOpacity && cacheICTFVG[idx].DrawLabel == drawLabel && cacheICTFVG[idx].LabelPosition == labelPosition && cacheICTFVG[idx].LabelFont == labelFont && cacheICTFVG[idx].LabelTextBrush == labelTextBrush && cacheICTFVG[idx].LabelBorderBrush == labelBorderBrush && cacheICTFVG[idx].LabelFillBrush == labelFillBrush && cacheICTFVG[idx].LabelFillOpacity == labelFillOpacity && cacheICTFVG[idx].EqualsInput(input))
                        return cacheICTFVG[idx];
            return CacheIndicator<Gemify.ICTFVG>(new Gemify.ICTFVG() { UseFVGDataSeries = useFVGDataSeries, FVGBarsPeriodType = fVGBarsPeriodType, FVGSeriesPeriod = fVGSeriesPeriod, MaxBars = maxBars, UseATR = useATR, ATRPeriod = aTRPeriod, ImpulseFactor = impulseFactor, MinimumFVGSize = minimumFVGSize, FillType = fillType, HideFilledGaps = hideFilledGaps, DisplayCE = displayCE, DownBrush = downBrush, DownAreaBrush = downAreaBrush, UpBrush = upBrush, UpAreaBrush = upAreaBrush, ActiveAreaOpacity = activeAreaOpacity, FilledAreaOpacity = filledAreaOpacity, DrawLabel = drawLabel, LabelPosition = labelPosition, LabelFont = labelFont, LabelTextBrush = labelTextBrush, LabelBorderBrush = labelBorderBrush, LabelFillBrush = labelFillBrush, LabelFillOpacity = labelFillOpacity }, input, ref cacheICTFVG);
        }
    }
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
    public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
    {
        public Indicators.Gemify.ICTFVG ICTFVG(bool useFVGDataSeries, FVGPeriodTypes fVGBarsPeriodType, int fVGSeriesPeriod, int maxBars, bool useATR, int aTRPeriod, double impulseFactor, double minimumFVGSize, FVGFillType fillType, bool hideFilledGaps, bool displayCE, Brush downBrush, Brush downAreaBrush, Brush upBrush, Brush upAreaBrush, int activeAreaOpacity, int filledAreaOpacity, bool drawLabel, TextPosition labelPosition, SimpleFont labelFont, Brush labelTextBrush, Brush labelBorderBrush, Brush labelFillBrush, int labelFillOpacity)
        {
            return indicator.ICTFVG(Input, useFVGDataSeries, fVGBarsPeriodType, fVGSeriesPeriod, maxBars, useATR, aTRPeriod, impulseFactor, minimumFVGSize, fillType, hideFilledGaps, displayCE, downBrush, downAreaBrush, upBrush, upAreaBrush, activeAreaOpacity, filledAreaOpacity, drawLabel, labelPosition, labelFont, labelTextBrush, labelBorderBrush, labelFillBrush, labelFillOpacity);
        }

        public Indicators.Gemify.ICTFVG ICTFVG(ISeries<double> input, bool useFVGDataSeries, FVGPeriodTypes fVGBarsPeriodType, int fVGSeriesPeriod, int maxBars, bool useATR, int aTRPeriod, double impulseFactor, double minimumFVGSize, FVGFillType fillType, bool hideFilledGaps, bool displayCE, Brush downBrush, Brush downAreaBrush, Brush upBrush, Brush upAreaBrush, int activeAreaOpacity, int filledAreaOpacity, bool drawLabel, TextPosition labelPosition, SimpleFont labelFont, Brush labelTextBrush, Brush labelBorderBrush, Brush labelFillBrush, int labelFillOpacity)
        {
            return indicator.ICTFVG(input, useFVGDataSeries, fVGBarsPeriodType, fVGSeriesPeriod, maxBars, useATR, aTRPeriod, impulseFactor, minimumFVGSize, fillType, hideFilledGaps, displayCE, downBrush, downAreaBrush, upBrush, upAreaBrush, activeAreaOpacity, filledAreaOpacity, drawLabel, labelPosition, labelFont, labelTextBrush, labelBorderBrush, labelFillBrush, labelFillOpacity);
        }
    }
}

namespace NinjaTrader.NinjaScript.Strategies
{
    public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
    {
        public Indicators.Gemify.ICTFVG ICTFVG(bool useFVGDataSeries, FVGPeriodTypes fVGBarsPeriodType, int fVGSeriesPeriod, int maxBars, bool useATR, int aTRPeriod, double impulseFactor, double minimumFVGSize, FVGFillType fillType, bool hideFilledGaps, bool displayCE, Brush downBrush, Brush downAreaBrush, Brush upBrush, Brush upAreaBrush, int activeAreaOpacity, int filledAreaOpacity, bool drawLabel, TextPosition labelPosition, SimpleFont labelFont, Brush labelTextBrush, Brush labelBorderBrush, Brush labelFillBrush, int labelFillOpacity)
        {
            return indicator.ICTFVG(Input, useFVGDataSeries, fVGBarsPeriodType, fVGSeriesPeriod, maxBars, useATR, aTRPeriod, impulseFactor, minimumFVGSize, fillType, hideFilledGaps, displayCE, downBrush, downAreaBrush, upBrush, upAreaBrush, activeAreaOpacity, filledAreaOpacity, drawLabel, labelPosition, labelFont, labelTextBrush, labelBorderBrush, labelFillBrush, labelFillOpacity);
        }

        public Indicators.Gemify.ICTFVG ICTFVG(ISeries<double> input, bool useFVGDataSeries, FVGPeriodTypes fVGBarsPeriodType, int fVGSeriesPeriod, int maxBars, bool useATR, int aTRPeriod, double impulseFactor, double minimumFVGSize, FVGFillType fillType, bool hideFilledGaps, bool displayCE, Brush downBrush, Brush downAreaBrush, Brush upBrush, Brush upAreaBrush, int activeAreaOpacity, int filledAreaOpacity, bool drawLabel, TextPosition labelPosition, SimpleFont labelFont, Brush labelTextBrush, Brush labelBorderBrush, Brush labelFillBrush, int labelFillOpacity)
        {
            return indicator.ICTFVG(input, useFVGDataSeries, fVGBarsPeriodType, fVGSeriesPeriod, maxBars, useATR, aTRPeriod, impulseFactor, minimumFVGSize, fillType, hideFilledGaps, displayCE, downBrush, downAreaBrush, upBrush, upAreaBrush, activeAreaOpacity, filledAreaOpacity, drawLabel, labelPosition, labelFont, labelTextBrush, labelBorderBrush, labelFillBrush, labelFillOpacity);
        }
    }
}

#endregion
