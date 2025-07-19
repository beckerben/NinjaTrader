//
// Copyright (C) 2025, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
#region Using declarations
using System.ComponentModel;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Gui;
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators
{
	public class Darvas : Indicator
	{
		private double			boxBottom				= double.MaxValue;
		private double			boxTop					= double.MinValue;
		private bool			buySignal;
		private double			currentBarHigh			= double.MinValue;
		private double			currentBarLow			= double.MaxValue;
		private bool			isRealtime;
		private int				savedCurrentBar			= -1;
		private bool			sellSignal;
		private int				startBarActBox;
		private int				state;

		private int				prevCurrentBar			= -1;
		private Series<double>	boxBottomSeries;
		private Series<double>	boxTopSeries;
		private Series<double>	currentBarHighSeries;
		private Series<double>	currentBarLowSeries;
		private Series<int>		startBarActBoxSeries;
		private Series<int>		stateSeries;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description	= Custom.Resource.NinjaScriptIndicatorDescriptionDarvas;
				Name		= Custom.Resource.NinjaScriptIndicatorNameDarvas;
				IsOverlay	= true;
				Calculate	= Calculate.OnBarClose;

				AddPlot(new Stroke(Brushes.Crimson,	2), PlotStyle.Square, Custom.Resource.NinjaScriptIndicatorLower);
				AddPlot(new Stroke(Brushes.DarkCyan,	2), PlotStyle.Square, Custom.Resource.NinjaScriptIndicatorUpper);
			}
			else if (State == State.DataLoaded)
			{
				if (BarsArray[0].BarsType.IsRemoveLastBarSupported)
				{
					boxBottomSeries			= new Series<double>(this);
					boxTopSeries			= new Series<double>(this);
					currentBarHighSeries	= new Series<double>(this);
					currentBarLowSeries		= new Series<double>(this);
					startBarActBoxSeries	= new Series<int>(this);
					stateSeries				= new Series<int>(this);
				}
			}
		}

		protected override void OnBarUpdate()
		{
			BuySignal	= false;
			SellSignal	= false;

			if (BarsArray[0].BarsType.IsRemoveLastBarSupported && CurrentBar < prevCurrentBar)
			{
				boxBottom		= boxBottomSeries[0];
				boxTop			= boxTopSeries[0];
				currentBarHigh	= currentBarHighSeries[0];
				currentBarLow	= currentBarLowSeries[0];
				startBarActBox	= startBarActBoxSeries[0];
				state			= stateSeries[0];
			}

			if (savedCurrentBar == -1)
			{
				currentBarHigh	= High[0];
				currentBarLow	= Low[0];
				state			= GetNextState();
				savedCurrentBar = CurrentBar;
			}
			else if (savedCurrentBar != CurrentBar)
			{
				// Check for new bar
				currentBarHigh	= isRealtime && Calculate == Calculate.OnEachTick ? High[1] : High[0];
				currentBarLow	= isRealtime && Calculate == Calculate.OnEachTick ? Low[1] : Low[0];

				// Today buy/sell signal is triggered
				if ((state == 5 && currentBarHigh > boxTop) || (state == 5 && currentBarLow < boxBottom))
				{
					if (state == 5 && currentBarHigh > boxTop)
						BuySignal = true;
					else
						SellSignal = true;

					state			 = 0;
					startBarActBox	 = CurrentBar;
				}

				state = GetNextState();
				// Draw with today
				if (boxBottom >= double.MaxValue)
					for (int i = CurrentBar - startBarActBox; i >= 0; i--)
						Upper[i] = boxTop;
				else
					for (int i = CurrentBar - startBarActBox; i >= 0; i--)
					{
						Upper[i] = boxTop;
						Lower[i] = boxBottom;
					}
			}
			else
			{
				isRealtime = true;

				// Today buy/sell signal is triggered
				if ((state == 5 && currentBarHigh > boxTop) || (state == 5 && currentBarLow < boxBottom))
				{
					if (state == 5 && currentBarHigh > boxTop)
						BuySignal = true;
					else
						SellSignal = true;

					startBarActBox = CurrentBar + 1;
					state = 0;
				}

				// Draw with today
				if (boxBottom >= double.MaxValue)
				{
					Upper[0] = boxTop;
				}
				else
				{
					Upper[0] = boxTop;
					Lower[0] = boxBottom;
				}
			}

			if (BarsArray[0].BarsType.IsRemoveLastBarSupported)
			{
				boxBottomSeries[0]		= boxBottom;
				boxTopSeries[0]			= boxTop;
				currentBarHighSeries[0]	= currentBarHigh;
				currentBarLowSeries[0]	= currentBarLow;
				startBarActBoxSeries[0]	= startBarActBox;
				stateSeries[0]			= state;
				prevCurrentBar			= CurrentBar;
			}
		}

		#region Miscellaneous
		private int GetNextState()
		{
			switch (state)
			{
				case 0:
					boxTop		= currentBarHigh;
					boxBottom	= double.MaxValue;
					return 1;

				case 1:
					if (boxTop > currentBarHigh)
						return 2;
					boxTop = currentBarHigh;
					return 1;

				case 2:
					if (boxTop > currentBarHigh)
					{
						boxBottom = currentBarLow;
						return 3;
					}

					boxTop = currentBarHigh;
					return 1;

				case 3:
					if (boxTop > currentBarHigh)
					{
						if (boxBottom < currentBarLow)
							return 4;
						boxBottom = currentBarLow;
						return 3;
					}

					boxTop		= currentBarHigh;
					boxBottom	= double.MaxValue;
					return 1;

				case 4:
					if (boxTop > currentBarHigh)
					{
						if (boxBottom < currentBarLow)
							return 5;
						boxBottom = currentBarLow;
						return 3;
					}

					boxTop		= currentBarHigh;
					boxBottom	= double.MaxValue;
					return 1;

				case 5:
					return 5;

				default:			// Should not happen
					return state;
			}

		}
		#endregion

		#region Properties
		[Browsable(false)]
		[XmlIgnore]
		public bool BuySignal
		{
			get
			{
				Update();
				return buySignal;
			}
			set => buySignal = value;
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> Lower => Values[0];

		[Browsable(false)]
		[XmlIgnore]
		public bool SellSignal
		{
			get
			{
				Update();
				return sellSignal;
			}
			set => sellSignal = value;
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> Upper => Values[1];

		#endregion

	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private Darvas[] cacheDarvas;
		public Darvas Darvas()
		{
			return Darvas(Input);
		}

		public Darvas Darvas(ISeries<double> input)
		{
			if (cacheDarvas != null)
				for (int idx = 0; idx < cacheDarvas.Length; idx++)
					if (cacheDarvas[idx] != null &&  cacheDarvas[idx].EqualsInput(input))
						return cacheDarvas[idx];
			return CacheIndicator<Darvas>(new Darvas(), input, ref cacheDarvas);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.Darvas Darvas()
		{
			return indicator.Darvas(Input);
		}

		public Indicators.Darvas Darvas(ISeries<double> input )
		{
			return indicator.Darvas(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.Darvas Darvas()
		{
			return indicator.Darvas(Input);
		}

		public Indicators.Darvas Darvas(ISeries<double> input )
		{
			return indicator.Darvas(input);
		}
	}
}

#endregion
