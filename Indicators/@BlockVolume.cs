//
// Copyright (C) 2025, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
#region Using declarations
using System;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Gui;
using NinjaTrader.Data;
#endregion

// This namespace holds indicators in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators
{
	public class BlockVolume : Indicator
	{
		private double 	blockValue;
		private int 	lastCurrentBar;
		private bool 	hasCarriedOverTransitionTick;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description	= Custom.Resource.NinjaScriptIndicatorDescriptionBlockVolume;
				Name		= Custom.Resource.NinjaScriptIndicatorNameBlockVolume;
				Calculate	= Calculate.OnBarClose;
				IsOverlay	= false;
				CountType	= CountType.Volume;
				BlockSize	= 80;

				AddPlot(new Stroke(Brushes.DarkRed, 2), PlotStyle.Bar, Custom.Resource.NinjaScriptIndicatorNameBlockVolume);
			}
			else if (State == State.Configure)
				AddDataSeries(BarsPeriodType.Tick, 1);
		}

		private void CalculateBlockVolume(bool forceCurrentBar)
		{
			bool inTransition 	= State == State.Realtime && BarsArray[1].Count - 1 - CurrentBars[1] > 1;
			int whatBar 		= State == State.Historical || inTransition || Calculate != Calculate.OnBarClose || forceCurrentBar ? CurrentBars[1] : Math.Min(CurrentBars[1] + 1, BarsArray[1].Count - 1);
			
			if ((Instrument.MasterInstrument.InstrumentType == Cbi.InstrumentType.CryptoCurrency ? Core.Globals.ToCryptocurrencyVolume(BarsArray[1].GetVolume(whatBar)) : BarsArray[1].GetVolume(whatBar)) >= BlockSize)
			{
				if (!inTransition && hasCarriedOverTransitionTick && !forceCurrentBar && Calculate == Calculate.OnBarClose)
					CalculateBlockVolume(true);
				
				hasCarriedOverTransitionTick 	= inTransition;
				blockValue 						+= CountType == CountType.Volume ? Instrument.MasterInstrument.InstrumentType == Cbi.InstrumentType.CryptoCurrency ? Core.Globals.ToCryptocurrencyVolume(BarsArray[1].GetVolume(whatBar)) : BarsArray[1].GetVolume(whatBar) : 1;
			}
		}
		
		protected override void OnBarUpdate()
		{			
			if (BarsInProgress == 0)
			{
				if (lastCurrentBar <= CurrentBars[0])
				{ 
					int indexOffset = BarsArray[1].Count - 1 - CurrentBars[1];

					if (lastCurrentBar < CurrentBars[0] && Calculate != Calculate.OnBarClose && (State == State.Realtime || BarsArray[0].IsTickReplay))
					{
						if (CurrentBars[0] > 0)
							Value[1] = blockValue;

						if (BarsArray[0].IsTickReplay || State == State.Realtime && indexOffset == 0)
							blockValue = 0;
					}

					Value[0] = blockValue;

					if (Calculate == Calculate.OnBarClose || lastCurrentBar < CurrentBars[0] && BarsArray[0].BarsType.IsIntraday && (State == State.Historical && BarsArray[0].Count - 1 - CurrentBars[0] > 0 || State == State.Realtime && indexOffset > 0))
						blockValue = 0;
				}

				lastCurrentBar = lastCurrentBar < CurrentBars[0] ? CurrentBars[0] : lastCurrentBar;
			}
			else
			{
				if (BarsArray[1].IsFirstBarOfSession && (Calculate != Calculate.OnBarClose || BarsArray[0].BarsType.IsIntraday))
					blockValue = 0;
				
				CalculateBlockVolume(false);
			}
		}

		#region Properties
		[Range(0.00000001, double.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "BlockTradeSize", GroupName = "NinjaScriptParameters", Order = 0)]
		public double BlockSize { get; set; }


		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "NinjaScriptIndicatorCount", GroupName = "NinjaScriptParameters", Order = 0)]
		public CountType CountType { get; set; }
		#endregion
	}
}

public enum CountType
{
	Trades,
	Volume
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private BlockVolume[] cacheBlockVolume;
		public BlockVolume BlockVolume(double blockSize, CountType countType)
		{
			return BlockVolume(Input, blockSize, countType);
		}

		public BlockVolume BlockVolume(ISeries<double> input, double blockSize, CountType countType)
		{
			if (cacheBlockVolume != null)
				for (int idx = 0; idx < cacheBlockVolume.Length; idx++)
					if (cacheBlockVolume[idx] != null && cacheBlockVolume[idx].BlockSize == blockSize && cacheBlockVolume[idx].CountType == countType && cacheBlockVolume[idx].EqualsInput(input))
						return cacheBlockVolume[idx];
			return CacheIndicator<BlockVolume>(new BlockVolume(){ BlockSize = blockSize, CountType = countType }, input, ref cacheBlockVolume);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.BlockVolume BlockVolume(double blockSize, CountType countType)
		{
			return indicator.BlockVolume(Input, blockSize, countType);
		}

		public Indicators.BlockVolume BlockVolume(ISeries<double> input , double blockSize, CountType countType)
		{
			return indicator.BlockVolume(input, blockSize, countType);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.BlockVolume BlockVolume(double blockSize, CountType countType)
		{
			return indicator.BlockVolume(Input, blockSize, countType);
		}

		public Indicators.BlockVolume BlockVolume(ISeries<double> input , double blockSize, CountType countType)
		{
			return indicator.BlockVolume(input, blockSize, countType);
		}
	}
}

#endregion
