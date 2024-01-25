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
	public class OrderFlowDivergence : Strategy
	{
		
		private OrderFlowDeltaDivergence div;
		private OrderFlowCumulativeDelta orderFlowCumulativeDelta1;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enters when there is a divergence in the bar direction and the buy/sell pressure.";
				Name										= "OrderFlowDivergence";
				Calculate									= Calculate.OnBarClose;
				EntriesPerDirection							= 1;
				EntryHandling								= EntryHandling.AllEntries;
				IsExitOnSessionCloseStrategy				= true;
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
				ProfitTarget					= 12;
				StopLoss = 10;
				DeltaSize = 1;
			}
			else if (State == State.Configure)
			{
				SetProfitTarget(CalculationMode.Ticks, ProfitTarget);
				SetStopLoss(CalculationMode.Ticks, StopLoss);
		
			}

			else if (State == State.DataLoaded)
			{
				orderFlowCumulativeDelta1 = OrderFlowCumulativeDelta(CumulativeDeltaType.BidAsk, CumulativeDeltaPeriod.Bar,0);
				div = OrderFlowDeltaDivergence(50);
				AddChartIndicator(orderFlowCumulativeDelta1);
				AddChartIndicator(div);
			}			
		}

		protected override void OnBarUpdate()
		{
			
			if (CurrentBars[0] < (BarsRequiredToTrade))
				return;
			
						
			bool deltaClosePositive = orderFlowCumulativeDelta1.DeltaClose[0] > 0 && Math.Abs(orderFlowCumulativeDelta1.DeltaClose[0]) >= DeltaSize; // &&; +
			bool deltaCloseNegative = orderFlowCumulativeDelta1.DeltaClose[0] < 0 && Math.Abs(orderFlowCumulativeDelta1.DeltaClose[0]) >= DeltaSize; //&& +
            bool barPriceDown = Close[0] < Close[1];
			
			if((deltaClosePositive && barPriceDown) && (High[0] == Darvas().Upper[0]))
			{
				EnterShort();
				//SetStopLoss(CalculationMode.Price,Darvas().Upper[0]+0.25);
				//Print("High: " + High[0].ToString() + " Darvas Upper: " + Darvas().Upper[0]+0.25);
			}
			
			if ((deltaCloseNegative && !barPriceDown) && (Low[0] == Darvas().Lower[0]))
			{
				EnterLong();
				//SetStopLoss(CalculationMode.Price, Darvas().Lower[0]-0.25);
			}
			
		}
		

		protected override void OnOrderUpdate(Order order, double limitPrice, double stopPrice, int quantity, int filled, double averageFillPrice,
                                    OrderState orderState, DateTime time, ErrorCode error, string nativeError)
		{

		    // Rejection handling
		    if (order.OrderState == OrderState.Rejected)
		    {
				Print(time.ToString() + " Error: Order rejected, order type " + order.OrderType + " " + nativeError); 
			}
		}
		
		#region Properties
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="ProfitTarget", Description="Profit target ticks", Order=1, GroupName="Parameters")]
		public int ProfitTarget
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="StopLoss", Description="Loss limit ticks", Order=1, GroupName="Parameters")]
		public int StopLoss
		{ get; set; }

		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="DeltaSize", Description="The delta close difference required to trigger", Order=1, GroupName="Parameters")]
		public int DeltaSize
		{ get; set; }		
		
		#endregion

	}
}
