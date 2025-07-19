//
// Copyright (C) 2025, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
#region Using declarations
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Data;
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators
{
	/// <summary>
	/// Displays ask, bid, and/or last lines on the chart.
	/// </summary>
	public class PriceLine : Indicator
	{
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description						= Custom.Resource.NinjaScriptIndicatorDescriptionPriceLine;
				Name							= Custom.Resource.NinjaScriptIndicatorNamePriceLine;
				Calculate						= Calculate.OnPriceChange;
				IsOverlay						= true;
				ShowTransparentPlotsInDataBox	= false;
				DrawOnPricePanel				= true;
				IsSuspendedWhileInactive 		= true;
				ShowAskLine 					= false;
				ShowBidLine 					= false;
				ShowLastLine 					= true;
				AskLineLength 					= 100;
				BidLineLength 					= 100;
				LastLineLength 					= 100;
				AskStroke						= new Stroke(Brushes.DarkGreen, DashStyleHelper.Dash, 1);
				BidStroke						= new Stroke(Brushes.Blue, DashStyleHelper.Dash, 1);
				LastStroke						= new Stroke(Brushes.Yellow, DashStyleHelper.Dash, 1);
			}
			else if (State == State.Configure)
			{
				AddPlot(ShowAskLine ? AskStroke.Brush : Brushes.Transparent,		Custom.Resource.PriceLinePlotAsk);
				AddPlot(ShowBidLine ? BidStroke.Brush : Brushes.Transparent,		Custom.Resource.PriceLinePlotBid);
				AddPlot(ShowLastLine ? LastStroke.Brush : Brushes.Transparent,	Custom.Resource.PriceLinePlotLast);
			}
		}

		protected override void OnBarUpdate() { }
		
		public override void OnCalculateMinMax()
		{
			double tmpMin = double.MaxValue;
			double tmpMax = double.MinValue;
			
			if (Values[0].Count > 0 && ShowAskLine && Values[0].IsValidDataPointAt(Values[0].Count - 1))
			{
				double askTmp = Values[0].GetValueAt(Values[0].Count - 1);
				tmpMin = Math.Min(tmpMin, askTmp);
				tmpMax = Math.Max(tmpMax, askTmp);
			}
			
			if (Values[1].Count > 0 && ShowBidLine && Values[1].IsValidDataPointAt(Values[1].Count - 1))
			{
				double bidTmp = Values[1].GetValueAt(Values[1].Count - 1);
				tmpMin = Math.Min(tmpMin, bidTmp);
				tmpMax = Math.Max(tmpMax, bidTmp);
			}
			
			if (Values[2].Count > 0 && ShowLastLine && Values[2].IsValidDataPointAt(Values[2].Count - 1))
			{
				double lastTmp = Values[2].GetValueAt(Values[2].Count - 1);
				tmpMin = Math.Min(tmpMin, lastTmp);
				tmpMax = Math.Max(tmpMax, lastTmp);
			}
			
			MinValue = tmpMin;
			MaxValue = tmpMax;
		}
		
		protected override void OnMarketData(MarketDataEventArgs e)
		{
			if (e.MarketDataType != MarketDataType.Last || CurrentBar < 0)
				return;
			
			Values[0][0] = e.Ask;
			Values[1][0] = e.Bid;
			Values[2][0] = e.Price;
		}

		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
			if (BarsArray[0] == null || ChartBars == null)
				return;
			
			ChartPanel	panel 	= chartControl.ChartPanels[chartScale.PanelIndex];
			float 		endX 	= panel.X + panel.W;
			
			if (Values[0].Count > 0 && ShowAskLine && Values[0].IsValidDataPointAt(Values[0].Count - 1))
			{
				float startX 	= Convert.ToSingle(panel.X + panel.W * (1 - AskLineLength / 100.0));
				float y 		= chartScale.GetYByValue(Values[0].GetValueAt(Values[0].Count - 1));
				
				RenderTarget.DrawLine(new SharpDX.Vector2(startX, y), new SharpDX.Vector2(endX, y), AskStroke.BrushDX, AskStroke.Width, AskStroke.StrokeStyle);
			}
			
			if (Values[1].Count > 0 && ShowBidLine && Values[1].IsValidDataPointAt(Values[1].Count - 1))
			{
				float startX 	= Convert.ToSingle(panel.X + panel.W * (1 - BidLineLength / 100.0));
				float y 		= chartScale.GetYByValue(Values[1].GetValueAt(Values[1].Count - 1));
				
				RenderTarget.DrawLine(new SharpDX.Vector2(startX, y), new SharpDX.Vector2(endX, y), BidStroke.BrushDX, BidStroke.Width, BidStroke.StrokeStyle);
			}
			
			if (Values[2].Count > 0 && ShowLastLine && Values[2].IsValidDataPointAt(Values[2].Count - 1))
			{
				float startX 	= Convert.ToSingle(panel.X + panel.W * (1 - LastLineLength / 100.0));
				float y 		= chartScale.GetYByValue(Values[2].GetValueAt(Values[2].Count - 1));
				
				RenderTarget.DrawLine(new SharpDX.Vector2(startX, y), new SharpDX.Vector2(endX, y), LastStroke.BrushDX, LastStroke.Width, LastStroke.StrokeStyle);
			}
		}
		
		public override void OnRenderTargetChanged()
		{
			AskStroke	.RenderTarget = RenderTarget;
			BidStroke	.RenderTarget = RenderTarget;
			LastStroke	.RenderTarget = RenderTarget;
		}
		
		#region Properties
		[XmlIgnore]
		[Browsable(false)]
		public double AskLine => Values[0].GetValueAt(Values[0].Count - 1);

		[XmlIgnore]
		[Browsable(false)]
		public double BidLine => Values[1].GetValueAt(Values[1].Count - 1);

		[XmlIgnore]
		[Browsable(false)]
		public double LastLine => Values[2].GetValueAt(Values[2].Count - 1);

		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "ShowAskLine", GroupName = "NinjaScriptParameters", Order = 0)]
		public bool ShowAskLine { get; set; }
		
		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "ShowBidLine", GroupName = "NinjaScriptParameters", Order = 1)]
		public bool ShowBidLine { get; set; }
		
		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "ShowLastLine", GroupName = "NinjaScriptParameters", Order = 2)]
		public bool ShowLastLine { get; set; }
		
		[Range(1, 100)]
		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "AskLineLength", GroupName = "NinjaScriptParameters", Order = 3)]
		public int AskLineLength { get; set; }
		
		[Range(1, 100)]
		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "BidLineLength", GroupName = "NinjaScriptParameters", Order = 4)]
		public int BidLineLength { get; set; }
		
		[Range(1, 100)]
		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "LastLineLength", GroupName = "NinjaScriptParameters", Order = 5)]
		public int LastLineLength { get; set; }
		
		[Display(ResourceType = typeof(Custom.Resource), Name = "AskLineStroke", GroupName = "NinjaScriptIndicatorVisualGroup", Order = 1800)]
		public Stroke AskStroke { get; set; }

		[Display(ResourceType = typeof(Custom.Resource), Name = "BidLineStroke", GroupName = "NinjaScriptIndicatorVisualGroup", Order = 1810)]
		public Stroke BidStroke { get; set; }

		[Display(ResourceType = typeof(Custom.Resource), Name = "LastLineStroke", GroupName = "NinjaScriptIndicatorVisualGroup", Order = 1820)]
		public Stroke LastStroke { get; set; }
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private PriceLine[] cachePriceLine;
		public PriceLine PriceLine(bool showAskLine, bool showBidLine, bool showLastLine, int askLineLength, int bidLineLength, int lastLineLength)
		{
			return PriceLine(Input, showAskLine, showBidLine, showLastLine, askLineLength, bidLineLength, lastLineLength);
		}

		public PriceLine PriceLine(ISeries<double> input, bool showAskLine, bool showBidLine, bool showLastLine, int askLineLength, int bidLineLength, int lastLineLength)
		{
			if (cachePriceLine != null)
				for (int idx = 0; idx < cachePriceLine.Length; idx++)
					if (cachePriceLine[idx] != null && cachePriceLine[idx].ShowAskLine == showAskLine && cachePriceLine[idx].ShowBidLine == showBidLine && cachePriceLine[idx].ShowLastLine == showLastLine && cachePriceLine[idx].AskLineLength == askLineLength && cachePriceLine[idx].BidLineLength == bidLineLength && cachePriceLine[idx].LastLineLength == lastLineLength && cachePriceLine[idx].EqualsInput(input))
						return cachePriceLine[idx];
			return CacheIndicator<PriceLine>(new PriceLine(){ ShowAskLine = showAskLine, ShowBidLine = showBidLine, ShowLastLine = showLastLine, AskLineLength = askLineLength, BidLineLength = bidLineLength, LastLineLength = lastLineLength }, input, ref cachePriceLine);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.PriceLine PriceLine(bool showAskLine, bool showBidLine, bool showLastLine, int askLineLength, int bidLineLength, int lastLineLength)
		{
			return indicator.PriceLine(Input, showAskLine, showBidLine, showLastLine, askLineLength, bidLineLength, lastLineLength);
		}

		public Indicators.PriceLine PriceLine(ISeries<double> input , bool showAskLine, bool showBidLine, bool showLastLine, int askLineLength, int bidLineLength, int lastLineLength)
		{
			return indicator.PriceLine(input, showAskLine, showBidLine, showLastLine, askLineLength, bidLineLength, lastLineLength);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.PriceLine PriceLine(bool showAskLine, bool showBidLine, bool showLastLine, int askLineLength, int bidLineLength, int lastLineLength)
		{
			return indicator.PriceLine(Input, showAskLine, showBidLine, showLastLine, askLineLength, bidLineLength, lastLineLength);
		}

		public Indicators.PriceLine PriceLine(ISeries<double> input , bool showAskLine, bool showBidLine, bool showLastLine, int askLineLength, int bidLineLength, int lastLineLength)
		{
			return indicator.PriceLine(input, showAskLine, showBidLine, showLastLine, askLineLength, bidLineLength, lastLineLength);
		}
	}
}

#endregion
