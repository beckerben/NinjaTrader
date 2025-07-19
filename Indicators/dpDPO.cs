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
	public class dpDPO : Indicator
	{
		Series<double> priceSeries;
		
		public enum Marker_Types
		{
			Dot,
			Triangle,
			Arrow
		}
		
		public enum MA_Type
		{
			off,
			SMA,
			EMA
		}
		
		int latestSignalBar = 0;
		NinjaTrader.Gui.Tools.SimpleFont textFont = new NinjaTrader.Gui.Tools.SimpleFont("Tahoma",12){Bold=true};
		const string arialUpTriangle = @"▲", arialDownTriangle = @"▼", arialDot = @"●", wingdingsUpArrow = "é", wingdingsDownArrow = "ê";
		SimpleFont markerFont, plmarkerFont;
		string upMarker = "", downMarker = "";
		string plupMarker = "", pldownMarker = "";
		
		int currentbar = 0;
		bool ftob = false;
		
		dpXRAY xray;
		double dummy = 0;
		
		Series<int> signals;
		Series<double> dpoSeries;
		Series<bool> plSentinelUp, plSentinelDn;

		private Brush barUpFaint, barDownFaint;
		
		// INTERNAL VARIABLES
			private Style On,Off,Long,Short;		
			private Chart				 			 	ChartWindow;
			private System.Windows.Controls.Button		longButton  = null;	
			private System.Windows.Controls.Button		shortButton  = null;	
			private System.Windows.Controls.Button		srButton   = null;
			private bool								IsToolBarButtonAdded;

		public override void OnRenderTargetChanged()
		{
			barUpFaint = null;
			barDownFaint = null;
		}

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Detrended Price Oscillator. Converted from TOS by TradingCoders";
				Name										= "dpDPO";
				Calculate									= Calculate.OnPriceChange;
				IsOverlay									= false;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= true;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				ShowTransparentPlotsInDataBox = true;
				//Disable this property if your indicator requires custom values that cumulate with each new market data event. 
				//See Help Guide for additional information.
				IsSuspendedWhileInactive					= true;
				
				Length					= 14;
				Smoothing				= 1;
				
				ADX_Period				= 14;
				ADX_Threshold			= 20;
				
				UpColor = Brushes.Green;
				DownColor = Brushes.Crimson;
				WeakUpColor = Brushes.PaleGreen;
				WeakDownColor = Brushes.Pink;
				
				OutlineUpColor = Brushes.DarkGreen;
				OutlineDownColor = Brushes.DarkRed;
				
				BackColorCrossings = true;
				BackColorCrossingsAllPanels = true;
				CrossingUpStroke = new Stroke(Brushes.DarkGreen,DashStyleHelper.Dot,1){IsOpacityVisible=true};
				CrossingDownStroke = new Stroke(Brushes.DarkRed,DashStyleHelper.Dot,1){IsOpacityVisible=true};
				
				Sentinel1Bar_Enabled		= true;
				Sentinel2Bar_Enabled		= true;
				MarkerType				= Marker_Types.Triangle;
				MarkerSize				= 10;
				MarkerOffset			= 10;
				Sentinel_ProximityLimit		= 3.0;
				ProximityLimit_MarkerType				= Marker_Types.Dot;
				ProximityLimit_MarkerSize				= 10;
				ProximityLimit_MarkerOffset				= 4;
				ProximityLimit_UpColor = Brushes.Lime;
				ProximityLimit_DownColor = Brushes.DeepPink;
				
				UpperStroke = new Stroke(Brushes.LimeGreen,10){IsOpacityVisible=true,Opacity=40};
				LowerStroke = new Stroke(Brushes.Red,10){IsOpacityVisible=true,Opacity=40};
				LineOffset = 0.01;
				
				// additional signals combined with dpXRAY
				DPO_XRAY_ComboSignals_Enabled = false;
				DPO_XRAY_MAFilter = MA_Type.off;
				DPO_XRAY_MAFilter_Period = 50;
				DPO_XRAY_StrongUp = Brushes.Blue;
				DPO_XRAY_StrongDown = Brushes.DarkOrange;
				//DPO_XRAY_Opacity = 50;
				DPO_Rectified = false;
					
					DPO_XRAY_RSI_Period					= 22;
					DPO_XRAY_RSI_Smooth					= 1;
					DPO_XRAY_MACD_Fast					= 26;
					DPO_XRAY_MACD_Slow					= 52;
					DPO_XRAY_MACD_Smooth				= 9;
					DPO_XRAY_ADX_Period					= 14;
				
				Longs_Enabled = true;
				Shorts_Enabled = true;
				SR_Enabled = false;

				BarColoring = true;
				BarColorUp = Brushes.Lime;
				BarColorDown = Brushes.OrangeRed;
				
				// PLOTS
				AddPlot(new Stroke(Brushes.Gray, 1), PlotStyle.Line, "DPO");
				AddPlot(new Stroke(Brushes.Gray, 2), PlotStyle.Bar, "DPOHisto");;
				AddPlot(new Stroke(Brushes.Transparent, 1), PlotStyle.Dot, "Crossings");
				AddPlot(new Stroke(Brushes.Transparent, 1), PlotStyle.Dot, "ComboSignals");
				AddLine(Brushes.SaddleBrown, 0, "ZeroLine");
				
			}
			else if (State == State.Configure)
			{
				AddLine(UpperStroke,LineOffset,"Upper");
				AddLine(LowerStroke,-LineOffset,"Lower");
			}
			else if (State == State.DataLoaded)
			{
				priceSeries = new Series<double>(this,MaximumBarsLookBack.Infinite);
				dpoSeries = new Series<double>(this,MaximumBarsLookBack.Infinite);
				plSentinelUp = new Series<bool>(this,MaximumBarsLookBack.Infinite);
				plSentinelDn = new Series<bool>(this,MaximumBarsLookBack.Infinite);
				
				markerFont = new SimpleFont(MarkerType==Marker_Types.Arrow?"Wingdings":"Arial",MarkerSize){Bold=true};
				switch (MarkerType) 
				{
					case Marker_Types.Triangle:
						upMarker = arialUpTriangle;
						downMarker = arialDownTriangle;
						break;
					case Marker_Types.Arrow:
						upMarker = wingdingsUpArrow;
						downMarker = wingdingsDownArrow;
						break;
					default:
						upMarker = arialDot;
						downMarker = arialDot;
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
				
				if (DPO_XRAY_ComboSignals_Enabled)
					xray = dpXRAY(DPO_XRAY_RSI_Period,DPO_XRAY_RSI_Smooth,DPO_XRAY_MACD_Fast,DPO_XRAY_MACD_Slow,DPO_XRAY_MACD_Smooth,DPO_XRAY_ADX_Period,1);
				
				signals = new Series<int>(this,MaximumBarsLookBack.Infinite);
			}
			else if (State == State.Historical)
			{
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
			        	longButton = new System.Windows.Controls.Button();
						//Set Button Style - Predefined above					
						longButton.Style = Longs_Enabled ? Long : Off;							
						//Set Button Content
						longButton.Content = "L-"+(Longs_Enabled?"on ":"off");
						longButton.IsEnabled = true;
						//Set Button Click Routed Event Handler
			        	longButton.Click += new RoutedEventHandler(longButton_Click);
						//Set Button Alignment
						longButton.HorizontalAlignment = HorizontalAlignment.Left;
			
						//Close Button
			        	srButton = new System.Windows.Controls.Button();
						//Set Button Style - Predefined above
						srButton.Style = SR_Enabled ? On : Off;
						//Set Button Content
						srButton.Content = "DPO SR-"+(SR_Enabled?"on ":"off");
						srButton.IsEnabled = true;
						//Set Button Click Routed Event Handler
			        	srButton.Click += new RoutedEventHandler(srButton_Click);
						//Set Button Alignment
						srButton.HorizontalAlignment = HorizontalAlignment.Left;
											
						//Short Button
			        	shortButton = new System.Windows.Controls.Button();
						//Set Button Style - Predefined above
						shortButton.Style = Shorts_Enabled ? Short : Off;;
						//Set Button Content
						shortButton.Content = "S-"+(Shorts_Enabled?"on ":"off");
						shortButton.IsEnabled = true;
						//Set Button Click Routed Event Handler
			        	shortButton.Click += new RoutedEventHandler(shortButton_Click);
						//Set Button Alignment
						shortButton.HorizontalAlignment = HorizontalAlignment.Left;
						
						
						// Add Button to Indicator's Chart ToolBar
						ChartWindow.MainMenu.Add(srButton);
						ChartWindow.MainMenu.Add(longButton);
						ChartWindow.MainMenu.Add(shortButton);
						
						//Prevent Button From Displaying On Charts ToolBar when WorkSpace Opens if Not Active Tab
						longButton.Visibility = Visibility.Collapsed;
						shortButton.Visibility = Visibility.Collapsed;
						srButton.Visibility = Visibility.Collapsed;
						foreach (System.Windows.Controls.TabItem tab in this.ChartWindow.MainTabControl.Items)
							if ((tab.Content as ChartTab).ChartControl == this.ChartControl && tab == this.ChartWindow.MainTabControl.SelectedItem)
							{
								longButton.Visibility = Visibility.Visible;			
								shortButton.Visibility = Visibility.Visible;
								srButton.Visibility = Visibility.Visible;
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
				if(srButton != null)	srButton.Visibility = tmpCT.ChartControl == this.ChartControl ? Visibility.Visible : Visibility.Collapsed;
				if(longButton != null)	longButton.Visibility = tmpCT.ChartControl == this.ChartControl ? Visibility.Visible : Visibility.Collapsed;
				if(shortButton != null)	shortButton.Visibility = tmpCT.ChartControl == this.ChartControl ? Visibility.Visible : Visibility.Collapsed;
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
						if(srButton != null)
						{
							//Remove Button from Indicator's Chart ToolBar
							ChartWindow.MainMenu.Remove(srButton);
							//Remove Button Event Handler
							srButton.Click -= new RoutedEventHandler(srButton_Click);
							//Set Button to Null - Not Needed - Done out of Habit
							srButton = null;
						}
						if(longButton != null)
						{
							//Remove Button from Indicator's Chart ToolBar
							ChartWindow.MainMenu.Remove(longButton);
							//Remove Button Event Handler
							longButton.Click -= new RoutedEventHandler(longButton_Click);
							//Set Button to Null - Not Needed - Done out of Habit
							longButton = null;
						}
						if(shortButton != null)
						{
							//Remove Button from Indicator's Chart ToolBar
							ChartWindow.MainMenu.Remove(shortButton);
							//Remove Button Event Handler
							shortButton.Click -= new RoutedEventHandler(shortButton_Click);
							//Set Button to Null - Not Needed - Done out of Habit
							shortButton = null;
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
		private void longButton_Click(object sender, RoutedEventArgs e)
		{
			// code here
			Longs_Enabled = !Longs_Enabled;
			Print("LongButton Click to "+Longs_Enabled);
			
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
		
		private void shortButton_Click(object sender, RoutedEventArgs e)
		{
			// code here
			Shorts_Enabled = !Shorts_Enabled;
			Print("ShortButton Click to "+Shorts_Enabled);
			
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
		
		
		private void srButton_Click(object sender, RoutedEventArgs e)
		{
			SR_Enabled = !SR_Enabled;
			Print("SRButton Click to "+SR_Enabled);	
			
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
						longButton.Style = Longs_Enabled ? Long : Off;							
						//Set Button Content
						longButton.Content = "L-"+(Longs_Enabled?"on ":"off");
						longButton.InvalidateVisual();
				
						shortButton.Style = Shorts_Enabled ? Short : Off;
						//Set Button Content
						shortButton.Content = "S-"+(Shorts_Enabled?"on ":"off");
						shortButton.InvalidateVisual();
				
						srButton.Style = SR_Enabled ? On : Off;
						//Set Button Content
						srButton.Content = "DPO SR-"+(SR_Enabled?"on ":"off");
						srButton.InvalidateVisual();
			}));
		}
		
		private void RepaintIndicator()
		{
			
			try
			{
				// manually remove all vertical lines
				for (int b = CurrentBars[0]-10; b >=0; b--)
				{
					if (BackColorCrossingsAllPanels)
						RemoveDrawObject("VLineChart"+(CurrentBar-b-1));
					RemoveDrawObject("VLinePanel"+(CurrentBar-b-1));
				}
				
				// recalculate vertical lines
				DoCrossingAndSentinels(CurrentBars[0]-10);
			}
			catch (Exception ex)
			{
				throw new Exception(ex.ToString());
			}
			
			ForceRefresh();
		}
		
		#endregion		
		
		protected override void OnBarUpdate()
		{
			if (CurrentBar < Length / 2 + 1)
				priceSeries[0] = Input[CurrentBar];
			else 
				priceSeries[0] = Input[Length / 2 + 1]; //price[length / 2 + 1],
			dpoSeries[0] = SMA(Input,Smoothing)[0] - SMA(priceSeries, Length)[0]; 
			DPO_Plot[0] = DPO_Rectified ? Math.Abs(dpoSeries[0]) : dpoSeries[0];
			DPO_Histo[0] = DPO_Plot[0];
			
			PlotBrushes[0][0] = dpoSeries[0] >= 0 ? UpColor : DownColor; // original; above below midline

			if (BarColoring)
			{
				if (barUpFaint == null)
				{
					barUpFaint = BarColorUp.Clone();
					barUpFaint.Opacity = 0.6;
					barUpFaint.Freeze();
					barDownFaint = BarColorDown.Clone();
					barDownFaint.Opacity = 0.6;
					barDownFaint.Freeze();
				}
				if (dpoSeries[0] >= 0)
				{
					BarBrush = Close[0] > Open[0] ? BarColorUp : barUpFaint;
					CandleOutlineBrush = Close[0] > Open[0] ? BarColorUp : null;
				}
				else
				{
					BarBrush = Close[0] < Open[0] ? BarColorDown : barDownFaint;
					CandleOutlineBrush = Close[0] < Open[0] ? BarColorDown : null;
				}
			}
			
			if (CurrentBar < ADX_Period)
				return;
			
			bool ADXRising = ADX(ADX_Period)[0] >= ADX(ADX_Period)[1] && ADX(ADX_Period)[0] >= ADX_Threshold;;
			PlotBrushes[1][0] = ADXRising ? (dpoSeries[0] >= 0 ? UpColor : DownColor) : (dpoSeries[0] >= 0 ? WeakUpColor : WeakDownColor);
			signals[0] = ADXRising ? (dpoSeries[0] >= 0 ? +2 : -2) : (dpoSeries[0] >= 0 ? +1 : -1); // record the histo 
			
			ftob = currentbar < CurrentBar;
			currentbar = Math.Max(currentbar,CurrentBar);
			if (ftob || IsFirstTickOfBar)
				latestSignalBar = 0;
			
			if (CurrentBar > Length)
			{
				// precalculate ProximityLimit sentinels
				plSentinelUp[0] = Sentinel_ProximityLimit > 0
								&&	(dpoSeries[0] > 0 + Sentinel_ProximityLimit
									&&	(	(dpoSeries[1] < 0 + Sentinel_ProximityLimit && dpoSeries[2] > 0)
										|| 	(dpoSeries[1] == 0 + Sentinel_ProximityLimit && dpoSeries[2] < 0 + Sentinel_ProximityLimit && dpoSeries[3] > 0)
										)
									&&	BeyondProximityLimitFirst(Sentinel_ProximityLimit,1)
								//&&	(dpoSeries[1] < dpoSeries[2] && dpoSeries[1] >= 0 && dpoSeries[1] <= Sentinel_ProximityLimit && dpoSeries[0] > 0 // DP's formula
									);
				plSentinelDn[0] = Sentinel_ProximityLimit > 0
								&&	(dpoSeries[0] < 0 - Sentinel_ProximityLimit
									&&	(	(dpoSeries[1] > 0 - Sentinel_ProximityLimit && dpoSeries[2] < 0)
										|| 	(dpoSeries[1] == 0 - Sentinel_ProximityLimit && dpoSeries[2] > 0 - Sentinel_ProximityLimit && dpoSeries[3] < 0)
										)
									&&	BeyondProximityLimitFirst(-Sentinel_ProximityLimit,1)
								//&&	(dpoSeries[1] > dpoSeries[2] && dpoSeries[1] <= 0 && dpoSeries[1] <= -Sentinel_ProximityLimit && dpoSeries[0] < 0 // DP's formula
									);
				
				DoCrossingAndSentinels(0);
			}
			
			// --------------------------------------------------------
			
			if (DPO_XRAY_ComboSignals_Enabled)
			{
				xray.Update();
				dummy = xray[0];
				
				if (signals[0] == +2 && xray.Signals[0] == +2
					&& (DPO_XRAY_MAFilter == MA_Type.off || (DPO_XRAY_MAFilter==MA_Type.SMA ? SMA(DPO_XRAY_MAFilter_Period)[0] : EMA(DPO_XRAY_MAFilter_Period)[0]) < Close[0])
					)
				{
					//NinjaTrader.NinjaScript.DrawingTools.Rectangle r = Draw.Rectangle(this,"Rec"+CurrentBar,false,1,High[0],-1,Low[0],Brushes.Transparent,DPO_XRAY_StrongUp,DPO_XRAY_Opacity,true);
					//r.ZOrderType = DrawingToolZOrder.AlwaysDrawnFirst;
					CandleOutlineBrush = DPO_XRAY_StrongUp;
					ComboSignals[0] = +1;
				}
				else
				if (signals[0] == -2 && xray.Signals[0] == -2
					&& (DPO_XRAY_MAFilter == MA_Type.off || (DPO_XRAY_MAFilter==MA_Type.SMA ? SMA(DPO_XRAY_MAFilter_Period)[0] : EMA(DPO_XRAY_MAFilter_Period)[0]) > Close[0])
					)
				{
					//NinjaTrader.NinjaScript.DrawingTools.Rectangle r = Draw.Rectangle(this,"Rec"+CurrentBar,false,1,High[0],-1,Low[0],Brushes.Transparent,DPO_XRAY_StrongDown,DPO_XRAY_Opacity,true);
					//r.ZOrderType = DrawingToolZOrder.AlwaysDrawnFirst;
					CandleOutlineBrush = DPO_XRAY_StrongDown;
					ComboSignals[0] = -1;
				}
				else
				{
					//RemoveDrawObject("Rec"+CurrentBar);
					CandleOutlineBrush = null;
					ComboSignals[0] = 0;
				}
			}
		}
		
		// ============================================================================================================================================
		
		private void DoCrossingAndSentinels(int barsBack)
		{
			for (int b = barsBack; b >= 0; b--)
			{
				bool crossingUP = dpoSeries[b+0] > 0
					&&	(dpoSeries[b+1] < 0
						|| (dpoSeries[b+1] == 0 && dpoSeries[b+2] < 0)
						);
				bool crossingDOWN = dpoSeries[b+0] < 0
					&&	(dpoSeries[b+1] > 0
						|| (dpoSeries[b+1] == 0 && dpoSeries[b+2] > 0)
						);
				
				if (barsBack > 0) Print("Recalulating barago "+b+", crossingUp="+crossingUP+", crossingDOWN="+crossingDOWN+", sentinelUP="+plSentinelUp[b]+", sentinelDN="+plSentinelDn[b]);//CHECKER

				if (	crossingUP || plSentinelUp[b]
					)
				{					
					if (Longs_Enabled)
					{
						if (BackColorCrossings && crossingUP
						&& (DPO_XRAY_MAFilter == MA_Type.off || (DPO_XRAY_MAFilter==MA_Type.SMA ? SMA(DPO_XRAY_MAFilter_Period)[b+0] : EMA(DPO_XRAY_MAFilter_Period)[b+0]) < Close[b+0])
							)
						{
							if (barsBack > 0) Print("At barsBack "+b+" verticalLine UP"); // CHECKER
							if (BackColorCrossingsAllPanels)
								Draw.VerticalLine(this,"VLineChart"+(CurrentBar-b),b,CrossingUpStroke.Brush,CrossingUpStroke.DashStyleHelper,(int)CrossingUpStroke.Width,true);
							
							Draw.VerticalLine(this,"VLinePanel"+(CurrentBar-b),b,CrossingUpStroke.Brush,CrossingUpStroke.DashStyleHelper,(int)CrossingUpStroke.Width,false);
								
							if (SR_Enabled)
							{
								// if they exist remove bearish lines from the prior bar
								if (BackColorCrossingsAllPanels)
									RemoveDrawObject("VLineChart"+(CurrentBar-b-1));
								RemoveDrawObject("VLinePanel"+(CurrentBar-b-1));
							}
						}
						Crossings[b+0] = 1;
						
						// SENTINELS
						if (barsBack == 0) // do NOT repaint at all, realtime only
						{
							int barsSincePriorCross = Crossings[b+1] == -1 ? 1 : (Crossings[b+2] == -1 ? 2 : 0);
							if (!crossingUP && plSentinelUp[b])
								barsSincePriorCross = 1;
							
							if ( 	(barsSincePriorCross == 1 && Sentinel1Bar_Enabled) 
								||	(barsSincePriorCross == 2 && Sentinel2Bar_Enabled)
								)
							{
								//if (barsBack > 0) Print("At barsBack "+b+" Text UP"); // CHECKER
								// mark on chart	
									DrawOnPricePanel = true;
									latestSignalBar = CurrentBar - (barsSincePriorCross==1?1:(Low[b+1]<Low[b+2]?1:2));
									int az = CurrentBar - b - latestSignalBar;
									if (crossingUP)
										Draw.Text(this,"SentinelUp"+(latestSignalBar),true,upMarker,
											az,Low[az]-ATR(10)[az]*MarkerOffset*0.1,0,UpColor,markerFont,TextAlignment.Center,Brushes.Transparent,Brushes.Transparent,0);	
									else if (plSentinelUp[b])
										Draw.Text(this,"SentinelUp"+(latestSignalBar),true,plupMarker,
											az,Low[az]-ATR(10)[az]*ProximityLimit_MarkerOffset*0.1,0,ProximityLimit_UpColor,plmarkerFont,TextAlignment.Center,Brushes.Transparent,Brushes.Transparent,0);	
									DrawOnPricePanel = false;
							}
						}
					}
				}
				else
				if (	crossingDOWN || plSentinelDn[b] 
					)
				{			
					if (Shorts_Enabled)
					{
						if (BackColorCrossings && crossingDOWN
						&& (DPO_XRAY_MAFilter == MA_Type.off || (DPO_XRAY_MAFilter==MA_Type.SMA ? SMA(DPO_XRAY_MAFilter_Period)[b+0] : EMA(DPO_XRAY_MAFilter_Period)[b+0]) > Close[b+0])
							)
						{
							if (barsBack > 0) Print("At barsBack "+b+" verticalLine DN"); // CHECKER
							if (BackColorCrossingsAllPanels)
								Draw.VerticalLine(this,"VLineChart"+(CurrentBar-b),b,CrossingDownStroke.Brush,CrossingDownStroke.DashStyleHelper,(int)CrossingDownStroke.Width,true);
							
							Draw.VerticalLine(this,"VLinePanel"+(CurrentBar-b),b,CrossingDownStroke.Brush,CrossingDownStroke.DashStyleHelper,(int)CrossingDownStroke.Width,false);
							
							if (SR_Enabled)
							{
								// if they exist remove bearish lines from the prior bar
								if (BackColorCrossingsAllPanels)
									RemoveDrawObject("VLineChart"+(CurrentBar-b-1));
								RemoveDrawObject("VLinePanel"+(CurrentBar-b-1));
							}
						}
						Crossings[b+0] = -1;
						
						// SENTINELS
						if (barsBack == 0) // do NOT repaint at all, realtime only
						{
							int barsSincePriorCross = Crossings[b+1] == 1 ? 1 : (Crossings[b+2] == 1 ? 2 : 0);
							if (!crossingDOWN && plSentinelDn[b])
								barsSincePriorCross = 1;
							
							if ( 	(barsSincePriorCross == 1 && Sentinel1Bar_Enabled) 
								||	(barsSincePriorCross == 2 && Sentinel2Bar_Enabled)
								)
							{
								//if (barsBack > 0) Print("At barsBack "+b+" Text DN"); // CHECKER
								// mark on chart	
									DrawOnPricePanel = true;
									latestSignalBar = CurrentBar - (barsSincePriorCross==1?1:(High[1]>High[2]?1:2));
									int az = CurrentBar - b - latestSignalBar;
									latestSignalBar = -latestSignalBar;
									if (crossingDOWN)
										Draw.Text(this,"SentinelDn"+(latestSignalBar),true,downMarker,
											az,High[az]+ATR(10)[az]*MarkerOffset*0.1,0,DownColor,markerFont,TextAlignment.Center,Brushes.Transparent,Brushes.Transparent,0);	
									else if (plSentinelDn[b])
										Draw.Text(this,"SentinelDn"+(latestSignalBar),true,pldownMarker,
											az,High[az]+ATR(10)[az]*ProximityLimit_MarkerOffset*0.1,0,ProximityLimit_DownColor,plmarkerFont,TextAlignment.Center,Brushes.Transparent,Brushes.Transparent,0);	
									DrawOnPricePanel = false;
							}
						}
					}
				}
				else
				{
					if (BackColorCrossingsAllPanels)
						RemoveDrawObject("VLineChart"+(CurrentBar-b));
					
					RemoveDrawObject("VLinePanel"+(CurrentBar-b));
					Crossings[b+0] = 0;
					
					if (latestSignalBar > 0)
						RemoveDrawObject("SentinelUp"+(latestSignalBar));
					else if (latestSignalBar < 0)
						RemoveDrawObject("SentinelDn"+(latestSignalBar));
					
				}
			}
		}
		
		private bool BeyondProximityLimitFirst(double value, int barsAgo)
		{
			for (int b = barsAgo; b < CurrentBar; b++)
			{
				if (value > 0)
				{
					if (dpoSeries[b] > value)
						return true;
					if (dpoSeries[b] < 0)
						return false;
				}
				if (value < 0)
				{
					if (dpoSeries[b] < value)
						return true;
					if (dpoSeries[b] > 0)
						return false;
				}
			}
			
			return false;
		}
				
		public override string DisplayName
		{
			get { string baseName = base.DisplayName;
				if (ChartControl != null && baseName.Length > 30 && Name!="" && !string.IsNullOrWhiteSpace(Name)) // looks to be default
					return Name;
				else
					return baseName; // maybe a custom label entered by user 
				}
		}
				

		#region Properties
		
	
		//[NinjaScriptProperty]
		[Display(Name="Longs_Enabled", Description="", Order=10, GroupName="Options")]
		public bool Longs_Enabled
		{ get; set; }
		
		//[NinjaScriptProperty]
		[Display(Name="Shorts_Enabled", Description="", Order=20, GroupName="Options")]
		public bool Shorts_Enabled
		{ get; set; }
		
		//[NinjaScriptProperty]
		[Display(Name="SR_Enabled", Description="", Order=30, GroupName="Options")]
		public bool SR_Enabled
		{ get; set; }
		
		//
		

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Length", Order=2, GroupName="Parameters")]
		public int Length
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Smoothing", Order=4, GroupName="Parameters")]
		public int Smoothing
		{ get; set; }
		

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="ADX_Period", Order=6, GroupName="Parameters")]
		public int ADX_Period
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, double.MaxValue)]
		[Display(Name="ADX_Threshold", Order=7, GroupName="Parameters")]
		public double ADX_Threshold
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, double.MaxValue)]
		[Display(Name="LineOffset", Order=8, GroupName="Parameters")]
		public double LineOffset
		{ get; set; }
		
		
		//[NinjaScriptProperty]
		[Display(Name="UpperStroke", Order=10, GroupName="Parameters")]
		public Stroke UpperStroke
		{ get; set; }
		
		//[NinjaScriptProperty]
		[Display(Name="LowerStroke", Order=12, GroupName="Parameters")]
		public Stroke LowerStroke
		{ get; set; }
		
		[XmlIgnore()]
		//[NinjaScriptProperty]
		[Display(Name="UpColor", Description="Color for uptrend", Order=10, GroupName="Visuals")]
        public Brush UpColor
		{ get; set; }
		
		// Serialize our Color object
		[Browsable(false)]
		public string UpColorSerialize
		{
			get { return Serialize.BrushToString(UpColor); }
			set { UpColor = Serialize.StringToBrush(value); }
		}
		
		[XmlIgnore()]
		//[NinjaScriptProperty]
		[Display(Name="DownColor", Description="Color for downtrend", Order=20, GroupName="Visuals")]
        public Brush DownColor
		{ get; set; }
		
		// Serialize our Color object
		[Browsable(false)]
		public string DownColorSerialize
		{
			get { return Serialize.BrushToString(DownColor); }
			set { DownColor = Serialize.StringToBrush(value); }
		}

		[XmlIgnore()]
		//[NinjaScriptProperty]
		[Display(Name="WeakUpColor", Description="Color for Weak uptrend", Order=15, GroupName="Visuals")]
        public Brush WeakUpColor
		{ get; set; }
		
		// Serialize our Color object
		[Browsable(false)]
		public string WeakUpColorSerialize
		{
			get { return Serialize.BrushToString(WeakUpColor); }
			set { WeakUpColor = Serialize.StringToBrush(value); }
		}
		
		[XmlIgnore()]
		//[NinjaScriptProperty]
		[Display(Name="WeakDownColor", Description="Color for Weak downtrend", Order=25, GroupName="Visuals")]
        public Brush WeakDownColor
		{ get; set; }
		
		// Serialize our Color object
		[Browsable(false)]
		public string WeakDownColorSerialize
		{
			get { return Serialize.BrushToString(WeakDownColor); }
			set { WeakDownColor = Serialize.StringToBrush(value); }
		}

		
		[XmlIgnore()]
		//[NinjaScriptProperty]
		[Display(Name="OutlineUpColor", Description="Color for uptrend", Order=30, GroupName="Visuals")]
        public Brush OutlineUpColor
		{ get; set; }
		
		// Serialize our Color object
		[Browsable(false)]
		public string OutlineUpColorSerialize
		{
			get { return Serialize.BrushToString(OutlineUpColor); }
			set { OutlineUpColor = Serialize.StringToBrush(value); }
		}
		
		[XmlIgnore()]
		//[NinjaScriptProperty]
		[Display(Name="OutlineDownColor", Description="Color for downtrend", Order=40, GroupName="Visuals")]
        public Brush OutlineDownColor
		{ get; set; }
		
		// Serialize our Color object
		[Browsable(false)]
		public string OutlineDownColorSerialize
		{
			get { return Serialize.BrushToString(OutlineDownColor); }
			set { OutlineDownColor = Serialize.StringToBrush(value); }
		}
		//
		
		
		[NinjaScriptProperty]
		[Display(Name="BackColorCrossings", Order=200, GroupName="Visuals")]
		public bool BackColorCrossings
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="BackColorCrossings show on all panels", Order=205, GroupName="Visuals")]
		public bool BackColorCrossingsAllPanels
		{ get; set; }
		
		//[XmlIgnore()]
		//[NinjaScriptProperty]
		[Display(Name="CrossingUp Stroke", Description="parameters for Crossing up", Order=230, GroupName="Visuals")]
        public Stroke CrossingUpStroke
		{ get; set; }
		
		
		//[XmlIgnore()]
		//[NinjaScriptProperty]
		[Display(Name="CrossingDown Stroke", Description="parameters for Crossing down", Order=240, GroupName="Visuals")]
        public Stroke CrossingDownStroke
		{ get; set; }
		
		
		//[NinjaScriptProperty]
		[Display(Name="DPO_Rectified", Order=310, GroupName="Visuals")]
		public bool DPO_Rectified
		{ get; set; }
		
		//
		
		
		
		[NinjaScriptProperty]
		[Display(Name="Sentinel 1Bar Enabled", Order=10, GroupName="Sentinel")]
		public bool Sentinel1Bar_Enabled
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Sentinel 2Bar Enabled", Order=20, GroupName="Sentinel")]
		public bool Sentinel2Bar_Enabled
		{ get; set; }
		
		
		[NinjaScriptProperty]
		[Display(Name="MarkerType", Order=20, GroupName="Sentinel")]
		public NinjaTrader.NinjaScript.Indicators.dpDPO.Marker_Types MarkerType
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1,100)]
		[Display(Name="MarkerSize", Order=30, GroupName="Sentinel")]
		public int MarkerSize
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0,100)]
		[Display(Name="MarkerOffset", Order=40, GroupName="Sentinel")]
		public int MarkerOffset
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0,double.MaxValue)]
		[Display(Name="Sentinel ProximityLimit", Order=100, GroupName="Sentinel")]
		public double Sentinel_ProximityLimit
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="ProximityLimit MarkerType", Order=110, GroupName="Sentinel")]
		public NinjaTrader.NinjaScript.Indicators.dpDPO.Marker_Types ProximityLimit_MarkerType
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1,100)]
		[Display(Name="ProximityLimit MarkerSize", Order=120, GroupName="Sentinel")]
		public int ProximityLimit_MarkerSize
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0,100)]
		[Display(Name="ProximityLimit MarkerOffset", Order=130, GroupName="Sentinel")]
		public int ProximityLimit_MarkerOffset
		{ get; set; }
		
		[XmlIgnore()]
		//[NinjaScriptProperty]
		[Display(Name="ProximityLimit UpColor", Description="Color for uptrend", Order=140, GroupName="Sentinel")]
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
		[Display(Name="ProximityLimit DownColor", Description="Color for downtrend", Order=150, GroupName="Sentinel")]
        public Brush ProximityLimit_DownColor
		{ get; set; }
		
		// Serialize our Color object
		[Browsable(false)]
		public string ProximityLimit_DownColorSerialize
		{
			get { return Serialize.BrushToString(ProximityLimit_DownColor); }
			set { ProximityLimit_DownColor = Serialize.StringToBrush(value); }
		}

		//
		
		[NinjaScriptProperty]
		[Display(Name="ComboSignals_Enabled", Order=1, GroupName="DPO_XRAY ComboSignals")]
		public bool DPO_XRAY_ComboSignals_Enabled
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="MAFilter", Order=3, GroupName="DPO_XRAY ComboSignals")]
		public NinjaTrader.NinjaScript.Indicators.dpDPO.MA_Type DPO_XRAY_MAFilter
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="MAFilter_Period", Order=5, GroupName="DPO_XRAY ComboSignals")]
		public int DPO_XRAY_MAFilter_Period
		{ get; set; }
		
		[XmlIgnore()]
		//[NinjaScriptProperty]
		[Display(Name="StrongUp", Description="Color for up", Order=7, GroupName="DPO_XRAY ComboSignals")]
        public Brush DPO_XRAY_StrongUp
		{ get; set; }
		
		// Serialize our Color object
		[Browsable(false)]
		public string DPO_XRAY_StrongUpSerialize
		{
			get { return Serialize.BrushToString(DPO_XRAY_StrongUp); }
			set { DPO_XRAY_StrongUp = Serialize.StringToBrush(value); }
		}
		
		[XmlIgnore()]
		//[NinjaScriptProperty]
		[Display(Name="StrongDown", Description="Color for down", Order=8, GroupName="DPO_XRAY ComboSignals")]
        public Brush DPO_XRAY_StrongDown
		{ get; set; }
		
		// Serialize our Color object
		[Browsable(false)]
		public string DPO_XRAY_StrongDownSerialize
		{
			get { return Serialize.BrushToString(DPO_XRAY_StrongDown); }
			set { DPO_XRAY_StrongDown = Serialize.StringToBrush(value); }
		}
		
//		[NinjaScriptProperty]
//		[Range(0, 100)]
//		[Display(Name="Opacity", Order=9, GroupName="DPO_XRAY ComboSignals")]
//		public int DPO_XRAY_Opacity
//		{ get; set; }
		
		//
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="RSI_Period", Order=10, GroupName="DPO_XRAY ComboSignals")]
		public int DPO_XRAY_RSI_Period
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="RSI_Smooth", Order=20, GroupName="DPO_XRAY ComboSignals")]
		public int DPO_XRAY_RSI_Smooth
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="MACD_Fast", Order=30, GroupName="DPO_XRAY ComboSignals")]
		public int DPO_XRAY_MACD_Fast
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="MACD_Slow", Order=40, GroupName="DPO_XRAY ComboSignals")]
		public int DPO_XRAY_MACD_Slow
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="MACD_Smooth", Order=50, GroupName="DPO_XRAY ComboSignals")]
		public int DPO_XRAY_MACD_Smooth
		{ get; set; }
		
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="ADX_Period", Order=6, GroupName="DPO_XRAY ComboSignals")]
		public int DPO_XRAY_ADX_Period
		{ get; set; }

		//

		//[NinjaScriptProperty]
		[Display(Name="BarColoring", Order=10, GroupName="BarColoring")]
		public bool BarColoring
		{ get; set; }

		[XmlIgnore()]
		//[NinjaScriptProperty]
		[Display(Name="BarColorUp", Description="Color for uptrend", Order=10, GroupName="BarColoring")]
		public Brush BarColorUp
		{ get; set; }

		// Serialize our Color object
		[Browsable(false)]
		public string BarColorUpSerialize
		{
			get { return Serialize.BrushToString(BarColorUp); }
			set { BarColorUp = Serialize.StringToBrush(value); }
		}

		[XmlIgnore()]
		//[NinjaScriptProperty]
		[Display(Name="BarColorDown", Description="Color for downtrend", Order=20, GroupName="BarColoring")]
		public Brush BarColorDown
		{ get; set; }

		// Serialize our Color object
		[Browsable(false)]
		public string BarColorDownSerialize
		{
			get { return Serialize.BrushToString(BarColorDown); }
			set { BarColorDown = Serialize.StringToBrush(value); }
		}

		// =============
		
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> DPO_Plot
		{
			get { return Values[0]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> DPO_Histo
		{
			get { return Values[1]; }
		}
		
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> Crossings
		{
			get { return Values[2]; }
		}
		
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> ComboSignals
		{
			get { return Values[3]; }
		}
		#endregion

	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private dpDPO[] cachedpDPO;
		public dpDPO dpDPO(int length, int smoothing, int aDX_Period, double aDX_Threshold, double lineOffset, bool backColorCrossings, bool backColorCrossingsAllPanels, bool sentinel1Bar_Enabled, bool sentinel2Bar_Enabled, NinjaTrader.NinjaScript.Indicators.dpDPO.Marker_Types markerType, int markerSize, int markerOffset, double sentinel_ProximityLimit, NinjaTrader.NinjaScript.Indicators.dpDPO.Marker_Types proximityLimit_MarkerType, int proximityLimit_MarkerSize, int proximityLimit_MarkerOffset, bool dPO_XRAY_ComboSignals_Enabled, NinjaTrader.NinjaScript.Indicators.dpDPO.MA_Type dPO_XRAY_MAFilter, int dPO_XRAY_MAFilter_Period, int dPO_XRAY_RSI_Period, int dPO_XRAY_RSI_Smooth, int dPO_XRAY_MACD_Fast, int dPO_XRAY_MACD_Slow, int dPO_XRAY_MACD_Smooth, int dPO_XRAY_ADX_Period)
		{
			return dpDPO(Input, length, smoothing, aDX_Period, aDX_Threshold, lineOffset, backColorCrossings, backColorCrossingsAllPanels, sentinel1Bar_Enabled, sentinel2Bar_Enabled, markerType, markerSize, markerOffset, sentinel_ProximityLimit, proximityLimit_MarkerType, proximityLimit_MarkerSize, proximityLimit_MarkerOffset, dPO_XRAY_ComboSignals_Enabled, dPO_XRAY_MAFilter, dPO_XRAY_MAFilter_Period, dPO_XRAY_RSI_Period, dPO_XRAY_RSI_Smooth, dPO_XRAY_MACD_Fast, dPO_XRAY_MACD_Slow, dPO_XRAY_MACD_Smooth, dPO_XRAY_ADX_Period);
		}

		public dpDPO dpDPO(ISeries<double> input, int length, int smoothing, int aDX_Period, double aDX_Threshold, double lineOffset, bool backColorCrossings, bool backColorCrossingsAllPanels, bool sentinel1Bar_Enabled, bool sentinel2Bar_Enabled, NinjaTrader.NinjaScript.Indicators.dpDPO.Marker_Types markerType, int markerSize, int markerOffset, double sentinel_ProximityLimit, NinjaTrader.NinjaScript.Indicators.dpDPO.Marker_Types proximityLimit_MarkerType, int proximityLimit_MarkerSize, int proximityLimit_MarkerOffset, bool dPO_XRAY_ComboSignals_Enabled, NinjaTrader.NinjaScript.Indicators.dpDPO.MA_Type dPO_XRAY_MAFilter, int dPO_XRAY_MAFilter_Period, int dPO_XRAY_RSI_Period, int dPO_XRAY_RSI_Smooth, int dPO_XRAY_MACD_Fast, int dPO_XRAY_MACD_Slow, int dPO_XRAY_MACD_Smooth, int dPO_XRAY_ADX_Period)
		{
			if (cachedpDPO != null)
				for (int idx = 0; idx < cachedpDPO.Length; idx++)
					if (cachedpDPO[idx] != null && cachedpDPO[idx].Length == length && cachedpDPO[idx].Smoothing == smoothing && cachedpDPO[idx].ADX_Period == aDX_Period && cachedpDPO[idx].ADX_Threshold == aDX_Threshold && cachedpDPO[idx].LineOffset == lineOffset && cachedpDPO[idx].BackColorCrossings == backColorCrossings && cachedpDPO[idx].BackColorCrossingsAllPanels == backColorCrossingsAllPanels && cachedpDPO[idx].Sentinel1Bar_Enabled == sentinel1Bar_Enabled && cachedpDPO[idx].Sentinel2Bar_Enabled == sentinel2Bar_Enabled && cachedpDPO[idx].MarkerType == markerType && cachedpDPO[idx].MarkerSize == markerSize && cachedpDPO[idx].MarkerOffset == markerOffset && cachedpDPO[idx].Sentinel_ProximityLimit == sentinel_ProximityLimit && cachedpDPO[idx].ProximityLimit_MarkerType == proximityLimit_MarkerType && cachedpDPO[idx].ProximityLimit_MarkerSize == proximityLimit_MarkerSize && cachedpDPO[idx].ProximityLimit_MarkerOffset == proximityLimit_MarkerOffset && cachedpDPO[idx].DPO_XRAY_ComboSignals_Enabled == dPO_XRAY_ComboSignals_Enabled && cachedpDPO[idx].DPO_XRAY_MAFilter == dPO_XRAY_MAFilter && cachedpDPO[idx].DPO_XRAY_MAFilter_Period == dPO_XRAY_MAFilter_Period && cachedpDPO[idx].DPO_XRAY_RSI_Period == dPO_XRAY_RSI_Period && cachedpDPO[idx].DPO_XRAY_RSI_Smooth == dPO_XRAY_RSI_Smooth && cachedpDPO[idx].DPO_XRAY_MACD_Fast == dPO_XRAY_MACD_Fast && cachedpDPO[idx].DPO_XRAY_MACD_Slow == dPO_XRAY_MACD_Slow && cachedpDPO[idx].DPO_XRAY_MACD_Smooth == dPO_XRAY_MACD_Smooth && cachedpDPO[idx].DPO_XRAY_ADX_Period == dPO_XRAY_ADX_Period && cachedpDPO[idx].EqualsInput(input))
						return cachedpDPO[idx];
			return CacheIndicator<dpDPO>(new dpDPO(){ Length = length, Smoothing = smoothing, ADX_Period = aDX_Period, ADX_Threshold = aDX_Threshold, LineOffset = lineOffset, BackColorCrossings = backColorCrossings, BackColorCrossingsAllPanels = backColorCrossingsAllPanels, Sentinel1Bar_Enabled = sentinel1Bar_Enabled, Sentinel2Bar_Enabled = sentinel2Bar_Enabled, MarkerType = markerType, MarkerSize = markerSize, MarkerOffset = markerOffset, Sentinel_ProximityLimit = sentinel_ProximityLimit, ProximityLimit_MarkerType = proximityLimit_MarkerType, ProximityLimit_MarkerSize = proximityLimit_MarkerSize, ProximityLimit_MarkerOffset = proximityLimit_MarkerOffset, DPO_XRAY_ComboSignals_Enabled = dPO_XRAY_ComboSignals_Enabled, DPO_XRAY_MAFilter = dPO_XRAY_MAFilter, DPO_XRAY_MAFilter_Period = dPO_XRAY_MAFilter_Period, DPO_XRAY_RSI_Period = dPO_XRAY_RSI_Period, DPO_XRAY_RSI_Smooth = dPO_XRAY_RSI_Smooth, DPO_XRAY_MACD_Fast = dPO_XRAY_MACD_Fast, DPO_XRAY_MACD_Slow = dPO_XRAY_MACD_Slow, DPO_XRAY_MACD_Smooth = dPO_XRAY_MACD_Smooth, DPO_XRAY_ADX_Period = dPO_XRAY_ADX_Period }, input, ref cachedpDPO);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.dpDPO dpDPO(int length, int smoothing, int aDX_Period, double aDX_Threshold, double lineOffset, bool backColorCrossings, bool backColorCrossingsAllPanels, bool sentinel1Bar_Enabled, bool sentinel2Bar_Enabled, NinjaTrader.NinjaScript.Indicators.dpDPO.Marker_Types markerType, int markerSize, int markerOffset, double sentinel_ProximityLimit, NinjaTrader.NinjaScript.Indicators.dpDPO.Marker_Types proximityLimit_MarkerType, int proximityLimit_MarkerSize, int proximityLimit_MarkerOffset, bool dPO_XRAY_ComboSignals_Enabled, NinjaTrader.NinjaScript.Indicators.dpDPO.MA_Type dPO_XRAY_MAFilter, int dPO_XRAY_MAFilter_Period, int dPO_XRAY_RSI_Period, int dPO_XRAY_RSI_Smooth, int dPO_XRAY_MACD_Fast, int dPO_XRAY_MACD_Slow, int dPO_XRAY_MACD_Smooth, int dPO_XRAY_ADX_Period)
		{
			return indicator.dpDPO(Input, length, smoothing, aDX_Period, aDX_Threshold, lineOffset, backColorCrossings, backColorCrossingsAllPanels, sentinel1Bar_Enabled, sentinel2Bar_Enabled, markerType, markerSize, markerOffset, sentinel_ProximityLimit, proximityLimit_MarkerType, proximityLimit_MarkerSize, proximityLimit_MarkerOffset, dPO_XRAY_ComboSignals_Enabled, dPO_XRAY_MAFilter, dPO_XRAY_MAFilter_Period, dPO_XRAY_RSI_Period, dPO_XRAY_RSI_Smooth, dPO_XRAY_MACD_Fast, dPO_XRAY_MACD_Slow, dPO_XRAY_MACD_Smooth, dPO_XRAY_ADX_Period);
		}

		public Indicators.dpDPO dpDPO(ISeries<double> input , int length, int smoothing, int aDX_Period, double aDX_Threshold, double lineOffset, bool backColorCrossings, bool backColorCrossingsAllPanels, bool sentinel1Bar_Enabled, bool sentinel2Bar_Enabled, NinjaTrader.NinjaScript.Indicators.dpDPO.Marker_Types markerType, int markerSize, int markerOffset, double sentinel_ProximityLimit, NinjaTrader.NinjaScript.Indicators.dpDPO.Marker_Types proximityLimit_MarkerType, int proximityLimit_MarkerSize, int proximityLimit_MarkerOffset, bool dPO_XRAY_ComboSignals_Enabled, NinjaTrader.NinjaScript.Indicators.dpDPO.MA_Type dPO_XRAY_MAFilter, int dPO_XRAY_MAFilter_Period, int dPO_XRAY_RSI_Period, int dPO_XRAY_RSI_Smooth, int dPO_XRAY_MACD_Fast, int dPO_XRAY_MACD_Slow, int dPO_XRAY_MACD_Smooth, int dPO_XRAY_ADX_Period)
		{
			return indicator.dpDPO(input, length, smoothing, aDX_Period, aDX_Threshold, lineOffset, backColorCrossings, backColorCrossingsAllPanels, sentinel1Bar_Enabled, sentinel2Bar_Enabled, markerType, markerSize, markerOffset, sentinel_ProximityLimit, proximityLimit_MarkerType, proximityLimit_MarkerSize, proximityLimit_MarkerOffset, dPO_XRAY_ComboSignals_Enabled, dPO_XRAY_MAFilter, dPO_XRAY_MAFilter_Period, dPO_XRAY_RSI_Period, dPO_XRAY_RSI_Smooth, dPO_XRAY_MACD_Fast, dPO_XRAY_MACD_Slow, dPO_XRAY_MACD_Smooth, dPO_XRAY_ADX_Period);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.dpDPO dpDPO(int length, int smoothing, int aDX_Period, double aDX_Threshold, double lineOffset, bool backColorCrossings, bool backColorCrossingsAllPanels, bool sentinel1Bar_Enabled, bool sentinel2Bar_Enabled, NinjaTrader.NinjaScript.Indicators.dpDPO.Marker_Types markerType, int markerSize, int markerOffset, double sentinel_ProximityLimit, NinjaTrader.NinjaScript.Indicators.dpDPO.Marker_Types proximityLimit_MarkerType, int proximityLimit_MarkerSize, int proximityLimit_MarkerOffset, bool dPO_XRAY_ComboSignals_Enabled, NinjaTrader.NinjaScript.Indicators.dpDPO.MA_Type dPO_XRAY_MAFilter, int dPO_XRAY_MAFilter_Period, int dPO_XRAY_RSI_Period, int dPO_XRAY_RSI_Smooth, int dPO_XRAY_MACD_Fast, int dPO_XRAY_MACD_Slow, int dPO_XRAY_MACD_Smooth, int dPO_XRAY_ADX_Period)
		{
			return indicator.dpDPO(Input, length, smoothing, aDX_Period, aDX_Threshold, lineOffset, backColorCrossings, backColorCrossingsAllPanels, sentinel1Bar_Enabled, sentinel2Bar_Enabled, markerType, markerSize, markerOffset, sentinel_ProximityLimit, proximityLimit_MarkerType, proximityLimit_MarkerSize, proximityLimit_MarkerOffset, dPO_XRAY_ComboSignals_Enabled, dPO_XRAY_MAFilter, dPO_XRAY_MAFilter_Period, dPO_XRAY_RSI_Period, dPO_XRAY_RSI_Smooth, dPO_XRAY_MACD_Fast, dPO_XRAY_MACD_Slow, dPO_XRAY_MACD_Smooth, dPO_XRAY_ADX_Period);
		}

		public Indicators.dpDPO dpDPO(ISeries<double> input , int length, int smoothing, int aDX_Period, double aDX_Threshold, double lineOffset, bool backColorCrossings, bool backColorCrossingsAllPanels, bool sentinel1Bar_Enabled, bool sentinel2Bar_Enabled, NinjaTrader.NinjaScript.Indicators.dpDPO.Marker_Types markerType, int markerSize, int markerOffset, double sentinel_ProximityLimit, NinjaTrader.NinjaScript.Indicators.dpDPO.Marker_Types proximityLimit_MarkerType, int proximityLimit_MarkerSize, int proximityLimit_MarkerOffset, bool dPO_XRAY_ComboSignals_Enabled, NinjaTrader.NinjaScript.Indicators.dpDPO.MA_Type dPO_XRAY_MAFilter, int dPO_XRAY_MAFilter_Period, int dPO_XRAY_RSI_Period, int dPO_XRAY_RSI_Smooth, int dPO_XRAY_MACD_Fast, int dPO_XRAY_MACD_Slow, int dPO_XRAY_MACD_Smooth, int dPO_XRAY_ADX_Period)
		{
			return indicator.dpDPO(input, length, smoothing, aDX_Period, aDX_Threshold, lineOffset, backColorCrossings, backColorCrossingsAllPanels, sentinel1Bar_Enabled, sentinel2Bar_Enabled, markerType, markerSize, markerOffset, sentinel_ProximityLimit, proximityLimit_MarkerType, proximityLimit_MarkerSize, proximityLimit_MarkerOffset, dPO_XRAY_ComboSignals_Enabled, dPO_XRAY_MAFilter, dPO_XRAY_MAFilter_Period, dPO_XRAY_RSI_Period, dPO_XRAY_RSI_Smooth, dPO_XRAY_MACD_Fast, dPO_XRAY_MACD_Slow, dPO_XRAY_MACD_Smooth, dPO_XRAY_ADX_Period);
		}
	}
}

#endregion
