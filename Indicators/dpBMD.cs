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
	public class dpBMD : Indicator
	{
		MACD macd;
		Bollinger bb;
		Series<int> directionSeries;
		
		const string arialUpTriangle = @"▲", arialDownTriangle = @"▼", arialDot = @"●", wingdingsUpArrow = "é", wingdingsDownArrow = "ê";
		SimpleFont markerFont, counterTrendMarkerFont;
		string upMarker = "", downMarker = "";
		string counterTrendUpMarker = "", counterTrendDownMarker = "";
		Stochastics stoch;
		
		int az = 0;
		
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
			EMA,
			LinReg
		}
		
		bool neverDone = true;
		
		Dictionary<string,NinjaTrader.NinjaScript.DrawingTools.Ray> rays;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"From TOS by TradingCoders.com";
				Name										= "dpBMD";
				Calculate									= Calculate.OnPriceChange;
				IsOverlay									= false;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= true;
				//DrawHorizontalGridLines						= true;
				//DrawVerticalGridLines						= true;
				PaintPriceMarkers							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				//Disable this property if your indicator requires custom values that cumulate with each new market data event. 
				//See Help Guide for additional information.
				IsSuspendedWhileInactive					= true;
				this.ShowTransparentPlotsInDataBox = true;
				
				// INPUT PARAMS
				BBLength					= 10;
				BBNum_Dev					= 1;
				BBSmooth					= 1;
				MACD_Fast					= 5;
				MACD_Slow					= 34;
				MACD_Smooth					= 5;
				UpColor					= Brushes.Lime;
				DownColor				= Brushes.Red;
				BarCountForSignal		= 3;
				
				MAFilter = MA_Type.off;
				MAFilter_Period = 50;
				MAFilter_CounterTrendSignals = true;
				MAFilter_CounterTrendMarkers = Marker_Types.Dot;
				MAFilter_CounterTrendMarkerSize = 12;
				MAFilter_CounterTrendMarkerOffset = 4;
				MAFilter_CounterTrendUpColor = Brushes.Blue;
				MAFilter_CounterTrendDownColor = Brushes.Purple;
				MAFilter_StochOBOS = 20;
				MA_Filter_CounterTrendEntryLine = new Stroke(Brushes.Gold,3);
				MA_Filter_CounterTrendEntryLine.IsOpacityVisible = true;
				MA_Filter_CounterTrendEntryOffset = 0.5;
				MA_Filter_CounterTrendBarsWide = 1;
				MA_Filter_CounterTrendBarsAuto = true;
				
				StochOverlaySignals = true;
				StochOverlayMarkers = Marker_Types.Triangle;
				StochOverlayMarkerSize = 12;
				StochOverlayMarkerOffset = 2;
				StochOverlayUpColor = Brushes.DeepSkyBlue;
				StochOverlayDownColor = Brushes.Magenta;
				StochOverlay_D = 3;
				StochOverlay_K = 8;
				StochOverlay_Smooth = 4;


				Midline_UpColor = Brushes.Lime;
				Midline_DownColor = Brushes.Magenta;
				Midline_FlatColor = Brushes.SlateBlue;
				Midline_FlatnessATRs = 0.05;

				ShowAlternateMidlineInsideBands = true;

				// PLOTS
				AddPlot(new Stroke(Brushes.Gray,2), PlotStyle.Line, "ZeroLine");
				AddPlot(new Stroke(Brushes.Gray,2), PlotStyle.Line, "Upper");
				AddPlot(new Stroke(Brushes.DimGray, DashStyleHelper.Solid,6), PlotStyle.Line,"MiddleShadow");
				AddPlot(new Stroke(Brushes.Gray, DashStyleHelper.Dash,2), PlotStyle.Line,"Middle");
				AddPlot(new Stroke(Brushes.Gray,2), PlotStyle.Line, "Lower");
				AddPlot(Brushes.SlateGray, "MACD_Line");
				AddPlot(new Stroke(Brushes.Gray, 2), PlotStyle.Dot, "MACD_Dots");
				
				AddPlot(Brushes.Transparent,"MACD_State");
				AddPlot(Brushes.Transparent,"BarCount_State");
				
				AddPlot(Brushes.Transparent,"StochOverlaySignals");
				AddPlot(new Stroke(Brushes.Yellow,2), PlotStyle.Line,"StochOverlay");
				AddPlot(new Stroke(Brushes.Yellow, 5), PlotStyle.Dot, "AlternateZeroLine");
				
				
				AddLine(Brushes.Crimson,0.001,"UpperLine");
				AddLine(Brushes.Lime,-0.001,"LowerLine");
				Lines[0].IsOpacityVisible = true;
				Lines[1].IsOpacityVisible = true;
			}
			else if (State == State.Configure)
			{
			}
			else if (State == State.DataLoaded)
			{
				macd = MACD(Input,MACD_Fast,MACD_Slow,MACD_Smooth);
				bb = Bollinger(macd.Default,BBNum_Dev,BBLength);
				
				directionSeries = new Series<int>(this);
				
				//if (StochOverlaySignals)
					stoch = Stochastics(StochOverlay_D,StochOverlay_K,StochOverlay_Smooth);
				
				
				markerFont = new SimpleFont(StochOverlayMarkers==Marker_Types.Arrow?"Wingdings":"Arial",StochOverlayMarkerSize){Bold=true};
				switch (StochOverlayMarkers) 
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
				
				counterTrendMarkerFont = new SimpleFont(MAFilter_CounterTrendMarkers==Marker_Types.Arrow?"Wingdings":"Arial",MAFilter_CounterTrendMarkerSize){Bold=true};
				switch ( MAFilter_CounterTrendMarkers) 
				{
					case Marker_Types.Triangle:
						counterTrendUpMarker = arialUpTriangle;
						counterTrendDownMarker = arialDownTriangle;
						break;
					case Marker_Types.Arrow:
						counterTrendUpMarker = wingdingsUpArrow;
						counterTrendDownMarker = wingdingsDownArrow;
						break;
					default:
						counterTrendUpMarker = arialDot;
						counterTrendDownMarker = arialDot;
						break;
				}
				
				rays = new Dictionary<string,NinjaTrader.NinjaScript.DrawingTools.Ray>();
			}
		}

		protected override void OnBarUpdate()
		{
			try
			{

				if (CurrentBar < BarsRequiredToPlot)
					return;

				// plotting
				MACD_Line[0] = macd.Default[0];
				MACD_Dots[0] = macd.Default[0];
				directionSeries[0] = macd.Default[0] >= macd.Default[1] ? 1 : -1;
				PlotBrushes[6][0] = (directionSeries[0] > 0 ? UpColor : DownColor);
				Upper[0] = SMA(bb.Upper, BBSmooth)[0];
				Middle[0] = SMA(bb.Middle, BBSmooth)[0];
				MiddleShadow[0] = Middle[0];
				Lower[0] = SMA(bb.Lower, BBSmooth)[0];
				ZeroLine[0] = 0;
				PlotBrushes[0][0] = macd.Default[0] >= 0 ? UpColor : DownColor;

				// now for custom signals:

				if (directionSeries[0] > 0) // up bars
				{
					if (macd.Default[0] > Upper[0])
						MACD_State[0] = 3;
					else if (macd.Default[0] > Middle[0])
						MACD_State[0] = 2;
					else if (macd.Default[0] > Lower[0])
						MACD_State[0] = 1;
					else
						MACD_State[0] = 0;
				}

				if (directionSeries[0] < 0) // up bars
				{
					if (macd.Default[0] < Lower[0])
						MACD_State[0] = -3;
					else if (macd.Default[0] < Middle[0])
						MACD_State[0] = -2;
					else if (macd.Default[0] < Upper[0])
						MACD_State[0] = -1;
					else
						MACD_State[0] = 0;
				}

				if (MACD_State[0] == 0)
					BarCount_State[0] = 0;
				else
				{
					if (directionSeries[0] != directionSeries[1])
						BarCount_State[0] = 1;
					else
						BarCount_State[0] = BarCount_State[1] + 1;
				}

				if (IsFirstTickOfBar && rays.Count > 0)
					TurnRaysToLinesIfIntercepted();

				if (IsFirstTickOfBar)
				{
					az = Calculate == Calculate.OnBarClose || (State == State.Historical && !Bars.IsTickReplay) ? 0 : 1;
					double ma = 0;
					if (MAFilter != MA_Type.off)
						ma = (MAFilter == MA_Type.SMA ? SMA(MAFilter_Period)[0]
								: (MAFilter == MA_Type.EMA ? EMA(MAFilter_Period)[0] : LinReg(MAFilter_Period)[0]));

					if (BarCount_State[0] == BarCountForSignal && BarCountForSignal > 0)
					{
						if (MACD_State[az] > 0
							&& (MAFilter == MA_Type.off
								|| ma < Close[0]
								)
							)
							Draw.ArrowUp(this, "SignalUp" + (CurrentBar - az), true, az, Low[az] - ATR(10)[az] / 3, UpColor, true);
						else
						if (MACD_State[az] < 0
							&& (MAFilter == MA_Type.off
								|| ma > Close[0]
								)
							)
							Draw.ArrowDown(this, "SignalDn" + (CurrentBar - az), true, az, High[az] + ATR(10)[az] / 3, DownColor, true);
					}

					if (BarCount_State[0] == BarCountForSignal && BarCountForSignal > 0)
					{
						if (MACD_State[az] > 0
							&& ((MAFilter_CounterTrendSignals && ma > Close[0] && Math.Min(stoch.K[1], stoch.K[0]) < MAFilter_StochOBOS)
								)
							)
						{
							Draw.Text(this, "CounterTrendUp" + (CurrentBar - az), true, counterTrendUpMarker,
								az, Low[az] - ATR(10)[az] * MAFilter_CounterTrendMarkerOffset * 0.1, 0, MAFilter_CounterTrendUpColor, counterTrendMarkerFont, TextAlignment.Center, Brushes.Transparent, Brushes.Transparent, 0);
							double price = High[az] - Range()[az] * MA_Filter_CounterTrendEntryOffset;
							if (!MA_Filter_CounterTrendBarsAuto)
								Draw.Line(this, "CounterTrendEntryUp" + (CurrentBar - az), true, az, price, (az - MA_Filter_CounterTrendBarsWide), price, MA_Filter_CounterTrendEntryLine.Brush, MA_Filter_CounterTrendEntryLine.DashStyleHelper, (int)MA_Filter_CounterTrendEntryLine.Width);
							else
								rays["CounterTrendEntryUp" + (CurrentBar - az)] = Draw.Ray(this, "CounterTrendEntryUp" + (CurrentBar - az), true, az, price, (az - 1), price, MA_Filter_CounterTrendEntryLine.Brush, MA_Filter_CounterTrendEntryLine.DashStyleHelper, (int)MA_Filter_CounterTrendEntryLine.Width);

						}
						else
						if (MACD_State[az] < 0
							&& ((MAFilter_CounterTrendSignals && ma < Close[0] && Math.Max(stoch.K[1], stoch.K[0]) > 100 - MAFilter_StochOBOS)
								)
							)
						{
							Draw.Text(this, "CounterTrendDn" + (CurrentBar - az), true, counterTrendDownMarker,
								az, High[az] + ATR(10)[az] * MAFilter_CounterTrendMarkerOffset * 0.1, 0, MAFilter_CounterTrendDownColor, counterTrendMarkerFont, TextAlignment.Center, Brushes.Transparent, Brushes.Transparent, 0);
							double price = Low[az] + Range()[az] * MA_Filter_CounterTrendEntryOffset;
							if (!MA_Filter_CounterTrendBarsAuto)
								Draw.Line(this, "CounterTrendEntryDn" + (CurrentBar - az), true, az, price, (az - MA_Filter_CounterTrendBarsWide), price, MA_Filter_CounterTrendEntryLine.Brush, MA_Filter_CounterTrendEntryLine.DashStyleHelper, (int)MA_Filter_CounterTrendEntryLine.Width);
							else
								rays["CounterTrendEntryDn" + (CurrentBar - az)] = Draw.Ray(this, "CounterTrendEntryDn" + (CurrentBar - az), true, az, price, (az - 1), price, MA_Filter_CounterTrendEntryLine.Brush, MA_Filter_CounterTrendEntryLine.DashStyleHelper, (int)MA_Filter_CounterTrendEntryLine.Width);
						}
					}

				}

				if ((IsFirstTickOfBar || neverDone)
					&& StochOverlaySignals
					&& ChartControl != null && ChartBars != null
					)
					DoStochOverlaySignals(az);


				if (CurrentBar > BarsRequiredToPlot)
				{
					// coloring of midline
					if (Math.Abs(Middle[0] - Middle[1]) < Midline_FlatnessATRs * ATR(BBLength)[1])
					{
						PlotBrushes[3][0] = Midline_FlatColor;
					}
					else
					{
						if (!Midline_UpColor.IsTransparent() && Middle[0] >= Middle[1])
							PlotBrushes[3][0] = Midline_UpColor;
						else if (!Midline_DownColor.IsTransparent() && Middle[0] < Middle[1])
							PlotBrushes[3][0] = Midline_DownColor;
					}

					// alternate midline
					if (ShowAlternateMidlineInsideBands)
					{
						if (MACD_Dots[0] <= Upper[0] && MACD_Dots[0] >= Lower[0])
							AlternateZeroLine[0] = 0;
						else
							AlternateZeroLine.Reset();
					}
				}

			}
			catch (Exception ex)
			{
				throw new Exception(ex.ToString());
			}
		}
		
		// ===================================================================================================================================
		
		private void DoStochOverlaySignals(int barsAgo)
		{
			CalculateNormalize(az);
			
			// so any normalized value = (value - soMin) * rangeRatio;
			int ago = barsAgo;
			if ((State==State.Realtime || (neverDone && CurrentBar == Count-1-(Calculate==Calculate.OnBarClose?1:0)))
				&& soMax > 0 && !double.IsInfinity(rangeRatio) && rangeRatio != 0.0
				) 
			{
				neverDone = false;
				for (int b = CurrentBar-barsAgo; b >= Math.Max(1,ChartBars.FromIndex); b--)
				{
					double value = UnNorm(stoch.K.GetValueAt(b));
					ago = CurrentBar - b;
					if (developer && !double.IsNaN(value) && !double.IsInfinity(value))
						Draw.Dot(this,"DebugDot"+(b),true,ago,value,Brushes.Purple,false); // checker
					
					// up cross
					if (	stoch.K[ago] > Norm(macd.Default[ago])
						&&	(stoch.K[ago+1] < Norm(macd.Default[ago+1])
							|| (stoch.K[ago+1] == Norm(macd.Default[ago+1]) && stoch.K[ago+2] < Norm(macd.Default[ago+2]))
							)
						)
					{
						// cross up
						StochOverlay_Signals[ago] = +1;

						Draw.Text(this,"StochOverlayUp"+(CurrentBar-ago),true,upMarker,
							ago,Low[ago]-ATR(10)[ago]*StochOverlayMarkerOffset*0.1,0,StochOverlayUpColor,markerFont,TextAlignment.Center,Brushes.Transparent,Brushes.Transparent,0);
					}
					
					// down cross
					if (	stoch.K[ago] < Norm(macd.Default[ago])
						&&	(stoch.K[ago+1] > Norm(macd.Default[ago+1])
							|| (stoch.K[ago+1] == Norm(macd.Default[ago+1]) && stoch.K[ago+2] > Norm(macd.Default[ago+2]))
							)
						)
					{
						// cross down
						StochOverlay_Signals[ago] = -1;

						Draw.Text(this,"StochOverlayDn"+(CurrentBar-ago),true,downMarker,
							ago,High[ago]+ATR(10)[ago]*StochOverlayMarkerOffset*0.1,0,StochOverlayDownColor,markerFont,TextAlignment.Center,Brushes.Transparent,Brushes.Transparent,0);
						
					}	
						
				}
			}
			
		}
		
		double rangeRatio = 0, soMin = 0, soMax = 0;
		private void CalculateNormalize(int ago)
		{
			if (soMax == 0 && soMin == 0)
				return;
			//Print("CalculateNormalize("+ago+") soMin="+soMin+" soMax="+soMax);
			try
			{
//				int lastBar = ChartBars.ToIndex;
//				int firstBar = Math.Max(0,ChartBars.FromIndex);
//				int period = lastBar-firstBar;
//				macd.Update();
//				double dummy = macd[0];
				//soMax = MAX(macd.Default,period).GetValueAt(CurrentBars[0]-ago);	// doesn't seem to work. wierd.
				//soMin = MIN(macd.Default,period).GetValueAt(CurrentBars[0]-ago);
//				soMax = double.MinValue;
//				soMin = double.MinValue;
//				for (int b = Math.Max(CurrentBars[0]-ago-period,0); b <= CurrentBars[0]-ago; b++)
//				{
//					//Print("asking on bar "+b+" macd valid = "+macd.Default.IsValidDataPointAt(b));
//					if (macd.Default.IsValidDataPointAt(b))
//					{
//						if (soMax == double.MinValue || macd.Default.GetValueAt(b) > soMax)
//							soMax = macd.Default.GetValueAt(b);
//						if (soMin == double.MinValue || macd.Default.GetValueAt(b) < soMin)
//							soMin = macd.Default.GetValueAt(b);
//					}
//				}
				// access soMax and soMin from OnRender;
				
				// normalize to stoch range of +100 to 0
				double range = soMax - soMin;
				//Print("[A] - CB="+CurrentBars[0]+", ToIndex="+ChartBars.ToIndex+" period = "+period+" firstBar="+firstBar+": soMax="+soMax+", soMin="+soMin+", range "+range); // checker
				if (range == 0)
					return; // invalid data
				rangeRatio = 100 / range;
				//Print("[B] - CB="+CurrentBars[0]+", ToIndex="+ChartBars.ToIndex+" soMax="+soMax+", soMin="+soMin+", range "+range+" rangeRatio "+rangeRatio); // checker
			}
			catch (Exception ex)
			{
				Print(ex.ToString());
				throw new Exception (ex.ToString());
			}
		}
		
		private double Norm(double value)
		{
			return ((value - soMin) * rangeRatio);	
		}
		
		private double UnNorm(double value)
		{
			return soMax - (100 - value) / rangeRatio;	
		}
				
		private void TurnRaysToLinesIfIntercepted()
		{
			int AZ = Calculate==Calculate.OnBarClose || (State==State.Historical && !Bars.IsTickReplay) ? 0 : 1;
			List<string> keys = rays.Keys.ToList();
			foreach (string k in keys)
			{
				int barsAgo = CurrentBar - AZ - Bars.GetBar(rays[k].StartAnchor.Time);
				if (barsAgo > MA_Filter_CounterTrendBarsWide)
				{
					double price = rnd(rays[k].StartAnchor.Price);
					double low = rnd(Low[AZ+0]), high = rnd(High[AZ+0]);
					if (rays[k].StartAnchor.Time < Time[AZ+1])
					{
						low = rnd(Math.Min(Low[AZ+0],High[AZ+1]));
						high = rnd(Math.Max(High[AZ+0],Low[AZ+1]));
					}
					// touching/crossed
					if (price >= low && price <= high)
					{
						if (developer) Print(Time[AZ]+" Ray from "+rays[k].StartAnchor.Time+" to line at "+price+" because low="+low+", high="+high);
						Draw.Line(this,rays[k].Tag+"Line",false,rays[k].StartAnchor.Time,price,Time[0],price,rays[k].Stroke.Brush,rays[k].Stroke.DashStyleHelper,(int)rays[k].Stroke.Width);
						RemoveDrawObject(rays[k].Tag);
						rays.Remove(k);
					}
				}
			}
		}
		
		protected bool developer
		{
			get 
			{ 
				return NinjaTrader.Cbi.License.MachineId == "00BF56A266B9517490AA96FFFD421EEB";  // JR development machine (win10 Parallels)
			}
		}
		
		private double rnd(double price)
		{
			return Instrument.MasterInstrument.RoundToTickSize(price);
		}

		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
			if (Bars==null || ChartBars==null || CurrentBars[0] < ChartBars.ToIndex)
				return;
			
			soMin = chartScale.MinValue;
			soMax = chartScale.MaxValue;
						
			base.OnRender(chartControl,chartScale);
			
		}

		#region Properties
		[NinjaScriptProperty]
		[Range(2, int.MaxValue)]
		[Display(Name="BBLength", Order=10, GroupName="Parameters")]
		public int BBLength
		{ get; set; }

		[NinjaScriptProperty]
		[Range(0, double.MaxValue)]
		[Display(Name="BBNum_Dev", Order=20, GroupName="Parameters")]
		public double BBNum_Dev
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="BBSmooth", Order=25, GroupName="Parameters")]
		public int BBSmooth
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="MACD_Fast", Order=30, GroupName="Parameters")]
		public int MACD_Fast
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="MACD_Slow", Order=40, GroupName="Parameters")]
		public int MACD_Slow
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="MACD_Smooth", Order=50, GroupName="Parameters")]
		public int MACD_Smooth
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="BarCountForSignal", Order=60, GroupName="Parameters")]
		public int BarCountForSignal
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
		
		
		[NinjaScriptProperty]
		[Display(Name="MAFilter", Order=3, GroupName="MAFilter")]
		public NinjaTrader.NinjaScript.Indicators.dpBMD.MA_Type MAFilter
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="MAFilter_Period", Order=5, GroupName="MAFilter")]
		public int MAFilter_Period
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="MAFilter_CounterTrendSignals", Order=10, GroupName="MAFilter")]
		public bool MAFilter_CounterTrendSignals
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0,100)]
		[Display(Name="MAFilter_StochOverbought/sold", Order=15, GroupName="MAFilter")]
		public int MAFilter_StochOBOS
		{ get; set; }
		
		
		[NinjaScriptProperty]
		[Display(Name="MAFilter_CounterTrend Markers", Order=225, GroupName="MAFilter")]
		public NinjaTrader.NinjaScript.Indicators.dpBMD.Marker_Types MAFilter_CounterTrendMarkers
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(6, int.MaxValue)]
		[Display(Name="MAFilter_CounterTrend MarkerSize", Order=228, GroupName="MAFilter")]
		public int MAFilter_CounterTrendMarkerSize
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name="MAFilter_CounterTrend MarkerOffset", Order=229, GroupName="MAFilter")]
		public int MAFilter_CounterTrendMarkerOffset
		{ get; set; }
		
		[XmlIgnore()]
		//[NinjaScriptProperty]
		[Display(Name="MAFilter_CounterTrend UpColor", Description="Color for up", Order=310, GroupName="MAFilter")]
        public Brush MAFilter_CounterTrendUpColor
		{ get; set; }
		
		// Serialize our Color object
		[Browsable(false)]
		public string MAFilter_CounterTrendUpColorSerialize
		{
			get { return Serialize.BrushToString(MAFilter_CounterTrendUpColor); }
			set { MAFilter_CounterTrendUpColor = Serialize.StringToBrush(value); }
		}
		
		[XmlIgnore()]
		//[NinjaScriptProperty]
		[Display(Name="MAFilter_CounterTrend DownColor", Description="Color for down", Order=315, GroupName="MAFilter")]
        public Brush MAFilter_CounterTrendDownColor
		{ get; set; }
		
		// Serialize our Color object
		[Browsable(false)]
		public string MAFilter_CounterTrendDownColorSerialize
		{
			get { return Serialize.BrushToString(MAFilter_CounterTrendDownColor); }
			set { MAFilter_CounterTrendDownColor = Serialize.StringToBrush(value); }
		}
		
		[NinjaScriptProperty]
		[Display(Name="MA_Filter_CounterTrend EntryLine", Order=345, GroupName="MAFilter")]
		public Stroke MA_Filter_CounterTrendEntryLine
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, 1)]
		[Display(Name="MA_Filter_CounterTrend EntryOffset", Order=350, GroupName="MAFilter")]
		public double MA_Filter_CounterTrendEntryOffset
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="MA_Filter_CounterTrend BarsWide/Minimum", Order=355, GroupName="MAFilter")]
		public int MA_Filter_CounterTrendBarsWide
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="MA_Filter_CounterTrend Auto BarsWide", Order=357, GroupName="MAFilter")]
		public bool MA_Filter_CounterTrendBarsAuto
		{ get; set; }
		
		//
		
		[NinjaScriptProperty]
		[Display(Name="StochOverlay Signals", Order=3, GroupName="Stochastic Overlay")]
		public bool StochOverlaySignals
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="StochOverlay Markers", Order=5, GroupName="Stochastic Overlay")]
		public NinjaTrader.NinjaScript.Indicators.dpBMD.Marker_Types StochOverlayMarkers
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(6, int.MaxValue)]
		[Display(Name="StochOverlay MarkerSize", Order=8, GroupName="Stochastic Overlay")]
		public int StochOverlayMarkerSize
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name="StochOverlay MarkerOffset", Order=9, GroupName="Stochastic Overlay")]
		public int StochOverlayMarkerOffset
		{ get; set; }
		
		[XmlIgnore()]
		//[NinjaScriptProperty]
		[Display(Name="StochOverlay UpColor", Description="Color for up", Order=10, GroupName="Stochastic Overlay")]
        public Brush StochOverlayUpColor
		{ get; set; }
		
		// Serialize our Color object
		[Browsable(false)]
		public string StochOverlayUpColorSerialize
		{
			get { return Serialize.BrushToString(StochOverlayUpColor); }
			set { StochOverlayUpColor = Serialize.StringToBrush(value); }
		}
		
		[XmlIgnore()]
		//[NinjaScriptProperty]
		[Display(Name="StochOverlay DownColor", Description="Color for down", Order=15, GroupName="Stochastic Overlay")]
        public Brush StochOverlayDownColor
		{ get; set; }
		
		// Serialize our Color object
		[Browsable(false)]
		public string StochOverlayDownColorSerialize
		{
			get { return Serialize.BrushToString(StochOverlayDownColor); }
			set { StochOverlayDownColor = Serialize.StringToBrush(value); }
		}
		
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="StochOverlay D", Order=110, GroupName="Stochastic Overlay")]
		public int StochOverlay_D
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="StochOverlay K", Order=120, GroupName="Stochastic Overlay")]
		public int StochOverlay_K
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="StochOverlay Smooth", Order=130, GroupName="Stochastic Overlay")]
		public int StochOverlay_Smooth
		{ get; set; }

		//


		[XmlIgnore()]
		//[NinjaScriptProperty]
		[Display(Name = "Midline_UpColor", Description = "Color for uptrend 1", Order = 610, GroupName = "Coloring")]
		public Brush Midline_UpColor
		{ get; set; }

		// Serialize our Color object
		[Browsable(false)]
		public string Midline_UpColorSerialize
		{
			get { return Serialize.BrushToString(Midline_UpColor); }
			set { Midline_UpColor = Serialize.StringToBrush(value); }
		}

		[XmlIgnore()]
		//[NinjaScriptProperty]
		[Display(Name = "Midline_DownColor", Description = "Color for downtrend 1", Order = 620, GroupName = "Coloring")]
		public Brush Midline_DownColor
		{ get; set; }

		// Serialize our Color object
		[Browsable(false)]
		public string Midline_DownColorSerialize
		{
			get { return Serialize.BrushToString(Midline_DownColor); }
			set { Midline_DownColor = Serialize.StringToBrush(value); }
		}
		[XmlIgnore()]
		//[NinjaScriptProperty]
		[Display(Name = "Midline_FlatColor", Description = "Color for flat trend 1", Order = 630, GroupName = "Coloring")]
		public Brush Midline_FlatColor
		{ get; set; }

		// Serialize our Color object
		[Browsable(false)]
		public string Midline_FlatColorSerialize
		{
			get { return Serialize.BrushToString(Midline_FlatColor); }
			set { Midline_FlatColor = Serialize.StringToBrush(value); }
		}

		[NinjaScriptProperty]
		[Range(0, double.MaxValue)]
		[Display(Name = "Midline_FlatnessATRs", Description = "ATR multiple defining flat trend", Order = 640, GroupName = "Coloring")]
		public double Midline_FlatnessATRs
		{ get; set; }

		
		[NinjaScriptProperty]
		[Display(Name = "ShowAlternateMidlineInsideBands", Description = "When dots are inside the band", Order = 640, GroupName = "Coloring")]
		public bool ShowAlternateMidlineInsideBands
		{ get; set; }

		
		//

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> ZeroLine
		{
			get { return Values[0]; }
		}
		
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> Upper
		{
			get { return Values[1]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> MiddleShadow
		{
			get { return Values[2]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> Middle
		{
			get { return Values[3]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> Lower
		{
			get { return Values[4]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> MACD_Line
		{
			get { return Values[5]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> MACD_Dots
		{
			get { return Values[6]; }
		}
		
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> MACD_State
		{
			get { return Values[7]; }
		}
		
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> BarCount_State
		{
			get { return Values[8]; }

		}
		
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> StochOverlay_Signals
		{
			get { return Values[9]; }
		}
		
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> StochOverlay
		{
			get { return Values[10]; }
		}
		
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> AlternateZeroLine
		{
			get { return Values[11]; }
		}
		
		#endregion

	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private dpBMD[] cachedpBMD;
		public dpBMD dpBMD(int bBLength, double bBNum_Dev, int bBSmooth, int mACD_Fast, int mACD_Slow, int mACD_Smooth, int barCountForSignal, NinjaTrader.NinjaScript.Indicators.dpBMD.MA_Type mAFilter, int mAFilter_Period, bool mAFilter_CounterTrendSignals, int mAFilter_StochOBOS, NinjaTrader.NinjaScript.Indicators.dpBMD.Marker_Types mAFilter_CounterTrendMarkers, int mAFilter_CounterTrendMarkerSize, int mAFilter_CounterTrendMarkerOffset, Stroke mA_Filter_CounterTrendEntryLine, double mA_Filter_CounterTrendEntryOffset, int mA_Filter_CounterTrendBarsWide, bool mA_Filter_CounterTrendBarsAuto, bool stochOverlaySignals, NinjaTrader.NinjaScript.Indicators.dpBMD.Marker_Types stochOverlayMarkers, int stochOverlayMarkerSize, int stochOverlayMarkerOffset, int stochOverlay_D, int stochOverlay_K, int stochOverlay_Smooth, double midline_FlatnessATRs, bool showAlternateMidlineInsideBands)
		{
			return dpBMD(Input, bBLength, bBNum_Dev, bBSmooth, mACD_Fast, mACD_Slow, mACD_Smooth, barCountForSignal, mAFilter, mAFilter_Period, mAFilter_CounterTrendSignals, mAFilter_StochOBOS, mAFilter_CounterTrendMarkers, mAFilter_CounterTrendMarkerSize, mAFilter_CounterTrendMarkerOffset, mA_Filter_CounterTrendEntryLine, mA_Filter_CounterTrendEntryOffset, mA_Filter_CounterTrendBarsWide, mA_Filter_CounterTrendBarsAuto, stochOverlaySignals, stochOverlayMarkers, stochOverlayMarkerSize, stochOverlayMarkerOffset, stochOverlay_D, stochOverlay_K, stochOverlay_Smooth, midline_FlatnessATRs, showAlternateMidlineInsideBands);
		}

		public dpBMD dpBMD(ISeries<double> input, int bBLength, double bBNum_Dev, int bBSmooth, int mACD_Fast, int mACD_Slow, int mACD_Smooth, int barCountForSignal, NinjaTrader.NinjaScript.Indicators.dpBMD.MA_Type mAFilter, int mAFilter_Period, bool mAFilter_CounterTrendSignals, int mAFilter_StochOBOS, NinjaTrader.NinjaScript.Indicators.dpBMD.Marker_Types mAFilter_CounterTrendMarkers, int mAFilter_CounterTrendMarkerSize, int mAFilter_CounterTrendMarkerOffset, Stroke mA_Filter_CounterTrendEntryLine, double mA_Filter_CounterTrendEntryOffset, int mA_Filter_CounterTrendBarsWide, bool mA_Filter_CounterTrendBarsAuto, bool stochOverlaySignals, NinjaTrader.NinjaScript.Indicators.dpBMD.Marker_Types stochOverlayMarkers, int stochOverlayMarkerSize, int stochOverlayMarkerOffset, int stochOverlay_D, int stochOverlay_K, int stochOverlay_Smooth, double midline_FlatnessATRs, bool showAlternateMidlineInsideBands)
		{
			if (cachedpBMD != null)
				for (int idx = 0; idx < cachedpBMD.Length; idx++)
					if (cachedpBMD[idx] != null && cachedpBMD[idx].BBLength == bBLength && cachedpBMD[idx].BBNum_Dev == bBNum_Dev && cachedpBMD[idx].BBSmooth == bBSmooth && cachedpBMD[idx].MACD_Fast == mACD_Fast && cachedpBMD[idx].MACD_Slow == mACD_Slow && cachedpBMD[idx].MACD_Smooth == mACD_Smooth && cachedpBMD[idx].BarCountForSignal == barCountForSignal && cachedpBMD[idx].MAFilter == mAFilter && cachedpBMD[idx].MAFilter_Period == mAFilter_Period && cachedpBMD[idx].MAFilter_CounterTrendSignals == mAFilter_CounterTrendSignals && cachedpBMD[idx].MAFilter_StochOBOS == mAFilter_StochOBOS && cachedpBMD[idx].MAFilter_CounterTrendMarkers == mAFilter_CounterTrendMarkers && cachedpBMD[idx].MAFilter_CounterTrendMarkerSize == mAFilter_CounterTrendMarkerSize && cachedpBMD[idx].MAFilter_CounterTrendMarkerOffset == mAFilter_CounterTrendMarkerOffset && cachedpBMD[idx].MA_Filter_CounterTrendEntryLine == mA_Filter_CounterTrendEntryLine && cachedpBMD[idx].MA_Filter_CounterTrendEntryOffset == mA_Filter_CounterTrendEntryOffset && cachedpBMD[idx].MA_Filter_CounterTrendBarsWide == mA_Filter_CounterTrendBarsWide && cachedpBMD[idx].MA_Filter_CounterTrendBarsAuto == mA_Filter_CounterTrendBarsAuto && cachedpBMD[idx].StochOverlaySignals == stochOverlaySignals && cachedpBMD[idx].StochOverlayMarkers == stochOverlayMarkers && cachedpBMD[idx].StochOverlayMarkerSize == stochOverlayMarkerSize && cachedpBMD[idx].StochOverlayMarkerOffset == stochOverlayMarkerOffset && cachedpBMD[idx].StochOverlay_D == stochOverlay_D && cachedpBMD[idx].StochOverlay_K == stochOverlay_K && cachedpBMD[idx].StochOverlay_Smooth == stochOverlay_Smooth && cachedpBMD[idx].Midline_FlatnessATRs == midline_FlatnessATRs && cachedpBMD[idx].ShowAlternateMidlineInsideBands == showAlternateMidlineInsideBands && cachedpBMD[idx].EqualsInput(input))
						return cachedpBMD[idx];
			return CacheIndicator<dpBMD>(new dpBMD(){ BBLength = bBLength, BBNum_Dev = bBNum_Dev, BBSmooth = bBSmooth, MACD_Fast = mACD_Fast, MACD_Slow = mACD_Slow, MACD_Smooth = mACD_Smooth, BarCountForSignal = barCountForSignal, MAFilter = mAFilter, MAFilter_Period = mAFilter_Period, MAFilter_CounterTrendSignals = mAFilter_CounterTrendSignals, MAFilter_StochOBOS = mAFilter_StochOBOS, MAFilter_CounterTrendMarkers = mAFilter_CounterTrendMarkers, MAFilter_CounterTrendMarkerSize = mAFilter_CounterTrendMarkerSize, MAFilter_CounterTrendMarkerOffset = mAFilter_CounterTrendMarkerOffset, MA_Filter_CounterTrendEntryLine = mA_Filter_CounterTrendEntryLine, MA_Filter_CounterTrendEntryOffset = mA_Filter_CounterTrendEntryOffset, MA_Filter_CounterTrendBarsWide = mA_Filter_CounterTrendBarsWide, MA_Filter_CounterTrendBarsAuto = mA_Filter_CounterTrendBarsAuto, StochOverlaySignals = stochOverlaySignals, StochOverlayMarkers = stochOverlayMarkers, StochOverlayMarkerSize = stochOverlayMarkerSize, StochOverlayMarkerOffset = stochOverlayMarkerOffset, StochOverlay_D = stochOverlay_D, StochOverlay_K = stochOverlay_K, StochOverlay_Smooth = stochOverlay_Smooth, Midline_FlatnessATRs = midline_FlatnessATRs, ShowAlternateMidlineInsideBands = showAlternateMidlineInsideBands }, input, ref cachedpBMD);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.dpBMD dpBMD(int bBLength, double bBNum_Dev, int bBSmooth, int mACD_Fast, int mACD_Slow, int mACD_Smooth, int barCountForSignal, NinjaTrader.NinjaScript.Indicators.dpBMD.MA_Type mAFilter, int mAFilter_Period, bool mAFilter_CounterTrendSignals, int mAFilter_StochOBOS, NinjaTrader.NinjaScript.Indicators.dpBMD.Marker_Types mAFilter_CounterTrendMarkers, int mAFilter_CounterTrendMarkerSize, int mAFilter_CounterTrendMarkerOffset, Stroke mA_Filter_CounterTrendEntryLine, double mA_Filter_CounterTrendEntryOffset, int mA_Filter_CounterTrendBarsWide, bool mA_Filter_CounterTrendBarsAuto, bool stochOverlaySignals, NinjaTrader.NinjaScript.Indicators.dpBMD.Marker_Types stochOverlayMarkers, int stochOverlayMarkerSize, int stochOverlayMarkerOffset, int stochOverlay_D, int stochOverlay_K, int stochOverlay_Smooth, double midline_FlatnessATRs, bool showAlternateMidlineInsideBands)
		{
			return indicator.dpBMD(Input, bBLength, bBNum_Dev, bBSmooth, mACD_Fast, mACD_Slow, mACD_Smooth, barCountForSignal, mAFilter, mAFilter_Period, mAFilter_CounterTrendSignals, mAFilter_StochOBOS, mAFilter_CounterTrendMarkers, mAFilter_CounterTrendMarkerSize, mAFilter_CounterTrendMarkerOffset, mA_Filter_CounterTrendEntryLine, mA_Filter_CounterTrendEntryOffset, mA_Filter_CounterTrendBarsWide, mA_Filter_CounterTrendBarsAuto, stochOverlaySignals, stochOverlayMarkers, stochOverlayMarkerSize, stochOverlayMarkerOffset, stochOverlay_D, stochOverlay_K, stochOverlay_Smooth, midline_FlatnessATRs, showAlternateMidlineInsideBands);
		}

		public Indicators.dpBMD dpBMD(ISeries<double> input , int bBLength, double bBNum_Dev, int bBSmooth, int mACD_Fast, int mACD_Slow, int mACD_Smooth, int barCountForSignal, NinjaTrader.NinjaScript.Indicators.dpBMD.MA_Type mAFilter, int mAFilter_Period, bool mAFilter_CounterTrendSignals, int mAFilter_StochOBOS, NinjaTrader.NinjaScript.Indicators.dpBMD.Marker_Types mAFilter_CounterTrendMarkers, int mAFilter_CounterTrendMarkerSize, int mAFilter_CounterTrendMarkerOffset, Stroke mA_Filter_CounterTrendEntryLine, double mA_Filter_CounterTrendEntryOffset, int mA_Filter_CounterTrendBarsWide, bool mA_Filter_CounterTrendBarsAuto, bool stochOverlaySignals, NinjaTrader.NinjaScript.Indicators.dpBMD.Marker_Types stochOverlayMarkers, int stochOverlayMarkerSize, int stochOverlayMarkerOffset, int stochOverlay_D, int stochOverlay_K, int stochOverlay_Smooth, double midline_FlatnessATRs, bool showAlternateMidlineInsideBands)
		{
			return indicator.dpBMD(input, bBLength, bBNum_Dev, bBSmooth, mACD_Fast, mACD_Slow, mACD_Smooth, barCountForSignal, mAFilter, mAFilter_Period, mAFilter_CounterTrendSignals, mAFilter_StochOBOS, mAFilter_CounterTrendMarkers, mAFilter_CounterTrendMarkerSize, mAFilter_CounterTrendMarkerOffset, mA_Filter_CounterTrendEntryLine, mA_Filter_CounterTrendEntryOffset, mA_Filter_CounterTrendBarsWide, mA_Filter_CounterTrendBarsAuto, stochOverlaySignals, stochOverlayMarkers, stochOverlayMarkerSize, stochOverlayMarkerOffset, stochOverlay_D, stochOverlay_K, stochOverlay_Smooth, midline_FlatnessATRs, showAlternateMidlineInsideBands);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.dpBMD dpBMD(int bBLength, double bBNum_Dev, int bBSmooth, int mACD_Fast, int mACD_Slow, int mACD_Smooth, int barCountForSignal, NinjaTrader.NinjaScript.Indicators.dpBMD.MA_Type mAFilter, int mAFilter_Period, bool mAFilter_CounterTrendSignals, int mAFilter_StochOBOS, NinjaTrader.NinjaScript.Indicators.dpBMD.Marker_Types mAFilter_CounterTrendMarkers, int mAFilter_CounterTrendMarkerSize, int mAFilter_CounterTrendMarkerOffset, Stroke mA_Filter_CounterTrendEntryLine, double mA_Filter_CounterTrendEntryOffset, int mA_Filter_CounterTrendBarsWide, bool mA_Filter_CounterTrendBarsAuto, bool stochOverlaySignals, NinjaTrader.NinjaScript.Indicators.dpBMD.Marker_Types stochOverlayMarkers, int stochOverlayMarkerSize, int stochOverlayMarkerOffset, int stochOverlay_D, int stochOverlay_K, int stochOverlay_Smooth, double midline_FlatnessATRs, bool showAlternateMidlineInsideBands)
		{
			return indicator.dpBMD(Input, bBLength, bBNum_Dev, bBSmooth, mACD_Fast, mACD_Slow, mACD_Smooth, barCountForSignal, mAFilter, mAFilter_Period, mAFilter_CounterTrendSignals, mAFilter_StochOBOS, mAFilter_CounterTrendMarkers, mAFilter_CounterTrendMarkerSize, mAFilter_CounterTrendMarkerOffset, mA_Filter_CounterTrendEntryLine, mA_Filter_CounterTrendEntryOffset, mA_Filter_CounterTrendBarsWide, mA_Filter_CounterTrendBarsAuto, stochOverlaySignals, stochOverlayMarkers, stochOverlayMarkerSize, stochOverlayMarkerOffset, stochOverlay_D, stochOverlay_K, stochOverlay_Smooth, midline_FlatnessATRs, showAlternateMidlineInsideBands);
		}

		public Indicators.dpBMD dpBMD(ISeries<double> input , int bBLength, double bBNum_Dev, int bBSmooth, int mACD_Fast, int mACD_Slow, int mACD_Smooth, int barCountForSignal, NinjaTrader.NinjaScript.Indicators.dpBMD.MA_Type mAFilter, int mAFilter_Period, bool mAFilter_CounterTrendSignals, int mAFilter_StochOBOS, NinjaTrader.NinjaScript.Indicators.dpBMD.Marker_Types mAFilter_CounterTrendMarkers, int mAFilter_CounterTrendMarkerSize, int mAFilter_CounterTrendMarkerOffset, Stroke mA_Filter_CounterTrendEntryLine, double mA_Filter_CounterTrendEntryOffset, int mA_Filter_CounterTrendBarsWide, bool mA_Filter_CounterTrendBarsAuto, bool stochOverlaySignals, NinjaTrader.NinjaScript.Indicators.dpBMD.Marker_Types stochOverlayMarkers, int stochOverlayMarkerSize, int stochOverlayMarkerOffset, int stochOverlay_D, int stochOverlay_K, int stochOverlay_Smooth, double midline_FlatnessATRs, bool showAlternateMidlineInsideBands)
		{
			return indicator.dpBMD(input, bBLength, bBNum_Dev, bBSmooth, mACD_Fast, mACD_Slow, mACD_Smooth, barCountForSignal, mAFilter, mAFilter_Period, mAFilter_CounterTrendSignals, mAFilter_StochOBOS, mAFilter_CounterTrendMarkers, mAFilter_CounterTrendMarkerSize, mAFilter_CounterTrendMarkerOffset, mA_Filter_CounterTrendEntryLine, mA_Filter_CounterTrendEntryOffset, mA_Filter_CounterTrendBarsWide, mA_Filter_CounterTrendBarsAuto, stochOverlaySignals, stochOverlayMarkers, stochOverlayMarkerSize, stochOverlayMarkerOffset, stochOverlay_D, stochOverlay_K, stochOverlay_Smooth, midline_FlatnessATRs, showAlternateMidlineInsideBands);
		}
	}
}

#endregion
