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
using System.Globalization;
#endregion

//This namespace holds Strategies in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Strategies
{
	public class RenkoStrategy : Strategy
	{
		private bool[] EntryBarArray;
		private bool[] ExitBarArray;
		private bool adhoc;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"The Renko Strategy as published in December 2019 Technical Analysis of Stocks and Commodities article titled 'Using Renko Charts' by John Devcic";
				Name										= "RenkoStrategy";
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
				StopLossValue					= 10;
				EntryStrength					= 2;
				ExitStrength					= 3;
				AllowLong						= true;
				AllowShort						= false;
				UseExitStrength					= false;
				UseTrailStop					= true;
				
			}
			else if (State == State.Configure)
			{
				EntryBarArray = new bool[EntryStrength];
				for(int i = 0; i < EntryStrength; i++)
					EntryBarArray[i] = false;
				ExitBarArray = new bool[ExitStrength];
				for(int i = 0; i < ExitStrength; i++)
					ExitBarArray[i] = false;
				
				if(UseTrailStop)
					SetTrailStop(CalculationMode.Ticks, StopLossValue);
			}
		}

		protected override void OnBarUpdate()
		{
			
			if(BarsPeriod.BarsPeriodType != BarsPeriodType.Renko)
			{
				Draw.TextFixed(this, "NinjaScriptInfo", "The RenkoStrategy must be ran on a Renko chart.", TextPosition.BottomRight);
				return;
			}
			
			if(CurrentBar < EntryStrength || CurrentBar < ExitStrength)
				return;
			
			if(Position.MarketPosition == MarketPosition.Flat)
			{
				for(int i = 0; i < EntryStrength; i++)
					EntryBarArray[i] = false;
				
				for(int i = EntryStrength-1; i >=0; i--)
				{
					if(Close[i] > Open[i])
					{
						EntryBarArray[i] = true;
					}
					else
					{
						EntryBarArray[i] = false;
					}
				}
				
				adhoc = true;
				foreach(bool b in EntryBarArray)
				{
					adhoc = adhoc && b;
				}
				
				if(adhoc && AllowLong)
				{
					EnterLong();
				}
				else
				{
					for(int i = 0; i < EntryStrength; i++)
						EntryBarArray[i] = false;
					
					for(int i = EntryStrength-1; i >=0; i--)
					{
						if(Close[i] < Open[i])
						{
							EntryBarArray[i] = true;
						}
						else
						{
							EntryBarArray[i] = false;
						}
					}
					
					adhoc = true;
					foreach(bool b in EntryBarArray)
					{
						adhoc = adhoc && b;
					}
					
					if(adhoc && AllowShort)
					{
						EnterShort();
					}
				}
				
			}
			
			for(int i = 0; i < ExitStrength; i++)
				ExitBarArray[i] = false;
			
			if(Position.MarketPosition == MarketPosition.Long && BarsSinceEntryExecution() >= 1)
			{
				for(int i = ExitStrength-1; i >=0; i--)
				{
					if(Close[i] < Open[i])
					{
						ExitBarArray[i] = true;
					}
				}
				
				adhoc = true;
				foreach(bool b in ExitBarArray)
				{
					adhoc = adhoc && b;
				}
				
				if(adhoc && UseExitStrength)
				{
					ExitLong();
				}
				
			}
			else if(Position.MarketPosition == MarketPosition.Short  && BarsSinceEntryExecution() >= 1)
			{
				for(int i = ExitStrength-1; i >=0; i--)
				{
					if(Close[i] > Open[i])
					{
						ExitBarArray[i] = true;
					}
				}
				
				adhoc = true;
				foreach(bool b in ExitBarArray)
				{
					adhoc = adhoc && b;
				}
				
				if(adhoc && UseExitStrength)
				{
					ExitShort();
				}
			}
		}

		#region Properties
		
		[NinjaScriptProperty]
		[Range(0, double.MaxValue)]
		[Display(Name="Stop Loss Value", Description="The value for the stop loss order.", Order=0, GroupName="Stop Loss Setup")]
		public double StopLossValue
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="Strength for entry", Description="The number of subseaquent up or down bars needed for an entry.", Order=1, GroupName="Parameters")]
		public int EntryStrength
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="Strength for exit", Description="The number of subseaquent up or down bars needed for an exit.", Order=2, GroupName="Parameters")]
		public int ExitStrength
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Use Exit Strength", Description="Enable Exit Strength Exits", Order=3, GroupName="Parameters")]
		public bool UseExitStrength
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Use Trail Stop", Description="Enable Trail Stop Exits", Order=4, GroupName="Parameters")]
		public bool UseTrailStop
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Allow Long Trades", Description="Enable Long Trades", Order=5, GroupName="Parameters")]
		public bool AllowLong
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Allow Short Trades", Description="Enable Short Trades", Order=6, GroupName="Parameters")]
		public bool AllowShort
		{ get; set; }
		

  
		#endregion

	}
}
