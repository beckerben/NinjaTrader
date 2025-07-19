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
	public class FairValueGap : Indicator
	{
		int _count = 0;
		int _lastUpGap = 0;
		double _lastUpGapPrice = 0;
		int _lastDownGap = 0;
		double _lastDownGapPrice = 0;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Indicator here.";
				Name										= "FairValueGap";
				Calculate									= Calculate.OnBarClose;
				IsOverlay									= false;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= true;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				//Disable this property if your indicator requires custom values that cumulate with each new market data event. 
				//See Help Guide for additional information.
				IsSuspendedWhileInactive					= true;
				
				Gap = 4;
				LongArrowBrush = Brushes.Blue;
				ShortArrowBrush = Brushes.Orange;
				
				AddPlot(Brushes.Goldenrod, "Gap");
			}
			else if (State == State.Configure)
			{
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar < 3) return;
			
			Value[0] = 0;
			
			if (Low[2] - High[0] > Gap * TickSize)
			{		
				Draw.Rectangle(this, "Gap " + _count, 2, Low[2], 0, High[0], ShortArrowBrush, true);
				_lastDownGap = CurrentBar + 1;
				_lastDownGapPrice = Low[2];
				_count++;
				Value[0] = -1;
			}
			
			if (Low[0] - High[2] > Gap * TickSize)
			{		
				Draw.Rectangle(this, "Gap " + _count, 2, High[2], 0, Low[0], LongArrowBrush, true);
				_lastUpGap = CurrentBar + 1;
				_lastUpGapPrice = High[2];
				_count++;
				Value[0] = 1;
			}
		}
			
		public int LastUpGap()
		{ return _lastUpGap; }
		
		public double LastUpGapPrice()
		{ return _lastUpGapPrice; }
		
		public int LastDownGap()
		{ return _lastDownGap; }
		
		public double LastDownGapPrice()
		{ return _lastDownGapPrice; }
		
		[NinjaScriptProperty]
		public int Gap
		{ get; set; }
		
				[XmlIgnore]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Pulled Ask Color", Description = "Sets the color of the box drawn when ask pulls.", GroupName = "Visual", Order = 0)]
		public System.Windows.Media.Brush LongArrowBrush
		{ 
			get; set;
		}

		[Browsable(false)]
		public string LongArrowBrushSerializable
		{
			get { return Serialize.BrushToString(LongArrowBrush); }
			set { LongArrowBrush = Serialize.StringToBrush(value); }
		}
		
		[XmlIgnore]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Pulled Bid Color", Description = "Sets the color of the box drawn when bid pulls.", GroupName = "Visual", Order = 1)]
		public System.Windows.Media.Brush ShortArrowBrush
		{ 
			get; set;
		}

		[Browsable(false)]
		public string ShortArrowBrushSerializable
		{
			get { return Serialize.BrushToString(ShortArrowBrush); }
			set { ShortArrowBrush = Serialize.StringToBrush(value); }
		}
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private FairValueGap[] cacheFairValueGap;
		public FairValueGap FairValueGap(int gap)
		{
			return FairValueGap(Input, gap);
		}

		public FairValueGap FairValueGap(ISeries<double> input, int gap)
		{
			if (cacheFairValueGap != null)
				for (int idx = 0; idx < cacheFairValueGap.Length; idx++)
					if (cacheFairValueGap[idx] != null && cacheFairValueGap[idx].Gap == gap && cacheFairValueGap[idx].EqualsInput(input))
						return cacheFairValueGap[idx];
			return CacheIndicator<FairValueGap>(new FairValueGap(){ Gap = gap }, input, ref cacheFairValueGap);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.FairValueGap FairValueGap(int gap)
		{
			return indicator.FairValueGap(Input, gap);
		}

		public Indicators.FairValueGap FairValueGap(ISeries<double> input , int gap)
		{
			return indicator.FairValueGap(input, gap);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.FairValueGap FairValueGap(int gap)
		{
			return indicator.FairValueGap(Input, gap);
		}

		public Indicators.FairValueGap FairValueGap(ISeries<double> input , int gap)
		{
			return indicator.FairValueGap(input, gap);
		}
	}
}

#endregion
