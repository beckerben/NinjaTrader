//
// Copyright (C) 2025, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
#region Using declarations
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
#endregion

//This namespace holds indicators in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators
{
	/// <summary>
	/// T3 Moving Average
	/// </summary>
	public class T3 : Indicator
	{
		private System.Collections.ArrayList seriesCollection;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= Custom.Resource.NinjaScriptIndicatorDescriptionT3;
				Name						= Custom.Resource.NinjaScriptIndicatorNameT3;
				IsSuspendedWhileInactive	= true;
				IsOverlay					= true;
				Period						= 14;
				TCount						= 3;
				VFactor						= 0.7;

				AddPlot(Brushes.DarkCyan, Custom.Resource.NinjaScriptIndicatorNameT3);
			}
		}

		protected override void OnBarUpdate()
		{
			if (TCount == 1)
			{
				CalculateGd(Inputs[0], Values[0]);
				return;
			}

			if (seriesCollection == null)
			{
				seriesCollection = new System.Collections.ArrayList();
				for (int i = 0; i < TCount - 1; i++)
					seriesCollection.Add(new Series<double>(this));
			}

			CalculateGd(Inputs[0], (Series<double>) seriesCollection[0]);

			for (int i = 0; i <= seriesCollection.Count - 2; i++)
				CalculateGd((Series<double>) seriesCollection[i], (Series<double>) seriesCollection[i + 1]);

			CalculateGd((Series<double>) seriesCollection[seriesCollection.Count - 1], Values[0]);
		}

		private void CalculateGd(ISeries<double> input, Series<double> output)
			=> output[0] = EMA(input, Period)[0] * (1 + VFactor) - EMA(EMA(input, Period), Period)[0] * VFactor;

		#region Properties
		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Period", GroupName = "NinjaScriptParameters", Order = 0)]
		public int Period { get; set; }

		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "TCount", GroupName = "NinjaScriptParameters", Order = 1)]
		public int TCount { get; set; }

		[Range(0, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "VFactor", GroupName = "NinjaScriptParameters", Order = 2)]
		public double VFactor { get; set; }
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private T3[] cacheT3;
		public T3 T3(int period, int tCount, double vFactor)
		{
			return T3(Input, period, tCount, vFactor);
		}

		public T3 T3(ISeries<double> input, int period, int tCount, double vFactor)
		{
			if (cacheT3 != null)
				for (int idx = 0; idx < cacheT3.Length; idx++)
					if (cacheT3[idx] != null && cacheT3[idx].Period == period && cacheT3[idx].TCount == tCount && cacheT3[idx].VFactor == vFactor && cacheT3[idx].EqualsInput(input))
						return cacheT3[idx];
			return CacheIndicator<T3>(new T3(){ Period = period, TCount = tCount, VFactor = vFactor }, input, ref cacheT3);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.T3 T3(int period, int tCount, double vFactor)
		{
			return indicator.T3(Input, period, tCount, vFactor);
		}

		public Indicators.T3 T3(ISeries<double> input , int period, int tCount, double vFactor)
		{
			return indicator.T3(input, period, tCount, vFactor);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.T3 T3(int period, int tCount, double vFactor)
		{
			return indicator.T3(Input, period, tCount, vFactor);
		}

		public Indicators.T3 T3(ISeries<double> input , int period, int tCount, double vFactor)
		{
			return indicator.T3(input, period, tCount, vFactor);
		}
	}
}

#endregion
