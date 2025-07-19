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

//This namespace holds Indicators in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators
{	
	public class dpQGP : Indicator
	{
		public enum Marker_Types
		{
			Dot,
			Triangle,
			Arrow
		}

		public enum Modes
		{
			Off,
			Single,
			Double
		}

		public enum MATypes
		{
			Off,
			SMA,
			WMA,
			EMA,
			ZLEMA,
			HMA
		}

		const string arialUpTriangle = @"▲", arialDownTriangle = @"▼", arialDot = @"●", wingdingsUpArrow = "é", wingdingsDownArrow = "ê";
		SimpleFont markerFontA;
		string upMarkerA = "", downMarkerA = "";
		SimpleFont markerFontB;
		string upMarkerB = "", downMarkerB = "";
		SimpleFont markerFontContinuation;
		string upMarkerContinuation = "", downMarkerContinuation = "";

		Brush bgbrushUpA, bgbrushDownA;
		Brush bgbrushUpB, bgbrushDownB;
		Brush tr1BrushUp, tr1BrushDown;

		SimpleFont plmarkerFont;
		string plupMarker = "", pldownMarker = "";

		// QG
		private Series<double> a1;
		private Series<double> b1;
		private Series<double> avg1, avg1PreSmooth;
		private Series<double> a2;
		private Series<double> b2;
		private Series<double> avg2, avg2PreSmooth;

		private Series<double> a3;
		private Series<double> b3;
		private Series<double> avg3;
		private Series<double> a4;
		private Series<double> b4;
		private Series<double> avg4;

		Series<bool> plProximityAlertUp, plProximityAlertDn;

		// INTERNAL VARIABLES
		private Style On,Off,Long,Short;
		private Chart				 			 	ChartWindow;
		private System.Windows.Controls.Button		modeButton  = null;

		private bool								IsToolBarButtonAdded;

		int trend1Dir = 0;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"By TradingCoders.com - plots two custom mov avgs and a nice fill, with crossing markers";
				Name										= "dpQGP";
				Calculate									= Calculate.OnPriceChange;
				IsOverlay									= true;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= true;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				//Disable this property if your indicator requires custom values that cumulate with each new market data event.
				//See Help Guide for additional information.
				IsSuspendedWhileInactive					= true;
				ShowTransparentPlotsInDataBox			= true;


				Trend1					= 12;
				Trend2					= 42;
				TrendSmoothingType12					= MATypes.SMA;
				TrendSmoothingPeriod12					= 1;
				Cloud_FastAboveSlowA					= Brushes.LimeGreen;
				Cloud_FastBelowSlowA					= Brushes.Red;
				Cloud_OpacityA					= 25;
				MarkerTypeA					= Marker_Types.Triangle;
				MarkerSizeA					= 21;
				MarkerOffsetA					= 5;
				UpColorA = Brushes.Green;
				DownColorA = Brushes.Crimson;
				Trend1_UpColor = Brushes.Lime;
				Trend1_DownColor = Brushes.Magenta;
				Alerts_Crossing12 = false;
				Alerts_Crossing12_AlertSoundBull = "Alert3.wav";
				Alerts_Crossing12_AlertSoundBear = "Alert4.wav";

				Trend3					= 150;
				Trend4					= 200;
				Cloud_FastAboveSlowB					= Brushes.Green;
				Cloud_FastBelowSlowB				= Brushes.Crimson;
				Cloud_OpacityB					= 25;
				MarkerTypeB					= Marker_Types.Triangle;
				MarkerSizeB					= 40;
				MarkerOffsetB					= 5;
				UpColorB = Brushes.RoyalBlue;
				DownColorB = Brushes.DarkMagenta;
				Alerts_Crossing34 = false;
				Alerts_Crossing34_AlertSoundBull = "Alert3.wav";
				Alerts_Crossing34_AlertSoundBear = "Alert4.wav";

				CurrentMode = Modes.Off;
				FilterViaSlowPair = true;

				//ContinuationSignals_Enabled				= true;
				ContinuationMaxBars						= 7;
				MarkerTypeContinuation					= Marker_Types.Dot;
				MarkerSizeContinuation					= 28;
				MarkerOffsetContinuation				= 5;
				UpColorContinuation 					= Brushes.DodgerBlue;
				DownColorContinuation 					= Brushes.OrangeRed;

				// from dpDPO, the Sentinal alerts retasked as proximity alerts
				ProximityAlert1Bar_Enabled		= true;
				ProximityAlert2Bar_Enabled		= true;
				// MarkerType				= Marker_Types.Triangle;
				// MarkerSize				= 10;
				// MarkerOffset			= 10;
				ProximityAlert_ProximityLimit		= 0.25;
				ProximityAlert_ProximityReset		= 1.5;
				ProximityLimit_MarkerType				= Marker_Types.Dot;
				ProximityLimit_MarkerSize				= 10;
				ProximityLimit_MarkerOffset				= 4;
				ProximityLimit_UpColor = Brushes.Lime;
				ProximityLimit_DownColor = Brushes.DeepPink;
				ProximityLimit_ATR_Period = 10;
				ProximityLimit_AlertSoundBull = "Alert1.wav";
				ProximityLimit_AlertSoundBear = "Alert2.wav";


				AddPlot(new Stroke(Brushes.Yellow,2), PlotStyle.Line, "QG1");
				AddPlot(new Stroke(Brushes.Yellow,2), PlotStyle.Line,"QG2");
				AddPlot(Brushes.Transparent, "FastTrendSignals");
				AddPlot(new Stroke(Brushes.DarkOrange,4), PlotStyle.Line, "QG3");
				AddPlot(new Stroke(Brushes.DarkOrange,4), PlotStyle.Line,"QG4");
				AddPlot(Brushes.Transparent, "ContinuationSignals");
				AddPlot(Brushes.Transparent, "SlowTrendSignals");

				AddPlot(Brushes.Transparent, "FastTrend"); //[7]
				AddPlot(Brushes.Transparent, "SlowTrend");

				for (int p = 0; p < Plots.Length; p++)
					Plots[p].IsOpacityVisible = true;
			}
			else if (State == State.DataLoaded)
			{
				markerFontA = new SimpleFont(MarkerTypeA==Marker_Types.Arrow?"Wingdings":"Arial",MarkerSizeA){Bold=true};
				switch (MarkerTypeA)
				{
					case Marker_Types.Triangle:
						upMarkerA = arialUpTriangle;
						downMarkerA = arialDownTriangle;
						break;
					case Marker_Types.Arrow:
						upMarkerA = wingdingsUpArrow;
						downMarkerA = wingdingsDownArrow;
						break;
					default:
						upMarkerA = arialDot;
						downMarkerA = arialDot;
						break;
				}

				markerFontB = new SimpleFont(MarkerTypeB==Marker_Types.Arrow?"Wingdings":"Arial",MarkerSizeB){Bold=true};
				switch (MarkerTypeB)
				{
					case Marker_Types.Triangle:
						upMarkerB = arialUpTriangle;
						downMarkerB = arialDownTriangle;
						break;
					case Marker_Types.Arrow:
						upMarkerB = wingdingsUpArrow;
						downMarkerB = wingdingsDownArrow;
						break;
					default:
						upMarkerB = arialDot;
						downMarkerB = arialDot;
						break;
				}

				markerFontContinuation = new SimpleFont(MarkerTypeContinuation==Marker_Types.Arrow?"Wingdings":"Arial",MarkerSizeContinuation){Bold=true};
				switch (MarkerTypeContinuation)
				{
					case Marker_Types.Triangle:
						upMarkerContinuation = arialUpTriangle;
						downMarkerContinuation = arialDownTriangle;
						break;
					case Marker_Types.Arrow:
						upMarkerContinuation = wingdingsUpArrow;
						downMarkerContinuation = wingdingsDownArrow;
						break;
					default:
						upMarkerContinuation = arialDot;
						downMarkerContinuation = arialDot;
						break;
				}

				plmarkerFont = new SimpleFont(ProximityLimit_MarkerType==Marker_Types.Arrow?"Wingdings":"Arial",ProximityLimit_MarkerSize){Bold=true};
				switch (ProximityLimit_MarkerType)
				{
					case Marker_Types.Triangle:
						plupMarker = arialUpTriangle;
						pldownMarker = arialDownTriangle;
						break;
					case Marker_Types.Arrow:
						plupMarker = wingdingsUpArrow;
						pldownMarker = wingdingsDownArrow;
						break;
					default:
						plupMarker = arialDot;
						pldownMarker = arialDot;
						break;
				}

				a1 = new Series<double>(this);
				b1 = new Series<double>(this);
				avg1 = new Series<double>(this);
				avg1PreSmooth = new Series<double>(this);
				a2 = new Series<double>(this);
				b2 = new Series<double>(this);
				avg2 = new Series<double>(this);
				avg2PreSmooth = new Series<double>(this);

				a3 = new Series<double>(this);
				b3 = new Series<double>(this);
				avg3 = new Series<double>(this);
				a4 = new Series<double>(this);
				b4 = new Series<double>(this);
				avg4 = new Series<double>(this);

				plProximityAlertUp = new Series<bool>(this,MaximumBarsLookBack.Infinite);
				plProximityAlertDn = new Series<bool>(this,MaximumBarsLookBack.Infinite);
			}
			else if (State == State.Historical)
			{
				SetZOrder(-2);

				//Call Our AddToolBarButton Method - Latching Bool used to Assure this is Executed Only Once!
				//(Done here so it is executed on UI Thread)
				if (ChartControl != null && !IsToolBarButtonAdded)
				{
					ChartControl.Dispatcher.InvokeAsync((Action)(() =>
					{
						IsToolBarButtonAdded = true;
						AddToolBarButton();
					}));
				}
			}
			else if (State == State.Terminated)
			{
				//Call Our DisposeMyStuff Method - and Dispose of Anything Needing Disposed of on Termination
				if (ChartControl != null)
				{
					ChartControl.Dispatcher.InvokeAsync((Action)(() =>
					{
						DisposeMyStuff();
					}));
				}
			}
		}


		#region Add ToolBar Button

			//Add Tool Bar Button Method - Called from State.Historical
			private void AddToolBarButton()
			{
				//Dispatcher used to Assure Executed on UI Thread
				//this.Dispatcher.Invoke((Action)(() =>
				//{
						//Indicator's Chart
						ChartWindow = Window.GetWindow(this.ChartControl.Parent) as Chart;
				        if (ChartWindow == null) { Print("ChartWindow is null"); return; }
						//Set Event Handle for Tab Change
						else ChartWindow.MainTabControl.SelectionChanged += TabChangedHandler;

						Short = new Style();
						Short.TargetType = typeof(System.Windows.Controls.Button);
						Short.Setters.Add(new Setter(System.Windows.Controls.Button.FontSizeProperty, 11.0));
						Short.Setters.Add(new Setter(System.Windows.Controls.Button.BackgroundProperty, Brushes.OrangeRed));
						Short.Setters.Add(new Setter(System.Windows.Controls.Button.ForegroundProperty, Brushes.Black));
						Short.Setters.Add(new Setter(System.Windows.Controls.Button.FontFamilyProperty, new FontFamily("Arial")));
						Short.Setters.Add(new Setter(System.Windows.Controls.Button.FontWeightProperty, FontWeights.Bold));

						Long = new Style();
						Long.TargetType = typeof(System.Windows.Controls.Button);
						Long.Setters.Add(new Setter(System.Windows.Controls.Button.FontSizeProperty, 11.0));
						Long.Setters.Add(new Setter(System.Windows.Controls.Button.BackgroundProperty, Brushes.LimeGreen));
						Long.Setters.Add(new Setter(System.Windows.Controls.Button.ForegroundProperty, Brushes.Black));
						Long.Setters.Add(new Setter(System.Windows.Controls.Button.FontFamilyProperty, new FontFamily("Arial")));
						Long.Setters.Add(new Setter(System.Windows.Controls.Button.FontWeightProperty, FontWeights.Bold));

						Off = new Style();
						Off.TargetType = typeof(System.Windows.Controls.Button);
						Off.Setters.Add(new Setter(System.Windows.Controls.Button.FontSizeProperty, 11.0));
						Off.Setters.Add(new Setter(System.Windows.Controls.Button.BackgroundProperty, Brushes.Silver));
						Off.Setters.Add(new Setter(System.Windows.Controls.Button.ForegroundProperty, Brushes.DimGray));
						Off.Setters.Add(new Setter(System.Windows.Controls.Button.FontFamilyProperty, new FontFamily("Arial")));
						Off.Setters.Add(new Setter(System.Windows.Controls.Button.FontWeightProperty, FontWeights.Bold));

						On = new Style();
						On.TargetType = typeof(System.Windows.Controls.Button);
						On.Setters.Add(new Setter(System.Windows.Controls.Button.FontSizeProperty, 11.0));
						On.Setters.Add(new Setter(System.Windows.Controls.Button.BackgroundProperty, Brushes.WhiteSmoke));
						On.Setters.Add(new Setter(System.Windows.Controls.Button.ForegroundProperty, Brushes.Blue));
						On.Setters.Add(new Setter(System.Windows.Controls.Button.FontFamilyProperty, new FontFamily("Arial")));
						On.Setters.Add(new Setter(System.Windows.Controls.Button.FontWeightProperty, FontWeights.Bold));


						//Long Button
			        	modeButton = new System.Windows.Controls.Button();
						//Set Button Style - Predefined above
						modeButton.Style = On;
						//Set Button Content
						modeButton.Content = "QGP:"+CurrentMode.ToString();
						modeButton.IsEnabled = true;
						//Set Button Click Routed Event Handler
						modeButton.Click += new RoutedEventHandler(modeButton_Click);
						//Set Button Alignment
						modeButton.HorizontalAlignment = HorizontalAlignment.Left;


						// Add Button to Indicator's Chart ToolBar
						ChartWindow.MainMenu.Add(modeButton);
						Print("added modeButton to ChartWindow.MainMenu	");

						//Prevent Button From Displaying On Charts ToolBar when WorkSpace Opens if Not Active Tab
						modeButton.Visibility = Visibility.Collapsed;
						foreach (System.Windows.Controls.TabItem tab in this.ChartWindow.MainTabControl.Items)
							if ((tab.Content as ChartTab).ChartControl == this.ChartControl && tab == this.ChartWindow.MainTabControl.SelectedItem)
							{
								modeButton.Visibility = Visibility.Visible;
							}
						//Latching Bool
						IsToolBarButtonAdded = true;
				//}));
			}

		#endregion

		#region Tab Changed Event Handler

		//Tab Changed Event Handler
		private void TabChangedHandler(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
		{
			//Null Check of Sorts
			if (e.AddedItems.Count <= 0) return;
			//Declare Temp Chart Tab from Event Args
			ChartTab tmpCT = (e.AddedItems[0] as System.Windows.Controls.TabItem).Content as ChartTab;
			//Temp ChartTab Null Check
			if (tmpCT != null)
			{
				// Set Button Visiblity Based on Selected Tab
				if(modeButton != null)	modeButton.Visibility = tmpCT.ChartControl == this.ChartControl ? Visibility.Visible : Visibility.Collapsed;
			}
		}

		#endregion

		#region Dispose MyStuff on Termination

			//Dispose on Termination Method - Called from State.Terminate
			private void DisposeMyStuff()
			{
				//ChartWindow Null Check
	            if (ChartWindow != null)
				{
					//Dispatcher used to Assure Executed on UI Thread
					//this.Dispatcher.Invoke((Action)(() =>
					//{
						//Button Null Check
						if(modeButton != null)
						{
							//Remove Button from Indicator's Chart ToolBar
							ChartWindow.MainMenu.Remove(modeButton);
							//Remove Button Event Handler
							modeButton.Click -= new RoutedEventHandler(modeButton_Click);
							//Set Button to Null - Not Needed - Done out of Habit
							modeButton = null;
						}

						//Remove Tab Changed Event Handler
						ChartWindow.MainTabControl.SelectionChanged -= TabChangedHandler;
						//Set ChartWindow to Null - Not Needed - Again Just Done out of Habit
						ChartWindow = null;
					//}));
				}
			}

		#endregion

		#region button click events


		//Button Click Routed Event Handler
		private void modeButton_Click(object sender, RoutedEventArgs e)
		{
			// code here
			if (CurrentMode == Modes.Off)
				CurrentMode = Modes.Single;
			else if (CurrentMode == Modes.Single)
				CurrentMode = Modes.Double;
			else if (CurrentMode == Modes.Double)
				CurrentMode = Modes.Off;

			Print("ModeButton Click to "+CurrentMode.ToString());

			UpdateButtons();

			TriggerCustomEvent (
			delegate (object state) {
				try
				{
					// code here
					RepaintIndicator();
				}
				catch { }
				},0,
				null
				);
		}

		private void UpdateButtons()
		{
			ChartControl.Dispatcher.InvokeAsync((Action)(() =>
			{
				modeButton.Content = "QGP:"+CurrentMode.ToString();
				modeButton.InvalidateVisual();
			}));
		}


		#endregion

		protected override void OnBarUpdate()
		{

			a1[0] = Input[0];
			b1[0] = Input[0];
			a2[0] = Input[0];
			b2[0] = Input[0];

			a3[0] = Input[0];
			b3[0] = Input[0];
			a4[0] = Input[0];
			b4[0] = Input[0];

			if (CurrentBar == 0)
				return;

			//

			if(Input[0] > a1[1])
			{
				a1[0]=Input[0] ;
			}
			else
			{
				a1[0] = a1[1] - (a1[1]-b1[1]) /Trend1;
			}
			if(Input[0] < b1[1])
			{
				b1[0]=Input[0];
			}
			else
			{
				b1[0] =b1[1] + (a1[1]-b1[1]) /Trend1;
			}

			avg1PreSmooth[0] = (a1[0]+b1[0])/2;

			//

			if(Input[0] > a2[1])
			{
				a2[0]=Input[0] ;
			}
			else
			{
				a2[0] = a2[1] - (a2[1]-b2[1]) /Trend2;
			}
			if(Input[0] < b2[1])
			{
				b2[0]=Input[0];
			}
			else
			{
				b2[0] =b2[1] + (a2[1]-b2[1]) /Trend2;
			}

			avg2PreSmooth[0] = (a2[0]+b2[0])/2;

			// smoothing
			if (TrendSmoothingType12==MATypes.Off || TrendSmoothingPeriod12 <= 1)
			{
				avg1[0] = avg1PreSmooth[0];
				avg2[0] = avg2PreSmooth[0];
			}
			else
			{
				if (TrendSmoothingType12 == MATypes.SMA)
				{
					avg1[0] = SMA(avg1PreSmooth,TrendSmoothingPeriod12)[0];
					avg2[0] = SMA(avg2PreSmooth,TrendSmoothingPeriod12)[0];
				}
				else
				if (TrendSmoothingType12 == MATypes.WMA)
				{
					avg1[0] = WMA(avg1PreSmooth,TrendSmoothingPeriod12)[0];
					avg2[0] = WMA(avg2PreSmooth,TrendSmoothingPeriod12)[0];
				}
				else
				if (TrendSmoothingType12 == MATypes.ZLEMA)
				{
					avg1[0] = ZLEMA(avg1PreSmooth,TrendSmoothingPeriod12)[0];
					avg2[0] = ZLEMA(avg2PreSmooth,TrendSmoothingPeriod12)[0];
				}
				else
				if (TrendSmoothingType12 == MATypes.EMA)
				{
					avg1[0] = EMA(avg1PreSmooth,TrendSmoothingPeriod12)[0];
					avg2[0] = EMA(avg2PreSmooth,TrendSmoothingPeriod12)[0];
				}
				else
				if (TrendSmoothingType12 == MATypes.HMA)
				{
					avg1[0] = HMA(avg1PreSmooth,TrendSmoothingPeriod12)[0];
					avg2[0] = HMA(avg2PreSmooth,TrendSmoothingPeriod12)[0];
				}
			}

			//

			if (tr1BrushUp == null)
			{
				tr1BrushUp = Trend1_UpColor.Clone();
				tr1BrushUp.Opacity = Plots[0].Opacity * 0.01;
				tr1BrushUp.Freeze();

				tr1BrushDown = Trend1_DownColor.Clone();
				tr1BrushDown.Opacity = Plots[0].Opacity * 0.01;
				tr1BrushDown.Freeze();
			}

			QG1[0] = avg1[0];
			QG2[0] = avg2[0];
			int az = State==State.Realtime && Calculate!=Calculate.OnBarClose ? 1 : 0;
			if (QG1[az] > QG1[az+1])
				trend1Dir = +1;
			else if (QG1[az] < QG1[az+1])
				trend1Dir = -1;
				
			if (!Trend1_UpColor.IsTransparent() && trend1Dir > 0)
				PlotBrushes[0][0] = tr1BrushUp;
			else if (!Trend1_DownColor.IsTransparent() && trend1Dir < 0)
				PlotBrushes[0][0] = tr1BrushDown;
			 
			// ----------------------------------

			if(Input[0] > a3[1])
			{
				a3[0]=Input[0] ;
			}
			else
			{
				a3[0] = a3[1] - (a3[1]-b3[1]) /Trend3;
			}
			if(Input[0] < b3[1])
			{
				b3[0]=Input[0];
			}
			else
			{
				b3[0] =b3[1] + (a3[1]-b3[1]) /Trend3;
			}

			avg3[0] = (a3[0]+b3[0])/2;

			//

			if(Input[0] > a4[1])
			{
				a4[0]=Input[0] ;
			}
			else
			{
				a4[0] = a4[1] - (a4[1]-b4[1]) /Trend4;
			}
			if(Input[0] < b4[1])
			{
				b4[0]=Input[0];
			}
			else
			{
				b4[0] =b4[1] + (a4[1]-b4[1]) /Trend4;
			}

			avg4[0] = (a4[0]+b4[0])/2;

			//

			QG3[0] = avg3[0];
			QG4[0] = avg4[0];

			// =========================================================================

			if (CurrentBar < 5)
				return;

			if (IsFirstTickOfBar)
			{
				int b = Calculate != Calculate.OnBarClose && State==State.Realtime ? 1 : 0;


				// SLOW:
				{

					bool crossingUP = QG3[b+0] > QG4[b+0]
						&&	(QG3[b+1] < QG4[b+1]
							|| (QG3[b+1] == QG4[b+1] && QG3[b+2] < QG4[b+2])
							|| (QG3[b+1] == QG4[b+1] && QG3[b+2] == QG4[b+2] && QG3[b+3] < QG4[b+3])
							);
					bool crossingDOWN = QG3[b+0] < QG4[b+0]
						&&	(QG3[b+1] > QG4[b+1]
							|| (QG3[b+1] == QG4[b+1] && QG3[b+2] > QG4[b+2])
							|| (QG3[b+1] == QG4[b+1] && QG3[b+2] == QG4[b+2] && QG3[b+3] > QG4[b+3])
							);

					if (crossingUP)
					{
						Draw.Text(this,"CrossAlertUpB"+(CurrentBar-b),true,upMarkerB,
							b,QG4[b]-ATR(ProximityLimit_ATR_Period)[b]*MarkerOffsetB*0.1,0,UpColorB,markerFontB,TextAlignment.Center,Brushes.Transparent,Brushes.Transparent,0);
						SlowTrendSignals[b] = +1;

						// trigger alert
						if (State == State.Realtime && Alerts_Crossing34)
							Alert("Alerts_Crossing12Up"+(CurrentBar-b), Priority.Medium, "Crossing 3/4 Up", alertPrefix+Alerts_Crossing34_AlertSoundBull, 1, Brushes.MintCream, Brushes.DarkGreen);
					}

					if (crossingDOWN)
					{
						Draw.Text(this,"CrossAlertDnB"+(CurrentBar-b),true,downMarkerB,
							b,QG4[b]+ATR(ProximityLimit_ATR_Period)[b]*MarkerOffsetB*0.1,0,DownColorB,markerFontB,TextAlignment.Center,Brushes.Transparent,Brushes.Transparent,0);
						SlowTrendSignals[b] = -1;

						// trigger alert
						if (State == State.Realtime && Alerts_Crossing34)
							Alert("Alerts_Crossing12Dn"+(CurrentBar-b), Priority.Medium, "Crossing 3/4 Down", alertPrefix+Alerts_Crossing34_AlertSoundBear, 1, Brushes.MistyRose, Brushes.Maroon);
					}

					// publish trend
					SlowTrend[b] = Math.Sign(QG3[b]-QG4[b]);

				}


				// FAST:
				{

					bool crossingUP = QG1[b+0] > QG2[b+0]
						&&	(QG1[b+1] < QG2[b+1]
							|| (QG1[b+1] == QG2[b+1] && QG1[b+2] < QG2[b+2])
							|| (QG1[b+1] == QG2[b+1] && QG1[b+2] == QG2[b+2] && QG1[b+3] < QG2[b+3])
							);
					bool crossingDOWN = QG1[b+0] < QG2[b+0]
						&&	(QG1[b+1] > QG2[b+1]
							|| (QG1[b+1] == QG2[b+1] && QG1[b+2] > QG2[b+2])
							|| (QG1[b+1] == QG2[b+1] && QG1[b+2] == QG2[b+2] && QG1[b+3] > QG2[b+3])
							);

					if (crossingUP
						&& (!FilterViaSlowPair || Close[b] > Math.Max(QG4[b],QG3[b]))
						)
					{
						Draw.Text(this,"CrossAlertUpA"+(CurrentBar-b),true,upMarkerA,
							b,QG2[b]-ATR(ProximityLimit_ATR_Period)[b]*MarkerOffsetA*0.1,0,UpColorA,markerFontA,TextAlignment.Center,Brushes.Transparent,Brushes.Transparent,0);
						FastTrendSignals[b] = +1;

						// trigger alert
						if (State == State.Realtime && Alerts_Crossing12)
							Alert("Alerts_Crossing12Up"+(CurrentBar-b), Priority.Medium, "Crossing 1/2 Up", alertPrefix+Alerts_Crossing12_AlertSoundBull, 1, Brushes.MintCream, Brushes.DarkGreen);
					}

					if (crossingDOWN
						&& (!FilterViaSlowPair || Close[b] < Math.Min(QG4[b],QG3[b]))
						)
					{
						Draw.Text(this,"CrossAlertDnA"+(CurrentBar-b),true,downMarkerA,
							b,QG2[b]+ATR(ProximityLimit_ATR_Period)[b]*MarkerOffsetA*0.1,0,DownColorA,markerFontA,TextAlignment.Center,Brushes.Transparent,Brushes.Transparent,0);
						FastTrendSignals[b] = -1;

						// trigger alert
						if (State == State.Realtime && Alerts_Crossing12)
							Alert("Alerts_Crossing12Dn"+(CurrentBar-b), Priority.Medium, "Crossing 1/2 Down", alertPrefix+Alerts_Crossing12_AlertSoundBear, 1, Brushes.MistyRose, Brushes.Maroon);
					}

					// publish trend
					FastTrend[b] = Math.Sign(QG1[b]-QG2[b]);
				}

				DoContinuationSignals(b);

				// proximity limit feature, Octo 2023
				if (CurrentBar > Math.Max(Trend1, Trend2))
					DoProximityAlerts();

			}
		}

		private void DoContinuationSignals(int barsAgo)
		{
			// CONTINUATION SIGNALS
			if (CurrentMode != Modes.Off)
			{
				int AZ = barsAgo;
				if (QG1[AZ] > QG2[AZ]
					&& (CurrentMode == Modes.Single || QG3[AZ] > QG4[AZ])
					&& (CurrentMode == Modes.Single || QG1[AZ] > QG3[AZ])
					&& Close[AZ] > QG1[AZ]
					&& Close[AZ+1] <= QG1[AZ+1]
					)
				{
					//Print(Time[AZ]+" Cont Up check AZ="+AZ+" Close "+Close[AZ]+" v QG1 "+QG1[AZ]); // CHECKER
					// count how many bars it was inside qg1
					int counted = 1;
					int x = AZ+2;
					bool ok = true;
					for (; x < CurrentBar; x++)
					{
						if (Close[x] > QG1[x]
							)
							break;
						if (QG1[x] < QG2[x])
						{
							ok = false;
							break;
						}
						counted++;
						if (counted >= ContinuationMaxBars)
							break;
					}
					if (counted <= ContinuationMaxBars && ok)
					{
						ContinuationSignals[AZ] = +1;
						Draw.Text(this, "ContinuationUp"+(CurrentBar-AZ), true, upMarkerContinuation,
							AZ, Low[AZ]-ATR(ProximityLimit_ATR_Period)[AZ]*MarkerOffsetContinuation*0.1, 0, UpColorContinuation, markerFontContinuation, TextAlignment.Center, Brushes.Transparent, Brushes.Transparent, 0);
					}
				}

				if (QG1[AZ] < QG2[AZ]
					&& (CurrentMode == Modes.Single || QG3[AZ] < QG4[AZ])
					&& (CurrentMode == Modes.Single || QG1[AZ] < QG3[AZ])
					&& Close[AZ] < QG1[AZ]
					&& Close[AZ+1] >= QG1[AZ+1]
					)
				{
					// count how many bars it was inside qg1
					int counted = 1;
					int x = AZ+2;
					bool ok = true;
					for (; x < CurrentBar; x++)
					{
						if (Close[x] < QG1[x]
							)
							break;
						if (QG1[x] > QG2[x])
						{
							ok = false;
							break;
						}
						counted++;
						if (counted >= ContinuationMaxBars)
							break;
					}
					if (counted <= ContinuationMaxBars && ok)
					{
						ContinuationSignals[AZ] = -1;
						Draw.Text(this, "ContinuationDn"+(CurrentBar-AZ), true, downMarkerContinuation,
							AZ, High[AZ]+ATR(ProximityLimit_ATR_Period)[AZ]*MarkerOffsetContinuation*0.1, 0, DownColorContinuation, markerFontContinuation, TextAlignment.Center, Brushes.Transparent, Brushes.Transparent, 0);
					}
				}
			}
		}

		private void DoProximityAlerts()
		{
			if (!IsFirstTickOfBar)
				return;
			if (!ProximityAlert1Bar_Enabled && !ProximityAlert2Bar_Enabled)
				return;

			if (CurrentMode ==	Modes.Off)
				return;
			
			int AZ = State == State.Realtime && Calculate != Calculate.OnBarClose ? 1 : 0;
			double vATR = ATR(ProximityLimit_ATR_Period)[AZ];

			// resets
			if (Low[AZ] > QG1[AZ]+vATR*ProximityAlert_ProximityReset)
				plProximityAlertUp.Reset();
			else
				plProximityAlertUp[AZ] = plProximityAlertUp[AZ+1];
			if (High[AZ] < QG1[AZ]-vATR*ProximityAlert_ProximityReset)
				plProximityAlertDn.Reset();
			else
				plProximityAlertDn[AZ] = plProximityAlertDn[AZ+1];

			// alerts and markers
			if ((ProximityAlert1Bar_Enabled
				&& High[AZ] >= QG1[AZ]-vATR*ProximityAlert_ProximityLimit
				&& Close[AZ] < Open[AZ]
				&& Close[AZ] < QG1[AZ]
				&& plProximityAlertDn[AZ] == false
				&& QG1[AZ] < QG2[AZ]
				)
				||
				(ProximityAlert2Bar_Enabled
				&& High[AZ+1] >= QG1[AZ+1]-vATR*ProximityAlert_ProximityLimit
				&& Close[AZ+1] >= Open[AZ+1]
				&& Close[AZ+1] < QG1[AZ+1]
				&& Close[AZ] < Open[AZ]
				&& Close[AZ] < QG1[AZ]
				&& plProximityAlertDn[AZ] == false
				&& QG1[AZ] < QG2[AZ]
				)
				)
			{
				plProximityAlertDn[AZ] = true;
				Draw.Text(this,"ProximityLimitDn"+(CurrentBar-AZ),true,pldownMarker,
					AZ,High[AZ]+vATR*ProximityLimit_MarkerOffset*0.1,0,ProximityLimit_DownColor,plmarkerFont,TextAlignment.Center,Brushes.Transparent,Brushes.Transparent,0);
				// trigger alert
				if (State == State.Realtime && Alerts_ProximityLimit)
					Alert("ProximityLimitDn"+(CurrentBar-AZ), Priority.Medium, "Proximity Limit Down", alertPrefix+ProximityLimit_AlertSoundBear, 1, Brushes.MistyRose, Brushes.Maroon);
			}
			// replicate for Up
			if ((ProximityAlert1Bar_Enabled
				&& Low[AZ] <= QG1[AZ]+vATR*ProximityAlert_ProximityLimit
				&& Close[AZ] > Open[AZ]
				&& Close[AZ] > QG1[AZ]
				&& plProximityAlertUp[AZ] == false
				&& QG1[AZ] > QG2[AZ]
				)
				||
				(ProximityAlert2Bar_Enabled
				&& Low[AZ+1] <= QG1[AZ+1]+vATR*ProximityAlert_ProximityLimit
				&& Close[AZ+1] <= Open[AZ+1]
				&& Close[AZ+1] > QG1[AZ+1]
				&& Close[AZ] > Open[AZ]
				&& Close[AZ] > QG1[AZ]
				&& plProximityAlertUp[AZ] == false
				&& QG1[AZ] > QG2[AZ]
				)
				)
			{
				plProximityAlertUp[AZ] = true;
				Draw.Text(this,"ProximityLimitUp"+(CurrentBar-AZ),true,plupMarker,
					AZ,Low[AZ]-vATR*ProximityLimit_MarkerOffset*0.1,0,ProximityLimit_UpColor,plmarkerFont,TextAlignment.Center,Brushes.Transparent,Brushes.Transparent,0);
				// trigger alert
				if (State == State.Realtime && Alerts_ProximityLimit)
					Alert("ProximityLimitUp"+(CurrentBar-AZ), Priority.Medium, "Proximity Limit Up", alertPrefix+ProximityLimit_AlertSoundBull, 1, Brushes.MintCream, Brushes.DarkGreen);
			}

		}

		private void RepaintIndicator()
		{

			try
			{
				// manually remove continuation texts
				for (int i = 0; i < CurrentBar; i++)
				{
					if (ContinuationSignals[i] != 0)
					{
						if (ContinuationSignals[i] > 0)
							RemoveDrawObject("ContinuationUp"+(CurrentBar-i));
						else
							RemoveDrawObject("ContinuationDn"+(CurrentBar-i));
						ContinuationSignals[i] = 0;
					}
				}
				// manually remove Proximity texts
				for (int i = 0; i < CurrentBar; i++)
				{
					if (plProximityAlertUp[i] || plProximityAlertDn[i])
					{
						if (plProximityAlertUp[i])
							RemoveDrawObject("ProximityLimitUp"+(CurrentBar-i));
						if (plProximityAlertDn[i])
							RemoveDrawObject("ProximityLimitDn"+(CurrentBar-i));
						plProximityAlertUp[i] = false;
						plProximityAlertDn[i] = false; 
					}
				}

				// recalculate continuations
				if (CurrentMode != Modes.Off)
				{
					for (int i = 5; i < CurrentBars[0]; i++)
					{
						DoContinuationSignals(i);
					}
				}
			}
			catch (Exception ex)
			{
				throw new Exception(ex.ToString());
			}

			ForceRefresh();
		}

		#region OnRender override

		public override void OnRenderTargetChanged()
		{
			bgbrushUpA = null;
			bgbrushDownA = null;
			bgbrushUpB = null;
			bgbrushDownB = null;
			tr1BrushUp = null;
			tr1BrushDown = null;
		}

		protected override void OnRender(Gui.Chart.ChartControl chartControl, Gui.Chart.ChartScale chartScale)
		{
			if (Bars == null || chartControl == null)
				return;
			IsValidDataPoint(0); // Make sure indicator is calculated until last (existing) bar

			if (CurrentBar < 20)
				return;

			if (Cloud_OpacityA > 0)
			{
				if (bgbrushUpA == null)
				{
					bgbrushUpA = Cloud_FastAboveSlowA.Clone();
					bgbrushUpA.Opacity = Cloud_OpacityA*0.01;
					bgbrushUpA.Freeze();
		            bgbrushDownA = Cloud_FastBelowSlowA.Clone();
					bgbrushDownA.Opacity = Cloud_OpacityA*0.01;
					bgbrushDownA.Freeze();
				}
				try
				{
					PlotRibbonIntersectionSmart(bgbrushUpA,bgbrushDownA,Values[0],Values[1],0,1,chartControl,chartScale);
				}
				catch (Exception ex)
				{
					Print("problem in PlotRibbon(A): "+ex.ToString());
				}
			}

			if (Cloud_OpacityB > 0)
			{
				if (bgbrushUpB == null)
				{
					bgbrushUpB = Cloud_FastAboveSlowB.Clone();
					bgbrushUpB.Opacity = Cloud_OpacityB*0.01;
					bgbrushUpB.Freeze();
		            bgbrushDownB = Cloud_FastBelowSlowB.Clone();
					bgbrushDownB.Opacity = Cloud_OpacityB*0.01;
					bgbrushDownB.Freeze();
				}
				try
				{
					PlotRibbonIntersectionSmart(bgbrushUpB,bgbrushDownB,Values[3],Values[4],3,4,chartControl,chartScale);
				}
				catch (Exception ex)
				{
					Print("problem in PlotRibbon(B): "+ex.ToString());
				}
			}

			base.OnRender(chartControl, chartScale);

		}


		private void PlotRibbonIntersectionSmart(Brush brushUP, Brush brushDOWN, Series<double> s0, Series<double> s1, int seriesIdx0, int seriesIdx1,
								Gui.Chart.ChartControl chartControl, Gui.Chart.ChartScale chartScale)
		{

			SharpDX.Direct2D1.Brush			brushUPDX			= brushUP.ToDxBrush(RenderTarget);
			SharpDX.Direct2D1.Brush			brushDOWNDX			= brushDOWN.ToDxBrush(RenderTarget);

			SharpDX.Direct2D1.AntialiasMode oldAntialiasMode	= RenderTarget.AntialiasMode;

			// -- do stuff here :
			SharpDX.Direct2D1.Brush brush = brushUPDX;

            int barcount = 0;								// Start with leftmost bar.
            bool firstbar = true;							// Plotting the first bar.
			double barWidth = chartControl.BarWidth;
			int totalBars = ChartBars.ToIndex - ChartBars.FromIndex + 1;
			if (totalBars < 5)
				return;

			SharpDX.Direct2D1.PathGeometry	g		= null;
			SharpDX.Direct2D1.GeometrySink	sink	= null;

			int safetyCheck = 0;
			List<SharpDX.Vector2> points = new List<SharpDX.Vector2>();

			for (int b = Math.Max(0,ChartBars.FromIndex-1); b<= Math.Min(ChartBars.ToIndex+1,CurrentBars[0]-1); b++)
			{
				if (!s0.IsValidDataPointAt(b) || !s1.IsValidDataPointAt(b))
					continue;
				points.Clear();
				double diff = s0.GetValueAt(b) - s1.GetValueAt(b);
				double diffNext = s0.GetValueAt(b+1) - s1.GetValueAt(b+1);


				if (Math.Sign(diff) == -Math.Sign(diffNext))
				{
					// special case, there is an intersection between this bar and the next one
					int x = chartControl.GetXByBarIndex(ChartBars,b);
					int xNext = chartControl.GetXByBarIndex(ChartBars,b+1);
					int y0 = chartScale.GetYByValue(s0.GetValueAt(b));
					int y1 = chartScale.GetYByValue(s1.GetValueAt(b));
					int y0Next = chartScale.GetYByValue(s0.GetValueAt(b+1));
					int y1Next = chartScale.GetYByValue(s1.GetValueAt(b+1));
					var intersection = FindIntersectionPoint(x,y0,xNext,y0Next,x,y1,xNext,y1Next);
					float xIntersec = (float)intersection[0];
					float yIntersec = (float)intersection[1];

					points.Add(new SharpDX.Vector2((float)x,(float)y0));
					points.Add(new SharpDX.Vector2((float)x,(float)y1));
					points.Add(new SharpDX.Vector2(xIntersec,yIntersec));
					brush = diff >= 0 ? brushUPDX : brushDOWNDX;

					foreach(SharpDX.Vector2 v in points)
					{
						if (sink == null)
						{
							g			= new SharpDX.Direct2D1.PathGeometry(Core.Globals.D2DFactory);
							sink		= g.Open();
							sink.BeginFigure(v, SharpDX.Direct2D1.FigureBegin.Filled);
						}
						else
							sink.AddLine(v);
					}
					if (sink != null)
					{
						sink.EndFigure(SharpDX.Direct2D1.FigureEnd.Open);
						sink.Close();
					}
					RenderTarget.FillGeometry(g,brush);
					//Print("filled  with "+brush.ToString());
	                sink = null;									// Eliminate points already colored.

					// -
					points.Clear();
					points.Add(new SharpDX.Vector2(xIntersec,yIntersec));
					points.Add(new SharpDX.Vector2((float)xNext,(float)y0Next));
					points.Add(new SharpDX.Vector2((float)xNext,(float)y1Next));
					brush = diffNext >= 0 ? brushUPDX : brushDOWNDX;

					foreach(SharpDX.Vector2 v in points)
					{
						if (sink == null)
						{
							g			= new SharpDX.Direct2D1.PathGeometry(Core.Globals.D2DFactory);
							sink		= g.Open();
							sink.BeginFigure(v, SharpDX.Direct2D1.FigureBegin.Filled);
						}
						else
							sink.AddLine(v);
					}
					if (sink != null)
					{
						sink.EndFigure(SharpDX.Direct2D1.FigureEnd.Open);
						sink.Close();
					}
					RenderTarget.FillGeometry(g,brush);
					//Print("filled  with "+brush.ToString());
	                sink = null;									// Eliminate points already colored.
				}
				else
				{
					// a normal render of one color
					int x = chartControl.GetXByBarIndex(ChartBars,b);
					int xNext = chartControl.GetXByBarIndex(ChartBars,b+1);
					int y0 = chartScale.GetYByValue(s0.GetValueAt(b));
					int y1 = chartScale.GetYByValue(s1.GetValueAt(b));
					int y0Next = chartScale.GetYByValue(s0.GetValueAt(b+1));
					int y1Next = chartScale.GetYByValue(s1.GetValueAt(b+1));
					points.Add(new SharpDX.Vector2((float)x,(float)y0));
					points.Add(new SharpDX.Vector2((float)x,(float)y1));
					points.Add(new SharpDX.Vector2((float)xNext,(float)y1Next));
					points.Add(new SharpDX.Vector2((float)xNext,(float)y0Next));
					brush = diff >= 0 ? brushUPDX : brushDOWNDX;

					foreach(SharpDX.Vector2 v in points)
					{
						if (sink == null)
						{
							g			= new SharpDX.Direct2D1.PathGeometry(Core.Globals.D2DFactory);
							sink		= g.Open();
							sink.BeginFigure(v, SharpDX.Direct2D1.FigureBegin.Filled);
						}
						else
							sink.AddLine(v);
					}
					if (sink != null)
					{
						sink.EndFigure(SharpDX.Direct2D1.FigureEnd.Open);
						sink.Close();
					}
					RenderTarget.FillGeometry(g,brush);
					//Print("filled  with "+brush.ToString());
	                sink = null;									// Eliminate points already colored.
				}
			}

			if (brush != null)
				brush.Dispose();

			RenderTarget.AntialiasMode					= oldAntialiasMode;

			brushUPDX.Dispose();
			brushDOWNDX.Dispose();
		}

		private double[] FindIntersectionPoint(double rearX1, double rearY1, double frontX1, double frontY1,     double rearX2, double rearY2, double frontX2, double frontY2)
		{
			/*
			Line-Line Intersection
			One of the most common tasks you will find in geometry problems is line intersection. Despite the fact that it is so common, a lot of coders still have trouble with it.
			The first question is, what form are we given our lines in, and what form would we like them in? Ideally, each of our lines will be in the form Ax+By=C,
			where A, B and C are the numbers which define the line. However, we are rarely given lines in this format, but we can easily generate such an equation from two points.
			Say we are given two different points, (x1, y1) and (x2, y2), and want to find A, B and C for the equation above. We can do so by setting
			A = y2-y1
			B = x1-x2
			C = A*x1+B*y1
			Regardless of how the lines are specified, you should be able to generate two different points along the line, and then generate A, B and C. Now, lets say that you have lines, given by the equations:
			A1x + B1y = C1
			A2x + B2y = C2
			To find the point at which the two lines intersect, we simply need to solve the two equations for the two unknowns, x and y.

				double det = A1*B2 - A2*B1
				if(det == 0){
					//Lines are parallel
				}else{
					double x = (B2*C1 - B1*C2)/det
					double y = (A1*C2 - A2*C1)/det
				}
			To see where this comes from, consider multiplying the top equation by B2, and the bottom equation by B1. This gives you
			A1B2x + B1B2y = B2C1
			A2B1x + B1B2y = B1C2
			Now, subtract the bottom equation from the top equation to get
			A1B2x - A2B1x = B2C1 - B1C2
			Finally, divide both sides by A1B2 - A2B1, and you get the equation for x. The equation for y can be derived similarly.

			This gives you the location of the intersection of two lines, but what if you have line segments, not lines. In this case, you need to make sure that the point you found is on both of the line segments.
			If your line segment goes from (x1,y1) to (x2,y2), then to check if (x,y) is on that segment, you just need to check that min(x1,x2) ≤ x ≤ max(x1,x2), and do the same thing for y.
			You must be careful about double precision issues though. If your point is right on the edge of the segment, or if the segment is horizontal or vertical, a simple comparison might be problematic.
			In these cases, you can either do your comparisons with some tolerance, or else use a fraction class.
			*/
			double a1 = frontY1 - rearY1;
			double b1 = rearX1 - frontX1;
			double c1 = a1*rearX1 + b1*rearY1;

			double a2 = frontY2 - rearY2;
			double b2 = rearX2 - frontX2;
			double c2 = a2*rearX2 + b2*rearY2;

			double det = a1*b2 - a2*b1;
			if (det == 0.0)
				return new double[]{0,0};	// lines are parallel
			double x = (b2*c1 - b1*c2)/det;
			double y = (a1*c2 - a2*c1)/det;
			return new double[]{x,y};
		}
		#endregion


		#region Properties


		// GENERATE SOUND FILES LIST (soundFiles variable) from the default NinjaTrader 7/sounds directory (generally located under Program Files)
		public class SoundConverter : TypeConverter
		{
			public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
			{
				if (context == null)
				{
					return null;
				}
				var list = new System.Collections.ArrayList();
				var dir = new System.IO.DirectoryInfo(NinjaTrader.Core.Globals.InstallDir + "sounds");
				var files= dir.GetFiles("*.wav");

				foreach (var file in files)
				{
					list.Add(file.Name);
				}

				files= dir.GetFiles("*.WAV");

				foreach (var file in files)
				{
					if (!list.Contains(file.Name))
						list.Add(file.Name);
				}

				return new TypeConverter.StandardValuesCollection(list);
			}

			public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
			{
				return true;
			}
		}

		private string alertPrefix = NinjaTrader.Core.Globals.InstallDir+@"\sounds\";

		//

		[NinjaScriptProperty]
		[Display(Name = "Mode", Order = 10, GroupName = "Mode")]
		public NinjaTrader.NinjaScript.Indicators.dpQGP.Modes CurrentMode
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Trend1", Order=10, GroupName="Parameters A")]
		public int Trend1
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Trend2", Order=20, GroupName="Parameters A")]
		public int Trend2
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name="Smoothing Type", Order=24, GroupName="Parameters A")]
		public NinjaTrader.NinjaScript.Indicators.dpQGP.MATypes TrendSmoothingType12
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Smoothing Period", Order=24, GroupName="Parameters A")]
		public int TrendSmoothingPeriod12
		{ get; set; }

		//[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="Cloud_FastAboveSlow", Order=30, GroupName="Parameters A")]
		public Brush Cloud_FastAboveSlowA
		{ get; set; }

		[Browsable(false)]
		public string Cloud_FastAboveSlowASerializable
		{
			get { return Serialize.BrushToString(Cloud_FastAboveSlowA); }
			set { Cloud_FastAboveSlowA = Serialize.StringToBrush(value); }
		}

		//[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="Cloud_FastBelowSlow", Order=40, GroupName="Parameters A")]
		public Brush Cloud_FastBelowSlowA
		{ get; set; }

		[Browsable(false)]
		public string Cloud_FastBelowSlowASerializable
		{
			get { return Serialize.BrushToString(Cloud_FastBelowSlowA); }
			set { Cloud_FastBelowSlowA = Serialize.StringToBrush(value); }
		}

		//[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="Cloud_Opacity", Order=50, GroupName="Parameters A")]
		public int Cloud_OpacityA
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name="MarkerType", Order=60, GroupName="Parameters A")]
		public NinjaTrader.NinjaScript.Indicators.dpQGP.Marker_Types MarkerTypeA
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="MarkerSize", Order=70, GroupName="Parameters A")]
		public int MarkerSizeA
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="MarkerOffset", Order=80, GroupName="Parameters A")]
		public int MarkerOffsetA
		{ get; set; }

		[XmlIgnore()]
		//[NinjaScriptProperty]
		[Display(Name="UpColor", Description="Color for uptrend", Order=110, GroupName="Parameters A")]
        public Brush UpColorA
		{ get; set; }

		// Serialize our Color object
		[Browsable(false)]
		public string UpColorASerialize
		{
			get { return Serialize.BrushToString(UpColorA); }
			set { UpColorA = Serialize.StringToBrush(value); }
		}

		[XmlIgnore()]
		//[NinjaScriptProperty]
		[Display(Name="DownColor", Description="Color for downtrend", Order=120, GroupName="Parameters A")]
        public Brush DownColorA
		{ get; set; }

		// Serialize our Color object
		[Browsable(false)]
		public string DownColorASerialize
		{
			get { return Serialize.BrushToString(DownColorA); }
			set { DownColorA = Serialize.StringToBrush(value); }
		}


		[NinjaScriptProperty]
		[Display(Name="FilterViaSlowPair", Order=100, GroupName="Parameters A")]
		public bool FilterViaSlowPair
		{ get; set; }


		[NinjaScriptProperty]
		[Display(Name="Alerts?", Description="Enable alert feature", Order=510, GroupName="Parameters A")]
		public bool Alerts_Crossing12
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name="AlertSound Bullish", Description="Sound for alert", Order=520, GroupName="Parameters A")]
		[TypeConverter(typeof(SoundConverter))]
		public string Alerts_Crossing12_AlertSoundBull
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name="AlertSound Bearish", Description="Sound for alert", Order=530, GroupName="Parameters A")]
		[TypeConverter(typeof(SoundConverter))]
		public string Alerts_Crossing12_AlertSoundBear
		{ get; set; }

		[XmlIgnore()]
		//[NinjaScriptProperty]
		[Display(Name="Trend1_UpColor", Description="Color for uptrend 1", Order=610, GroupName="Parameters A")]
		public Brush Trend1_UpColor
		{ get; set; }
		
		// Serialize our Color object
		[Browsable(false)]
		public string Trend1_UpColorSerialize
		{
			get { return Serialize.BrushToString(Trend1_UpColor); }
			set { Trend1_UpColor = Serialize.StringToBrush(value); }
		}
		
		[XmlIgnore()]
		//[NinjaScriptProperty]
		[Display(Name="Trend1_DownColor", Description="Color for downtrend 1", Order=620, GroupName="Parameters A")]
		public Brush Trend1_DownColor
		{ get; set; }
		
		// Serialize our Color object
		[Browsable(false)]
		public string Trend1_DownColorSerialize
		{
			get { return Serialize.BrushToString(Trend1_DownColor); }
			set { Trend1_DownColor = Serialize.StringToBrush(value); }
		}


		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Trend3", Order=10, GroupName="Parameters B")]
		public int Trend3
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Trend4", Order=20, GroupName="Parameters B")]
		public int Trend4
		{ get; set; }

		//[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="Cloud_FastAboveSlow", Order=30, GroupName="Parameters B")]
		public Brush Cloud_FastAboveSlowB
		{ get; set; }

		[Browsable(false)]
		public string Cloud_FastAboveSlowBSerializable
		{
			get { return Serialize.BrushToString(Cloud_FastAboveSlowB); }
			set { Cloud_FastAboveSlowB = Serialize.StringToBrush(value); }
		}

		//[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="Cloud_FastBelowSlow", Order=40, GroupName="Parameters B")]
		public Brush Cloud_FastBelowSlowB
		{ get; set; }

		[Browsable(false)]
		public string Cloud_FastBelowBSlowSerializable
		{
			get { return Serialize.BrushToString(Cloud_FastBelowSlowB); }
			set { Cloud_FastBelowSlowB = Serialize.StringToBrush(value); }
		}

		//[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="Cloud_Opacity", Order=50, GroupName="Parameters B")]
		public int Cloud_OpacityB
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name="MarkerType", Order=60, GroupName="Parameters B")]
		public NinjaTrader.NinjaScript.Indicators.dpQGP.Marker_Types MarkerTypeB
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="MarkerSize", Order=70, GroupName="Parameters B")]
		public int MarkerSizeB
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="MarkerOffset", Order=80, GroupName="Parameters B")]
		public int MarkerOffsetB
		{ get; set; }

		[XmlIgnore()]
		//[NinjaScriptProperty]
		[Display(Name="UpColor", Description="Color for uptrend", Order=110, GroupName="Parameters B")]
        public Brush UpColorB
		{ get; set; }

		// Serialize our Color object
		[Browsable(false)]
		public string UpColorBSerialize
		{
			get { return Serialize.BrushToString(UpColorB); }
			set { UpColorB = Serialize.StringToBrush(value); }
		}

		[XmlIgnore()]
		//[NinjaScriptProperty]
		[Display(Name="DownColor", Description="Color for downtrend", Order=120, GroupName="Parameters B")]
        public Brush DownColorB
		{ get; set; }

		// Serialize our Color object
		[Browsable(false)]
		public string DownColorBSerialize
		{
			get { return Serialize.BrushToString(DownColorB); }
			set { DownColorB = Serialize.StringToBrush(value); }
		}

		[NinjaScriptProperty]
		[Display(Name="Alerts?", Description="Enable alert feature", Order=510, GroupName="Parameters B")]
		public bool Alerts_Crossing34
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name="AlertSound Bullish", Description="Sound for alert", Order=520, GroupName="Parameters B")]
		[TypeConverter(typeof(SoundConverter))]
		public string Alerts_Crossing34_AlertSoundBull
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name="AlertSound Bearish", Description="Sound for alert", Order=530, GroupName="Parameters B")]
		[TypeConverter(typeof(SoundConverter))]
		public string Alerts_Crossing34_AlertSoundBear
		{ get; set; }

		//
		// REPLACED BY THE CURRENTMODE
		// [NinjaScriptProperty]
		// [Display(Name="ContinuationSignals", Order=50, GroupName="Continuation")]
		// public bool ContinuationSignals_Enabled
		// { get; set; }

		[NinjaScriptProperty]
		[Range(1,250)]
		[Display(Name="ContinuationSignals", Order=55, GroupName="Continuation")]
		public int ContinuationMaxBars
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name="MarkerType", Order=60, GroupName="Continuation")]
		public NinjaTrader.NinjaScript.Indicators.dpQGP.Marker_Types MarkerTypeContinuation
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="MarkerSize", Order=70, GroupName="Continuation")]
		public int MarkerSizeContinuation
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="MarkerOffset", Order=80, GroupName="Continuation")]
		public int MarkerOffsetContinuation
		{ get; set; }

		[XmlIgnore()]
		//[NinjaScriptProperty]
		[Display(Name="UpColor", Description="Color for signal", Order=110, GroupName="Continuation")]
        public Brush UpColorContinuation
		{ get; set; }

		// Serialize our Color object
		[Browsable(false)]
		public string UpColorContinuationBSerialize
		{
			get { return Serialize.BrushToString(UpColorContinuation); }
			set { UpColorContinuation = Serialize.StringToBrush(value); }
		}

		[XmlIgnore()]
		//[NinjaScriptProperty]
		[Display(Name="DownColor", Description="Color for downtrend", Order=120, GroupName="Continuation")]
        public Brush DownColorContinuation
		{ get; set; }

		// Serialize our Color object
		[Browsable(false)]
		public string DownColorContinuationSerialize
		{
			get { return Serialize.BrushToString(DownColorContinuation); }
			set { DownColorContinuation = Serialize.StringToBrush(value); }
		}

		//

		[NinjaScriptProperty]
		[Display(Name="ProximityAlert 1Bar Enabled", Order=10, GroupName="ProximityAlert")]
		public bool ProximityAlert1Bar_Enabled
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name="ProximityAlert 2Bar Enabled", Order=20, GroupName="ProximityAlert")]
		public bool ProximityAlert2Bar_Enabled
		{ get; set; }


		// [NinjaScriptProperty]
		// [Display(Name="MarkerType", Order=20, GroupName="ProximityAlert")]
		// public NinjaTrader.NinjaScript.Indicators.dpQGP.Marker_Types MarkerType
		// { get; set; }
		//
		// [NinjaScriptProperty]
		// [Range(1,100)]
		// [Display(Name="MarkerSize", Order=30, GroupName="ProximityAlert")]
		// public int MarkerSize
		// { get; set; }

		// [NinjaScriptProperty]
		// [Range(0,100)]
		// [Display(Name="MarkerOffset", Order=40, GroupName="ProximityAlert")]
		// public int MarkerOffset
		// { get; set; }

		[NinjaScriptProperty]
		[Range(0,double.MaxValue)]
		[Display(Name="ProximityLimit ATRs", Order=100, GroupName="ProximityAlert")]
		public double ProximityAlert_ProximityLimit
		{ get; set; }

		[NinjaScriptProperty]
		[Range(0,double.MaxValue)]
		[Display(Name="ProximityReset ATRs", Order=105, GroupName="ProximityAlert")]
		public double ProximityAlert_ProximityReset
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name="MarkerType", Order=110, GroupName="ProximityAlert")]
		public NinjaTrader.NinjaScript.Indicators.dpQGP.Marker_Types ProximityLimit_MarkerType
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1,100)]
		[Display(Name="MarkerSize", Order=120, GroupName="ProximityAlert")]
		public int ProximityLimit_MarkerSize
		{ get; set; }

		[NinjaScriptProperty]
		[Range(0,100)]
		[Display(Name="MarkerOffset", Order=130, GroupName="ProximityAlert")]
		public int ProximityLimit_MarkerOffset
		{ get; set; }

		[XmlIgnore()]
		//[NinjaScriptProperty]
		[Display(Name="UpColor", Description="Color for uptrend", Order=140, GroupName="ProximityAlert")]
        public Brush ProximityLimit_UpColor
		{ get; set; }

		// Serialize our Color object
		[Browsable(false)]
		public string ProximityLimit_UpColorSerialize
		{
			get { return Serialize.BrushToString(ProximityLimit_UpColor); }
			set { ProximityLimit_UpColor = Serialize.StringToBrush(value); }
		}

		[XmlIgnore()]
		//[NinjaScriptProperty]
		[Display(Name="DownColor", Description="Color for downtrend", Order=150, GroupName="ProximityAlert")]
        public Brush ProximityLimit_DownColor
		{ get; set; }

		// Serialize our Color object
		[Browsable(false)]
		public string ProximityLimit_DownColorSerialize
		{
			get { return Serialize.BrushToString(ProximityLimit_DownColor); }
			set { ProximityLimit_DownColor = Serialize.StringToBrush(value); }
		}

		[NinjaScriptProperty]
		[Range(1,250)]
		[Display(Name="ATR Period", Order=160, GroupName="ProximityAlert")]
		public int ProximityLimit_ATR_Period
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name="Alerts?", Description="Enable alert feature", Order=510, GroupName="ProximityAlert")]
		public bool Alerts_ProximityLimit
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name="AlertSound Bullish", Description="Sound for alert", Order=520, GroupName="ProximityAlert")]
		[TypeConverter(typeof(SoundConverter))]
		public string ProximityLimit_AlertSoundBull
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name="AlertSound Bearish", Description="Sound for alert", Order=530, GroupName="ProximityAlert")]
		[TypeConverter(typeof(SoundConverter))]
		public string ProximityLimit_AlertSoundBear
		{ get; set; }


		//

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> QG1
		{
			get { return Values[0]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> QG2
		{
			get { return Values[1]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> FastTrendSignals
		{
			get { return Values[2]; }
		}


		[Browsable(false)]
		[XmlIgnore]
		public Series<double> QG3
		{
			get { return Values[3]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> QG4
		{
			get { return Values[4]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> ContinuationSignals
		{
			get { return Values[5]; }
		}


		[Browsable(false)]
		[XmlIgnore]
		public Series<double> SlowTrendSignals
		{
			get { return Values[6]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> FastTrend
		{
			get { return Values[7]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> SlowTrend
		{
			get { return Values[8]; }
		}


		#endregion

	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private dpQGP[] cachedpQGP;
		public dpQGP dpQGP(NinjaTrader.NinjaScript.Indicators.dpQGP.Modes currentMode, int trend1, int trend2, NinjaTrader.NinjaScript.Indicators.dpQGP.MATypes trendSmoothingType12, int trendSmoothingPeriod12, NinjaTrader.NinjaScript.Indicators.dpQGP.Marker_Types markerTypeA, int markerSizeA, int markerOffsetA, bool filterViaSlowPair, bool alerts_Crossing12, string alerts_Crossing12_AlertSoundBull, string alerts_Crossing12_AlertSoundBear, int trend3, int trend4, NinjaTrader.NinjaScript.Indicators.dpQGP.Marker_Types markerTypeB, int markerSizeB, int markerOffsetB, bool alerts_Crossing34, string alerts_Crossing34_AlertSoundBull, string alerts_Crossing34_AlertSoundBear, int continuationMaxBars, NinjaTrader.NinjaScript.Indicators.dpQGP.Marker_Types markerTypeContinuation, int markerSizeContinuation, int markerOffsetContinuation, bool proximityAlert1Bar_Enabled, bool proximityAlert2Bar_Enabled, double proximityAlert_ProximityLimit, double proximityAlert_ProximityReset, NinjaTrader.NinjaScript.Indicators.dpQGP.Marker_Types proximityLimit_MarkerType, int proximityLimit_MarkerSize, int proximityLimit_MarkerOffset, int proximityLimit_ATR_Period, bool alerts_ProximityLimit, string proximityLimit_AlertSoundBull, string proximityLimit_AlertSoundBear)
		{
			return dpQGP(Input, currentMode, trend1, trend2, trendSmoothingType12, trendSmoothingPeriod12, markerTypeA, markerSizeA, markerOffsetA, filterViaSlowPair, alerts_Crossing12, alerts_Crossing12_AlertSoundBull, alerts_Crossing12_AlertSoundBear, trend3, trend4, markerTypeB, markerSizeB, markerOffsetB, alerts_Crossing34, alerts_Crossing34_AlertSoundBull, alerts_Crossing34_AlertSoundBear, continuationMaxBars, markerTypeContinuation, markerSizeContinuation, markerOffsetContinuation, proximityAlert1Bar_Enabled, proximityAlert2Bar_Enabled, proximityAlert_ProximityLimit, proximityAlert_ProximityReset, proximityLimit_MarkerType, proximityLimit_MarkerSize, proximityLimit_MarkerOffset, proximityLimit_ATR_Period, alerts_ProximityLimit, proximityLimit_AlertSoundBull, proximityLimit_AlertSoundBear);
		}

		public dpQGP dpQGP(ISeries<double> input, NinjaTrader.NinjaScript.Indicators.dpQGP.Modes currentMode, int trend1, int trend2, NinjaTrader.NinjaScript.Indicators.dpQGP.MATypes trendSmoothingType12, int trendSmoothingPeriod12, NinjaTrader.NinjaScript.Indicators.dpQGP.Marker_Types markerTypeA, int markerSizeA, int markerOffsetA, bool filterViaSlowPair, bool alerts_Crossing12, string alerts_Crossing12_AlertSoundBull, string alerts_Crossing12_AlertSoundBear, int trend3, int trend4, NinjaTrader.NinjaScript.Indicators.dpQGP.Marker_Types markerTypeB, int markerSizeB, int markerOffsetB, bool alerts_Crossing34, string alerts_Crossing34_AlertSoundBull, string alerts_Crossing34_AlertSoundBear, int continuationMaxBars, NinjaTrader.NinjaScript.Indicators.dpQGP.Marker_Types markerTypeContinuation, int markerSizeContinuation, int markerOffsetContinuation, bool proximityAlert1Bar_Enabled, bool proximityAlert2Bar_Enabled, double proximityAlert_ProximityLimit, double proximityAlert_ProximityReset, NinjaTrader.NinjaScript.Indicators.dpQGP.Marker_Types proximityLimit_MarkerType, int proximityLimit_MarkerSize, int proximityLimit_MarkerOffset, int proximityLimit_ATR_Period, bool alerts_ProximityLimit, string proximityLimit_AlertSoundBull, string proximityLimit_AlertSoundBear)
		{
			if (cachedpQGP != null)
				for (int idx = 0; idx < cachedpQGP.Length; idx++)
					if (cachedpQGP[idx] != null && cachedpQGP[idx].CurrentMode == currentMode && cachedpQGP[idx].Trend1 == trend1 && cachedpQGP[idx].Trend2 == trend2 && cachedpQGP[idx].TrendSmoothingType12 == trendSmoothingType12 && cachedpQGP[idx].TrendSmoothingPeriod12 == trendSmoothingPeriod12 && cachedpQGP[idx].MarkerTypeA == markerTypeA && cachedpQGP[idx].MarkerSizeA == markerSizeA && cachedpQGP[idx].MarkerOffsetA == markerOffsetA && cachedpQGP[idx].FilterViaSlowPair == filterViaSlowPair && cachedpQGP[idx].Alerts_Crossing12 == alerts_Crossing12 && cachedpQGP[idx].Alerts_Crossing12_AlertSoundBull == alerts_Crossing12_AlertSoundBull && cachedpQGP[idx].Alerts_Crossing12_AlertSoundBear == alerts_Crossing12_AlertSoundBear && cachedpQGP[idx].Trend3 == trend3 && cachedpQGP[idx].Trend4 == trend4 && cachedpQGP[idx].MarkerTypeB == markerTypeB && cachedpQGP[idx].MarkerSizeB == markerSizeB && cachedpQGP[idx].MarkerOffsetB == markerOffsetB && cachedpQGP[idx].Alerts_Crossing34 == alerts_Crossing34 && cachedpQGP[idx].Alerts_Crossing34_AlertSoundBull == alerts_Crossing34_AlertSoundBull && cachedpQGP[idx].Alerts_Crossing34_AlertSoundBear == alerts_Crossing34_AlertSoundBear && cachedpQGP[idx].ContinuationMaxBars == continuationMaxBars && cachedpQGP[idx].MarkerTypeContinuation == markerTypeContinuation && cachedpQGP[idx].MarkerSizeContinuation == markerSizeContinuation && cachedpQGP[idx].MarkerOffsetContinuation == markerOffsetContinuation && cachedpQGP[idx].ProximityAlert1Bar_Enabled == proximityAlert1Bar_Enabled && cachedpQGP[idx].ProximityAlert2Bar_Enabled == proximityAlert2Bar_Enabled && cachedpQGP[idx].ProximityAlert_ProximityLimit == proximityAlert_ProximityLimit && cachedpQGP[idx].ProximityAlert_ProximityReset == proximityAlert_ProximityReset && cachedpQGP[idx].ProximityLimit_MarkerType == proximityLimit_MarkerType && cachedpQGP[idx].ProximityLimit_MarkerSize == proximityLimit_MarkerSize && cachedpQGP[idx].ProximityLimit_MarkerOffset == proximityLimit_MarkerOffset && cachedpQGP[idx].ProximityLimit_ATR_Period == proximityLimit_ATR_Period && cachedpQGP[idx].Alerts_ProximityLimit == alerts_ProximityLimit && cachedpQGP[idx].ProximityLimit_AlertSoundBull == proximityLimit_AlertSoundBull && cachedpQGP[idx].ProximityLimit_AlertSoundBear == proximityLimit_AlertSoundBear && cachedpQGP[idx].EqualsInput(input))
						return cachedpQGP[idx];
			return CacheIndicator<dpQGP>(new dpQGP(){ CurrentMode = currentMode, Trend1 = trend1, Trend2 = trend2, TrendSmoothingType12 = trendSmoothingType12, TrendSmoothingPeriod12 = trendSmoothingPeriod12, MarkerTypeA = markerTypeA, MarkerSizeA = markerSizeA, MarkerOffsetA = markerOffsetA, FilterViaSlowPair = filterViaSlowPair, Alerts_Crossing12 = alerts_Crossing12, Alerts_Crossing12_AlertSoundBull = alerts_Crossing12_AlertSoundBull, Alerts_Crossing12_AlertSoundBear = alerts_Crossing12_AlertSoundBear, Trend3 = trend3, Trend4 = trend4, MarkerTypeB = markerTypeB, MarkerSizeB = markerSizeB, MarkerOffsetB = markerOffsetB, Alerts_Crossing34 = alerts_Crossing34, Alerts_Crossing34_AlertSoundBull = alerts_Crossing34_AlertSoundBull, Alerts_Crossing34_AlertSoundBear = alerts_Crossing34_AlertSoundBear, ContinuationMaxBars = continuationMaxBars, MarkerTypeContinuation = markerTypeContinuation, MarkerSizeContinuation = markerSizeContinuation, MarkerOffsetContinuation = markerOffsetContinuation, ProximityAlert1Bar_Enabled = proximityAlert1Bar_Enabled, ProximityAlert2Bar_Enabled = proximityAlert2Bar_Enabled, ProximityAlert_ProximityLimit = proximityAlert_ProximityLimit, ProximityAlert_ProximityReset = proximityAlert_ProximityReset, ProximityLimit_MarkerType = proximityLimit_MarkerType, ProximityLimit_MarkerSize = proximityLimit_MarkerSize, ProximityLimit_MarkerOffset = proximityLimit_MarkerOffset, ProximityLimit_ATR_Period = proximityLimit_ATR_Period, Alerts_ProximityLimit = alerts_ProximityLimit, ProximityLimit_AlertSoundBull = proximityLimit_AlertSoundBull, ProximityLimit_AlertSoundBear = proximityLimit_AlertSoundBear }, input, ref cachedpQGP);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.dpQGP dpQGP(NinjaTrader.NinjaScript.Indicators.dpQGP.Modes currentMode, int trend1, int trend2, NinjaTrader.NinjaScript.Indicators.dpQGP.MATypes trendSmoothingType12, int trendSmoothingPeriod12, NinjaTrader.NinjaScript.Indicators.dpQGP.Marker_Types markerTypeA, int markerSizeA, int markerOffsetA, bool filterViaSlowPair, bool alerts_Crossing12, string alerts_Crossing12_AlertSoundBull, string alerts_Crossing12_AlertSoundBear, int trend3, int trend4, NinjaTrader.NinjaScript.Indicators.dpQGP.Marker_Types markerTypeB, int markerSizeB, int markerOffsetB, bool alerts_Crossing34, string alerts_Crossing34_AlertSoundBull, string alerts_Crossing34_AlertSoundBear, int continuationMaxBars, NinjaTrader.NinjaScript.Indicators.dpQGP.Marker_Types markerTypeContinuation, int markerSizeContinuation, int markerOffsetContinuation, bool proximityAlert1Bar_Enabled, bool proximityAlert2Bar_Enabled, double proximityAlert_ProximityLimit, double proximityAlert_ProximityReset, NinjaTrader.NinjaScript.Indicators.dpQGP.Marker_Types proximityLimit_MarkerType, int proximityLimit_MarkerSize, int proximityLimit_MarkerOffset, int proximityLimit_ATR_Period, bool alerts_ProximityLimit, string proximityLimit_AlertSoundBull, string proximityLimit_AlertSoundBear)
		{
			return indicator.dpQGP(Input, currentMode, trend1, trend2, trendSmoothingType12, trendSmoothingPeriod12, markerTypeA, markerSizeA, markerOffsetA, filterViaSlowPair, alerts_Crossing12, alerts_Crossing12_AlertSoundBull, alerts_Crossing12_AlertSoundBear, trend3, trend4, markerTypeB, markerSizeB, markerOffsetB, alerts_Crossing34, alerts_Crossing34_AlertSoundBull, alerts_Crossing34_AlertSoundBear, continuationMaxBars, markerTypeContinuation, markerSizeContinuation, markerOffsetContinuation, proximityAlert1Bar_Enabled, proximityAlert2Bar_Enabled, proximityAlert_ProximityLimit, proximityAlert_ProximityReset, proximityLimit_MarkerType, proximityLimit_MarkerSize, proximityLimit_MarkerOffset, proximityLimit_ATR_Period, alerts_ProximityLimit, proximityLimit_AlertSoundBull, proximityLimit_AlertSoundBear);
		}

		public Indicators.dpQGP dpQGP(ISeries<double> input , NinjaTrader.NinjaScript.Indicators.dpQGP.Modes currentMode, int trend1, int trend2, NinjaTrader.NinjaScript.Indicators.dpQGP.MATypes trendSmoothingType12, int trendSmoothingPeriod12, NinjaTrader.NinjaScript.Indicators.dpQGP.Marker_Types markerTypeA, int markerSizeA, int markerOffsetA, bool filterViaSlowPair, bool alerts_Crossing12, string alerts_Crossing12_AlertSoundBull, string alerts_Crossing12_AlertSoundBear, int trend3, int trend4, NinjaTrader.NinjaScript.Indicators.dpQGP.Marker_Types markerTypeB, int markerSizeB, int markerOffsetB, bool alerts_Crossing34, string alerts_Crossing34_AlertSoundBull, string alerts_Crossing34_AlertSoundBear, int continuationMaxBars, NinjaTrader.NinjaScript.Indicators.dpQGP.Marker_Types markerTypeContinuation, int markerSizeContinuation, int markerOffsetContinuation, bool proximityAlert1Bar_Enabled, bool proximityAlert2Bar_Enabled, double proximityAlert_ProximityLimit, double proximityAlert_ProximityReset, NinjaTrader.NinjaScript.Indicators.dpQGP.Marker_Types proximityLimit_MarkerType, int proximityLimit_MarkerSize, int proximityLimit_MarkerOffset, int proximityLimit_ATR_Period, bool alerts_ProximityLimit, string proximityLimit_AlertSoundBull, string proximityLimit_AlertSoundBear)
		{
			return indicator.dpQGP(input, currentMode, trend1, trend2, trendSmoothingType12, trendSmoothingPeriod12, markerTypeA, markerSizeA, markerOffsetA, filterViaSlowPair, alerts_Crossing12, alerts_Crossing12_AlertSoundBull, alerts_Crossing12_AlertSoundBear, trend3, trend4, markerTypeB, markerSizeB, markerOffsetB, alerts_Crossing34, alerts_Crossing34_AlertSoundBull, alerts_Crossing34_AlertSoundBear, continuationMaxBars, markerTypeContinuation, markerSizeContinuation, markerOffsetContinuation, proximityAlert1Bar_Enabled, proximityAlert2Bar_Enabled, proximityAlert_ProximityLimit, proximityAlert_ProximityReset, proximityLimit_MarkerType, proximityLimit_MarkerSize, proximityLimit_MarkerOffset, proximityLimit_ATR_Period, alerts_ProximityLimit, proximityLimit_AlertSoundBull, proximityLimit_AlertSoundBear);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.dpQGP dpQGP(NinjaTrader.NinjaScript.Indicators.dpQGP.Modes currentMode, int trend1, int trend2, NinjaTrader.NinjaScript.Indicators.dpQGP.MATypes trendSmoothingType12, int trendSmoothingPeriod12, NinjaTrader.NinjaScript.Indicators.dpQGP.Marker_Types markerTypeA, int markerSizeA, int markerOffsetA, bool filterViaSlowPair, bool alerts_Crossing12, string alerts_Crossing12_AlertSoundBull, string alerts_Crossing12_AlertSoundBear, int trend3, int trend4, NinjaTrader.NinjaScript.Indicators.dpQGP.Marker_Types markerTypeB, int markerSizeB, int markerOffsetB, bool alerts_Crossing34, string alerts_Crossing34_AlertSoundBull, string alerts_Crossing34_AlertSoundBear, int continuationMaxBars, NinjaTrader.NinjaScript.Indicators.dpQGP.Marker_Types markerTypeContinuation, int markerSizeContinuation, int markerOffsetContinuation, bool proximityAlert1Bar_Enabled, bool proximityAlert2Bar_Enabled, double proximityAlert_ProximityLimit, double proximityAlert_ProximityReset, NinjaTrader.NinjaScript.Indicators.dpQGP.Marker_Types proximityLimit_MarkerType, int proximityLimit_MarkerSize, int proximityLimit_MarkerOffset, int proximityLimit_ATR_Period, bool alerts_ProximityLimit, string proximityLimit_AlertSoundBull, string proximityLimit_AlertSoundBear)
		{
			return indicator.dpQGP(Input, currentMode, trend1, trend2, trendSmoothingType12, trendSmoothingPeriod12, markerTypeA, markerSizeA, markerOffsetA, filterViaSlowPair, alerts_Crossing12, alerts_Crossing12_AlertSoundBull, alerts_Crossing12_AlertSoundBear, trend3, trend4, markerTypeB, markerSizeB, markerOffsetB, alerts_Crossing34, alerts_Crossing34_AlertSoundBull, alerts_Crossing34_AlertSoundBear, continuationMaxBars, markerTypeContinuation, markerSizeContinuation, markerOffsetContinuation, proximityAlert1Bar_Enabled, proximityAlert2Bar_Enabled, proximityAlert_ProximityLimit, proximityAlert_ProximityReset, proximityLimit_MarkerType, proximityLimit_MarkerSize, proximityLimit_MarkerOffset, proximityLimit_ATR_Period, alerts_ProximityLimit, proximityLimit_AlertSoundBull, proximityLimit_AlertSoundBear);
		}

		public Indicators.dpQGP dpQGP(ISeries<double> input , NinjaTrader.NinjaScript.Indicators.dpQGP.Modes currentMode, int trend1, int trend2, NinjaTrader.NinjaScript.Indicators.dpQGP.MATypes trendSmoothingType12, int trendSmoothingPeriod12, NinjaTrader.NinjaScript.Indicators.dpQGP.Marker_Types markerTypeA, int markerSizeA, int markerOffsetA, bool filterViaSlowPair, bool alerts_Crossing12, string alerts_Crossing12_AlertSoundBull, string alerts_Crossing12_AlertSoundBear, int trend3, int trend4, NinjaTrader.NinjaScript.Indicators.dpQGP.Marker_Types markerTypeB, int markerSizeB, int markerOffsetB, bool alerts_Crossing34, string alerts_Crossing34_AlertSoundBull, string alerts_Crossing34_AlertSoundBear, int continuationMaxBars, NinjaTrader.NinjaScript.Indicators.dpQGP.Marker_Types markerTypeContinuation, int markerSizeContinuation, int markerOffsetContinuation, bool proximityAlert1Bar_Enabled, bool proximityAlert2Bar_Enabled, double proximityAlert_ProximityLimit, double proximityAlert_ProximityReset, NinjaTrader.NinjaScript.Indicators.dpQGP.Marker_Types proximityLimit_MarkerType, int proximityLimit_MarkerSize, int proximityLimit_MarkerOffset, int proximityLimit_ATR_Period, bool alerts_ProximityLimit, string proximityLimit_AlertSoundBull, string proximityLimit_AlertSoundBear)
		{
			return indicator.dpQGP(input, currentMode, trend1, trend2, trendSmoothingType12, trendSmoothingPeriod12, markerTypeA, markerSizeA, markerOffsetA, filterViaSlowPair, alerts_Crossing12, alerts_Crossing12_AlertSoundBull, alerts_Crossing12_AlertSoundBear, trend3, trend4, markerTypeB, markerSizeB, markerOffsetB, alerts_Crossing34, alerts_Crossing34_AlertSoundBull, alerts_Crossing34_AlertSoundBear, continuationMaxBars, markerTypeContinuation, markerSizeContinuation, markerOffsetContinuation, proximityAlert1Bar_Enabled, proximityAlert2Bar_Enabled, proximityAlert_ProximityLimit, proximityAlert_ProximityReset, proximityLimit_MarkerType, proximityLimit_MarkerSize, proximityLimit_MarkerOffset, proximityLimit_ATR_Period, alerts_ProximityLimit, proximityLimit_AlertSoundBull, proximityLimit_AlertSoundBear);
		}
	}
}

#endregion
