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
					"reversal,"+
				    "trendsequence," + 
					"adl,"+
					"adx," +
					"adxr,"+
					"apz_lower,"+
					"apz_upper,"+
					"aroonoscillator,"+
					"atr,"+
					"bollinger_lower,"+
					"bollinger_middle,"+
					"bollinger_upper,"+
					"bop,"+
					"camarilla_r1,"+
					"camarilla_r2,"+
					"camarilla_r3,"+
					"camarilla_r4,"+
					"camarilla_s1,"+
					"camarilla_s2,"+
					"camarilla_s3,"+
					"camarilla_s4,"+
					"cci,"+
					"chaikinmoneyflow,"+
					"chaikinoscillator,"+
					"chaikinvolatility,"+
					"choppinessindex,"+
					"cmo,"+
					"currentday_open,"+
					"currentday_low,"+
					"currentday_high,"+				
					"disparityindex,"+
					"dm_diplus,"+
					"dm_diminus,"+
					"dmi,"+
					"dmindex,"+
					"donchian_lower,"+
					"donchian_mean,"+
					"donchian_upper,"+
					"doublestochastics_k,"+
					"easeofmovement,"+
					"fibonacci_pp,"+
					"fibonacci_r1,"+
					"fibonacci_r2,"+
					"fibonacci_r3,"+
					"fibonacci_s1,"+
					"fibonacci_s2,"+
					"fibonacci_s3,"+
					"fisherstransform,"+
					"fosc,"+
					"kama,"+
					"keltner_lower,"+
					"keltner_mean,"+
					"keltner_upper,"+
					"linreg,"+
					"linregintercept,"+
					"linregslope,"+
					"macd,"+
					"macd_avg,"+
					"macd_diff,"+
					"mama_default,"+
					"mama_kama,"+
					"mfi,"+
					"momentum,"+
					"moneyflowoscillator,"+
					"orderflowcumulativedelta_deltaopen,"+
					"orderflowcumulativedelta_deltaclose,"+
					"orderflowcumulativedelta_deltahigh,"+
					"orderflowcumulativedelta_deltalow,"+
					"orderflowvwap_vwap,"+
					"orderflowvwap_s1_lower,"+
					"orderflowvwap_s1_higher,"+
					"orderflowvwap_s2_lower,"+
					"orderflowvwap_s2_higher,"+
					"orderflowvwap_s3_lower,"+
					"orderflowvwap_s3_higher,"+
					"parabolic_sar,"+
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
					"trix,"+
					"trix_signal,"+
					"tsf,"+
					"tsi,"+
					"ultimateoscillator,"+
					"vortex_viplus,"+
					"vortex_viminus,"+
					"volma,"+
					"volume_oscillator,"+
					"vroc,"+
					"williamsr,"+
					"wisemanawesomeoscillator,"+
					"woodiescci,"+
					"woodiescci_turbo,"+
					"woodiespivot_pp,"+
					"woodiespivot_r1,"+
					"woodiespivot_r2,"+
					"woodiespivot_s1,"+
					"woodiespivot_s2"
				); 						
			}
		}
		
	    public static double CalculatePricePCT(double basePrice, double indPrice, int intPrecision)
	    {
	        if (basePrice == 0)
	        {
	            throw new ArgumentException("basePrice cannot be zero.");
	        }
	        double difference = indPrice - basePrice;
	        double percentageDifference = (difference / basePrice) * 100;
	        return Math.Round(percentageDifference, intPrecision);
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
				OrderFillResolution							= OrderFillResolution.High;
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
			
			bool reversal = false;
			if (priorCloseHigher != closehigher)
			{
			 	trendSequence = 1;
				reversal = true;
			}
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
				reversal.ToString() + "," +
				trendSequence.ToString() + "," +
				ADL().AD[0].ToString() + "," + 
				Math.Round(ADX(14)[0],0).ToString() + "," + 
				Math.Round(ADXR(10,14)[0],0).ToString() + "," + 
				CalculatePricePCT(Close[0],APZ(2,20).Lower[0],3) + "," + 
				CalculatePricePCT(Close[0],APZ(2,20).Upper[0],3) + "," + 
				Math.Round(AroonOscillator(14)[0],0).ToString()+ "," + 
				Math.Round(ATR(14)[0],1).ToString()  + "," +
				CalculatePricePCT(Close[0],Bollinger(2,14).Lower[0],3) + "," +
				CalculatePricePCT(Close[0],Bollinger(2,14)[0],3) + "," +
				CalculatePricePCT(Close[0],Bollinger(2,14).Upper[0],3) + "," +
				Math.Round(BOP(14)[0],3).ToString() + "," +
				CalculatePricePCT(Close[0],CamarillaPivots(PivotRange.Daily,HLCCalculationMode.CalcFromIntradayData,0, 0, 0, 20).R1[0],3) + "," +
				CalculatePricePCT(Close[0],CamarillaPivots(PivotRange.Daily,HLCCalculationMode.CalcFromIntradayData,0, 0, 0, 20).R2[0],3) + "," +
				CalculatePricePCT(Close[0],CamarillaPivots(PivotRange.Daily,HLCCalculationMode.CalcFromIntradayData,0, 0, 0, 20).R3[0],3) + "," +
				CalculatePricePCT(Close[0],CamarillaPivots(PivotRange.Daily,HLCCalculationMode.CalcFromIntradayData,0, 0, 0, 20).R4[0],3) + "," +			
				CalculatePricePCT(Close[0],CamarillaPivots(PivotRange.Daily,HLCCalculationMode.CalcFromIntradayData,0, 0, 0, 20).S1[0],3) + "," +
				CalculatePricePCT(Close[0],CamarillaPivots(PivotRange.Daily,HLCCalculationMode.CalcFromIntradayData,0, 0, 0, 20).S2[0],3) + "," +
				CalculatePricePCT(Close[0],CamarillaPivots(PivotRange.Daily,HLCCalculationMode.CalcFromIntradayData,0, 0, 0, 20).S3[0],3) + "," +
				CalculatePricePCT(Close[0],CamarillaPivots(PivotRange.Daily,HLCCalculationMode.CalcFromIntradayData,0, 0, 0, 20).S4[0],3) + "," +			
				Math.Round(CCI(14)[0],0).ToString()  + "," +
				Math.Round(ChaikinMoneyFlow(21)[0],0).ToString()  + "," +
				Math.Round(ChaikinOscillator(3,10)[0],0).ToString()  + "," +
				Math.Round(ChaikinVolatility(10,10)[0],0).ToString()  + "," +
				Math.Round(ChoppinessIndex(14)[0],0).ToString()  + "," +
				Math.Round(CMO(14)[0],0).ToString()+ "," +
				CalculatePricePCT(Close[0],CurrentDayOHL().CurrentOpen[0],3)+ "," +
				CalculatePricePCT(Close[0],CurrentDayOHL().CurrentLow[0],3)+ "," +
				CalculatePricePCT(Close[0],CurrentDayOHL().CurrentHigh[0],3)+ "," +
				Math.Round(DisparityIndex(25)[0],3).ToString() + "," +
				Math.Round(DM(14).DiPlus[0],0).ToString() + "," +
				Math.Round(DM(14).DiMinus[0],0).ToString() + "," +
				Math.Round(DMI(14)[0],0).ToString() + "," +
				Math.Round(DMIndex(3)[0],0).ToString() + "," +
				CalculatePricePCT(Close[0],DonchianChannel(14).Lower[0],3) + "," +
				CalculatePricePCT(Close[0],DonchianChannel(14)[0],3) + "," +
				CalculatePricePCT(Close[0],DonchianChannel(14).Upper[0],3) + "," +			
				Math.Round(DoubleStochastics(10).K[0],0).ToString() + "," +
				Math.Round(EaseOfMovement(10,1000)[0],0).ToString() + "," + 
				CalculatePricePCT(Close[0],FibonacciPivots(PivotRange.Daily,HLCCalculationMode.CalcFromIntradayData,0,0,0,20).Pp[0],3) + "," +	
				CalculatePricePCT(Close[0],FibonacciPivots(PivotRange.Daily,HLCCalculationMode.CalcFromIntradayData,0,0,0,20).R1[0],3) + "," +
				CalculatePricePCT(Close[0],FibonacciPivots(PivotRange.Daily,HLCCalculationMode.CalcFromIntradayData,0,0,0,20).R2[0],3) + "," +
				CalculatePricePCT(Close[0],FibonacciPivots(PivotRange.Daily,HLCCalculationMode.CalcFromIntradayData,0,0,0,20).R3[0],3) + "," +
				CalculatePricePCT(Close[0],FibonacciPivots(PivotRange.Daily,HLCCalculationMode.CalcFromIntradayData,0,0,0,20).S1[0],3) + "," +
				CalculatePricePCT(Close[0],FibonacciPivots(PivotRange.Daily,HLCCalculationMode.CalcFromIntradayData,0,0,0,20).S2[0],3) + "," +
				CalculatePricePCT(Close[0],FibonacciPivots(PivotRange.Daily,HLCCalculationMode.CalcFromIntradayData,0,0,0,20).S3[0],3) + "," +
				Math.Round(FisherTransform(10)[0],1).ToString() + "," + 
				Math.Round(FOSC(14)[0],2).ToString() + "," +
				CalculatePricePCT(Close[0],KAMA(2,10,30)[0],3) + "," +
				CalculatePricePCT(Close[0],KeltnerChannel(1.5,10).Lower[0],3) + "," +
				CalculatePricePCT(Close[0],KeltnerChannel(1.5,10)[0],3) + "," +
				CalculatePricePCT(Close[0],KeltnerChannel(1.5,10).Upper[0],3) + "," +
				CalculatePricePCT(Close[0],LinReg(14)[0],3) + "," +
				CalculatePricePCT(Close[0],LinRegIntercept(14)[0],3) + "," +
				Math.Round(LinRegSlope(14)[0],1).ToString() + "," +
				Math.Round(MACD(12,26,9)[0],1).ToString() + "," +
				Math.Round(MACD(12,26,9).Avg[0],1).ToString() + "," +
				Math.Round(MACD(12,26,9).Diff[0],1).ToString() + "," +
				CalculatePricePCT(Close[0],MAMA(0.5,0.05).Default[0],3) + "," +
				CalculatePricePCT(Close[0],MAMA(0.5,0.05).Fama[0],3) + "," +
				Math.Round(MFI(14)[0],0).ToString() + "," +
				Math.Round(Momentum(14)[0],0).ToString() + "," +
				Math.Round(MoneyFlowOscillator(20)[0],2).ToString() + "," +
				OrderFlowCumulativeDelta(CumulativeDeltaType.BidAsk,CumulativeDeltaPeriod.Bar,0).DeltaOpen[0].ToString() + "," + 
				OrderFlowCumulativeDelta(CumulativeDeltaType.BidAsk,CumulativeDeltaPeriod.Bar,0).DeltaClose[0].ToString() + "," +
				OrderFlowCumulativeDelta(CumulativeDeltaType.BidAsk,CumulativeDeltaPeriod.Bar,0).DeltaHigh[0].ToString() + "," +
				OrderFlowCumulativeDelta(CumulativeDeltaType.BidAsk,CumulativeDeltaPeriod.Bar,0).DeltaLow[0].ToString() + "," + 
				CalculatePricePCT(Close[0],OrderFlowVWAP(VWAPResolution.Standard,Bars.TradingHours,VWAPStandardDeviations.Three,1,2,3).VWAP[0],3) + "," +
				CalculatePricePCT(Close[0],OrderFlowVWAP(VWAPResolution.Standard,Bars.TradingHours,VWAPStandardDeviations.Three,1,2,3).StdDev1Lower[0],3) + "," +			
				CalculatePricePCT(Close[0],OrderFlowVWAP(VWAPResolution.Standard,Bars.TradingHours,VWAPStandardDeviations.Three,1,2,3).StdDev1Upper[0],3) + "," +
				CalculatePricePCT(Close[0],OrderFlowVWAP(VWAPResolution.Standard,Bars.TradingHours,VWAPStandardDeviations.Three,1,2,3).StdDev2Lower[0],3) + "," +
				CalculatePricePCT(Close[0],OrderFlowVWAP(VWAPResolution.Standard,Bars.TradingHours,VWAPStandardDeviations.Three,1,2,3).StdDev2Upper[0],3) + "," +
				CalculatePricePCT(Close[0],OrderFlowVWAP(VWAPResolution.Standard,Bars.TradingHours,VWAPStandardDeviations.Three,1,2,3).StdDev3Lower[0],3) + "," +
				CalculatePricePCT(Close[0],OrderFlowVWAP(VWAPResolution.Standard,Bars.TradingHours,VWAPStandardDeviations.Three,1,2,3).StdDev3Upper[0],3) + "," +
				CalculatePricePCT(Close[0],ParabolicSAR(0.02,0.2,0.02)[0],3) + "," +
				Math.Round(PFE(14,10)[0],2).ToString() + "," +
				Math.Round(PPO(12,26,9).Smoothed[0],3).ToString() + "," +
				Math.Round(PriceOscillator(12,26,9)[0],1).ToString() + "," +
				PsychologicalLine(10)[0].ToString() + "," +
				Math.Round(RSquared(8)[0],2).ToString() + "," +
				Math.Round(RelativeVigorIndex(10)[0],2).ToString() + "," +
				Math.Round(RIND(3,10)[0],0).ToString() + "," +
				Math.Round(ROC(14)[0],2).ToString() + "," +
				Math.Round(RSI(14,3)[0],0).ToString() + "," +
				Math.Round(RSI(14,3).Avg[0],0).ToString() + "," +
				Math.Round(RSS(10,40,5)[0],0).ToString() + "," +
				Math.Round(RVI(14)[0],0).ToString() + "," +
				Math.Round(StdDev(14)[0],1).ToString() + "," +
				Math.Round(StochRSI(14)[0],2).ToString() + "," +
				Math.Round(Stochastics(7,14,3).D[0],0).ToString() + "," +
				Math.Round(Stochastics(7,14,3).K[0],0).ToString() + "," +
				Math.Round(StochasticsFast(3,14).D[0],0).ToString() + "," +
				Math.Round(StochasticsFast(3,14).K[0],0).ToString() + "," +
				Math.Round(TRIX(14,3)[0],4).ToString() + "," +
				Math.Round(TRIX(14,3).Signal[0],4).ToString() + "," +
				CalculatePricePCT(Close[0],TSF(3,14)[0],3) + "," +
				Math.Round(TSI(3,14)[0],0).ToString() + "," +
				Math.Round(UltimateOscillator(7,14,28)[0],0).ToString() + "," +
				Math.Round(Vortex(14).VIPlus[0],1).ToString() + "," +
				Math.Round(Vortex(14).VIMinus[0],1).ToString() + "," +
				Math.Round(VOLMA(14)[0],0).ToString() + "," +
				Math.Round(VolumeOscillator(12,26)[0],0).ToString() + "," +
				Math.Round(VROC(14,3)[0],0).ToString() + "," +
				Math.Round(WilliamsR(14)[0],0).ToString() + "," +
				Math.Round(WisemanAwesomeOscillator()[0],1).ToString() + "," +
				Math.Round(WoodiesCCI(2,5,14,34,25,6,60,100,2)[0],0).ToString() + "," +
				Math.Round(WoodiesCCI(2,5,14,34,25,6,60,100,2).Turbo[0],0).ToString() + "," +
				CalculatePricePCT(Close[0],WoodiesPivots(HLCCalculationModeWoodie.CalcFromIntradayData,20).PP[0],3) + "," +
				CalculatePricePCT(Close[0],WoodiesPivots(HLCCalculationModeWoodie.CalcFromIntradayData,20).R1[0],3) + "," +
				CalculatePricePCT(Close[0],WoodiesPivots(HLCCalculationModeWoodie.CalcFromIntradayData,20).R2[0],3) + "," +
				CalculatePricePCT(Close[0],WoodiesPivots(HLCCalculationModeWoodie.CalcFromIntradayData,20).S1[0],3) + "," +
				CalculatePricePCT(Close[0],WoodiesPivots(HLCCalculationModeWoodie.CalcFromIntradayData,20).S2[0],3)
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

/// <summary>
/// This region holds all the todo items
/// </summary>
#region Todo
/// - add formatting of output
#endregion // Todo