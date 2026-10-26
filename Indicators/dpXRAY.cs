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
	public class dpXRAY : Indicator
	{
		
		Brush upFaint, downFaint, weakUpFaint, weakDownFaint;
		Series<int> signals;
		Brush b;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"By TradingCoders.com - implementing formula RSI.22 +(AVGC26-AVGC52)- (AVG(AVGC26,9)-AVG(AVGC52,9))";
				Name										= "dpXRAY";
				Calculate									= Calculate.OnPriceChange;
				
				
				IsOverlay									= false;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= true;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Overlay;
				//Disable this property if your indicator requires custom values that cumulate with each new market data event. 
				//See Help Guide for additional information.
				IsSuspendedWhileInactive					= true;
				RSI_Period					= 22;
				RSI_Smooth					= 1;
				MACD_Fast					= 26;
				MACD_Slow					= 52;
				MACD_Smooth					= 9;
				
				
				
				ADX_Period				= 14;
				UpColor = Brushes.Green;
				DownColor = Brushes.Crimson;
				WeakUpColor = Brushes.PaleGreen;
				WeakDownColor = Brushes.Pink;
				
				Z_Order = -1;
				
				AddPlot(new Stroke(Brushes.Silver, 2), PlotStyle.Line, "RSIMACD");
				Plots[0].IsOpacityVisible = true;
				AddPlot(new Stroke(Brushes.Silver, 5), PlotStyle.Line, "RSIMACD Strong");
				
				AddLine(Brushes.SaddleBrown, 0, "ZeroLine");
			}
			else if (State == State.Configure)
			{
			}
			else if (State == State.DataLoaded)
			{
				signals = new Series<int>(this,MaximumBarsLookBack.Infinite);	
			}
			else if (State == State.Historical)
			{
				SetZOrder(Z_Order);
			}
		}
		
		public override void OnRenderTargetChanged()
		{
			upFaint = null;
			downFaint = null;
			weakUpFaint = null;
			weakDownFaint = null;
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar < 2)
				return;
			
			RSIMACD[0] = RSI(RSI_Period,RSI_Smooth).Avg[0] + (SMA(Close,MACD_Fast)[0] - SMA(Close,MACD_Slow)[0]) - (SMA(SMA(Close,MACD_Fast),MACD_Smooth)[0] - SMA(SMA(Close,MACD_Slow),MACD_Smooth)[0]) - 50; // center around zero
			
			// coloring:
			if (upFaint == null)
			{
				upFaint = UpColor.Clone();
				upFaint.Opacity = Plots[0].Brush.Opacity;
				upFaint.Freeze();
				
				downFaint = DownColor.Clone();
				downFaint.Opacity = Plots[0].Brush.Opacity;
				downFaint.Freeze();
				
				weakUpFaint = WeakUpColor.Clone();
				weakUpFaint.Opacity = Plots[0].Brush.Opacity;
				weakUpFaint.Freeze();
				
				weakDownFaint = WeakDownColor.Clone();
				weakDownFaint.Opacity = Plots[0].Brush.Opacity;
				weakDownFaint.Freeze();
			}
			bool ADXRising = ADX(ADX_Period)[0] >= ADX(ADX_Period)[1];
			b = ADXRising ? (RSIMACD[0] >= RSIMACD[1] ? upFaint : downFaint) : (RSIMACD[0] >= RSIMACD[1] ? weakUpFaint : weakDownFaint);
			{
				PlotBrushes[0][0] = b;
				if (ADXRising)
				{
					RSIMACDStrong[0] = RSIMACD[0];
					PlotBrushes[1][0] = b;
					if (Plots[1].PlotStyle == PlotStyle.Line)
						RSIMACDStrong[1] = RSIMACD[1]; // ensure it immediately is visible
				}
				else
				{
					RSIMACDStrong.Reset();
					if (!(ADX(ADX_Period)[1] >= ADX(ADX_Period)[2]))
						RSIMACDStrong.Reset(1);
				}					
			}
			signals[0] = ADXRising ? (RSIMACD[0] >= RSIMACD[1] ? 2 : -2) : (RSIMACD[0] >= RSIMACD[1] ? 1 : -1);
		}

		#region Properties
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="RSI_Period", Order=1, GroupName="Parameters")]
		public int RSI_Period
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="RSI_Smooth", Order=20, GroupName="Parameters")]
		public int RSI_Smooth
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
		[Range(1, int.MaxValue)]
		[Display(Name="ADX_Period", Order=6, GroupName="Visuals")]
		public int ADX_Period
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name="ZOrder", Order=150, GroupName="Visuals")]
		public int Z_Order
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

		
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> RSIMACD
		{
			get { return Values[0]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> RSIMACDStrong
		{
			get { return Values[1]; }
		}

		/// <summary>
		/// +2 and -2 are strong signals, +1 and -1 are 'weak' signals
		/// </summary>
		[Browsable(false)]
		[XmlIgnore]
		public Series<int> Signals
		{
			get { return signals; }
		}

		
		#endregion

	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private dpXRAY[] cachedpXRAY;
		public dpXRAY dpXRAY(int rSI_Period, int rSI_Smooth, int mACD_Fast, int mACD_Slow, int mACD_Smooth, int aDX_Period, int z_Order)
		{
			return dpXRAY(Input, rSI_Period, rSI_Smooth, mACD_Fast, mACD_Slow, mACD_Smooth, aDX_Period, z_Order);
		}

		public dpXRAY dpXRAY(ISeries<double> input, int rSI_Period, int rSI_Smooth, int mACD_Fast, int mACD_Slow, int mACD_Smooth, int aDX_Period, int z_Order)
		{
			if (cachedpXRAY != null)
				for (int idx = 0; idx < cachedpXRAY.Length; idx++)
					if (cachedpXRAY[idx] != null && cachedpXRAY[idx].RSI_Period == rSI_Period && cachedpXRAY[idx].RSI_Smooth == rSI_Smooth && cachedpXRAY[idx].MACD_Fast == mACD_Fast && cachedpXRAY[idx].MACD_Slow == mACD_Slow && cachedpXRAY[idx].MACD_Smooth == mACD_Smooth && cachedpXRAY[idx].ADX_Period == aDX_Period && cachedpXRAY[idx].Z_Order == z_Order && cachedpXRAY[idx].EqualsInput(input))
						return cachedpXRAY[idx];
			return CacheIndicator<dpXRAY>(new dpXRAY(){ RSI_Period = rSI_Period, RSI_Smooth = rSI_Smooth, MACD_Fast = mACD_Fast, MACD_Slow = mACD_Slow, MACD_Smooth = mACD_Smooth, ADX_Period = aDX_Period, Z_Order = z_Order }, input, ref cachedpXRAY);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.dpXRAY dpXRAY(int rSI_Period, int rSI_Smooth, int mACD_Fast, int mACD_Slow, int mACD_Smooth, int aDX_Period, int z_Order)
		{
			return indicator.dpXRAY(Input, rSI_Period, rSI_Smooth, mACD_Fast, mACD_Slow, mACD_Smooth, aDX_Period, z_Order);
		}

		public Indicators.dpXRAY dpXRAY(ISeries<double> input , int rSI_Period, int rSI_Smooth, int mACD_Fast, int mACD_Slow, int mACD_Smooth, int aDX_Period, int z_Order)
		{
			return indicator.dpXRAY(input, rSI_Period, rSI_Smooth, mACD_Fast, mACD_Slow, mACD_Smooth, aDX_Period, z_Order);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.dpXRAY dpXRAY(int rSI_Period, int rSI_Smooth, int mACD_Fast, int mACD_Slow, int mACD_Smooth, int aDX_Period, int z_Order)
		{
			return indicator.dpXRAY(Input, rSI_Period, rSI_Smooth, mACD_Fast, mACD_Slow, mACD_Smooth, aDX_Period, z_Order);
		}

		public Indicators.dpXRAY dpXRAY(ISeries<double> input , int rSI_Period, int rSI_Smooth, int mACD_Fast, int mACD_Slow, int mACD_Smooth, int aDX_Period, int z_Order)
		{
			return indicator.dpXRAY(input, rSI_Period, rSI_Smooth, mACD_Fast, mACD_Slow, mACD_Smooth, aDX_Period, z_Order);
		}
	}
}

#endregion
