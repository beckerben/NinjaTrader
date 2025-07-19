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

//This namespace holds indicators in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators
{
	/// <summary>
	/// The PPO (Percentage Price Oscillator) is based on two moving averages expressed as
	/// a percentage. The PPO is found by subtracting the longer MA from the shorter MA and
	/// then dividing the difference by the longer MA.
	/// </summary>
	public class PPO : Indicator
	{
		private EMA emaFast;
		private EMA emaSlow;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= Custom.Resource.NinjaScriptIndicatorDescriptionPPO;
				Name						= Custom.Resource.NinjaScriptIndicatorNamePPO;
				IsSuspendedWhileInactive	= true;
				Fast						= 12;
				Slow						= 26;
				Smooth						= 9;

				AddPlot(Brushes.DimGray,		Custom.Resource.NinjaScriptIndicatorDefault);
				AddPlot(Brushes.Crimson,		Custom.Resource.PPOSmoothed);
				AddLine(Brushes.DarkGray,	0,	Custom.Resource.NinjaScriptIndicatorZeroLine);
			}
			else if (State == State.DataLoaded)
			{
				emaFast = EMA(Fast);
				emaSlow = EMA(Slow);
			}
		}

		protected override void OnBarUpdate()
		{
			double emaSlow0		= emaSlow[0];
			Default[0] 			= 100 * ((emaFast[0] - emaSlow0) / emaSlow0);
			Smoothed[0]			= EMA(Values[0], Smooth)[0];
		}

		#region Properties
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> Default => Values[0];

		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Fast", GroupName = "NinjaScriptParameters", Order = 0)]
		public int Fast { get; set; }

		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Slow", GroupName = "NinjaScriptParameters", Order = 1)]
		public int Slow { get; set; }

		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Smooth", GroupName = "NinjaScriptParameters", Order = 2)]
		public int Smooth { get; set; }

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> Smoothed => Values[1];
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private PPO[] cachePPO;
		public PPO PPO(int fast, int slow, int smooth)
		{
			return PPO(Input, fast, slow, smooth);
		}

		public PPO PPO(ISeries<double> input, int fast, int slow, int smooth)
		{
			if (cachePPO != null)
				for (int idx = 0; idx < cachePPO.Length; idx++)
					if (cachePPO[idx] != null && cachePPO[idx].Fast == fast && cachePPO[idx].Slow == slow && cachePPO[idx].Smooth == smooth && cachePPO[idx].EqualsInput(input))
						return cachePPO[idx];
			return CacheIndicator<PPO>(new PPO(){ Fast = fast, Slow = slow, Smooth = smooth }, input, ref cachePPO);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.PPO PPO(int fast, int slow, int smooth)
		{
			return indicator.PPO(Input, fast, slow, smooth);
		}

		public Indicators.PPO PPO(ISeries<double> input , int fast, int slow, int smooth)
		{
			return indicator.PPO(input, fast, slow, smooth);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.PPO PPO(int fast, int slow, int smooth)
		{
			return indicator.PPO(Input, fast, slow, smooth);
		}

		public Indicators.PPO PPO(ISeries<double> input , int fast, int slow, int smooth)
		{
			return indicator.PPO(input, fast, slow, smooth);
		}
	}
}

#endregion
