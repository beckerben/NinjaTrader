//
// Copyright (C) 2025, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
#region Using declarations
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators
{
	public class CandlestickPattern : Indicator
	{
		private Brush					downBrush				= Brushes.DimGray;
		private CandleStickPatternLogic	logic;
		private int 					numPatternsFound;
		private readonly TextPosition	textBoxPosition			= TextPosition.BottomRight;
		private Brush					textBrush				= Brushes.DimGray;
		private Brush					upBrush					= Brushes.DimGray;
		
		private void DrawText(string text, int barsAgo, double price, int yOffset)
		{
			Draw.Text(this, text + CurrentBar, false, text + " # " + ++numPatternsFound, barsAgo, price, yOffset, textBrush, TextFont, 
				TextAlignment.Center, Brushes.Transparent, Brushes.Transparent, 0);
		}

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description			= Custom.Resource.NinjaScriptIndicatorDescriptionCandlestickPattern;
				Name				= Custom.Resource.NinjaScriptIndicatorNameCandlestickPattern;
				Calculate			= Calculate.OnBarClose;
				IsOverlay			= true;
				DrawOnPricePanel	= true;
				DisplayInDataBox	= false;
				IsAutoScale			= false;
				PaintPriceMarkers	= false;
				Pattern				= ChartPattern.MorningStar;
				ShowAlerts			= true;
				ShowPatternCount	= true;
				TrendStrength		= 4;
				TextFont			= new Gui.Tools.SimpleFont { Size = 14 };

				downBrush			= Brushes.DimGray;
				upBrush				= Brushes.DimGray;
				textBrush			= Brushes.DimGray;

				AddPlot(Brushes.Transparent, Custom.Resource.CandlestickPatternFound);
			}
			else if (State == State.Configure)
				Calculate	= Calculate.OnBarClose;
			else if (State == State.DataLoaded)
				logic = new CandleStickPatternLogic(this, TrendStrength);
			else if (State == State.Historical)
			{
				if (ChartControl != null)
				{
					downBrush	= ChartControl.Properties.AxisPen.Brush;
					textBrush	= ChartControl.Properties.ChartText;
				}

				if (downBrush == upBrush)
					upBrush	= Brushes.Transparent;
			}
		}

		protected override void OnBarUpdate()
		{
			PatternFound[0] = logic.Evaluate(Pattern) ? 1 : 0;
			
			if (Math.Abs(PatternFound[0] - 1) < 0.0000000000001)
			{
				bool 	isBearish 	= false;
				string 	text 		= string.Empty;
				
				switch (Pattern)
				{
					case ChartPattern.BearishBeltHold: 		text = "Bearish Belt Hold"; 	isBearish = true; 	break;
					case ChartPattern.BearishEngulfing: 	text = "Bearish Engulfing"; 	isBearish = true; 	break;
					case ChartPattern.BearishHarami: 		text = "Bearish Harami"; 		isBearish = true; 	break;
					case ChartPattern.BearishHaramiCross: 	text = "Bearish Harami Cross"; 	isBearish = true; 	break;
					case ChartPattern.BullishBeltHold: 		text = "Bullish Belt Hold"; 						break;
					case ChartPattern.BullishEngulfing: 	text = "Bullish Engulfing"; 						break;
					case ChartPattern.BullishHarami: 		text = "Bullish Harami"; 							break;
					case ChartPattern.BullishHaramiCross: 	text = "Bullish Harami Cross"; 						break;
				}
				
				if (!string.IsNullOrEmpty(text))
				{
					BarBrushes[1] 			= isBearish 								? upBrush 	: downBrush;
					BarBrushes[0] 			= isBearish 								? downBrush : upBrush;
					CandleOutlineBrushes[1] = Pattern == ChartPattern.BearishBeltHold 	? downBrush : CandleOutlineBrushes[1];
					CandleOutlineBrushes[0] = !isBearish 								? downBrush : CandleOutlineBrushes[0];
					DrawText(text, 0, isBearish ? Math.Max(High[0], High[1]) : Math.Min(Low[0], Low[1]), isBearish ? 40 : 10);
				}
				
				switch (Pattern)
				{
					case ChartPattern.DarkCloudCover:
						BarBrushes[1] 			= upBrush;
						BarBrushes[0] 			= downBrush;
						CandleOutlineBrushes[1] = downBrush;
						DrawText("Dark Cloud Cover", 1, Math.Max(High[0], High[1]), 50);
						break;
					case ChartPattern.Doji:
						BarBrushes[0] 			= upBrush;
						CandleOutlineBrushes[0] = downBrush;
						int yOffset 			= Close[0] > Close[Math.Min(1, CurrentBar)] ? 20 : -20;
						DrawText("Doji", 0, yOffset > 0 ? High[0] : Low[0], yOffset);
						break;
					case ChartPattern.DownsideTasukiGap:
						BarBrushes[2] 			= downBrush;
						BarBrushes[1] 			= downBrush;
						BarBrushes[0] 			= upBrush;
						CandleOutlineBrushes[0] = downBrush;
						DrawText("Downside Tasuki Gap", 1, MAX(High, 3)[0], 10);
						break;
					case ChartPattern.EveningStar:
						BarBrushes[2] 			= Close[2] > Open[2] ? upBrush : downBrush;
						BarBrushes[1] 			= Close[1] > Open[1] ? upBrush : downBrush;
						BarBrushes[0] 			= Close[0] > Open[0] ? upBrush : downBrush;
						CandleOutlineBrushes[2] = Close[2] > Open[2] ? downBrush : CandleOutlineBrushes[2];
						CandleOutlineBrushes[1] = Close[1] > Open[1] ? downBrush : CandleOutlineBrushes[1];
						CandleOutlineBrushes[0] = Close[0] > Open[0] ? downBrush : CandleOutlineBrushes[0];
						DrawText("Evening Star", 1, MAX(High, 3)[0], 40);
						break;
					case ChartPattern.FallingThreeMethods:
						BarBrushes[4] = downBrush;
						BarBrushes[0] = downBrush;
						for (int i = 1; i < 4; i++)
						{
							BarBrushes[i] 			= Close[i] > Open[i] ? upBrush : downBrush;
							CandleOutlineBrushes[i] = Close[i] > Open[i] ? downBrush : CandleOutlineBrushes[i];
						}
						DrawText("Falling Three Methods", 2, Math.Max(High[0], High[4]), 40);
						break;
					case ChartPattern.Hammer:
						BarBrushes[0] 			= Close[0] > Open[0] ? upBrush : downBrush;
						CandleOutlineBrushes[0] = Close[0] > Open[0] ? downBrush : CandleOutlineBrushes[0];
						DrawText("Hammer", 0, Low[0], -20);
						break;
					case ChartPattern.HangingMan:
						BarBrushes[0] 			= Close[0] > Open[0] ? upBrush : downBrush;
						CandleOutlineBrushes[0] = Close[0] > Open[0] ? downBrush : CandleOutlineBrushes[0];
						DrawText("Hanging Man", 0, Low[0], -20);
						break;
					case ChartPattern.InvertedHammer:
						BarBrushes[0] 			= Close[0] > Open[0] ? upBrush : downBrush;
						CandleOutlineBrushes[0] = Close[0] > Open[0] ? downBrush : CandleOutlineBrushes[0];
						DrawText("Inverted Hammer", 0, Low[0] - 2 * TickSize, 20);
						break;
					case ChartPattern.MorningStar:
						BarBrushes[2] 			= Close[2] > Open[2] ? upBrush : downBrush;
						BarBrushes[1] 			= Close[1] > Open[1] ? upBrush : downBrush;
						BarBrushes[0] 			= Close[0] > Open[0] ? upBrush : downBrush;
						CandleOutlineBrushes[2] = Close[2] > Open[2] ? downBrush : CandleOutlineBrushes[2];
						CandleOutlineBrushes[1] = Close[1] > Open[1] ? downBrush : CandleOutlineBrushes[1];
						CandleOutlineBrushes[0] = Close[0] > Open[0] ? downBrush : CandleOutlineBrushes[0];
						DrawText("Morning Star", 1, MIN(Low, 3)[0], -20);
						break;
					case ChartPattern.PiercingLine:
						BarBrushes[1] 			= upBrush;
						BarBrushes[0] 			= downBrush;
						CandleOutlineBrushes[1] = downBrush;
						DrawText("Piercing Line", 1, Low[0], -10);
						break;
					case ChartPattern.RisingThreeMethods:
						BarBrushes[4] 			= upBrush;
						BarBrushes[0] 			= upBrush;
						CandleOutlineBrushes[4] = downBrush;
						CandleOutlineBrushes[0] = downBrush;
						for (int i = 1; i < 4; i++)
						{
							BarBrushes[i] 			= Close[i] > Open[i] ? upBrush : downBrush;
							CandleOutlineBrushes[i] = Close[i] > Open[i] ? downBrush : CandleOutlineBrushes[i];
						}
						DrawText("Rising Three Methods", 2, MIN(Low, 5)[0], -10);
						break;
					case ChartPattern.ShootingStar:
						BarBrushes[0] = downBrush;
						DrawText("Shooting Star", 0, High[0], 30);
						break;
					case ChartPattern.StickSandwich:
						BarBrushes[2] 			= downBrush;
						BarBrushes[1] 			= upBrush;
						BarBrushes[0] 			= downBrush;
						CandleOutlineBrushes[1] = downBrush;
						DrawText("Stick Sandwich", 1, MAX(High, 3)[0], 50);
						break;
					case ChartPattern.ThreeBlackCrows:
						BarBrushes[2] 			= downBrush;
						BarBrushes[1] 			= downBrush;
						BarBrushes[0] 			= downBrush;
						DrawText("Three Black Crows", 1, MAX(High, 3)[0], 50);
						break;
					case ChartPattern.ThreeWhiteSoldiers:
						BarBrushes[2] 			= upBrush;
						BarBrushes[1] 			= upBrush;
						BarBrushes[0] 			= upBrush;
						CandleOutlineBrushes[2] = downBrush;
						CandleOutlineBrushes[1] = downBrush;
						CandleOutlineBrushes[0] = downBrush;
						DrawText("Three White Soldiers", 1, Low[2], -10);
						break;
					case ChartPattern.UpsideGapTwoCrows:
						BarBrushes[2] 			= upBrush;
						BarBrushes[1] 			= downBrush;
						BarBrushes[0] 			= downBrush;
						CandleOutlineBrushes[2] = downBrush;
						DrawText("Upside Gap Two Crows", 1, Math.Max(High[0], High[1]), 10);
						break;
					case ChartPattern.UpsideTasukiGap:
						BarBrushes[2] 			= upBrush;
						BarBrushes[1] 			= upBrush;
						BarBrushes[0] 			= downBrush;
						CandleOutlineBrushes[2] = downBrush;
						CandleOutlineBrushes[1] = downBrush;
						DrawText("Upide Tasuki Gap", 1, MIN(Low, 3)[0], -20);
						break;

					}

				if (ShowAlerts)
					Alert("myAlert", Priority.Low, $"Pattern(s) found: {numPatternsFound} {Pattern} on {Instrument.FullName} {BarsPeriod.Value} {BarsPeriod.BarsPeriodType} Chart", "Alert3.wav", 10, Brushes.Transparent, textBrush);
			}
			
			if (ShowPatternCount)
				Draw.TextFixed(this, "Count", $"{numPatternsFound} {Pattern}\n patterns found", textBoxPosition, textBrush, TextFont, 
					Brushes.Transparent, Brushes.Transparent, 0);
		}

		public override string ToString() => $"{Name}({Pattern})";

		#region Properties
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> PatternFound => Values[0];

		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "SelectPattern", Description = "SelectPatternDescription", GroupName = "NinjaScriptGeneral", Order = 1)]
		public ChartPattern Pattern { get; set; }

		[Display(ResourceType = typeof(Custom.Resource), Name = "SendAlerts", Description = "SendAlertsDescription", GroupName = "NinjaScriptGeneral", Order = 2)]
		public bool ShowAlerts { get; set; }

		[Display(ResourceType = typeof(Custom.Resource), Name = "ShowPatternCount", Description = "ShowPatternCountDescription", GroupName = "NinjaScriptGeneral", Order = 3)]
		public bool ShowPatternCount { get; set; }

		[Display(ResourceType = typeof(Custom.Resource), Name = "TextFont", Description = "TextFontDescription", GroupName = "NinjaScriptGeneral", Order = 4)]
		public Gui.Tools.SimpleFont TextFont { get; set; }

		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(ResourceType = typeof(Custom.Resource), Name = "TrendStrength", Description = "TrendStrengthDescription",
		GroupName = "NinjaScriptGeneral", Order = 5)]
		public int TrendStrength { get; set; }
		#endregion
	}

	public class CandleStickPatternLogic
	{
		private				bool				isInDownTrend;
		private				bool				isInUpTrend;
		private				MAX					max;
		private				MIN					min;
		private readonly	NinjaScriptBase		ninjaScript;
		private readonly	bool[]				prior = new bool[2];		// Check if there was any pattern in the last 2 bars. Ignore a match in case.
		private				Swing				swing;
		private readonly	int					trendStrength;

		public CandleStickPatternLogic(NinjaScriptBase ninjaScript, int trendStrength)
		{
			this.ninjaScript	= ninjaScript;
			this.trendStrength	= trendStrength;
		}

		public bool Evaluate(ChartPattern pattern)
		{
			if (ninjaScript.CurrentBar < trendStrength || ninjaScript.CurrentBar < 2)
				return false;

			if (max == null && trendStrength > 0 && (pattern == ChartPattern.HangingMan || pattern == ChartPattern.InvertedHammer))
			{
				max = new MAX { Period = trendStrength };
				try 
				{
					max.SetState(State.Configure);
				}
				catch (Exception exp)
				{
					Cbi.Log.Process(typeof(Resource), "CbiUnableToCreateInstance2", new object[] { max.Name, exp.InnerException != null ? exp.InnerException.ToString() : exp.ToString() }, Cbi.LogLevel.Error, Cbi.LogCategories.Default);
					max.SetState(State.Finalized);
				}

				max.Parent = ninjaScript;
				max.SetInput(ninjaScript.High);

				lock (ninjaScript.NinjaScripts)
					ninjaScript.NinjaScripts.Add(max);

				try
				{
					max.SetState(ninjaScript.State);
				}
				catch (Exception exp)
				{
					Cbi.Log.Process(typeof(Resource), "CbiUnableToCreateInstance2", new object[] { max.Name, exp.InnerException != null ? exp.InnerException.ToString() : exp.ToString() }, Cbi.LogLevel.Error, Cbi.LogCategories.Default);
					max.SetState(State.Finalized);
					return false;
				}
			}

			if (min == null && trendStrength > 0 && pattern == ChartPattern.Hammer)
			{
				min = new MIN { Period = trendStrength };
				try 
				{
					min.SetState(State.Configure);
				}
				catch (Exception exp)
				{
					Cbi.Log.Process(typeof(Resource), "CbiUnableToCreateInstance2", new object[] { min.Name, exp.InnerException != null ? exp.InnerException.ToString() : exp.ToString() }, Cbi.LogLevel.Error, Cbi.LogCategories.Default);
					min.SetState(State.Finalized);
				}

				min.Parent = ninjaScript;
				min.SetInput(ninjaScript.Low);

				lock (ninjaScript.NinjaScripts)
					ninjaScript.NinjaScripts.Add(min);

				try
				{
					min.SetState(ninjaScript.State);
				}
				catch (Exception exp)
				{
					Cbi.Log.Process(typeof(Resource), "CbiUnableToCreateInstance2", new object[] { min.Name, exp.InnerException != null ? exp.InnerException.ToString() : exp.ToString() }, Cbi.LogLevel.Error, Cbi.LogCategories.Default);
					min.SetState(State.Finalized);
					return false;
				}
			}

			if (pattern != ChartPattern.Doji
					&& pattern != ChartPattern.DownsideTasukiGap
					&& pattern != ChartPattern.EveningStar
					&& pattern != ChartPattern.FallingThreeMethods
					&& pattern != ChartPattern.MorningStar
					&& pattern != ChartPattern.RisingThreeMethods
					&& pattern != ChartPattern.StickSandwich
					&& pattern != ChartPattern.UpsideTasukiGap)
			{
				if (trendStrength == 0)
				{
					isInDownTrend = true;
					isInUpTrend = true;
				}
				else
				{
					if (swing == null)
					{
						swing = new Swing { Strength = trendStrength };
						try
						{
							swing.SetState(State.Configure);
						}
						catch (Exception exp)
						{
							Cbi.Log.Process(typeof(Resource), "CbiUnableToCreateInstance2", new object[] { swing.Name, exp.InnerException != null ? exp.InnerException.ToString() : exp.ToString() }, Cbi.LogLevel.Error, Cbi.LogCategories.Default);
							swing.SetState(State.Finalized);
						}

						swing.Parent = ninjaScript;
						swing.SetInput(ninjaScript.Input);

						lock (ninjaScript.NinjaScripts)
							ninjaScript.NinjaScripts.Add(swing);

						try
						{
							swing.SetState(ninjaScript.State);
						}
						catch (Exception exp)
						{
							Cbi.Log.Process(typeof(Resource), "CbiUnableToCreateInstance2", new object[] { swing.Name, exp.InnerException != null ? exp.InnerException.ToString() : exp.ToString() }, Cbi.LogLevel.Error, Cbi.LogCategories.Default);
							swing.SetState(State.Finalized);
							return false;
						}
					}

					// Calculate up trend line
					int upTrendStartBarsAgo = 0;
					int upTrendEndBarsAgo = 0;
					int upTrendOccurence = 1;

					while (ninjaScript.Low[upTrendEndBarsAgo] <= ninjaScript.Low[upTrendStartBarsAgo])
					{
						upTrendStartBarsAgo = swing.SwingLowBar(0, upTrendOccurence + 1, ninjaScript.CurrentBar);
						upTrendEndBarsAgo = swing.SwingLowBar(0, upTrendOccurence, ninjaScript.CurrentBar);

						if (upTrendStartBarsAgo < 0 || upTrendEndBarsAgo < 0)
							break;

						upTrendOccurence++;
					}

					// Calculate down trend line
					int downTrendStartBarsAgo = 0;
					int downTrendEndBarsAgo = 0;
					int downTrendOccurence = 1;

					while (ninjaScript.High[downTrendEndBarsAgo] >= ninjaScript.High[downTrendStartBarsAgo])
					{

						downTrendStartBarsAgo = swing.SwingHighBar(0, downTrendOccurence + 1, ninjaScript.CurrentBar);
						downTrendEndBarsAgo = swing.SwingHighBar(0, downTrendOccurence, ninjaScript.CurrentBar);

						if (downTrendStartBarsAgo < 0 || downTrendEndBarsAgo < 0)
							break;

						downTrendOccurence++;
					}

					if (upTrendStartBarsAgo > 0 && upTrendEndBarsAgo > 0 && upTrendStartBarsAgo < downTrendStartBarsAgo)
					{
						isInDownTrend = false;
						isInUpTrend = true;
					}
					else if (downTrendStartBarsAgo > 0 && downTrendEndBarsAgo > 0 && upTrendStartBarsAgo > downTrendStartBarsAgo)
					{
						isInDownTrend = true;
						isInUpTrend = false;
					}
					else
					{
						isInDownTrend = false;
						isInUpTrend = false;
					}
				}
			}

			bool found = false;
			NinjaScriptBase n		= ninjaScript;
			if (!prior[0] && !prior[1])				// no pattern found on the last 2 bars
				switch (pattern)
				{
					case ChartPattern.BearishBeltHold:		found = isInUpTrend && n.Close[1] > n.Open[1] && n.Open[0] > n.Close[1] + 5 * n.TickSize && n.Open[0] == n.High[0] && n.Close[0] < n.Open[0]; break;
					case ChartPattern.BearishEngulfing:		found = isInUpTrend && n.Close[1] > n.Open[1] && n.Close[0] < n.Open[0] && n.Open[0] > n.Close[1] && n.Close[0] < n.Open[1]; break;
					case ChartPattern.BearishHarami:		found = isInUpTrend && n.Close[0] < n.Open[0] && n.Close[1] > n.Open[1] && n.Low[0] >= n.Open[1] && n.High[0] <= n.Close[1]; break;
					case ChartPattern.BearishHaramiCross:	found = isInUpTrend && n.High[0] <= n.Close[1] && n.Low[0] >= n.Open[1] && n.Open[0] <= n.Close[1] && n.Close[0] >= n.Open[1]
																	&& ((n.Close[0] >= n.Open[0] && n.Close[0] <= n.Open[0] + n.TickSize) || (n.Close[0] <= n.Open[0] && n.Close[0] >= n.Open[0] - n.TickSize)); break;
					case ChartPattern.BullishBeltHold:		found = isInDownTrend && n.Close[1] < n.Open[1] && n.Open[0] < n.Close[1] - 5 * n.TickSize && n.Open[0] == n.Low[0] && n.Close[0] > n.Open[0]; break;
					case ChartPattern.BullishEngulfing:		found = isInDownTrend && n.Close[1] < n.Open[1] && n.Close[0] > n.Open[0] && n.Close[0] > n.Open[1] && n.Open[0] < n.Close[1]; break;
					case ChartPattern.BullishHarami:		found = isInDownTrend && n.Close[0] > n.Open[0] && n.Close[1] < n.Open[1] && n.Low[0] >= n.Close[1] && n.High[0] <= n.Open[1]; break;
					case ChartPattern.BullishHaramiCross:	found = isInDownTrend && n.High[0] <= n.Open[1] && n.Low[0] >= n.Close[1] && n.Open[0] >= n.Close[1] && n.Close[0] <= n.Open[1]
																	&& ((n.Close[0] >= n.Open[0] && n.Close[0] <= n.Open[0] + n.TickSize) || (n.Close[0] <= n.Open[0] && n.Close[0] >= n.Open[0] - n.TickSize)); break;
					case ChartPattern.DarkCloudCover:		found = isInUpTrend && n.Open[0] > n.High[1] && n.Close[1] > n.Open[1] && n.Close[0] < n.Open[0] && n.Close[0] <= n.Close[1] - (n.Close[1] - n.Open[1]) / 2 && n.Close[0] >= n.Open[1]; break;
					case ChartPattern.Doji:					found = Math.Abs(n.Close[0] - n.Open[0]) <= (n.High[0] - n.Low[0]) * 0.07; break;
					case ChartPattern.DownsideTasukiGap:	found = n.Close[2] < n.Open[2] && n.Close[1] < n.Open[1] && n.Close[0] > n.Open[0] && n.High[1] < n.Low[2]
																	&& n.Open[0] > n.Close[1] && n.Open[0] < n.Open[1] && n.Close[0] > n.Open[1] && n.Close[0] < n.Close[2]; break;
					case ChartPattern.EveningStar:			found = n.Close[2] > n.Open[2] && n.Close[1] > n.Close[2] && n.Open[0] < Math.Abs((n.Close[1] - n.Open[1]) / 2) + n.Open[1] && n.Close[0] < n.Open[0]; break;
					case ChartPattern.FallingThreeMethods:	found = n.CurrentBar > 5 && n.Close[4] < n.Open[4] && n.Close[0] < n.Open[0] && n.Close[0] < n.Low[4] && n.High[3] < n.High[4] && n.Low[3] > n.Low[4]
																	&& n.High[2] < n.High[4] && n.Low[2] > n.Low[4] && n.High[1] < n.High[4] && n.Low[1] > n.Low[4]; break;
					case ChartPattern.Hammer:				found = isInDownTrend && (min == null || min[0] == n.Low[0]) && n.Low[0] < n.Open[0] - 5 * n.TickSize 
																	&& Math.Abs(n.Open[0] - n.Close[0]) < 0.10 * (n.High[0] - n.Low[0]) && n.High[0] - n.Close[0] < 0.25 * (n.High[0] - n.Low[0]); break;
					case ChartPattern.HangingMan:			found = isInUpTrend && (max == null || max[0] == n.High[0]) && n.Low[0] < n.Open[0] - 5 * n.TickSize 
																	&& Math.Abs(n.Open[0] - n.Close[0]) < 0.10 * (n.High[0] - n.Low[0]) && n.High[0] - n.Close[0] < 0.25 * (n.High[0] - n.Low[0]); break;
					case ChartPattern.InvertedHammer:		found = isInUpTrend && (max == null || max[0] == n.High[0]) && n.High[0] > n.Open[0] + 5 * n.TickSize 
																	&& Math.Abs(n.Open[0] - n.Close[0]) < 0.10 * (n.High[0] - n.Low[0]) && n.Close[0] - n.Low[0] < 0.25 * (n.High[0] - n.Low[0]); break;
					case ChartPattern.MorningStar:			found = n.Close[2] < n.Open[2] && n.Close[1] < n.Close[2] && n.Open[0] > Math.Abs((n.Close[1] - n.Open[1]) / 2) + n.Open[1] && n.Close[0] > n.Open[0]; break;
					case ChartPattern.PiercingLine:			found = isInDownTrend && n.Open[0] < n.Low[1] && n.Close[1] < n.Open[1] && n.Close[0] > n.Open[0] && n.Close[0] >= n.Close[1] + (n.Open[1] - n.Close[1]) / 2 && n.Close[0] <= n.Open[1]; break;
					case ChartPattern.RisingThreeMethods:	found = n.CurrentBar > 5 && n.Close[4] > n.Open[4] && n.Close[0] > n.Open[0] && n.Close[0] > n.High[4] && n.High[3] < n.High[4] && n.Low[3] > n.Low[4]
																	&& n.High[2] < n.High[4] && n.Low[2] > n.Low[4] && n.High[1] < n.High[4] && n.Low[1] > n.Low[4]; break;
					case ChartPattern.ShootingStar:			found = isInUpTrend && n.High[0] > n.Open[0] && n.High[0] - n.Open[0] >= 2 * (n.Open[0] - n.Close[0]) && n.Close[0] < n.Open[0] && n.Close[0] - n.Low[0] <= 2 * n.TickSize; break;
					case ChartPattern.StickSandwich:		found = n.Close[2] == n.Close[0] && n.Close[2] < n.Open[2] && n.Close[1] > n.Open[1] && n.Close[0] < n.Open[0]; break;
					case ChartPattern.ThreeBlackCrows:		found = isInUpTrend && n.Close[0] < n.Open[0] && n.Close[1] < n.Open[1] && n.Close[2] < n.Open[2] && n.Close[0] < n.Close[1] && n.Close[1] < n.Close[2]
																	&& n.Open[0] < n.Open[1] && n.Open[0] > n.Close[1] && n.Open[1] < n.Open[2] && n.Open[1] > n.Close[2]; break;
					case ChartPattern.ThreeWhiteSoldiers:	found = isInDownTrend && n.Close[0] > n.Open[0] && n.Close[1] > n.Open[1] && n.Close[2] > n.Open[2] && n.Close[0] > n.Close[1] && n.Close[1] > n.Close[2]
																	&& n.Open[0] < n.Close[1] && n.Open[0] > n.Open[1] && n.Open[1] < n.Close[2] && n.Open[1] > n.Open[2]; break;
					case ChartPattern.UpsideGapTwoCrows:	found = isInUpTrend && n.Close[2] > n.Open[2] && n.Close[1] < n.Open[1] && n.Close[0] < n.Open[0] && n.Low[1] > n.High[2]
																	&& n.Close[0] > n.High[2] && n.Close[0] < n.Close[1] && n.Open[0] > n.Open[1]; break;
					case ChartPattern.UpsideTasukiGap:		found = n.Close[2] > n.Open[2] && n.Close[1] > n.Open[1] && n.Close[0] < n. Open[0] && n.Low[1] > n.High[2]
																	&& n.Open[0] < n.Close[1] && n.Open[0] > n.Open[1] && n.Close[0] < n.Open[1] && n.Close[0] > n.Close[2]; break;
				}
			prior[n.CurrentBars[0] % 2] = found;

			return found;
		}
	}
}

public enum ChartPattern
{
	BearishBeltHold			= 0,
	BearishEngulfing		= 1,
	BearishHarami			= 2,
	BearishHaramiCross		= 3,
	BullishBeltHold			= 4,
	BullishEngulfing		= 5,
	BullishHarami			= 6,
	BullishHaramiCross		= 7,
	DarkCloudCover			= 8,
	Doji					= 9,
	DownsideTasukiGap		= 10,
	EveningStar				= 11,
	FallingThreeMethods		= 12,
	Hammer					= 13,
	HangingMan				= 14,
	InvertedHammer			= 15,
	MorningStar				= 16,
	PiercingLine			= 17,
	RisingThreeMethods		= 18,
	ShootingStar			= 19,
	StickSandwich			= 20,
	ThreeBlackCrows			= 21,
	ThreeWhiteSoldiers		= 22,
	UpsideGapTwoCrows		= 23,
	UpsideTasukiGap			= 24,
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private CandlestickPattern[] cacheCandlestickPattern;
		public CandlestickPattern CandlestickPattern(ChartPattern pattern, int trendStrength)
		{
			return CandlestickPattern(Input, pattern, trendStrength);
		}

		public CandlestickPattern CandlestickPattern(ISeries<double> input, ChartPattern pattern, int trendStrength)
		{
			if (cacheCandlestickPattern != null)
				for (int idx = 0; idx < cacheCandlestickPattern.Length; idx++)
					if (cacheCandlestickPattern[idx] != null && cacheCandlestickPattern[idx].Pattern == pattern && cacheCandlestickPattern[idx].TrendStrength == trendStrength && cacheCandlestickPattern[idx].EqualsInput(input))
						return cacheCandlestickPattern[idx];
			return CacheIndicator<CandlestickPattern>(new CandlestickPattern(){ Pattern = pattern, TrendStrength = trendStrength }, input, ref cacheCandlestickPattern);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.CandlestickPattern CandlestickPattern(ChartPattern pattern, int trendStrength)
		{
			return indicator.CandlestickPattern(Input, pattern, trendStrength);
		}

		public Indicators.CandlestickPattern CandlestickPattern(ISeries<double> input , ChartPattern pattern, int trendStrength)
		{
			return indicator.CandlestickPattern(input, pattern, trendStrength);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.CandlestickPattern CandlestickPattern(ChartPattern pattern, int trendStrength)
		{
			return indicator.CandlestickPattern(Input, pattern, trendStrength);
		}

		public Indicators.CandlestickPattern CandlestickPattern(ISeries<double> input , ChartPattern pattern, int trendStrength)
		{
			return indicator.CandlestickPattern(input, pattern, trendStrength);
		}
	}
}

#endregion
