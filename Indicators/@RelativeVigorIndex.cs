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
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators
{
	/// <summary>
	/// The Relative Vigor Index measures the strength of a trend by comparing an instruments closing price to its price range. It's based on the fact that prices tend to close higher than they open in up trends, and closer lower than they open in downtrends.
	/// </summary>
	public class RelativeVigorIndex : Indicator
	{
		private Series<double> series1;
		private Series<double> series2;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= Custom.Resource.NinjaScriptIndicatorDescriptionRelativeVigorIndex;
				Name						= Custom.Resource.NinjaScriptIndicatorNameRelativeVigorIndex;
				Calculate					= Calculate.OnBarClose;
				IsOverlay					= false;
				DrawOnPricePanel			= false;
				IsSuspendedWhileInactive	= true;
				Period 						= 10;

				AddPlot(Brushes.Green,	Custom.Resource.NinjaScriptIndicatorRelativeVigorIndex);
				AddPlot(Brushes.Red,		Custom.Resource.NinjaScriptIndicatorSignal);
			}
			else if (State == State.DataLoaded)
			{
				series1 = new Series<double>(this);
				series2 = new Series<double>(this);
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar < 3)
				return;
			
			series1[0] = (Close[0] - Open[0] + 2 * (Close[1] - Open[1]) + 2 * (Close[2] - Open[2]) + (Close[3] - Open[3])) / 6.0;
			series2[0] = (High[0] - Low[0] + 2 * (High[1] - Low[1]) + 2 * (High[2] - Low[2]) + (High[3] - Low[3])) / 6.0;

			double numerator 	= 0;
			double denominator 	= 0;

			for (int i = 0; i < Math.Min(CurrentBar, Period); i++)
			{
				numerator 	+= series1[i];
				denominator += series2[i];
			}
			
			if (denominator != 0)
			{
				Value[0] 	= numerator / denominator;
				Signal[0] 	= (Value[0] + 2 * Value[1] + 2 * Value[2] + Value[3]) / 6.0;
			}
		}

		#region Properties
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> Default => Values[0];

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> Signal => Values[1];

		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Period", GroupName = "NinjaScriptParameters", Order = 0)]
		public int Period { get; set; }
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private RelativeVigorIndex[] cacheRelativeVigorIndex;
		public RelativeVigorIndex RelativeVigorIndex(int period)
		{
			return RelativeVigorIndex(Input, period);
		}

		public RelativeVigorIndex RelativeVigorIndex(ISeries<double> input, int period)
		{
			if (cacheRelativeVigorIndex != null)
				for (int idx = 0; idx < cacheRelativeVigorIndex.Length; idx++)
					if (cacheRelativeVigorIndex[idx] != null && cacheRelativeVigorIndex[idx].Period == period && cacheRelativeVigorIndex[idx].EqualsInput(input))
						return cacheRelativeVigorIndex[idx];
			return CacheIndicator<RelativeVigorIndex>(new RelativeVigorIndex(){ Period = period }, input, ref cacheRelativeVigorIndex);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.RelativeVigorIndex RelativeVigorIndex(int period)
		{
			return indicator.RelativeVigorIndex(Input, period);
		}

		public Indicators.RelativeVigorIndex RelativeVigorIndex(ISeries<double> input , int period)
		{
			return indicator.RelativeVigorIndex(input, period);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.RelativeVigorIndex RelativeVigorIndex(int period)
		{
			return indicator.RelativeVigorIndex(Input, period);
		}

		public Indicators.RelativeVigorIndex RelativeVigorIndex(ISeries<double> input , int period)
		{
			return indicator.RelativeVigorIndex(input, period);
		}
	}
}

#endregion
