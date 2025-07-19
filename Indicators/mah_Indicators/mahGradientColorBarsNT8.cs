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
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion


    /// Ref: https://futures.io/elite-circle/20413-gradient-color-bars.html
    /*
	// Written and published By: Aligator on BMT Forum
	// This indicator is intended for TRUE ELITE users at BMT.
	// PLEASE DO NOT DISTRIBUTE OUTSIDE THE ELITE GROUP ON BMT
	
	REVISION HISTORY
	. Rev 0, Original framework - May 21, 2011 - NT7
	. Revision 2, added candle bar code on startup and an option for candle opasity - May 23, 2012 - NT7
	. Revision 3, added candle outline - Jnue 20, 2012 - NT7
	. Revision NT8, June 12, 2018 compiled under NT8.014.0

	. Note: When customizing, use NinjaTrader default sequesnce for indicators' peroids input.
		i.e. for default Stochastics(Period01, Period02, Period03); 7,14,3
	
	Gradient Color Bars:
	________________________________________
	Gradient Color Bars Indicator

	The concept of coloring bars is nothing new and everyone does it for certain filters.
	However, the idea for using gradient color bars to spot price divergence may not be 
	new, but has gone un-noticed by many.

	The fact that no one (I know, there will be some claims to the contrary) has been able
	to write a decent divergence indicator is because computers can only follow rules and
	limits. Human eye on the other hand can spot a bird in the sky whether it is 100 yards
	away or 99.99999 or 100.00001, regardless of the number of decimals (yes, provided the
	person is not blind).

	So, it makes sense if one can spot divergence by subtle changes in the gradient coloring
	of the bars where a computer might miss because it was not instructed to “see.”

	This indicator is very simple and basic and the bar coloring idea is not original. What
	I think is original (for future claims) is applying the concept to a variety of conditions
	and filters to spot divergences (among other price behaviors) where the usual indicators
	and computers will miss most of the time.

	This simple indicator has much potential. So, I am posting it here and hope some of you
	brilliant people will contribute to the idea. The current version is based on one filter
	that you can easily change. The goal is to bundle a number of filters and let the
	coloring be done based on the most relevant and promising filter.
		
	As with any indicator (Stoch, RSI, etc.), this filter cannot be used on its own alone.
	I am considering to use one version of this indicator for confirming price action and
	formations at different time frames as a trading tool.
		
	So, good luck and please feel free to post and contribute ideas and modifications on this
	thread. Please post any new revisions, templates, etc. under the second post of this
	thread until a complete version can be developed and placed in BMT download area.
	Stay positive, Thanks (thanks will lead to better versions).

	Cheers!!
	*/

#region Divergence Method Enums
public enum DivergenceMethodNT8
	{
		Stochastics,
		RMI,
		RSI,
	}
#endregion	
	
	
namespace NinjaTrader.NinjaScript.Indicators.mah_Indicators
{
    [Description("Colors bars based on the value of the indicator or conditions programed")]
    public class mahGradientColorBarsNT8 : Indicator
    {
		private bool 		outlineColor		= true;
		private Brush 		upOutlineColor 		= Brushes.Ivory;
		private Brush 		downOutlineColor	= Brushes.DimGray;		
		private	int 		opacity				= 3;		
		private bool 		candles				= true;
		
		private			DivergenceMethodNT8 divType			= DivergenceMethodNT8.RMI;
		private			Series<double> 						typeValue;
		private			Series<double>				 		divTool;		
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description							= @"Colors bars based on the value of the indicator or conditions programed";
				Name								= "mahGradientColorBarsNT8";
				IsSuspendedWhileInactive 	= true;
				IsOverlay					= true;
				Period01					= 14;	// Slow period
				Period02	    			= 3;	// Fast period
				Period03					= 3;	// Smooth period
				
				typeValue			= new Series<double> (this);
				divTool				= new Series<double> (this); 				
			}

			else if (State == State.Configure)
			{
				if (ChartControl != null && ChartBars.Properties.ChartStyleType == ChartStyleType.CandleStick)					 
					 
			     {
					  candles = true;
				 }
			    else
				 {
						 candles = false;
				 }
				 
			// make sure custom brushws are "Frozen" to avoid multi-threading issues
				 
			upOutlineColor.Freeze();
			downOutlineColor.Freeze();
				 		 
			}
		}

        protected override void OnBarUpdate()
        {
 
		    if (CurrentBar < 26)
            return;
			
//New			
			double typeValue = 0;

			//Calculate Indicator Value			
			switch (DivType) 
			{	
				case DivergenceMethodNT8.Stochastics:
					typeValue = Stochastics(Period01, Period02, Period03).D[0];
					break;

				case DivergenceMethodNT8.RMI:
					typeValue = RMI(Period01,Period02).Default[0];
					break;
				
				case DivergenceMethodNT8.RSI:
					typeValue = RSI(Period01,Period02).Default[0];
					break;
			}			
					

			int upColor = 0;
			int downColor = 0;
			double Value = typeValue; //Stochastics(Period01,Period02, Period03).D[0];
			downColor = (int) (255*(Value)/100);
			upColor = 255-downColor;
			
			Brush hollow = new SolidColorBrush(Color.FromRgb(Convert.ToByte(downColor), Convert.ToByte(upColor), Convert.ToByte(0)));			
			hollow.Freeze(); 

			BarBrush = hollow;

			//Color Candle Outline
			if (OutlineColor)
			{
			if (Open[0] >= Close[0])
			CandleOutlineBrush = downOutlineColor;
			else CandleOutlineBrush = upOutlineColor;
			}
        }

        #region Properties
		[Browsable(false)]
		[XmlIgnore()]
		public Series<double> TypeValue
		{
			get { return Values[0]; }
		}			
		
		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Period01", GroupName = "NinjaScriptParameters", Order = 0)]
		public int Period01
		{ get; set; }

		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Period02", GroupName = "NinjaScriptParameters", Order = 1)]
		public int Period02
		{ get; set; }
		
		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Period03", GroupName = "NinjaScriptParameters", Order = 2)]
		public int Period03
		{ get; set; }
		
		[Display(Name="OutlineColor", Description="Gradient Candle Outline Colors?", GroupName="Candle Color Parameters", Order = 3)]
		public bool OutlineColor
		{ get; set; }
		
		[Display(Name = "Up Candle Outline", Description = "Color for Up Candle Outline", GroupName = "2. Candle Color Parameters", Order = 4)]		
        public Brush UpOutlineColor
        {
            get { return upOutlineColor; }
            set { upOutlineColor = value; }
        }
		
		[Browsable(false)]
		public string UpOutlineColorSerialize
		{
			get { return Serialize.BrushToString(upOutlineColor); }
			set { upOutlineColor = Serialize.StringToBrush(value); }
		}		
		
		[Display(Name = "Down Candle Outline", Description = "Color for Down Candle Outline", GroupName = "2. Candle Color Parameters", Order = 5)]	
        public Brush DownOutlineColor
        {
            get { return downOutlineColor; }
            set { downOutlineColor = value; }
        }
		
		[Browsable(false)]
		public string DownOutlineColorSerialize
		{
			get { return Serialize.BrushToString(downOutlineColor); }
			set { downOutlineColor = Serialize.StringToBrush(value); }
		}
		
		[NinjaScriptProperty]		
		[Display(Name = "1.Divergence Type", Description = "Indicator Method", GroupName = "1. Divergence Indicators", Order = 12)]		
		public DivergenceMethodNT8 DivType
		{
			get { return divType; }
			set { divType = value; }
		}			
		#endregion
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private mah_Indicators.mahGradientColorBarsNT8[] cachemahGradientColorBarsNT8;
		public mah_Indicators.mahGradientColorBarsNT8 mahGradientColorBarsNT8(int period01, int period02, int period03, DivergenceMethodNT8 divType)
		{
			return mahGradientColorBarsNT8(Input, period01, period02, period03, divType);
		}

		public mah_Indicators.mahGradientColorBarsNT8 mahGradientColorBarsNT8(ISeries<double> input, int period01, int period02, int period03, DivergenceMethodNT8 divType)
		{
			if (cachemahGradientColorBarsNT8 != null)
				for (int idx = 0; idx < cachemahGradientColorBarsNT8.Length; idx++)
					if (cachemahGradientColorBarsNT8[idx] != null && cachemahGradientColorBarsNT8[idx].Period01 == period01 && cachemahGradientColorBarsNT8[idx].Period02 == period02 && cachemahGradientColorBarsNT8[idx].Period03 == period03 && cachemahGradientColorBarsNT8[idx].DivType == divType && cachemahGradientColorBarsNT8[idx].EqualsInput(input))
						return cachemahGradientColorBarsNT8[idx];
			return CacheIndicator<mah_Indicators.mahGradientColorBarsNT8>(new mah_Indicators.mahGradientColorBarsNT8(){ Period01 = period01, Period02 = period02, Period03 = period03, DivType = divType }, input, ref cachemahGradientColorBarsNT8);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.mah_Indicators.mahGradientColorBarsNT8 mahGradientColorBarsNT8(int period01, int period02, int period03, DivergenceMethodNT8 divType)
		{
			return indicator.mahGradientColorBarsNT8(Input, period01, period02, period03, divType);
		}

		public Indicators.mah_Indicators.mahGradientColorBarsNT8 mahGradientColorBarsNT8(ISeries<double> input , int period01, int period02, int period03, DivergenceMethodNT8 divType)
		{
			return indicator.mahGradientColorBarsNT8(input, period01, period02, period03, divType);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.mah_Indicators.mahGradientColorBarsNT8 mahGradientColorBarsNT8(int period01, int period02, int period03, DivergenceMethodNT8 divType)
		{
			return indicator.mahGradientColorBarsNT8(Input, period01, period02, period03, divType);
		}

		public Indicators.mah_Indicators.mahGradientColorBarsNT8 mahGradientColorBarsNT8(ISeries<double> input , int period01, int period02, int period03, DivergenceMethodNT8 divType)
		{
			return indicator.mahGradientColorBarsNT8(input, period01, period02, period03, divType);
		}
	}
}

#endregion
