//
// Copyright (C) 2025, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
#region Using declarations
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using System.Xml.Serialization;
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators
{
	/// <summary>
	/// The Disparity Index measures the difference between the price and an exponential moving average. A value greater could suggest bullish momentum, while a value less than zero could suggest bearish momentum.
	/// </summary>
	public class DisparityIndex : Indicator
	{
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= Custom.Resource.NinjaScriptIndicatorDescriptionDisparityIndex;
				Name						= Custom.Resource.NinjaScriptIndicatorNameDisparityIndex;
				IsOverlay					= false;
				IsSuspendedWhileInactive	= true;
				Period 						= 25;
				
				AddPlot(Brushes.DodgerBlue,   Custom.Resource.NinjaScriptIndicatorDisparityLine);
				AddLine(Brushes.DarkGray, 0,  Custom.Resource.NinjaScriptIndicatorZeroLine);
			}
		}

		protected override void OnBarUpdate()
		{
			if (Close[0] <= 0)
				return;
			
			DisparityLine[0] = 100 * (Close[0] - EMA(Close, Period)[0]) / Close[0];
		}	
		
		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Period", GroupName = "NinjaScriptParameters", Order = 0)]
		public int Period { get; set; }

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> DisparityLine => Values[0];
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private DisparityIndex[] cacheDisparityIndex;
		public DisparityIndex DisparityIndex(int period)
		{
			return DisparityIndex(Input, period);
		}

		public DisparityIndex DisparityIndex(ISeries<double> input, int period)
		{
			if (cacheDisparityIndex != null)
				for (int idx = 0; idx < cacheDisparityIndex.Length; idx++)
					if (cacheDisparityIndex[idx] != null && cacheDisparityIndex[idx].Period == period && cacheDisparityIndex[idx].EqualsInput(input))
						return cacheDisparityIndex[idx];
			return CacheIndicator<DisparityIndex>(new DisparityIndex(){ Period = period }, input, ref cacheDisparityIndex);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.DisparityIndex DisparityIndex(int period)
		{
			return indicator.DisparityIndex(Input, period);
		}

		public Indicators.DisparityIndex DisparityIndex(ISeries<double> input , int period)
		{
			return indicator.DisparityIndex(input, period);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.DisparityIndex DisparityIndex(int period)
		{
			return indicator.DisparityIndex(Input, period);
		}

		public Indicators.DisparityIndex DisparityIndex(ISeries<double> input , int period)
		{
			return indicator.DisparityIndex(input, period);
		}
	}
}

#endregion
