using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using FinancialForecastingDSS.indicators;
using FinancialForecastingDSS.ml;
using FinancialForecastingDSS.ml.algorithms;

namespace FinancialForecastingDSS
{
    class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            /* database connection */
            MongoDBService.InitiateService("mongodb://localhost:20000", "financialData", "data");

            RunSparkStreamingApplication();
            Console.ReadLine();
        }

        private static void RunFeatureSelector(int mlAlgorithm, bool isCrossValidationEnabled, double trainingSetPercentage)
        {
            string code = "AKBNK";
            DateTime targetDate = new DateTime(2018, 11, 1).ToLocalTime();

            int numberOfData = 4000;

            var data = IndicatorService.GetData(code, targetDate, new string[] { "Tarih", "Kapanis" }, numberOfData + 1);
            double[] sma = MovingAverage.Simple(code, targetDate, 14, numberOfData);
            double[] wma = MovingAverage.Weighted(code, targetDate, 14, numberOfData);
            double[] ema = MovingAverage.Exponential(code, targetDate, 14, numberOfData);
            MovingAverageConvergenceDivergence macd = new MovingAverageConvergenceDivergence(code, targetDate, 12, 26, 9, numberOfData);
            double[] rsi = RelativeStrengthIndex.Rsi(code, targetDate, 14, numberOfData);
            double[] williams = WilliamsR.Wsr(code, targetDate, 14, numberOfData);
            Stochastics stochastics = new Stochastics(code, targetDate, 14, 3, 3, numberOfData);

            double[] closesOut = IndicatorDataPreprocessor.GetClosesOut(numberOfData, data);
            double[] smaOut = IndicatorDataPreprocessor.GetSMAOut(sma);
            double[] wmaOut = IndicatorDataPreprocessor.GetWMAOut(wma);
            double[] emaOut = IndicatorDataPreprocessor.GetEMAOut(ema);
            double[] macdOut = IndicatorDataPreprocessor.GetMACDOut(macd);
            double[] rsiOut = IndicatorDataPreprocessor.GetRSIOut(rsi, false);
            double[] williamsROut = IndicatorDataPreprocessor.GetWilliamsROut(williams, false);
            double[] stochasticsOut = IndicatorDataPreprocessor.GetStochasticsOut(stochastics, false);

            int minRowCount;
            minRowCount = smaOut.Length;
            minRowCount = minRowCount < wmaOut.Length ? minRowCount : wmaOut.Length;
            minRowCount = minRowCount < emaOut.Length ? minRowCount : emaOut.Length;
            minRowCount = minRowCount < macdOut.Length ? minRowCount : macdOut.Length;
            minRowCount = minRowCount < rsiOut.Length ? minRowCount : rsiOut.Length;
            minRowCount = minRowCount < williamsROut.Length ? minRowCount : williamsROut.Length;
            minRowCount = minRowCount < stochasticsOut.Length ? minRowCount : stochasticsOut.Length;
            minRowCount = minRowCount < closesOut.Length ? minRowCount : closesOut.Length;

            int numberOfIndicators = IndicatorService.indicators.Length;
            int numberOfCombinations = (int)Math.Pow(2, numberOfIndicators) - 1;
            for (int i = 1; i <= numberOfCombinations; i++)
            {
                int tmp = i;
                List<int> indicators = new List<int>();
                for (int j = 0; j < numberOfIndicators; j++)
                {
                    if ((tmp & 1) == 1)
                        indicators.Add(IndicatorService.indicators[j]);
                    tmp >>= 1;
                }

                double accuracy = CalculateAccuracy(indicators, mlAlgorithm, isCrossValidationEnabled, minRowCount, trainingSetPercentage, smaOut, wmaOut, emaOut, macdOut, rsiOut, williamsROut, stochasticsOut, closesOut);
                if (indicators.Contains(IndicatorService.SMA))
                    Console.Write("SMA ");
                if (indicators.Contains(IndicatorService.WMA))
                    Console.Write("WMA ");
                if (indicators.Contains(IndicatorService.EMA))
                    Console.Write("EMA ");
                if (indicators.Contains(IndicatorService.MACD))
                    Console.Write("MACD ");
                if (indicators.Contains(IndicatorService.RSI))
                    Console.Write("RSI ");
                if (indicators.Contains(IndicatorService.WilliamsR))
                    Console.Write("WilliamsR ");
                if (indicators.Contains(IndicatorService.Stochastics))
                    Console.Write("Stochastics ");
                Console.WriteLine("=>\t" + accuracy);
            }

        }

        private static double CalculateAccuracy(List<int> indicators, int mlAlgorithm, bool isCrossValidationEnabled, int minRowCount, double trainingSetPercentage, double[] smaOut, double[] wmaOut, double[] emaOut, double[] macdOut, double[] rsiOut, double[] williamsROut, double[] stochasticsOut, double[] closesOut)
        {
            FeatureVector vector = new FeatureVector();
            if (indicators.Contains(IndicatorService.SMA))
                vector.AddColumn("SMA", smaOut.Select(p => (object)p.ToString(CultureInfo.InvariantCulture)).Take(minRowCount).ToArray());
            if (indicators.Contains(IndicatorService.WMA))
                vector.AddColumn("WMA", wmaOut.Select(p => (object)p.ToString(CultureInfo.InvariantCulture)).Take(minRowCount).ToArray());
            if (indicators.Contains(IndicatorService.EMA))
                vector.AddColumn("EMA", emaOut.Select(p => (object)p.ToString(CultureInfo.InvariantCulture)).Take(minRowCount).ToArray());
            if (indicators.Contains(IndicatorService.MACD))
                vector.AddColumn("MACD", macdOut.Select(p => (object)p.ToString(CultureInfo.InvariantCulture)).Take(minRowCount).ToArray());
            if (indicators.Contains(IndicatorService.RSI))
                vector.AddColumn("RSI", rsiOut.Select(p => (object)p.ToString(CultureInfo.InvariantCulture)).Take(minRowCount).ToArray());
            if (indicators.Contains(IndicatorService.WilliamsR))
                vector.AddColumn("WilliamsR", williamsROut.Select(p => (object)p.ToString(CultureInfo.InvariantCulture)).Take(minRowCount).ToArray());
            if (indicators.Contains(IndicatorService.Stochastics))
                vector.AddColumn("Stochastics", stochasticsOut.Select(p => (object)p.ToString(CultureInfo.InvariantCulture)).Take(minRowCount).ToArray());
            vector.AddColumn("label", closesOut.Select(p => (object)string.Format("{0:0.0}", p).ToString(CultureInfo.InvariantCulture)).Take(minRowCount).ToArray());

            new CSVExporter(vector).Export("c:\\users\\yasin\\indicatorOutput.csv");
            int count = vector.Values[0].Length;
            FeatureVector training = new FeatureVector();
            for (int i = 0; i < vector.ColumnName.Count; i++)
            {
                training.AddColumn(vector.ColumnName[i], vector.Values[i].Take((int)(count * trainingSetPercentage)).ToArray());
            }

            FeatureVector test = new FeatureVector();
            for (int i = 0; i < vector.ColumnName.Count; i++)
            {
                test.AddColumn(vector.ColumnName[i], vector.Values[i].Skip((int)(count * trainingSetPercentage)).Take(count).ToArray());
            }

            double accuracy = 0;
            if (mlAlgorithm == MLAService.LIN_REG)
            {
                var linReg = new LinearRegression();
                var bce = new BinaryClassificationEvaluator();
                if (isCrossValidationEnabled)
                {
                    var cv = new CrossValidator(linReg, bce, 10);
                    var cvModel = (CrossValidatorModel)cv.Fit(training);
                    var predictions = cvModel.transform(test);
                    bce.evaluate(predictions);
                    accuracy = bce.Accuracy;
                }
                else
                {
                    var linRegModel = (LinearRegressionModel)linReg.Fit(training);
                    var predictions = linRegModel.transform(test);
                    bce.evaluate(predictions);
                    accuracy = bce.Accuracy;
                }
            }
            else if (mlAlgorithm == MLAService.LOG_REG)
            {
                var logReg = new LogisticRegression();
                var bce = new BinaryClassificationEvaluator();
                if (isCrossValidationEnabled)
                {
                    var cv = new CrossValidator(logReg, bce, 10);
                    var cvModel = (CrossValidatorModel)cv.Fit(training);
                    var predictions = cvModel.transform(test);
                    bce.evaluate(predictions);
                    accuracy = bce.Accuracy;
                }
                else
                {
                    var logRegModel = (LogisticRegressionModel)logReg.Fit(training);
                    var predictions = logRegModel.transform(test);
                    bce.evaluate(predictions);
                    accuracy = bce.Accuracy;
                }
            }
            else if (mlAlgorithm == MLAService.NAI_BAY)
            {
                var naiBay = new NaiveBayes();
                var bce = new BinaryClassificationEvaluator();
                if (isCrossValidationEnabled)
                {
                    var cv = new CrossValidator(naiBay, bce, 10);
                    var cvModel = (CrossValidatorModel)cv.Fit(training);
                    var predictions = cvModel.transform(test);
                    bce.evaluate(predictions);
                    accuracy = bce.Accuracy;
                }
                else
                {
                    var naiBayModel = (NaiveBayesModel)naiBay.Fit(training);
                    var predictions = naiBayModel.transform(test);
                    bce.evaluate(predictions);
                    accuracy = bce.Accuracy;
                }
            }
            return accuracy;
        }

        public static void RunFormApplication()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new form.View());
        }

        private static void ExportFeaturesAndLabel(string code)
        {
            string filePath = (Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX) ? Environment.GetEnvironmentVariable("HOME") : Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%");
            filePath += "\\indicatorOutput.txt";

            DateTime targetDate = IndicatorService.LastDate(code);

            int numberOfData = 4000;

            var data = IndicatorService.GetData(code, targetDate, new string[] { "Tarih", "Kapanis" }, numberOfData + 1);
            double[] sma = MovingAverage.Simple(code, targetDate, 14, numberOfData);
            double[] wma = MovingAverage.Weighted(code, targetDate, 14, numberOfData);
            double[] ema = MovingAverage.Exponential(code, targetDate, 14, numberOfData);
            MovingAverageConvergenceDivergence macd = new MovingAverageConvergenceDivergence(code, targetDate, 12, 26, 9, numberOfData);
            double[] rsi = RelativeStrengthIndex.Rsi(code, targetDate, 14, numberOfData);
            double[] williams = WilliamsR.Wsr(code, targetDate, 14, numberOfData);
            Stochastics stochastics = new Stochastics(code, targetDate, 14, 3, 3, numberOfData);

            double[] closesOut = IndicatorDataPreprocessor.GetClosesOut(numberOfData, data);
            double[] smaOut = IndicatorDataPreprocessor.GetSMAOut(sma);
            double[] wmaOut = IndicatorDataPreprocessor.GetWMAOut(wma);
            double[] emaOut = IndicatorDataPreprocessor.GetEMAOut(ema);
            double[] macdOut = IndicatorDataPreprocessor.GetMACDOut(macd);
            double[] rsiOut = IndicatorDataPreprocessor.GetRSIOut(rsi);
            double[] williamsROut = IndicatorDataPreprocessor.GetWilliamsROut(williams);
            double[] stochasticsOut = IndicatorDataPreprocessor.GetStochasticsOut(stochastics);

            int minRowCount;
            minRowCount = smaOut.Length;
            minRowCount = minRowCount < wmaOut.Length ? minRowCount : wmaOut.Length;
            minRowCount = minRowCount < emaOut.Length ? minRowCount : emaOut.Length;
            minRowCount = minRowCount < macdOut.Length ? minRowCount : macdOut.Length;
            minRowCount = minRowCount < rsiOut.Length ? minRowCount : rsiOut.Length;
            minRowCount = minRowCount < williamsROut.Length ? minRowCount : williamsROut.Length;
            minRowCount = minRowCount < stochasticsOut.Length ? minRowCount : stochasticsOut.Length;
            minRowCount = minRowCount < closesOut.Length ? minRowCount : closesOut.Length;

            FeatureVector vector = new FeatureVector();
            vector.AddColumn("SMA", smaOut.Select(p => (object)p.ToString(CultureInfo.InvariantCulture)).Take(minRowCount).ToArray());
            vector.AddColumn("WMA", wmaOut.Select(p => (object)p.ToString(CultureInfo.InvariantCulture)).Take(minRowCount).ToArray());
            vector.AddColumn("EMA", emaOut.Select(p => (object)p.ToString(CultureInfo.InvariantCulture)).Take(minRowCount).ToArray());
            vector.AddColumn("MACD", macdOut.Select(p => (object)p.ToString(CultureInfo.InvariantCulture)).Take(minRowCount).ToArray());
            vector.AddColumn("RSI", rsiOut.Select(p => (object)p.ToString(CultureInfo.InvariantCulture)).Take(minRowCount).ToArray());
            vector.AddColumn("WilliamsR", williamsROut.Select(p => (object)p.ToString(CultureInfo.InvariantCulture)).Take(minRowCount).ToArray());
            vector.AddColumn("Stochastics", stochasticsOut.Select(p => (object)p.ToString(CultureInfo.InvariantCulture)).Take(minRowCount).ToArray());
            vector.AddColumn("label", closesOut.Select(p => (object)string.Format("{0:0.0}", p).ToString(CultureInfo.InvariantCulture)).Take(minRowCount).ToArray());

            new CSVExporter(vector).Export(filePath);
            Console.WriteLine("Operations completed.");
        }

        private static void RunConsoleApplication()
        {
            string filePath = (Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX) ? Environment.GetEnvironmentVariable("HOME") : Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%");
            filePath += "\\indicatorOutput.txt";

            string code = "AKBNK";
            DateTime targetDate = new DateTime(2018, 11, 1).ToLocalTime();

            int numberOfData = 1000;

            var data = IndicatorService.GetData(code, targetDate, new string[] { "Tarih", "Kapanis" }, numberOfData + 1);
            double[] sma = MovingAverage.Simple(code, targetDate, 14, numberOfData);
            double[] wma = MovingAverage.Weighted(code, targetDate, 14, numberOfData);
            double[] ema = MovingAverage.Exponential(code, targetDate, 14, numberOfData);
            MovingAverageConvergenceDivergence macd = new MovingAverageConvergenceDivergence(code, targetDate, 12, 26, 9, numberOfData);
            double[] rsi = RelativeStrengthIndex.Rsi(code, targetDate, 14, numberOfData);
            double[] williams = WilliamsR.Wsr(code, targetDate, 14, numberOfData);
            Stochastics stochastics = new Stochastics(code, targetDate, 14, 3, 3, numberOfData);

            double[] closesOut = IndicatorDataPreprocessor.GetClosesOut(numberOfData, data);
            double[] smaOut = IndicatorDataPreprocessor.GetSMAOut(sma);
            double[] wmaOut = IndicatorDataPreprocessor.GetWMAOut(wma);
            double[] emaOut = IndicatorDataPreprocessor.GetEMAOut(ema);
            double[] macdOut = IndicatorDataPreprocessor.GetMACDOut(macd);
            double[] rsiOut = IndicatorDataPreprocessor.GetRSIOut(rsi);
            double[] williamsROut = IndicatorDataPreprocessor.GetWilliamsROut(williams);
            double[] stochasticsOut = IndicatorDataPreprocessor.GetStochasticsOut(stochastics);

            int minRowCount;
            minRowCount = smaOut.Length;
            minRowCount = minRowCount < wmaOut.Length ? minRowCount : wmaOut.Length;
            minRowCount = minRowCount < emaOut.Length ? minRowCount : emaOut.Length;
            minRowCount = minRowCount < macdOut.Length ? minRowCount : macdOut.Length;
            minRowCount = minRowCount < rsiOut.Length ? minRowCount : rsiOut.Length;
            minRowCount = minRowCount < williamsROut.Length ? minRowCount : williamsROut.Length;
            minRowCount = minRowCount < stochasticsOut.Length ? minRowCount : stochasticsOut.Length;
            minRowCount = minRowCount < closesOut.Length ? minRowCount : closesOut.Length;
            FeatureVector vector = new FeatureVector();
            vector.AddColumn("SMA", smaOut.Select(p => (object)p.ToString(CultureInfo.InvariantCulture)).Take(minRowCount).ToArray());
            vector.AddColumn("WMA", wmaOut.Select(p => (object)p.ToString(CultureInfo.InvariantCulture)).Take(minRowCount).ToArray());
            vector.AddColumn("EMA", emaOut.Select(p => (object)p.ToString(CultureInfo.InvariantCulture)).Take(minRowCount).ToArray());
            vector.AddColumn("MACD", macdOut.Select(p => (object)p.ToString(CultureInfo.InvariantCulture)).Take(minRowCount).ToArray());
            vector.AddColumn("RSI", rsiOut.Select(p => (object)p.ToString(CultureInfo.InvariantCulture)).Take(minRowCount).ToArray());
            vector.AddColumn("WilliamsR", williamsROut.Select(p => (object)p.ToString(CultureInfo.InvariantCulture)).Take(minRowCount).ToArray());
            vector.AddColumn("Stochastics", stochasticsOut.Select(p => (object)p.ToString(CultureInfo.InvariantCulture)).Take(minRowCount).ToArray());
            vector.AddColumn("label", closesOut.Select(p => (object)string.Format("{0:0.0}", p).ToString(CultureInfo.InvariantCulture)).Take(minRowCount).ToArray());

            new LabeledPointExporter(vector).Export(filePath);

            int count = vector.Values[0].Length;
            FeatureVector training = new FeatureVector();
            for (int i = 0; i < vector.ColumnName.Count; i++)
            {
                training.AddColumn(vector.ColumnName[i], vector.Values[i].Take(count / 2).ToArray());
            }

            FeatureVector test = new FeatureVector();
            for (int i = 0; i < vector.ColumnName.Count; i++)
            {
                test.AddColumn(vector.ColumnName[i], vector.Values[i].Skip(count / 2).Take(count / 2).ToArray());
            }

            //TestNaiveBayes(training, test);
            //TestNaiveBayesUsingCrossValidation(training, test);
            //TestLinearRegression(training, test);
            //TestLinearRegressionUsingCrossValidation(training, test);
            //TestLogisticRegression(training, test);
            //TestLogisticRegressionUsingCrossValidation(training, test);
        }

        private static void TestNaiveBayes(FeatureVector training, FeatureVector test)
        {
            NaiveBayes nb = new NaiveBayes();
            NaiveBayesModel nbModel = (NaiveBayesModel)nb.Fit(training);
            FeatureVector predictions = nbModel.transform(test);
            PrintPredictionsAndEvaluate(predictions);
        }

        private static void TestNaiveBayesUsingCrossValidation(FeatureVector training, FeatureVector test)
        {
            CrossValidator cv = new CrossValidator(new NaiveBayes(), new BinaryClassificationEvaluator(), 10);
            CrossValidatorModel cvModel = (CrossValidatorModel)cv.Fit(training);
            Console.WriteLine("10-fold cross validator accuracy: " + cv.Accuracy);
            FeatureVector predictions = cvModel.transform(test);
            PrintPredictionsAndEvaluate(predictions);
        }

        private static void TestLinearRegression(FeatureVector training, FeatureVector test)
        {
            LinearRegression lr = new LinearRegression();
            LinearRegressionModel lrModel = (LinearRegressionModel)lr.Fit(training);
            FeatureVector predictions = lrModel.transform(test);
            PrintPredictionsAndEvaluate(predictions);
        }

        private static void TestLinearRegressionUsingCrossValidation(FeatureVector training, FeatureVector test)
        {
            CrossValidator cv = new CrossValidator(new LinearRegression(), new BinaryClassificationEvaluator(), 10);
            CrossValidatorModel cvModel = (CrossValidatorModel)cv.Fit(training);
            FeatureVector predictions = cvModel.transform(test);
            PrintPredictionsAndEvaluate(predictions);
        }

        private static void TestLogisticRegression(FeatureVector training, FeatureVector test)
        {
            LogisticRegression lr = new LogisticRegression(0.1, 3000);
            LogisticRegressionModel lrModel = (LogisticRegressionModel)lr.Fit(training);
            FeatureVector predictions = lrModel.transform(test);
            PrintPredictionsAndEvaluate(predictions);
        }

        private static void TestLogisticRegressionUsingCrossValidation(FeatureVector training, FeatureVector test)
        {
            CrossValidator cv = new CrossValidator(new LogisticRegression(), new BinaryClassificationEvaluator(), 10);
            CrossValidatorModel cvModel = (CrossValidatorModel)cv.Fit(training);
            Console.WriteLine("10-fold cross validator accuracy: " + cv.Accuracy);
            FeatureVector predictions = cvModel.transform(test);
            PrintPredictionsAndEvaluate(predictions);
        }

        public static void PrintPredictionsAndEvaluate(FeatureVector predictions)
        {
            for (int i = 0; i < predictions.ColumnName.Count; i++)
            {
                Console.Write(predictions.ColumnName[i] + "\t");
            }
            Console.WriteLine();

            for (int i = 0; i < predictions.Values[0].Length; i++)
            {
                for (int j = 0; j < predictions.Values.Count; j++)
                {
                    Console.Write(predictions.Values[j][i] + "\t");
                }
                Console.WriteLine();
            }

            BinaryClassificationEvaluator bce = new BinaryClassificationEvaluator();
            bce.evaluate(predictions);
            Console.WriteLine("TN: " + bce.confusionMatrix.TN);
            Console.WriteLine("TP: " + bce.confusionMatrix.TP);
            Console.WriteLine("FN: " + bce.confusionMatrix.FN);
            Console.WriteLine("FP: " + bce.confusionMatrix.FP);
            Console.WriteLine("ACCURACY = " + bce.Accuracy);
        }

        private static void RunSparkStreamingApplication()
        {
            bool isSMA, isWMA, isEMA, isMACD, isRSI, isStochastics, isWilliamsR;
            isSMA = isIndicatorWillBeUsed("Simple Moving Average");
            isWMA = isIndicatorWillBeUsed("Weighted Moving Average");
            isEMA = isIndicatorWillBeUsed("Exponential Moving Average");
            isMACD = isIndicatorWillBeUsed("Moving Average Convergence Divergence");
            isRSI = isIndicatorWillBeUsed("Relative Strength Index");
            isStochastics = isIndicatorWillBeUsed("Stochastic Oscillator");
            isWilliamsR = isIndicatorWillBeUsed("Williams' %R");

            SparkStreamingApplication sparkStreamingApplication = new SparkStreamingApplication("AKBNK", new DateTime(2018, 11, 1).ToLocalTime(), 4000, 100, 80, isSMA, isWMA, isEMA, isMACD, isRSI, isStochastics, isWilliamsR);
            sparkStreamingApplication.Run();
        }

        private static bool isIndicatorWillBeUsed(string indicatorName)
        {
            bool condition;
            Console.Write("Do you want to use " + indicatorName + " (y/n): ");
            string response = Console.ReadLine().ToLower();
            while (response != "y" && response != "n")
            {
                Console.Write("Please enter a valid answer (y/n): ");
                response = Console.ReadLine().ToLower();
            }
            condition = response == "y" ? true : false;
            return condition;
        }
    } // End of class Program
} // End of namespace FinancialForecastingDSS
