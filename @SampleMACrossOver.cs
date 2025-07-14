//
// Copyright (C) 2025, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
#region Using declarations
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.NinjaScript.Indicators;
#endregion

//This namespace holds strategies in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Strategies
{
	public class SampleMACrossOver : Strategy
	{
		private SMA smaFast;
		private SMA smaSlow;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description	= Custom.Resource.NinjaScriptStrategyDescriptionSampleMACrossOver;
				Name		= Custom.Resource.NinjaScriptStrategyNameSampleMACrossOver;
				Fast		= 10;
				Slow		= 25;
				// This strategy has been designed to take advantage of performance gains in Strategy Analyzer optimizations
				// See the Help Guide for additional information
				IsInstantiatedOnEachOptimizationIteration = false;
			}
			else if (State == State.DataLoaded)
			{
				smaFast = SMA(Fast);
				smaSlow = SMA(Slow);

				smaFast.Plots[0].Brush = Brushes.Goldenrod;
				smaSlow.Plots[0].Brush = Brushes.SeaGreen;

				AddChartIndicator(smaFast);
				AddChartIndicator(smaSlow);
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar < BarsRequiredToTrade)
				return;

			if (CrossAbove(smaFast, smaSlow, 1))
				EnterLong();
			else if (CrossBelow(smaFast, smaSlow, 1))
				EnterShort();
		}

		#region Properties
		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Fast", GroupName = "NinjaScriptStrategyParameters", Order = 0)]
		public int Fast { get; set; }

		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Slow", GroupName = "NinjaScriptStrategyParameters", Order = 1)]
		public int Slow { get; set; }
		#endregion
	}
}
