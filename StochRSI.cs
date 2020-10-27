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
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//This namespace holds Strategies in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Strategies
{
	public class StochRSI : Strategy
	{
		private Series<double> closeOpen;
		private Series<double> highLow;
		private Series<double> rvi;
		private Series<double> sig;
		private Series<double> stoch;
		private Series<double> k;
		private Series<double> d;
		
	
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"From Tradingview";
				Name										= "StochRSI";
				Calculate									= Calculate.OnBarClose;
				EntriesPerDirection							= 1;
				EntryHandling								= EntryHandling.AllEntries;
				IsExitOnSessionCloseStrategy				= false;
				ExitOnSessionCloseSeconds					= 30;
				IsFillLimitOnTouch							= false;
				MaximumBarsLookBack							= MaximumBarsLookBack.TwoHundredFiftySix;
				OrderFillResolution							= OrderFillResolution.Standard;
				Slippage									= 0;
				StartBehavior								= StartBehavior.WaitUntilFlat;
				TimeInForce									= TimeInForce.Gtc;
				TraceOrders									= false;
				RealtimeErrorHandling						= RealtimeErrorHandling.StopCancelClose;
				StopTargetHandling							= StopTargetHandling.PerEntryExecution;
				BarsRequiredToTrade							= 20;
				// Disable this property for performance gains in Strategy Analyzer optimizations
				// See the Help Guide for additional information
				IsInstantiatedOnEachOptimizationIteration	= true;
				
				length = 5;
				smoothK = 3;
				smoothD = 3;
				lengthRSI = 14;
				lengthStoch = 14;
	
				profitTaker = 1000;
				stopLoss = 1000;
				
				
			}
			else if (State == State.Configure)
			{
				SetStopLoss(CalculationMode.Ticks, stopLoss);
				SetProfitTarget(CalculationMode.Ticks, profitTaker);
			}
			else if (State == State.DataLoaded)
			{
				closeOpen = new Series<double>(this,MaximumBarsLookBack.Infinite);
				highLow = new Series<double>(this,MaximumBarsLookBack.Infinite);
				rvi = new Series<double>(this,MaximumBarsLookBack.Infinite);
				sig = new Series<double>(this,MaximumBarsLookBack.Infinite);
				stoch = new Series<double>(this,MaximumBarsLookBack.Infinite);
				k = new Series<double>(this,MaximumBarsLookBack.Infinite);
				d = new Series<double>(this,MaximumBarsLookBack.Infinite);
				//AddChartIndicator(NBarsUp(barCount,false,true,true));	

				
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBars[0] < (BarsRequiredToTrade))
				return;
			
			closeOpen[0] = Close[0]-Open[0];
			highLow[0] = High[0]-Low[0];
			

			stoch[0] = 100 * ((RSI(Close,lengthRSI,1)[0] - MIN(RSI(Close,lengthRSI,1),lengthStoch)[0]) / (MAX(RSI(Close,lengthRSI,1),lengthStoch)[0] - MIN(RSI(Close,lengthRSI,1),lengthStoch)[0]) );
			k[0] = SMA(stoch,smoothK)[0];
			d[0] = SMA(k, smoothD)[0];
			rvi[0] = (SUM(TMA(closeOpen,4),length)[0] / SUM(TMA(highLow,4),length)[0]);
			sig[0] = TMA(rvi,4)[0];
			
			//Print(Time[0].ToLongTimeString() + " K " + k[0].ToString() + " D " + d[0].ToString() + " rvi " + rvi[0].ToString() + " sig " + sig[0].ToString());
			
			if (k[0] <= d[0] &&
					k[0] > 70 &&
					rvi[0] <= sig[0] &&
					rvi[1] >= sig[1])
				EnterShort();
			
			if (k[0] >= d[0] &&
					k[0] < 30 &&
					rvi[0] >= sig[0] &&
					rvi[1] <= sig[1])
				EnterLong();
				
						
		}
		
		protected override void OnExecutionUpdate(Execution execution, string executionId, double price, int quantity, MarketPosition marketPosition, string orderId, DateTime time)
		{

			
			
		}		
		
		#region Properties
				
		
			[Range(1, int.MaxValue), NinjaScriptProperty]
			[Display(Name="Length", Description="", Order=1, GroupName="Parameters")]
			public int length
			{ get; set; }
			
			[Range(1, int.MaxValue), NinjaScriptProperty]
			[Display(Name="SmoothK", Description="", Order=1, GroupName="Parameters")]
			public int smoothK
			{ get; set; }
			
			[Range(1, int.MaxValue), NinjaScriptProperty]
			[Display(Name="SmoothD", Description="", Order=1, GroupName="Parameters")]
			public int smoothD
			{ get; set; }

			[Range(1, int.MaxValue), NinjaScriptProperty]
			[Display(Name="Length RSI", Description="", Order=1, GroupName="Parameters")]
			public int lengthRSI
			{ get; set; }			
			
			[Range(1, int.MaxValue), NinjaScriptProperty]
			[Display(Name="Length Stoch", Description="", Order=1, GroupName="Parameters")]
			public int lengthStoch
			{ get; set; }			
			
			
			[NinjaScriptProperty]
			[Range(2, 1024)]
			[Display(Name="Profit Taker", Description="Take Profit Ticks", Order=9, GroupName="Parameters")]
			public int profitTaker
			{ get; set; }			
			
			[NinjaScriptProperty]
			[Range(1, 1024)]
			[Display(Name="Stop Loss", Description="Stop Loss Ticks", Order=10, GroupName="Parameters")]
			public int stopLoss
			{ get; set; }			
			
			
		#endregion

	}
}


//todo: playing short side? does trading hours matter? hows it perofrm on nasdaq? 
//anyway to prevent sideways entries maybe slope angle?  
//add confirmation of nasdaq signal too?
