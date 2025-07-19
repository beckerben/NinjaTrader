//
// Copyright (C) 2025, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Core;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript.DrawingTools;
using SharpDX.DirectWrite;
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators
{
	[TypeConverter("NinjaTrader.NinjaScript.Indicators.FibonacciPivotsTypeConverter")]
	public class FibonacciPivots : Indicator
	{
		private DateTime				cacheMonthlyEndDate		= Globals.MinDate;
		private DateTime				cacheSessionDate		= Globals.MinDate;
		private DateTime				cacheSessionEnd			= Globals.MinDate;
		private DateTime				cacheTime;
		private DateTime				cacheWeeklyEndDate		= Globals.MinDate;
		private DateTime				currentDate				= Globals.MinDate;
		private DateTime				currentMonth			= Globals.MinDate;
		private DateTime				currentWeek				= Globals.MinDate;
		private DateTime				sessionDateTmp			= Globals.MinDate;
		private SessionIterator			storedSession;
		private double					currentClose;
		private double					currentHigh				= double.MinValue;
		private double					currentLow				= double.MaxValue;
		private double					dailyBarClose			= double.MinValue;
		private double					dailyBarHigh			= double.MinValue;
		private double					dailyBarLow				= double.MinValue;
		private double					pp;
		private double					r1;
		private double					r2;
		private double					r3;
		private double					s1;
		private double					s2;
		private double					s3;
		private int						cacheBar;
		private readonly List<int>		newSessionBarIdxArr		= new();

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description				= Custom.Resource.NinjaScriptIndicatorDescriptionFibonacciPivots;
				Name					= Custom.Resource.NinjaScriptIndicatorNameFibonacciPivots;
				Calculate				= Calculate.OnBarClose;
				DisplayInDataBox		= true;
				DrawOnPricePanel		= false;
				IsAutoScale				= false;
				IsOverlay				= true;
				PaintPriceMarkers		= true;
				ScaleJustification		= ScaleJustification.Right;

				AddPlot(Brushes.Goldenrod,	Custom.Resource.PivotsPP);
				AddPlot(Brushes.DodgerBlue,	Custom.Resource.PivotsR1);
				AddPlot(Brushes.DodgerBlue,	Custom.Resource.PivotsR2);
				AddPlot(Brushes.DodgerBlue,	Custom.Resource.PivotsR3);
				AddPlot(Brushes.Crimson,	Custom.Resource.PivotsS1);
				AddPlot(Brushes.Crimson,	Custom.Resource.PivotsS2);
				AddPlot(Brushes.Crimson,	Custom.Resource.PivotsS3);
			}
			else if (State == State.Configure)
			{
				if (PriorDayHlc == HLCCalculationMode.DailyBars)
					AddDataSeries(BarsPeriodType.Day, 1);
			}
			else if (State == State.DataLoaded)
			{
				storedSession = new SessionIterator(Bars);
			}
			else if (State == State.Historical)
			{
				if (PriorDayHlc == HLCCalculationMode.DailyBars && BarsArray[1].DayCount <= 0)
				{
					Draw.TextFixed(this, "NinjaScriptInfo", Custom.Resource.PiviotsDailyDataError, TextPosition.BottomRight);
					Log(Custom.Resource.PiviotsDailyDataError, LogLevel.Error);
					return;
				}

				if (!Bars.BarsType.IsIntraday && BarsPeriod.BarsPeriodType != BarsPeriodType.Day && (BarsPeriod.BarsPeriodType != BarsPeriodType.HeikenAshi && BarsPeriod.BarsPeriodType != BarsPeriodType.PriceOnVolume && BarsPeriod.BarsPeriodType != BarsPeriodType.Volumetric || BarsPeriod.BaseBarsPeriodType != BarsPeriodType.Day))
				{
					Draw.TextFixed(this, "NinjaScriptInfo", Custom.Resource.PiviotsDailyBarsError, TextPosition.BottomRight);
					Log(Custom.Resource.PiviotsDailyBarsError, LogLevel.Error);
				}
				if ((BarsPeriod.BarsPeriodType == BarsPeriodType.Day || ((BarsPeriod.BarsPeriodType == BarsPeriodType.HeikenAshi || BarsPeriod.BarsPeriodType == BarsPeriodType.PriceOnVolume || BarsPeriod.BarsPeriodType == BarsPeriodType.Volumetric) && BarsPeriod.BaseBarsPeriodType == BarsPeriodType.Day)) && PivotRangeType == PivotRange.Daily)
				{
					Draw.TextFixed(this, "NinjaScriptInfo", Custom.Resource.PiviotsWeeklyBarsError, TextPosition.BottomRight);
					Log(Custom.Resource.PiviotsWeeklyBarsError, LogLevel.Error);
				}
				if ((BarsPeriod.BarsPeriodType == BarsPeriodType.Day || ((BarsPeriod.BarsPeriodType == BarsPeriodType.HeikenAshi || BarsPeriod.BarsPeriodType == BarsPeriodType.PriceOnVolume || BarsPeriod.BarsPeriodType == BarsPeriodType.Volumetric) && BarsPeriod.BaseBarsPeriodType == BarsPeriodType.Day)) && BarsPeriod.Value > 1)
				{
					Draw.TextFixed(this, "NinjaScriptInfo", Custom.Resource.PiviotsPeriodTypeError, TextPosition.BottomRight);
					Log(Custom.Resource.PiviotsPeriodTypeError, LogLevel.Error);
				}
				if ((PriorDayHlc == HLCCalculationMode.DailyBars &&
					(PivotRangeType == PivotRange.Monthly && BarsArray[1].GetTime(0).Date >= BarsArray[1].GetTime(BarsArray[1].Count - 1).Date.AddMonths(-1)
					|| PivotRangeType == PivotRange.Weekly && BarsArray[1].GetTime(0).Date >= BarsArray[1].GetTime(BarsArray[1].Count - 1).Date.AddDays(-7)
					|| PivotRangeType == PivotRange.Daily && BarsArray[1].GetTime(0).Date >= BarsArray[1].GetTime(BarsArray[1].Count - 1).Date.AddDays(-1)))
					|| PivotRangeType == PivotRange.Monthly && BarsArray[0].GetTime(0).Date >= BarsArray[0].GetTime(BarsArray[0].Count - 1).Date.AddMonths(-1)
					|| PivotRangeType == PivotRange.Weekly && BarsArray[0].GetTime(0).Date >= BarsArray[0].GetTime(BarsArray[0].Count - 1).Date.AddDays(-7)
					|| PivotRangeType == PivotRange.Daily && BarsArray[0].GetTime(0).Date >= BarsArray[0].GetTime(BarsArray[0].Count - 1).Date.AddDays(-1)
					)
				{
					Draw.TextFixed(this, "NinjaScriptInfo", Custom.Resource.PiviotsInsufficentDataError, TextPosition.BottomRight);
					Log(Custom.Resource.PiviotsInsufficentDataError, LogLevel.Error);
				}
			}
		}

		protected override void OnBarUpdate()
		{
			if (BarsInProgress != 0)
				return;

			if ((PriorDayHlc == HLCCalculationMode.DailyBars && BarsArray[1].DayCount <= 0)
					|| (!Bars.BarsType.IsIntraday && BarsPeriod.BarsPeriodType != BarsPeriodType.Day && (BarsPeriod.BarsPeriodType != BarsPeriodType.HeikenAshi && BarsPeriod.BarsPeriodType != BarsPeriodType.PriceOnVolume && BarsPeriod.BarsPeriodType != BarsPeriodType.Volumetric || BarsPeriod.BaseBarsPeriodType != BarsPeriodType.Day))
					|| ((BarsPeriod.BarsPeriodType == BarsPeriodType.Day || ((BarsPeriod.BarsPeriodType == BarsPeriodType.HeikenAshi || BarsPeriod.BarsPeriodType == BarsPeriodType.PriceOnVolume || BarsPeriod.BarsPeriodType == BarsPeriodType.Volumetric) && BarsPeriod.BaseBarsPeriodType == BarsPeriodType.Day)) && PivotRangeType == PivotRange.Daily)
					|| ((BarsPeriod.BarsPeriodType == BarsPeriodType.Day || ((BarsPeriod.BarsPeriodType == BarsPeriodType.HeikenAshi || BarsPeriod.BarsPeriodType == BarsPeriodType.PriceOnVolume || BarsPeriod.BarsPeriodType == BarsPeriodType.Volumetric) && BarsPeriod.BaseBarsPeriodType == BarsPeriodType.Day)) && BarsPeriod.Value > 1)
					|| (PriorDayHlc == HLCCalculationMode.DailyBars && (PivotRangeType == PivotRange.Monthly && BarsArray[1].GetTime(0).Date >= BarsArray[1].GetTime(BarsArray[1].Count - 1).Date.AddMonths(-1)
																			|| PivotRangeType == PivotRange.Weekly && BarsArray[1].GetTime(0).Date >= BarsArray[1].GetTime(BarsArray[1].Count - 1).Date.AddDays(-7)
																			|| PivotRangeType == PivotRange.Daily && BarsArray[1].GetTime(0).Date >= BarsArray[1].GetTime(BarsArray[1].Count - 1).Date.AddDays(-1)))
					|| PivotRangeType == PivotRange.Monthly && BarsArray[0].GetTime(0).Date >= BarsArray[0].GetTime(BarsArray[0].Count - 1).Date.AddMonths(-1)
					|| PivotRangeType == PivotRange.Weekly && BarsArray[0].GetTime(0).Date >= BarsArray[0].GetTime(BarsArray[0].Count - 1).Date.AddDays(-7)
					|| PivotRangeType == PivotRange.Daily && BarsArray[0].GetTime(0).Date >= BarsArray[0].GetTime(BarsArray[0].Count - 1).Date.AddDays(-1))
				return;

			RemoveDrawObject("NinjaScriptInfo");

			if (PriorDayHlc == HLCCalculationMode.DailyBars && CurrentBars[1] >= 0)
			{
				// Get daily bars like this to avoid situation where primary series moves to next session before previous day OHLC are added
				if (cacheTime != Times[0][0])
				{
					cacheTime	= Times[0][0];
					cacheBar	= BarsArray[1].GetBar(Times[0][0]);
				}
				dailyBarHigh	= BarsArray[1].GetHigh(cacheBar);
				dailyBarLow		= BarsArray[1].GetLow(cacheBar);
				dailyBarClose	= BarsArray[1].GetClose(cacheBar);
			}
			else
			{
				dailyBarHigh	= double.MinValue;
				dailyBarLow		= double.MinValue;
				dailyBarClose	= double.MinValue;
			}

			double high		= dailyBarHigh <= double.MinValue	? Highs[0][0]	: dailyBarHigh;
			double low		= dailyBarLow <= double.MinValue	? Lows[0][0]	: dailyBarLow;
			double close	= dailyBarClose <= double.MinValue	? Closes[0][0]	: dailyBarClose;

			DateTime lastBarTimeStamp = GetLastBarSessionDate(Times[0][0], PivotRangeType);

			if ((currentDate != Globals.MinDate && PivotRangeType == PivotRange.Daily && lastBarTimeStamp != currentDate)
				|| (currentWeek != Globals.MinDate && PivotRangeType == PivotRange.Weekly && lastBarTimeStamp != currentWeek)
				|| (currentMonth != Globals.MinDate && PivotRangeType == PivotRange.Monthly && lastBarTimeStamp != currentMonth))
			{
				pp				= (currentHigh + currentLow + currentClose) / 3;
				s1				= pp - (currentHigh - currentLow) * 0.382;
				r1				= pp + (currentHigh - currentLow) * 0.382;
				s2				= pp - (currentHigh - currentLow) * 0.618;
				r2				= pp + (currentHigh - currentLow) * 0.618;
				s3				= pp - (currentHigh - currentLow) * 1.000;
				r3				= pp + (currentHigh - currentLow) * 1.000;
				currentClose	= PriorDayHlc == HLCCalculationMode.UserDefinedValues ? UserDefinedClose	: close;
				currentHigh		= PriorDayHlc == HLCCalculationMode.UserDefinedValues ? UserDefinedHigh	: high;
				currentLow		= PriorDayHlc == HLCCalculationMode.UserDefinedValues ? UserDefinedLow	: low;
			}
			else
			{
				currentClose	= PriorDayHlc == HLCCalculationMode.UserDefinedValues ? UserDefinedClose	: close;
				currentHigh		= PriorDayHlc == HLCCalculationMode.UserDefinedValues ? UserDefinedHigh	: Math.Max(currentHigh, high);
				currentLow		= PriorDayHlc == HLCCalculationMode.UserDefinedValues ? UserDefinedLow	: Math.Min(currentLow, low);
			}


			if (PivotRangeType == PivotRange.Daily)
				currentDate = lastBarTimeStamp;
			if (PivotRangeType == PivotRange.Weekly)
				currentWeek = lastBarTimeStamp;
			if (PivotRangeType == PivotRange.Monthly)
				currentMonth = lastBarTimeStamp;

			if ((PivotRangeType == PivotRange.Daily && currentDate != Globals.MinDate)
				|| (PivotRangeType == PivotRange.Weekly && currentWeek != Globals.MinDate)
				|| (PivotRangeType == PivotRange.Monthly && currentMonth != Globals.MinDate))
			{
				Pp[0] = pp;
				R1[0] = r1;
				R2[0] = r2;
				R3[0] = r3;
				S1[0] = s1;
				S2[0] = s2;
				S3[0] = s3;
			}
		}

		#region Misc
		private DateTime GetLastBarSessionDate(DateTime time, PivotRange pivotRange)
		{
			// Check the time[0] against the previous session end
			if (time > cacheSessionEnd)
			{
				if (Bars.BarsType.IsIntraday)
				{
					// Make use of the stored session iterator to find the next session...
					storedSession.GetNextSession(time, true);
					// Store the actual session's end datetime as the session
					cacheSessionEnd = storedSession.ActualSessionEnd;
					// We need to convert that time from the session to the users time zone settings
					sessionDateTmp = TimeZoneInfo.ConvertTime(cacheSessionEnd.AddSeconds(-1), Globals.GeneralOptions.TimeZoneInfo, Bars.TradingHours.TimeZoneInfo).Date;
				}
				else
					sessionDateTmp = time.Date;
			}

			if (pivotRange == PivotRange.Daily)
			{
				if (sessionDateTmp != cacheSessionDate)
				{
					if (newSessionBarIdxArr.Count == 0 || newSessionBarIdxArr.Count > 0 && CurrentBar > newSessionBarIdxArr[newSessionBarIdxArr.Count - 1])
						newSessionBarIdxArr.Add(CurrentBar);
					cacheSessionDate = sessionDateTmp;
				}
				return sessionDateTmp;
			}

			DateTime tmpWeeklyEndDate = RoundUpTimeToPeriodTime(sessionDateTmp, PivotRange.Weekly);
			if (pivotRange == PivotRange.Weekly)
			{
				if (tmpWeeklyEndDate != cacheWeeklyEndDate)
				{
					if (newSessionBarIdxArr.Count == 0 || newSessionBarIdxArr.Count > 0 && CurrentBar > newSessionBarIdxArr[newSessionBarIdxArr.Count - 1])
						newSessionBarIdxArr.Add(CurrentBar);
					cacheWeeklyEndDate = tmpWeeklyEndDate;
				}
				return tmpWeeklyEndDate;
			}

			DateTime tmpMonthlyEndDate = RoundUpTimeToPeriodTime(sessionDateTmp, PivotRange.Monthly);
			if (tmpMonthlyEndDate != cacheMonthlyEndDate)
			{
				if (newSessionBarIdxArr.Count == 0 || newSessionBarIdxArr.Count > 0 && CurrentBar > newSessionBarIdxArr[newSessionBarIdxArr.Count - 1])
					newSessionBarIdxArr.Add(CurrentBar);
				cacheMonthlyEndDate = tmpMonthlyEndDate;
			}
			return tmpMonthlyEndDate;
		}

		private DateTime RoundUpTimeToPeriodTime(DateTime time, PivotRange pivotRange)
		{
			if (pivotRange == PivotRange.Weekly)
				return Gui.Tools.Extensions.GetEndOfWeekTime(time);
			if (pivotRange == PivotRange.Monthly)
				return Gui.Tools.Extensions.GetEndOfMonthTime(time);
			return time;
		}

		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
			// Set text to chart label color and font
			TextFormat	textFormat			= chartControl.Properties.LabelFont.ToDirectWriteTextFormat();

			// Loop through each Plot Values on the chart
			for (int seriesCount = 0; seriesCount < Values.Length; seriesCount++)
			{
				double	y					= -1;
				double	startX				= -1;
				double	endX				= -1;
				int		firstBarIdxToPaint	= -1;
				int		firstBarPainted		= ChartBars.FromIndex;
				int		lastBarPainted		= ChartBars.ToIndex;
				Plot	plot				= Plots[seriesCount];

				for (int i = newSessionBarIdxArr.Count - 1; i >= 0; i--)
				{
					int prevSessionBreakIdx = newSessionBarIdxArr[i];
					if (prevSessionBreakIdx <= lastBarPainted)
					{
						firstBarIdxToPaint = prevSessionBreakIdx;
						break;
					}
				}

				// Loop through visble bars to render plot values
				for (int idx = lastBarPainted; idx >= Math.Max(firstBarPainted, lastBarPainted - Width); idx--)
				{
					if (idx < firstBarIdxToPaint)
						break;

					startX		= chartControl.GetXByBarIndex(ChartBars, idx);
					endX		= chartControl.GetXByBarIndex(ChartBars, lastBarPainted);
					double val	= Values[seriesCount].GetValueAt(idx);
					y			= chartScale.GetYByValue(val);
				}

				// Draw pivot lines
				Point startPoint	= new(startX, y);
				Point endPoint		= new(endX, y);
				RenderTarget.DrawLine(startPoint.ToVector2(), endPoint.ToVector2(), plot.BrushDX, plot.Width, plot.StrokeStyle);

				// Draw pivot text
				TextLayout textLayout = new(Globals.DirectWriteFactory, plot.Name, textFormat, ChartPanel.W, textFormat.FontSize);
				RenderTarget.DrawTextLayout(startPoint.ToVector2(), textLayout, plot.BrushDX);
				textLayout.Dispose();
			}
			textFormat.Dispose();
		}
		#endregion

		#region Properties

		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "PivotRange", GroupName = "NinjaScriptParameters", Order = 0)]
		public PivotRange PivotRangeType { get; set; }

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> Pp => Values[0];

		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "HLCCalculationMode", GroupName = "NinjaScriptParameters", Order = 1)]
		[RefreshProperties(RefreshProperties.All)] // Update UI when value is changed
		public HLCCalculationMode PriorDayHlc { get; set; }

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> R1 => Values[1];

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> R2 => Values[2];

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> R3 => Values[3];

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> S1 => Values[4];

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> S2 => Values[5];

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> S3 => Values[6];

		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "UserDefinedClose", GroupName = "NinjaScriptParameters", Order = 2)]
		public double UserDefinedClose { get; set; }

		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "UserDefinedHigh", GroupName = "NinjaScriptParameters", Order = 3)]
		public double UserDefinedHigh { get; set; }

		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "UserDefinedLow", GroupName = "NinjaScriptParameters", Order = 4)]
		public double UserDefinedLow { get; set; }

		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Width", GroupName = "NinjaScriptParameters", Order = 5)]
		public int Width { get; set; } = 20;

		#endregion
	}
	
	// Hide UserDefinedValues properties when not in use by the HLCCalculationMode.UserDefinedValues
	// When creating a custom type converter for indicators it must inherit from NinjaTrader.NinjaScript.IndicatorBaseConverter to work correctly with indicators
	public class FibonacciPivotsTypeConverter : IndicatorBaseConverter
	{
		public override bool GetPropertiesSupported(ITypeDescriptorContext context) { return true; }

		public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes)
		{
			PropertyDescriptorCollection propertyDescriptorCollection = base.GetPropertiesSupported(context) ? base.GetProperties(context, value, attributes) : TypeDescriptor.GetProperties(value, attributes);

			FibonacciPivots		thisPivotsInstance			= (FibonacciPivots) value;
			HLCCalculationMode	selectedHLCCalculationMode	= thisPivotsInstance.PriorDayHlc;
			if (selectedHLCCalculationMode == HLCCalculationMode.UserDefinedValues)
				return propertyDescriptorCollection;

			PropertyDescriptorCollection adjusted = new(null);
			foreach (PropertyDescriptor thisDescriptor in propertyDescriptorCollection)
			{
				if (thisDescriptor.Name is "UserDefinedClose" or "UserDefinedHigh" or "UserDefinedLow")
					adjusted.Add(new PropertyDescriptorExtended(thisDescriptor, _ => value, null, new Attribute[] {new BrowsableAttribute(false), }));
				else
					adjusted.Add(thisDescriptor);
			}
			return adjusted;
		}
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private FibonacciPivots[] cacheFibonacciPivots;
		public FibonacciPivots FibonacciPivots(PivotRange pivotRangeType, HLCCalculationMode priorDayHlc, double userDefinedClose, double userDefinedHigh, double userDefinedLow, int width)
		{
			return FibonacciPivots(Input, pivotRangeType, priorDayHlc, userDefinedClose, userDefinedHigh, userDefinedLow, width);
		}

		public FibonacciPivots FibonacciPivots(ISeries<double> input, PivotRange pivotRangeType, HLCCalculationMode priorDayHlc, double userDefinedClose, double userDefinedHigh, double userDefinedLow, int width)
		{
			if (cacheFibonacciPivots != null)
				for (int idx = 0; idx < cacheFibonacciPivots.Length; idx++)
					if (cacheFibonacciPivots[idx] != null && cacheFibonacciPivots[idx].PivotRangeType == pivotRangeType && cacheFibonacciPivots[idx].PriorDayHlc == priorDayHlc && cacheFibonacciPivots[idx].UserDefinedClose == userDefinedClose && cacheFibonacciPivots[idx].UserDefinedHigh == userDefinedHigh && cacheFibonacciPivots[idx].UserDefinedLow == userDefinedLow && cacheFibonacciPivots[idx].Width == width && cacheFibonacciPivots[idx].EqualsInput(input))
						return cacheFibonacciPivots[idx];
			return CacheIndicator<FibonacciPivots>(new FibonacciPivots(){ PivotRangeType = pivotRangeType, PriorDayHlc = priorDayHlc, UserDefinedClose = userDefinedClose, UserDefinedHigh = userDefinedHigh, UserDefinedLow = userDefinedLow, Width = width }, input, ref cacheFibonacciPivots);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.FibonacciPivots FibonacciPivots(PivotRange pivotRangeType, HLCCalculationMode priorDayHlc, double userDefinedClose, double userDefinedHigh, double userDefinedLow, int width)
		{
			return indicator.FibonacciPivots(Input, pivotRangeType, priorDayHlc, userDefinedClose, userDefinedHigh, userDefinedLow, width);
		}

		public Indicators.FibonacciPivots FibonacciPivots(ISeries<double> input , PivotRange pivotRangeType, HLCCalculationMode priorDayHlc, double userDefinedClose, double userDefinedHigh, double userDefinedLow, int width)
		{
			return indicator.FibonacciPivots(input, pivotRangeType, priorDayHlc, userDefinedClose, userDefinedHigh, userDefinedLow, width);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.FibonacciPivots FibonacciPivots(PivotRange pivotRangeType, HLCCalculationMode priorDayHlc, double userDefinedClose, double userDefinedHigh, double userDefinedLow, int width)
		{
			return indicator.FibonacciPivots(Input, pivotRangeType, priorDayHlc, userDefinedClose, userDefinedHigh, userDefinedLow, width);
		}

		public Indicators.FibonacciPivots FibonacciPivots(ISeries<double> input , PivotRange pivotRangeType, HLCCalculationMode priorDayHlc, double userDefinedClose, double userDefinedHigh, double userDefinedLow, int width)
		{
			return indicator.FibonacciPivots(input, pivotRangeType, priorDayHlc, userDefinedClose, userDefinedHigh, userDefinedLow, width);
		}
	}
}

#endregion
