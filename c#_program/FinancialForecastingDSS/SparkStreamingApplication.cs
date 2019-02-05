using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using FinancialForecastingDSS.indicators;
using FinancialForecastingDSS.ml;

namespace FinancialForecastingDSS
{
    class SparkStreamingApplication
    {
        // query conditions
        private string code { get; set; }
        private DateTime targetDate { get; set; }
        int numberOfData { get; set; }
        private int minRowCount { get; set; }

        // training set's number of splits and precentage
        private int numberOfSplits { get; set; }
        private int trainingPercentage { get; set; }

        // which indicators are going to be used.
        private bool isSMA { get; set; }
        private bool isWMA { get; set; }
        private bool isEMA { get; set; }
        private bool isMACD { get; set; }
        private bool isRSI { get; set; }
        private bool isStochastics { get; set; }
        private bool isWilliamsR { get; set; }

        // the features and labels will be exported
        private FeatureVector training { get; set; }
        private FeatureVector test { get; set; }

        public SparkStreamingApplication(string code, DateTime targetDate, int numberOfData, int numberOfSplits, int trainingPercentage, bool isSMA = true, bool isWMA = true, bool isEMA = true, bool isMACD = true, bool isRSI = true, bool isStochastics = true, bool isWilliamsR = true)
        {
            this.code = code;
            this.targetDate = targetDate;
            this.numberOfData = numberOfData;
            this.numberOfSplits = numberOfSplits;
            this.trainingPercentage = trainingPercentage;
            this.isSMA = isSMA;
            this.isWMA = isWMA;
            this.isEMA = isEMA;
            this.isMACD = isMACD;
            this.isRSI = isRSI;
            this.isStochastics = isStochastics;
            this.isWilliamsR = isWilliamsR;
            PreprocessIndicators();
        }

        private void PreprocessIndicators()
        {
            var data = IndicatorService.GetData(code, targetDate, new string[] { "Tarih", "Kapanis" }, numberOfData + 1);

            double[] sma = null;
            double[] wma = null;
            double[] ema = null;
            MovingAverageConvergenceDivergence macd = null;
            double[] rsi = null;
            double[] williamsR = null;
            Stochastics stochastics = null;

            if (isSMA)
                sma = MovingAverage.Simple(code, targetDate, 14, numberOfData);
            if (isWMA)
                wma = MovingAverage.Weighted(code, targetDate, 14, numberOfData);
            if (isEMA)
                ema = MovingAverage.Exponential(code, targetDate, 14, numberOfData);
            if (isMACD)
                macd = new MovingAverageConvergenceDivergence(code, targetDate, 12, 26, 9, numberOfData);
            if (isRSI)
                rsi = RelativeStrengthIndex.Rsi(code, targetDate, 14, numberOfData);
            if (isWilliamsR)
                williamsR = WilliamsR.Wsr(code, targetDate, 14, numberOfData);
            if (isStochastics)
                stochastics = new Stochastics(code, targetDate, 14, 3, 3, numberOfData);

            double[] closesOut = IndicatorDataPreprocessor.GetClosesOut(numberOfData, data);
            double[] smaOut = null;
            double[] wmaOut = null;
            double[] emaOut = null;
            double[] macdOut = null;
            double[] rsiOut = null;
            double[] williamsROut = null;
            double[] stochasticsOut = null;

            if (isSMA)
                smaOut = IndicatorDataPreprocessor.GetSMAOut(sma);
            if (isWMA)
                wmaOut = IndicatorDataPreprocessor.GetWMAOut(wma);
            if (isEMA)
                emaOut = IndicatorDataPreprocessor.GetEMAOut(ema);
            if (isMACD)
                macdOut = IndicatorDataPreprocessor.GetMACDOut(macd);
            if (isRSI)
                rsiOut = IndicatorDataPreprocessor.GetRSIOut(rsi, false);
            if (isWilliamsR)
                williamsROut = IndicatorDataPreprocessor.GetWilliamsROut(williamsR, false);
            if (isStochastics)
                stochasticsOut = IndicatorDataPreprocessor.GetStochasticsOut(stochastics, false);

            minRowCount = closesOut.Length;
            if (isSMA) minRowCount = minRowCount < smaOut.Length ? minRowCount : smaOut.Length;
            if (isWMA) minRowCount = minRowCount < wmaOut.Length ? minRowCount : wmaOut.Length;
            if (isEMA) minRowCount = minRowCount < emaOut.Length ? minRowCount : emaOut.Length;
            if (isMACD) minRowCount = minRowCount < macdOut.Length ? minRowCount : macdOut.Length;
            if (isRSI) minRowCount = minRowCount < rsiOut.Length ? minRowCount : rsiOut.Length;
            if (isWilliamsR) minRowCount = minRowCount < williamsROut.Length ? minRowCount : williamsROut.Length;
            if (isStochastics) minRowCount = minRowCount < stochasticsOut.Length ? minRowCount : stochasticsOut.Length;

            FeatureVector featureVector = new FeatureVector();
            if (isSMA)
                featureVector.AddColumn("SMA", smaOut.Select(p => (object)p.ToString(CultureInfo.InvariantCulture)).Take(minRowCount).ToArray());
            if (isWMA)
                featureVector.AddColumn("WMA", wmaOut.Select(p => (object)p.ToString(CultureInfo.InvariantCulture)).Take(minRowCount).ToArray());
            if (isEMA)
                featureVector.AddColumn("EMA", emaOut.Select(p => (object)p.ToString(CultureInfo.InvariantCulture)).Take(minRowCount).ToArray());
            if (isMACD)
                featureVector.AddColumn("MACD", macdOut.Select(p => (object)p.ToString(CultureInfo.InvariantCulture)).Take(minRowCount).ToArray());
            if (isRSI)
                featureVector.AddColumn("RSI", rsiOut.Select(p => (object)p.ToString(CultureInfo.InvariantCulture)).Take(minRowCount).ToArray());
            if (isWilliamsR)
                featureVector.AddColumn("WilliamsR", williamsROut.Select(p => (object)p.ToString(CultureInfo.InvariantCulture)).Take(minRowCount).ToArray());
            if (isStochastics)
                featureVector.AddColumn("Stochastics", stochasticsOut.Select(p => (object)p.ToString(CultureInfo.InvariantCulture)).Take(minRowCount).ToArray());
            featureVector.AddColumn("label", closesOut.Select(p => (object)string.Format("{0:0.0}", p).ToString(CultureInfo.InvariantCulture)).Take(minRowCount).ToArray());

            int count = featureVector.Values[0].Length;
            training = new FeatureVector();
            test = new FeatureVector();

            for (int i = 0; i < featureVector.ColumnName.Count; i++)
            {
                training.AddColumn(featureVector.ColumnName[i], featureVector.Values[i].Take((int)(count * trainingPercentage / 100.0)).ToArray());
                test.AddColumn(featureVector.ColumnName[i], featureVector.Values[i].Skip((int)(count * trainingPercentage / 100.0)).Take(count).ToArray()); // Take(count) means take the rest of all elements, number of the rest of the elements is smaller than count.
            }
        }

        public void Run()
        {
            string filePath = (Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX) ? Environment.GetEnvironmentVariable("HOME") : Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%");
            RunSparkStreaming(filePath);
            Console.WriteLine("Sleep for 10 seconds.");
            Thread.Sleep(10000);

            var splittedExporterTest = new LabeledPointSplittedExporter(training, numberOfSplits);
            var splittedExporterTraining = new LabeledPointSplittedExporter(training, numberOfSplits);

            splittedExporterTest.InitialWrite(filePath + "\\test\\test" + 0 + ".txt", 500);
            splittedExporterTraining.InitialWrite(filePath + "\\training\\training" + 0 + ".txt", 500);

            Console.WriteLine("Model is trained by 500 rows.");

            for (int i = 500; i < minRowCount; i++)
            {
                //new LabeledPointExporter(test).Export(filePath + "\\test\\test" + i + ".txt");
                splittedExporterTest.Export(filePath + "\\test\\test" + i + ".txt");
                Thread.Sleep(5000);
                splittedExporterTraining.Export(filePath + "\\training\\training" + i + ".txt");
                Thread.Sleep(5000);
                Console.WriteLine((i + 1) + "/" + minRowCount + " of data is sent to Spark.");
            }
        }

        private void RunSparkStreaming(string filePath)
        {
            RunCommand("del " + filePath + "\\spark_evaluation.txt", true);
            DeleteDirectoryContents(filePath + "\\spark_output");
            DeleteDirectoryContents(filePath + "\\training");
            DeleteDirectoryContents(filePath + "\\test");
        }

        /**
            @see: https://stackoverflow.com/a/1288747
        */
        private static void DeleteDirectoryContents(string directoryPath)
        {
            System.IO.DirectoryInfo di = new DirectoryInfo(directoryPath);

            foreach (FileInfo file in di.GetFiles())
            {
                file.Delete();
            }
            foreach (DirectoryInfo dir in di.GetDirectories())
            {
                dir.Delete(true);
            }
        }

        private static void RunCommand(string command, bool hideCMD = false)
        {
            if (!hideCMD)
            {
                string strCmdText = "/C " + command;
                System.Diagnostics.Process.Start("CMD.exe", strCmdText);
            }
            else
            {
                System.Diagnostics.Process process = new System.Diagnostics.Process();
                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                startInfo.FileName = "cmd.exe";
                startInfo.Arguments = "/C " + command;
                process.StartInfo = startInfo;
                process.Start();

            }

        }
    }
}
