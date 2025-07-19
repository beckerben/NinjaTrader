// #############################################################
// #														   #
// #                PriceActionSwingOscillator                 #
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
using NinjaTrader.Gui;
using NinjaTrader.NinjaScript.Indicators.PriceActionSwing;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using System.Xml.Serialization;
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators.PriceActionSwing
{
    #region Categories definitions
    [CategoryOrder("Parameters", 0)]
    [CategoryOrder("Features", 10)]
    [CategoryOrder("Visualization", 20)]
    [CategoryOrder("Data Series", 100)]
    [CategoryOrder("Setup", 110)]
    [CategoryOrder("Visual", 120)]
    [CategoryOrder("Plots", 130)]
    #endregion

    /// <summary>
    /// PriceActionSwingOscillator shows the trend direction, swing relation or developing 
    /// swing volume. !!! The volume is repainting, in case the swing direction changes. !!!
    /// </summary>
    [TypeConverter("NinjaTrader.NinjaScript.Indicators.PriceActionSwing.PriceActionSwingOscillatorConverter")]
    public class PriceActionSwingOscillator : Indicator
    {
        #region Swing calculation member
        private PriceActionSwingBase swingCalculation;

        public PriceActionSwingOscillator()
        {
            swingCalculation = new PriceActionSwingBase(this);
        }
        #endregion

        #region Variables
        private int oldTrend = 0;
        private bool ignoreSwings = true;

        private Series<double> VolumeHighSeries;
        private Series<double> VolumeLowSeries;
        #endregion

        #region OnStateChange()
        //=========================================================================================
        protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"PriceActionSwingOscillator shows the trend direction, swing relation or developing swing volume. !!! The volume is repainting. !!!";
				Name										= "PriceActionSwingOscillator";
				Calculate									= Calculate.OnEachTick;
				IsOverlay									= false;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= true;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= true;
                MaximumBarsLookBack                         = MaximumBarsLookBack.Infinite;
                ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				//Disable this property if your indicator requires custom values that cumulate with each new market data event. 
				//See Help Guide for additional information.
				IsSuspendedWhileInactive					= true;

                AddPlot(new Stroke(Brushes.Lime, DashStyleHelper.Solid, 2), PlotStyle.Bar, "VHigh");
				AddPlot(new Stroke(Brushes.Red, DashStyleHelper.Solid, 2), PlotStyle.Bar, "VLow");
				AddPlot(new Stroke(Brushes.Lime, DashStyleHelper.Solid, 2), PlotStyle.Square, "VHighCurrent");
				AddPlot(new Stroke(Brushes.Red, DashStyleHelper.Solid, 2), PlotStyle.Square, "VLowCurrent");

                AddPlot(new Stroke(Brushes.Transparent, DashStyleHelper.Solid, 20), PlotStyle.Square, "AnalyzerTrend");
                AddPlot(new Stroke(Brushes.Transparent, DashStyleHelper.Solid, 20), PlotStyle.Square, "SwingTrend");

                AddPlot(new Stroke(Brushes.Transparent, DashStyleHelper.Solid, 20), PlotStyle.Square, "AnalyzerRelation");
                AddPlot(new Stroke(Brushes.Transparent, DashStyleHelper.Solid, 20), PlotStyle.Square, "SwingRelation");

                SwingStyleType = SwingStyle.Standard;
                SwingSize = 7;
                DtbStrength = 20;
                UseCloseValues = false;

                IgnoreInsideBars = true;
                UseBreakouts = true;

                ShowIndicationType = ShowIndicationStyle.Trend;
                UseOldTrend = true;

                BaseValue = 1.5;
                UpTrendColor = Brushes.Green;
                NoTrendColor = Brushes.Gold;
                DownTrendColor = Brushes.Red;

                LLandLHColor = Brushes.Red;
                DTColor = Brushes.Firebrick;
                HHandHLColor = Brushes.Green;
                DBColor = Brushes.LimeGreen;
            }
            else if (State == State.Configure)
            {
                swingCalculation.SetUserParameters(UseCloseValues, SwingStyleType, SwingSize,
                    DtbStrength, IgnoreInsideBars, UseBreakouts);

                VolumeHighSeries = new Series<double>(this);
                VolumeLowSeries = new Series<double>(this);
            }
            else if (State == State.DataLoaded)
            {
                // now all input should be loaded and we can set some dependent data values
                swingCalculation.SetAdditionalValues();
            }
        }
        //=========================================================================================
        #endregion

        #region OnBarUpdate
        protected override void OnBarUpdate()
        {
            // checks to ensure there are enough bars before beginning
            if (CurrentBar <= swingCalculation.calculationStartBar)
                return;

            swingCalculation.InitAndResetSwingCalculation();
            swingCalculation.CalculateSwings();

            if (ShowIndicationType == ShowIndicationStyle.Volume)
            {
                #region Volume
                //-----------------------------------------------------------------------------
                // New swing high: update
                if (swingCalculation.SwingHigh.New == 3)
                {
                    double swingVolume = 0.0;
                    for (int i = CurrentBar - swingCalculation.SwingLow.CurBar - 1; i > -1; i--)
                    {
                        swingVolume = swingVolume + Volume[i];
                        VolumeHighSeries[i] = swingVolume;
                        VolumeLowSeries[i] = 0;

                        VLow.Reset(i);
                        VHighCurrent.Reset(i);
                        VHigh[i] = swingVolume;
                    }
                    swingVolume = 0.0;
                    for (int i = CurrentBar - swingCalculation.SwingHigh.CurBar - 1; i > -1; i--)
                    {
                        swingVolume = swingVolume + Volume[i];
                        VolumeLowSeries[i] = swingVolume;
                        VLowCurrent[i] = swingVolume;
                    }
                }
                // New swing high: initial
                else if (swingCalculation.SwingHigh.New == 2)
                {
                    double tmp = VolumeHighSeries[1] + Volume[0];
                    VolumeHighSeries[0] = tmp;
                    for (int i = CurrentBar - swingCalculation.SwingLow.CurBar - 1; i > -1; i--)
                    {
                        VLowCurrent.Reset(i);
                        VolumeLowSeries[i] = 0;
                    }
                    VHigh[0] = tmp;
                }
                // New swing low: update
                else if (swingCalculation.SwingLow.New == 3)
                {
                    double swingVolume = 0.0;
                    for (int i = CurrentBar - swingCalculation.SwingHigh.CurBar - 1; i > -1; i--)
                    {
                        swingVolume = swingVolume + Volume[i];
                        VolumeLowSeries[i] = swingVolume;
                        VolumeHighSeries[i] = 0;

                        VHigh.Reset(i);
                        VLowCurrent.Reset(i);
                        VLow[i] = swingVolume;
                    }
                    swingVolume = 0.0;
                    for (int i = CurrentBar - swingCalculation.SwingLow.CurBar - 1; i > -1; i--)
                    {
                        swingVolume = swingVolume + Volume[i];
                        VolumeHighSeries[i] = swingVolume;
                        VHighCurrent[i] = swingVolume;
                    }
                }
                // New swing low: initial
                else if (swingCalculation.SwingLow.New == 2)
                {
                    double tmp = VolumeLowSeries[1] + Volume[0];
                    VolumeLowSeries[0] = tmp;
                    for (int i = CurrentBar - swingCalculation.SwingHigh.CurBar - 1; i > -1; i--)
                    {
                        VHighCurrent.Reset(i);
                        VolumeHighSeries[i] = 0;
                    }
                    VLow[0] = tmp;
                }
                // No new swing high or swing low
                else
                {
                    double tmpH = VolumeHighSeries[1] + Volume[0];
                    double tmpL = VolumeLowSeries[1] + Volume[0];
                    VolumeHighSeries[0] = tmpH;
                    VolumeLowSeries[0] = tmpL;

                    if (swingCalculation.CurrentSwing.SwingSlope == -1)
                    {
                        VLow[0] = tmpL;
                        VHighCurrent[0] = tmpH;
                    }
                    else
                    {
                        VHigh[0] = tmpH;
                        VLowCurrent[0] = tmpL;
                    }

                }
                //-----------------------------------------------------------------------------
                #endregion
            }
            else
            {
                #region Swing relation
                if (swingCalculation.SwingLow.CurRelation == 0)
                    AnalyzerRelation[0] = 2;
                else if (swingCalculation.SwingHigh.CurRelation == 0)
                    AnalyzerRelation[0] = -2;
                else if (swingCalculation.SwingHigh.CurRelation == 1 && swingCalculation.SwingLow.CurRelation == 1)
                    AnalyzerRelation[0] = 1;
                else if (swingCalculation.SwingHigh.CurRelation == -1 && swingCalculation.SwingLow.CurRelation == -1)
                    AnalyzerRelation[0] = -1;
                else
                    AnalyzerRelation[0] = 0;
                #endregion

                if (ShowIndicationType == ShowIndicationStyle.Relation)
                {
                    #region Draw relation
                    SwingRelation[0] = BaseValue;
                    int relation = Convert.ToInt32(AnalyzerRelation[0]);
                    switch (relation)
                    {
                        case -2:
                            PlotBrushes[7][0] = DTColor;
                            break;
                        case -1:
                            PlotBrushes[7][0] = LLandLHColor;
                            break;
                        case 0:
                            PlotBrushes[7][0] = NoTrendColor;
                            break;
                        case 1:
                            PlotBrushes[7][0] = HHandHLColor;
                            break;
                        case 2:
                            PlotBrushes[7][0] = DBColor;
                            break;
                        default:
                            PlotBrushes[7][0] = NoTrendColor;
                            break;
                    }
                    #endregion
                }
                else
                {
                    // ShowIndicationStyle.Trend
                    #region Swing trend
                    if ((swingCalculation.SwingHigh.CurRelation == 1 && swingCalculation.SwingLow.CurRelation == 1)
                            || (AnalyzerTrend[1] == 1 && swingCalculation.CurrentSwing.SwingSlope == 1)
                            || (ignoreSwings && AnalyzerTrend[1] == 1 && swingCalculation.SwingLow.CurRelation == 1)
                            || (((AnalyzerTrend[1] == 1) || (swingCalculation.CurrentSwing.SwingSlope == 1
                                && swingCalculation.SwingHigh.CurRelation == 1))
                                && swingCalculation.SwingLow.CurRelation == 0))
                        AnalyzerTrend[0] = 1;
                    else if ((swingCalculation.SwingHigh.CurRelation == -1 && swingCalculation.SwingLow.CurRelation == -1)
                            || (AnalyzerTrend[1] == -1 && swingCalculation.CurrentSwing.SwingSlope == -1)
                            || (ignoreSwings && AnalyzerTrend[1] == -1 && swingCalculation.SwingHigh.CurRelation == -1)
                            || (((AnalyzerTrend[1] == -1) || (swingCalculation.CurrentSwing.SwingSlope == -1
                                && swingCalculation.SwingLow.CurRelation == -1))
                                && swingCalculation.SwingHigh.CurRelation == 0))
                        AnalyzerTrend[0] = -1;
                    else
                        AnalyzerTrend[0] = 0;

                    SwingTrend[0] = BaseValue;
                    int trend = Convert.ToInt32(AnalyzerTrend[0]);
                    switch (trend)
                    {
                        case -1:
                            PlotBrushes[5][0] = DownTrendColor;
                            oldTrend = -1;
                            break;
                        case 1:
                            PlotBrushes[5][0] = UpTrendColor;
                            oldTrend = 1;
                            break;
                        default:
                            if (UseOldTrend)
                            {
                                if (oldTrend == 1)
                                    PlotBrushes[5][0] = UpTrendColor;
                                else if (oldTrend == -1)
                                    PlotBrushes[5][0] = DownTrendColor;
                            }
                            else
                                PlotBrushes[5][0] = NoTrendColor;
                            break;
                    }
                    #endregion
                }
            }
		}
        #endregion

        #region Properties
        //=========================================================================================
        #region Plots
        // Plots ==================================================================================
		[Browsable(false)]
        [XmlIgnore]
        public Series<double> VHigh
        {
            get { return Values[0]; }
        }
		[Browsable(false)]
        [XmlIgnore]
        public Series<double> VLow
        {
            get { return Values[1]; }
        }
		[Browsable(false)]
        [XmlIgnore]
        public Series<double> VHighCurrent
        {
            get { return Values[2]; }
        }
        [Browsable(false)]
        [XmlIgnore]
        public Series<double> VLowCurrent
        {
            get { return Values[3]; }
        }
        [Browsable(false)]
        [XmlIgnore]
        public Series<double> AnalyzerTrend
        {
            get { return Values[4]; }
        }
        [Browsable(false)]
        [XmlIgnore]
        public Series<double> SwingTrend
        {
            get { return Values[5]; }
        }
        [Browsable(false)]
        [XmlIgnore]
        public Series<double> AnalyzerRelation
        {
            get { return Values[6]; }
        }
        [Browsable(false)]
        [XmlIgnore]
        public Series<double> SwingRelation
        {
            get { return Values[7]; }
        }
        //=========================================================================================
        #endregion

        #region UI Parameters
        [RefreshProperties(RefreshProperties.All)]
        [NinjaScriptProperty]
        [Display(Name = "Swing Type", Description = "Represents the swing type for the swings.", Order = 1, GroupName = "Parameters")]
        public SwingStyle SwingStyleType
        { get; set; }

        [Range(0.00000001, double.MaxValue)]
        [NinjaScriptProperty]
        [Display(Name = "Swing Size", Description = "Represents the swing size for the swings. E.g. 1 = small and 5 = bigger swings.", Order = 2, GroupName = "Parameters")]
        public double SwingSize
        { get; set; }

        [Range(1, int.MaxValue)]
        [NinjaScriptProperty]
        [Display(Name = "Double Top/-Bottom Strength", Description = "Represents the double top/-bottom strength. Increase the value to get more DBs/DTs.", Order = 3, GroupName = "Parameters")]
        public int DtbStrength
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Use Close Values", Description = "Indicates if high and low prices are used for the swing calculations or close values.", Order = 4, GroupName = "Parameters")]
        public bool UseCloseValues
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Ignore Inside Bars", Description = "Indicates if inside bars are ignored. If set to true it is possible that between consecutive up/down bars are inside bars. Only used if calculationSize > 1.", Order = 5, GroupName = "Parameters")]
        public bool IgnoreInsideBars
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Use Breakouts", Description = "Indicates if the swings are updated if the last swing high/low is broken. Only used if calculationSize > 1.", Order = 6, GroupName = "Parameters")]
        public bool UseBreakouts
        { get; set; }
        #endregion

        #region Features
        [RefreshProperties(RefreshProperties.All)]
        [NinjaScriptProperty]
        [Display(Name = "Choose indication", Description = "Represents which swing indication is shown. Trend | Relation | Volume (repainting).", Order = 1, GroupName = "Features")]
        public ShowIndicationStyle ShowIndicationType
        { get; set; }
        [NinjaScriptProperty]
        [Display(Name = "Trend change", Description = "Indicates if the trend direction is changed when the old trend ends or whether a new trend must start first.", Order = 2, GroupName = "Features")]
        public bool UseOldTrend
        { get; set; }
        #endregion

        #region Visualization
        [Display(Name = "Base value", Description = "Represents the base value of the plot.", Order = 0, GroupName = "Visualization")]
        public double BaseValue
        { get; set; }

        [XmlIgnore]
        [Display(Name = "Up trend color", Description = "Represents the color of for up trends.", Order = 1, GroupName = "Visualization")]
        public virtual Brush UpTrendColor
        { get; set; }

        [Browsable(false)]
        public virtual string UpTrendColorSerializable
        {
            get { return Serialize.BrushToString(UpTrendColor); }
            set { UpTrendColor = Serialize.StringToBrush(value); }
        }
        [XmlIgnore]
        [Display(Name = "HH and HL color", Description = "Represents the color if the swings make higher highs and higher lows.", Order = 2, GroupName = "Visualization")]
        public virtual Brush HHandHLColor
        { get; set; }

        [Browsable(false)]
        public virtual string HHandHLColorSerializable
        {
            get { return Serialize.BrushToString(HHandHLColor); }
            set { HHandHLColor = Serialize.StringToBrush(value); }
        }
        [XmlIgnore]
        [Display(Name = "Double bottom color", Description = "Represents the color if the swing is a double bottom.", Order = 3, GroupName = "Visualization")]
        public virtual Brush DBColor
        { get; set; }

        [Browsable(false)]
        public virtual string DBColorSerializable
        {
            get { return Serialize.BrushToString(DBColor); }
            set { DBColor = Serialize.StringToBrush(value); }
        }
        [XmlIgnore]
        [Display(Name = "No trend color", Description = "Represents the color of for no trends.", Order = 4, GroupName = "Visualization")]
        public virtual Brush NoTrendColor
        { get; set; }

        [Browsable(false)]
        public virtual string NoTrendColorSerializable
        {
            get { return Serialize.BrushToString(NoTrendColor); }
            set { NoTrendColor = Serialize.StringToBrush(value); }
        }

        [XmlIgnore]
        [Display(Name = "Down trend color", Description = "Represents the color of for down trends.", Order = 5, GroupName = "Visualization")]
        public virtual Brush DownTrendColor
        { get; set; }

        [Browsable(false)]
        public virtual string DownTrendColorSerializable
        {
            get { return Serialize.BrushToString(DownTrendColor); }
            set { DownTrendColor = Serialize.StringToBrush(value); }
        }
        [XmlIgnore]
        [Display(Name = "LL and LH color", Description = "Represents the color if the swings make lower lows and lower highs.", Order = 6, GroupName = "Visualization")]
        public virtual Brush LLandLHColor
        { get; set; }

        [Browsable(false)]
        public virtual string LLandLHColorSerializable
        {
            get { return Serialize.BrushToString(LLandLHColor); }
            set { LLandLHColor = Serialize.StringToBrush(value); }
        }
        [XmlIgnore]
        [Display(Name = "Double top color", Description = "Represents the color if the swing is a double top.", Order = 7, GroupName = "Visualization")]
        public virtual Brush DTColor
        { get; set; }

        [Browsable(false)]
        public virtual string DTColorSerializable
        {
            get { return Serialize.BrushToString(DTColor); }
            set { DTColor = Serialize.StringToBrush(value); }
        }
        #endregion
        //=========================================================================================
        #endregion
    }

    #region Show/hide properties
    // This custom TypeConverter is applied ot the entire indicator object and handles our use cases
    // IMPORTANT: Inherit from IndicatorBaseConverter so we get default NinjaTrader property handling logic
    // IMPORTANT: Not doing this will completely break the property grids!
    // If targeting a "Strategy", use the "StrategyBaseConverter" base type instead
    public class PriceActionSwingOscillatorConverter : IndicatorBaseConverter
    {
        public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object component, Attribute[] attrs)
        {
            // we need the indicator instance which actually exists on the grid
            PriceActionSwingOscillator indicator = component as PriceActionSwingOscillator;

            // base.GetProperties ensures we have all the properties (and associated property grid editors)
            // NinjaTrader internal logic determines for a given indicator
            PropertyDescriptorCollection propertyDescriptorCollection = base.GetPropertiesSupported(context)
                                                                        ? base.GetProperties(context, component, attrs)
                                                                        : TypeDescriptor.GetProperties(component, attrs);

            if (indicator == null || propertyDescriptorCollection == null)
                return propertyDescriptorCollection;

            // These values will be shown/hidden (toggled)
            PropertyDescriptor IgnoreInsideBarsPropertyDescriptor = propertyDescriptorCollection["IgnoreInsideBars"];
            PropertyDescriptor UseBreakoutsPropertyDescriptor = propertyDescriptorCollection["UseBreakouts"];
            // This removes the following properties from the grid to start off with
            // Parameters
            propertyDescriptorCollection.Remove(IgnoreInsideBarsPropertyDescriptor);
            propertyDescriptorCollection.Remove(UseBreakoutsPropertyDescriptor);

            // Now that We've removed the default property descriptors, we can decide if they need to be re-added
            // If "xxx" is set to true, re-add these values to the property collection
            if (indicator.SwingStyleType == SwingStyle.Gann)
            {
                propertyDescriptorCollection.Add(IgnoreInsideBarsPropertyDescriptor);
                propertyDescriptorCollection.Add(UseBreakoutsPropertyDescriptor);
            }

            // These values will be shown/hidden (toggled)
            PropertyDescriptor BaseValuePropertyDescriptor = propertyDescriptorCollection["BaseValue"];
            PropertyDescriptor UseOldTrendPropertyDescriptor = propertyDescriptorCollection["UseOldTrend"];
            PropertyDescriptor UpTrendColorPropertyDescriptor = propertyDescriptorCollection["UpTrendColor"];
            PropertyDescriptor DownTrendColorPropertyDescriptor = propertyDescriptorCollection["DownTrendColor"];
            PropertyDescriptor NoTrendColorPropertyDescriptor = propertyDescriptorCollection["NoTrendColor"];
            PropertyDescriptor HHandHLColorPropertyDescriptor = propertyDescriptorCollection["HHandHLColor"];
            PropertyDescriptor DBColorPropertyDescriptor = propertyDescriptorCollection["DBColor"];
            PropertyDescriptor LLandLHColorPropertyDescriptor = propertyDescriptorCollection["LLandLHColor"];
            PropertyDescriptor DTColorPropertyDescriptor = propertyDescriptorCollection["DTColor"];
            // This removes the following properties from the grid to start off with
            // Parameters
            propertyDescriptorCollection.Remove(BaseValuePropertyDescriptor);
            propertyDescriptorCollection.Remove(UseOldTrendPropertyDescriptor);
            propertyDescriptorCollection.Remove(UpTrendColorPropertyDescriptor);
            propertyDescriptorCollection.Remove(DownTrendColorPropertyDescriptor);
            propertyDescriptorCollection.Remove(NoTrendColorPropertyDescriptor);
            propertyDescriptorCollection.Remove(HHandHLColorPropertyDescriptor);
            propertyDescriptorCollection.Remove(DBColorPropertyDescriptor);
            propertyDescriptorCollection.Remove(LLandLHColorPropertyDescriptor);
            propertyDescriptorCollection.Remove(DTColorPropertyDescriptor);

            // Now that We've removed the default property descriptors, we can decide if they need to be re-added
            // If "xxx" is set to true, re-add these values to the property collection
            if (indicator.ShowIndicationType == ShowIndicationStyle.Trend)
            {
                propertyDescriptorCollection.Add(BaseValuePropertyDescriptor);
                propertyDescriptorCollection.Add(UseOldTrendPropertyDescriptor);
                propertyDescriptorCollection.Add(UpTrendColorPropertyDescriptor);
                propertyDescriptorCollection.Add(DownTrendColorPropertyDescriptor);
                propertyDescriptorCollection.Add(NoTrendColorPropertyDescriptor);
            }
            else if (indicator.ShowIndicationType == ShowIndicationStyle.Relation)
            {
                propertyDescriptorCollection.Add(BaseValuePropertyDescriptor);
                propertyDescriptorCollection.Add(NoTrendColorPropertyDescriptor);
                propertyDescriptorCollection.Add(HHandHLColorPropertyDescriptor);
                propertyDescriptorCollection.Add(DBColorPropertyDescriptor);
                propertyDescriptorCollection.Add(LLandLHColorPropertyDescriptor);
                propertyDescriptorCollection.Add(DTColorPropertyDescriptor);
            }

            // otherwise, nothing else to do since they were already removed
            return propertyDescriptorCollection;
        }

        // Important: This must return true otherwise the type convetor will not be called
        public override bool GetPropertiesSupported(ITypeDescriptorContext context)
        { return true; }
    }
    #endregion
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private PriceActionSwing.PriceActionSwingOscillator[] cachePriceActionSwingOscillator;
		public PriceActionSwing.PriceActionSwingOscillator PriceActionSwingOscillator(SwingStyle swingStyleType, double swingSize, int dtbStrength, bool useCloseValues, bool ignoreInsideBars, bool useBreakouts, ShowIndicationStyle showIndicationType, bool useOldTrend)
		{
			return PriceActionSwingOscillator(Input, swingStyleType, swingSize, dtbStrength, useCloseValues, ignoreInsideBars, useBreakouts, showIndicationType, useOldTrend);
		}

		public PriceActionSwing.PriceActionSwingOscillator PriceActionSwingOscillator(ISeries<double> input, SwingStyle swingStyleType, double swingSize, int dtbStrength, bool useCloseValues, bool ignoreInsideBars, bool useBreakouts, ShowIndicationStyle showIndicationType, bool useOldTrend)
		{
			if (cachePriceActionSwingOscillator != null)
				for (int idx = 0; idx < cachePriceActionSwingOscillator.Length; idx++)
					if (cachePriceActionSwingOscillator[idx] != null && cachePriceActionSwingOscillator[idx].SwingStyleType == swingStyleType && cachePriceActionSwingOscillator[idx].SwingSize == swingSize && cachePriceActionSwingOscillator[idx].DtbStrength == dtbStrength && cachePriceActionSwingOscillator[idx].UseCloseValues == useCloseValues && cachePriceActionSwingOscillator[idx].IgnoreInsideBars == ignoreInsideBars && cachePriceActionSwingOscillator[idx].UseBreakouts == useBreakouts && cachePriceActionSwingOscillator[idx].ShowIndicationType == showIndicationType && cachePriceActionSwingOscillator[idx].UseOldTrend == useOldTrend && cachePriceActionSwingOscillator[idx].EqualsInput(input))
						return cachePriceActionSwingOscillator[idx];
			return CacheIndicator<PriceActionSwing.PriceActionSwingOscillator>(new PriceActionSwing.PriceActionSwingOscillator(){ SwingStyleType = swingStyleType, SwingSize = swingSize, DtbStrength = dtbStrength, UseCloseValues = useCloseValues, IgnoreInsideBars = ignoreInsideBars, UseBreakouts = useBreakouts, ShowIndicationType = showIndicationType, UseOldTrend = useOldTrend }, input, ref cachePriceActionSwingOscillator);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.PriceActionSwing.PriceActionSwingOscillator PriceActionSwingOscillator(SwingStyle swingStyleType, double swingSize, int dtbStrength, bool useCloseValues, bool ignoreInsideBars, bool useBreakouts, ShowIndicationStyle showIndicationType, bool useOldTrend)
		{
			return indicator.PriceActionSwingOscillator(Input, swingStyleType, swingSize, dtbStrength, useCloseValues, ignoreInsideBars, useBreakouts, showIndicationType, useOldTrend);
		}

		public Indicators.PriceActionSwing.PriceActionSwingOscillator PriceActionSwingOscillator(ISeries<double> input , SwingStyle swingStyleType, double swingSize, int dtbStrength, bool useCloseValues, bool ignoreInsideBars, bool useBreakouts, ShowIndicationStyle showIndicationType, bool useOldTrend)
		{
			return indicator.PriceActionSwingOscillator(input, swingStyleType, swingSize, dtbStrength, useCloseValues, ignoreInsideBars, useBreakouts, showIndicationType, useOldTrend);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.PriceActionSwing.PriceActionSwingOscillator PriceActionSwingOscillator(SwingStyle swingStyleType, double swingSize, int dtbStrength, bool useCloseValues, bool ignoreInsideBars, bool useBreakouts, ShowIndicationStyle showIndicationType, bool useOldTrend)
		{
			return indicator.PriceActionSwingOscillator(Input, swingStyleType, swingSize, dtbStrength, useCloseValues, ignoreInsideBars, useBreakouts, showIndicationType, useOldTrend);
		}

		public Indicators.PriceActionSwing.PriceActionSwingOscillator PriceActionSwingOscillator(ISeries<double> input , SwingStyle swingStyleType, double swingSize, int dtbStrength, bool useCloseValues, bool ignoreInsideBars, bool useBreakouts, ShowIndicationStyle showIndicationType, bool useOldTrend)
		{
			return indicator.PriceActionSwingOscillator(input, swingStyleType, swingSize, dtbStrength, useCloseValues, ignoreInsideBars, useBreakouts, showIndicationType, useOldTrend);
		}
	}
}

#endregion
