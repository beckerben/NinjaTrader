//
// Copyright (C) 2025, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
#region Using declarations
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Gui;
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators
{
	/// <summary>
	/// The Moving Average Ribbon is a series of incrementing moving averages.
	/// </summary>
	public class MovingAverageRibbon : Indicator
	{
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= Custom.Resource.NinjaScriptIndicatorDescriptionMovingAverageRibbon;
				Name						= Custom.Resource.NinjaScriptIndicatorNameMovingAverageRibbon;
				Calculate					= Calculate.OnBarClose;
				IsOverlay					= true;
				DrawOnPricePanel			= true;
				IsSuspendedWhileInactive	= true;
				MovingAverage 				= RibbonMAType.Exponential;
				BasePeriod 					= 10;
				IncrementalPeriod 			= 10;
				
				AddPlot(new Stroke(Brushes.Yellow,		DashStyleHelper.Solid, 1), PlotStyle.Line, Custom.Resource.MovingAverageRibbonPlot1);
				AddPlot(new Stroke(Brushes.Gold,			DashStyleHelper.Solid, 1), PlotStyle.Line, Custom.Resource.MovingAverageRibbonPlot2);
				AddPlot(new Stroke(Brushes.Goldenrod,		DashStyleHelper.Solid, 1), PlotStyle.Line, Custom.Resource.MovingAverageRibbonPlot3);
				AddPlot(new Stroke(Brushes.Orange,		DashStyleHelper.Solid, 1), PlotStyle.Line, Custom.Resource.MovingAverageRibbonPlot4);
				AddPlot(new Stroke(Brushes.DarkOrange,	DashStyleHelper.Solid, 1), PlotStyle.Line, Custom.Resource.MovingAverageRibbonPlot5);
				AddPlot(new Stroke(Brushes.Chocolate,		DashStyleHelper.Solid, 1), PlotStyle.Line, Custom.Resource.MovingAverageRibbonPlot6);
				AddPlot(new Stroke(Brushes.OrangeRed,		DashStyleHelper.Solid, 1), PlotStyle.Line, Custom.Resource.MovingAverageRibbonPlot7);
				AddPlot(new Stroke(Brushes.Red,			DashStyleHelper.Solid, 1), PlotStyle.Line, Custom.Resource.MovingAverageRibbonPlot8);
			}
		}

		protected override void OnBarUpdate()
		{
			for (int i = 0; i < 8; i++)
				Values[i][0] = MovingAverage switch
				{
					RibbonMAType.Exponential	=> EMA(Input, BasePeriod + IncrementalPeriod * i)[0],
					RibbonMAType.Hull			=> HMA(Input, BasePeriod + IncrementalPeriod * i)[0],
					RibbonMAType.Simple			=> SMA(Input, BasePeriod + IncrementalPeriod * i)[0],
					RibbonMAType.Weighted		=> WMA(Input, BasePeriod + IncrementalPeriod * i)[0],
					_							=> Values[i][0]
				};
		}
		
		#region Properties
		[XmlIgnore]
		[Browsable(false)]
		public Series<double> MovingAverage1 => Values[0];

		[XmlIgnore]
		[Browsable(false)]
		public Series<double> MovingAverage2 => Values[1];

		[XmlIgnore]
		[Browsable(false)]
		public Series<double> MovingAverage3 => Values[2];

		[XmlIgnore]
		[Browsable(false)]
		public Series<double> MovingAverage4 => Values[3];

		[XmlIgnore]
		[Browsable(false)]
		public Series<double> MovingAverage5 => Values[4];

		[XmlIgnore]
		[Browsable(false)]
		public Series<double> MovingAverage6 => Values[5];

		[XmlIgnore]
		[Browsable(false)]
		public Series<double> MovingAverage7 => Values[6];

		[XmlIgnore]
		[Browsable(false)]
		public Series<double> MovingAverage8 => Values[7];

		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "MovingAverage", GroupName = "NinjaScriptParameters", Order = 0)]
		public RibbonMAType MovingAverage { get; set; }
		
		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "BasePeriod", GroupName = "NinjaScriptParameters", Order = 1)]
		public int BasePeriod { get; set; }
		
		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "IncrementalPeriod", GroupName = "NinjaScriptParameters", Order = 2)]
		public int IncrementalPeriod { get; set; }
		#endregion
	}
}

public enum RibbonMAType
{
	Exponential = 0,
	Hull 		= 1,
	Simple 		= 2,
	Weighted 	= 3
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private MovingAverageRibbon[] cacheMovingAverageRibbon;
		public MovingAverageRibbon MovingAverageRibbon(RibbonMAType movingAverage, int basePeriod, int incrementalPeriod)
		{
			return MovingAverageRibbon(Input, movingAverage, basePeriod, incrementalPeriod);
		}

		public MovingAverageRibbon MovingAverageRibbon(ISeries<double> input, RibbonMAType movingAverage, int basePeriod, int incrementalPeriod)
		{
			if (cacheMovingAverageRibbon != null)
				for (int idx = 0; idx < cacheMovingAverageRibbon.Length; idx++)
					if (cacheMovingAverageRibbon[idx] != null && cacheMovingAverageRibbon[idx].MovingAverage == movingAverage && cacheMovingAverageRibbon[idx].BasePeriod == basePeriod && cacheMovingAverageRibbon[idx].IncrementalPeriod == incrementalPeriod && cacheMovingAverageRibbon[idx].EqualsInput(input))
						return cacheMovingAverageRibbon[idx];
			return CacheIndicator<MovingAverageRibbon>(new MovingAverageRibbon(){ MovingAverage = movingAverage, BasePeriod = basePeriod, IncrementalPeriod = incrementalPeriod }, input, ref cacheMovingAverageRibbon);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.MovingAverageRibbon MovingAverageRibbon(RibbonMAType movingAverage, int basePeriod, int incrementalPeriod)
		{
			return indicator.MovingAverageRibbon(Input, movingAverage, basePeriod, incrementalPeriod);
		}

		public Indicators.MovingAverageRibbon MovingAverageRibbon(ISeries<double> input , RibbonMAType movingAverage, int basePeriod, int incrementalPeriod)
		{
			return indicator.MovingAverageRibbon(input, movingAverage, basePeriod, incrementalPeriod);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.MovingAverageRibbon MovingAverageRibbon(RibbonMAType movingAverage, int basePeriod, int incrementalPeriod)
		{
			return indicator.MovingAverageRibbon(Input, movingAverage, basePeriod, incrementalPeriod);
		}

		public Indicators.MovingAverageRibbon MovingAverageRibbon(ISeries<double> input , RibbonMAType movingAverage, int basePeriod, int incrementalPeriod)
		{
			return indicator.MovingAverageRibbon(input, movingAverage, basePeriod, incrementalPeriod);
		}
	}
}

#endregion
