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
using System.IO;
#endregion

//This namespace holds Strategies in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Strategies
{
	public class Exporter : Strategy
	{
		private ParabolicSAR ParabolicSAR1;
		private StreamWriter sw; // a variable for the StreamWriter that will be used 
		private bool priorCloseHigher = false;
		private int trendSequence = 1;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Exporter";
				Name										= "Exporter";
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
				outputFile = "C:\\temp\\NQ.csv"; 
				// Disable this property for performance gains in Strategy Analyzer optimizations
				// See the Help Guide for additional information
				IsInstantiatedOnEachOptimizationIteration	= true;
				
				//ticks = 15;
				//barsBack = 10;
				
			}
			// Necessary to call in order to clean up resources used by the StreamWriter object
			else if(State == State.Terminated)
			{
				if (sw != null)
				{
					sw.Close();
					sw.Dispose();
					sw = null;
				}
			}			
			else if (State == State.Configure)
			{
			}
			else if (State == State.DataLoaded)
			{				
				sw = File.AppendText(outputFile);  // Open the path for writing
			}
		}

		protected override void OnBarUpdate()
		{
			
			if (CurrentBars[0] == 0) 
			{
				//sw = File.AppendText(path);  // Open the path for writing
				sw.WriteLine("barcount," + 
					"date," + 
					"open,"+
					"high,"+
					"low,"+
					"close,"+
					"volume,"+
					"higherclose,"+
				    "trendsequence," + 
					"adl,"+
					"adx," +
					"adxr,"+
					"aroon_up,"+
					"aroon_down,"+
					"aroonoscillator,"+
					"atr,"+
					"bop,"+
					"cci,"+
					"chaikinmoneyflow,"+
					"chaikinoscillator,"+
					"chaikinvolatility,"+
					"choppinessindex,"+
					"cmo,"+
					"disparityindex,"+
					"dm_diplus,"+
					"dm_diminus,"+
					"dmi,"+
					"doublestochastics_k,"+
					"easeofmovement,"+
					"fisherstransform,"+
					"fosc,"+
					"macd,"+
					"macd_avg,"+
					"macd_diff,"+
					//"mcclellanoscillator,"+
					"mfi,"+
					"momentum,"+
					"moneyflowoscillator,"+
					"obv,"+
					//"orderflowcumulativedelta_deltaopen,"+
					//"orderflowcumulativedelta_deltaclose,"+
					//"orderflowcumulativedelta_deltahigh,"+
					//"orderflowcumulativedelta_deltalow,"+
					"pfe,"+
					"ppo,"+
					"priceoscillator,"+
					"psychologicalline,"+
					"rsquared,"+
					"relativevigorindex,"+
					"rind,"+
					"roc,"+
					"rsi,"+
					"rsi_avg,"+
					"rss,"+
					"rvi,"+
					"stddev,"+
					"stochrsi,"+
					"stochastics_d,"+
					"stochastics_k,"+
					"stochasticsfast_d,"+
					"stochasticsfast_k,"+
					"sum,"+
					"trix,"+
					"trix_signal,"+
					"tsi,"+
					"ultimateoscillator,"+
					"zlema,"+
					"volumeupdown_upvolume,"+
					"volumeupdown_downvolume,"+
					"vortex_viplus,"+
					"vortex_viminus,"+
					"vroc,"+
					"williamsr,"+
					"wisemanawesomeoscillator,"+
					"woodiescci,"+
					"woodiescci_turbo"
				); // Append a new line to the file
				//sw.Close(); // Close the file to allow future calls to access the file again.					
			}
			
			if (CurrentBars[0] < BarsRequiredToTrade) return;
			
			bool closehigher = false;
			if(Close[0] > Close[1]) 
				closehigher = true;
			else 
				closehigher = false;
			
			if (priorCloseHigher != closehigher)
			 	trendSequence = 1;
			else
				trendSequence++;
			
			priorCloseHigher = closehigher;
			
			//sw = File.AppendText(path);  // Open the path for writing
			sw.WriteLine(CurrentBar.ToString() + "," + Time[0].ToString("yyyy-MM-dd HH:mm:ss") + "," + 
				Open[0].ToString() + "," +
				High[0].ToString() + "," + 
				Low[0].ToString() + "," + 
				Close[0].ToString() + "," + 
				Volume[0].ToString() + "," +
				closehigher.ToString() + "," +
				trendSequence.ToString() + "," +
				ADL().AD[0].ToString() + "," + 
				ADX(14)[0].ToString() + "," + 
				ADXR(10,14)[0].ToString() + "," + 
				Aroon(14).Up[0].ToString() + "," + 
				Aroon(14).Down[0].ToString() + "," + 
				AroonOscillator(14)[0].ToString()+ "," + 
				ATR(14)[0].ToString()  + "," +
				BOP(14)[0].ToString() + "," +
				CCI(14)[0].ToString()  + "," +
				ChaikinMoneyFlow(21)[0].ToString()  + "," +
				ChaikinOscillator(3,10)[0].ToString()  + "," +
				ChaikinVolatility(10,10)[0].ToString()  + "," +
				ChoppinessIndex(14)[0].ToString()  + "," +
				CMO(14)[0].ToString()+ "," +
				DisparityIndex(25)[0].ToString() + "," +
				DM(14).DiPlus[0].ToString() + "," +
				DM(14).DiMinus[0].ToString() + "," +
				DMI(14)[0].ToString() + "," +
				DoubleStochastics(10).K[0].ToString() + "," +
				EaseOfMovement(10,1000)[0].ToString() + "," + 
				FisherTransform(10)[0].ToString() + "," + 
				FOSC(14)[0].ToString() + "," +
				MACD(12,26,9)[0].ToString() + "," +
				MACD(12,26,9).Avg[0].ToString() + "," +
				MACD(12,26,9).Diff[0].ToString() + "," +
				//McClellanOscillator(19,30)[0].ToString() + "," +
				MFI(14)[0].ToString() + "," +
				Momentum(14)[0].ToString() + "," +
				MoneyFlowOscillator(20)[0].ToString() + "," +
				OBV()[0].ToString() + "," + 
				//OrderFlowCumulativeDelta(CumulativeDeltaType.BidAsk,CumulativeDeltaPeriod.Session,0).DeltaOpen[0].ToString() + "," + 
				//OrderFlowCumulativeDelta(CumulativeDeltaType.BidAsk,CumulativeDeltaPeriod.Session,0).DeltaClose[0].ToString() + "," +
				//OrderFlowCumulativeDelta(CumulativeDeltaType.BidAsk,CumulativeDeltaPeriod.Session,0).DeltaHigh[0].ToString() + "," +
				//OrderFlowCumulativeDelta(CumulativeDeltaType.BidAsk,CumulativeDeltaPeriod.Session,0).DeltaLow[0].ToString() + "," + 
				PFE(14,10)[0].ToString() + "," +
				PPO(12,26,9).Smoothed[0].ToString() + "," +
				PriceOscillator(12,26,9)[0].ToString() + "," +
				PsychologicalLine(10)[0].ToString() + "," +
				RSquared(8)[0].ToString() + "," +
				RelativeVigorIndex(10)[0].ToString() + "," +
				RIND(3,10)[0].ToString() + "," +
				ROC(14)[0].ToString() + "," +
				RSI(14,3)[0].ToString() + "," +
				RSI(14,3).Avg[0].ToString() + "," +
				RSS(10,40,5)[0].ToString() + "," +
				RVI(14)[0].ToString() + "," +
				StdDev(14)[0].ToString() + "," +
				StochRSI(14)[0].ToString() + "," +
				Stochastics(7,14,3).D[0].ToString() + "," +
				Stochastics(7,14,3).K[0].ToString() + "," +
				StochasticsFast(3,14).D[0].ToString() + "," +
				StochasticsFast(3,14).K[0].ToString() + "," +
				SUM(14)[0].ToString() + "," +
				TRIX(14,3)[0].ToString() + "," +
				TRIX(14,3).Signal[0].ToString() + "," +
				TSI(3,14)[0].ToString() + "," +
				UltimateOscillator(7,14,28)[0].ToString() + "," +
				ZLEMA(14)[0].ToString() + "," +
				VolumeUpDown().UpVolume[0].ToString() + "," +
				VolumeUpDown().DownVolume[0].ToString() + "," +
				Vortex(14).VIPlus[0].ToString() + "," +
				Vortex(14).VIMinus[0].ToString() + "," +
				VROC(14,3)[0].ToString() + "," +
				WilliamsR(14)[0].ToString() + "," +
				WisemanAwesomeOscillator()[0].ToString() + "," +
				WoodiesCCI(2,5,14,34,25,6,60,100,2)[0].ToString() + "," +
				WoodiesCCI(2,5,14,34,25,6,60,100,2).Turbo[0].ToString() 
			); // Append a new line to the file
			//sw.Close(); // Close the file to allow future calls to access the file again.
			
		}
		
		#region Properties
		
			[Display(Name="Output file", Description="e.g. c:\\temp\\out.csv",Order=1,GroupName="Parameters")]
			public string outputFile
			{get; set;}		
			
//			[Range(1, int.MaxValue), NinjaScriptProperty]
//			[Display(Name="Desired Ticks", Description="Desired Ticks", Order=1, GroupName="Parameters")]
//			public int ticks
//			{ get; set; }
			
//			[Range(1, int.MaxValue), NinjaScriptProperty]
//			[Display(Name="Bars Back", Description="Bars to go back to compare high", Order=2, GroupName="Parameters")]
//			public int barsBack
//			{ get; set; }
			
		#endregion
	}
}
