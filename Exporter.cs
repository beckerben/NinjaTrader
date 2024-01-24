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
	/// <summary>
	/// Strategy to write data to disk for analysis via ML
	/// </summary>
	public class Exporter : Strategy
	{
		
		#region Variables
		
		private ParabolicSAR ParabolicSAR1;
		private StreamWriter sw;
		private bool priorCloseHigher = false;
		private int trendSequence = 1;
		private TimeSpan startTime;
		private TimeSpan endTime;
		
		#endregion // variables
		
		#region Private methods
		
		/// <summary>
		/// Determines if it is currently trading time based on the specified start and end times.
		/// </summary>
		/// <returns><c>true</c> if it is trading time; otherwise, <c>false</c>.</returns>
		private bool IsTradingTime()
		{
			var result = true;
			var barTimestamp  = Time[0].Hour * 60 + Time[0].Minute;
			var startTimestamp = Start_Time.Hour * 60 + Start_Time.Minute;
			var endTimestamp = End_Time.Hour * 60 + End_Time.Minute;
			var isNoTradingTime = (barTimestamp < startTimestamp || barTimestamp >= endTimestamp) && Enable_Time;
			if (isNoTradingTime)
			{
				result = false;
			}
			return result;
		}		

		private void WriteHeader(StreamWriter writer)
		{
			if (writer != null)
			{
				writer.WriteLine("barcount," + 
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
				    "nbarsdown,"+
				    "nbarsup,"+
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
				); 						
			}
		}
		#endregion // Private methods
		
		#region Main methods
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
				MaximumBarsLookBack							= MaximumBarsLookBack.Infinite;
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
				Enable_Time = false;
				outputPath = "C:\\Temp";
				outputFile = null;
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
				// Check if outputFile is null or empty
				if (string.IsNullOrEmpty(outputFile))
				{
				    // Create a default file name based on the current date and time
				    string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
				    outputFile = $"{Instrument.FullName.Replace(" ", "").Replace("-", "")}_{timestamp}.csv";
				}
				
				// Combine the outputPath and outputFile to create the full file path
				string fullPath = Path.Combine(outputPath, outputFile);				
				
				// Check if the file exists
				if (!File.Exists(fullPath))
				{
				    // Create the file and write the header
				    using (StreamWriter sw = File.CreateText(fullPath))
				    {
				        WriteHeader(sw);
				    }
				}
				else
				{
				    // Check if the file is empty
				    if (new FileInfo(fullPath).Length == 0)
				    {
				        using (StreamWriter sw = File.AppendText(fullPath))
				        {
				            WriteHeader(sw);
				        }
				    }
				}
				//leave writer open for append
				sw = File.AppendText(fullPath);
			}
		}

		protected override void OnBarUpdate()
		{
			if (!IsTradingTime()) return;
		
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
				//McClellanOscillator(19,39)[0].ToString() + "," +
				MFI(14)[0].ToString() + "," +
				Momentum(14)[0].ToString() + "," +
				MoneyFlowOscillator(20)[0].ToString() + "," +
			    NBarsDown(3,true,true,true)[0].ToString() + "," +
			    NBarsUp(3,true,true,true)[0].ToString()  + "," +
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
		
		#endregion // Main methods

		#region Properties
		
			[Display(Name="Output path", Description="e.g. c:\\temp",Order=1,GroupName="Output")]
			public string outputPath
			{get; set;}				
		
			[Display(Name="Output file", Description="e.g. output.csv",Order=2,GroupName="Output")]
			public string outputFile
			{get; set;}		

			[NinjaScriptProperty]
			[Display(Name="Enable Time Filter", Description="Enable time filter", Order = 1, GroupName="Filters")]
			public bool Enable_Time
			{ get; set; }
			
			[Gui.PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
			[Display(Name="Start Time", Description="Start time", Order = 3, GroupName="Filters")]
			public DateTime Start_Time { get; set; }
			
			[Gui.PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
			[Display(Name="End Time", Description="End time", Order = 4, GroupName="Filters")]
			public DateTime End_Time { get; set; }
			
		
		#endregion // Properties
				
	}
}
