// 
// Copyright (C) 2010 Trendseek, for FREE use on your own risk!
// Based on "New Trading Dimensions" by Erich Florek and EasyLanguage Coding 
//

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
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

// This namespace holds all indicators and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators
{
	/// <summary>
	/// The RSI (Relative Strength Index) is a price-following oscillator that ranges between 0 and 100.
	/// </summary>
	public class RMI : Indicator
	{
		#region Variables
			private int								period	= 14;
			private int								shift	= 3;	
		
			private Series<double>						avgUp;
			private Series<double>						avgDown;
		
		#endregion

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                    Name = "RMI";
                    Description = "The RMI (Relative Momentum Index), develloped by Roger Altman. Similar to RSI but based on comparing Momentums instead of prices. Typical Settings are 3/8 or 5/13";
					AddPlot(Brushes.Blue, "RMI");
			
					AddLine(Brushes.Green, 30, "Lower");
					AddLine(Brushes.Black, 50, "Center");
					AddLine(Brushes.Red, 70, "Upper");
			}
			else if (State == State.DataLoaded)
			{
					avgUp				= new Series<double>(this);
					avgDown				= new Series<double>(this);					
            }
        }

		protected override void OnBarUpdate()
		{
			double amountUp 	= 0;
			double amountDown 	= 0;
			double rmi			= 0;
			
			if (CurrentBar ==0)
			{
				avgUp[0] = 0;
				avgDown[0] = 0;			
				
				return;
			
			}
			
			if (CurrentBar < (Period + shift)) 
			{
				return;
			}
			else if (CurrentBar == (Period + shift))
			{			
				double sumUp = 0;
				double sumDown = 0;
				
				for (int barsAgo = 0; barsAgo < Period; barsAgo++)
				{
					amountUp = Input[barsAgo] - Input[barsAgo + shift];
					if (amountUp >= 0) 
					{
						amountDown 	= 0;
					}
					else
					{
						amountDown 	= -amountUp;
						amountUp	= 0;
					}
					sumUp	=	sumUp + amountUp;
					sumDown	=	sumDown + amountDown;
				}
				avgUp[0] = sumUp / Period;
				avgDown[0] = sumDown / Period;			
			}
			else
			{
				amountUp = Input[0] - Input[shift];
				if (amountUp >= 0)
				{
					amountDown 	= 0;
				}
				else
				{
					amountDown 	= -amountUp;
					amountUp	= 0;
				}
				avgUp[0] = (avgUp[1] * (Period - 1) + amountUp) / Period;
				avgDown[0] = (avgDown[1] * (Period - 1) + amountDown) / Period;
			}
			
			if ((avgUp[0] + avgDown[0]) != 0)
			{
				rmi = 100 * avgUp[0] / (avgUp[0] + avgDown[0]);
			}
			else rmi = 0;

			
			Value[0] = rmi;
		}

		#region Properties
		/// <summary>
		/// </summary>
		[Browsable(false)]
		[XmlIgnore()]
		public Series<double> Default
		{
			get { return Values[0]; }
		}
		
		/// <summary>
		/// </summary>
		[NinjaScriptProperty]		
		[Display(Description = "Numbers of bars used for calculations", GroupName = "Parameters", Order = 1)]		
		public int Period
		{
			get { return period; }
			set { period = Math.Max(2, value); }
		}

		/// <summary>
		/// </summary>
		[NinjaScriptProperty]
		[Display(Description = "Number of bars for smoothing", GroupName = "Parameters", Order = 1)]
		public int Shift
		{
			get { return shift; }
			set { shift = Math.Max(1, value); }
		}
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private RMI[] cacheRMI;
		public RMI RMI(int period, int shift)
		{
			return RMI(Input, period, shift);
		}

		public RMI RMI(ISeries<double> input, int period, int shift)
		{
			if (cacheRMI != null)
				for (int idx = 0; idx < cacheRMI.Length; idx++)
					if (cacheRMI[idx] != null && cacheRMI[idx].Period == period && cacheRMI[idx].Shift == shift && cacheRMI[idx].EqualsInput(input))
						return cacheRMI[idx];
			return CacheIndicator<RMI>(new RMI(){ Period = period, Shift = shift }, input, ref cacheRMI);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.RMI RMI(int period, int shift)
		{
			return indicator.RMI(Input, period, shift);
		}

		public Indicators.RMI RMI(ISeries<double> input , int period, int shift)
		{
			return indicator.RMI(input, period, shift);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.RMI RMI(int period, int shift)
		{
			return indicator.RMI(Input, period, shift);
		}

		public Indicators.RMI RMI(ISeries<double> input , int period, int shift)
		{
			return indicator.RMI(input, period, shift);
		}
	}
}

#endregion
