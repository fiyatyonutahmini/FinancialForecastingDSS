using System;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using FinancialForecastingDSS.indicators;
using FinancialForecastingDSS.ml;
using FinancialForecastingDSS.ml.algorithms;
using MongoDB.Bson;
using MongoDB.Driver;

namespace FinancialForecastingDSS.form
{
    public partial class View : Form
    {
        private string code;
        private DateTime targetDate;
        private int numberOfData;
        private double trainingSetPercentage;
        private int numFolds;
        private double linRegAcc, logRegAcc, naiBayAcc;

        private int smaPeriod;
        private int wmaPeriod;
        private int emaPeriod;
        private int firstPeriod, secondPeriod, triggerPeriod; // MACD Parameters
        private int rsiPeriod; // RSI Parameter
        private int fastKPeriod, fastDPeriod, slowDPeriod; // Stochastics Parameters
        private int williamsRPeriod; // Williams' %R Parameter

        private bool isSMAChecked = false;
        private bool isWMAChecked = false;
        private bool isEMAChecked = false;
        private bool isMACDChecked = false;
        private bool isStochasticsChecked = false;
        private bool isWilliamsRChecked = false;
        private bool isRSIChecked = false;

        public View()
        {
            InitializeComponent();
        }

        private void View_Load(object sender, EventArgs e)
        {
            Location = new Point(200, 200);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;

            panelForCodeSelection.BringToFront();
            comboBoxForCodeSelection.Items.AddRange(IndicatorService.GetCodeList().Select(p => p.GetElement(0).Value.ToString()).OrderBy(p => p).ToArray());
            comboBoxForCodeSelection.SelectedIndex = 0;

            numericUpDownForNumberOfData.Minimum = 1;
            numericUpDownForNumberOfData.Maximum = 1000;
            numericUpDownForNumberOfData.Value = 300;
        }

        private void buttonForCodeSelectionNext_Click(object sender, EventArgs e)
        {
            code = comboBoxForCodeSelection.SelectedItem.ToString();
            labelForDateSelectionText.Text = code + " verileri üzerinde hangi tarihten itibaren geriye doğru kaç günlük veri üzerinde çalışılacağını seçiniz.";

            PipelineDefinition<BsonDocument, BsonDocument> pipeline = new BsonDocument[] {
                new BsonDocument("$match", new BsonDocument("Kod", code)),
                new BsonDocument() { { "$group",
                        new BsonDocument() {
                            { "_id", new BsonDocument() },
                            { "minDate", new BsonDocument("$min", "$Tarih") },
                            { "maxDate", new BsonDocument("$max", "$Tarih") },
                            { "count", new BsonDocument("$sum", 1) }
                        } } },
                new BsonDocument("$project", new BsonDocument("_id", 0))
            };

            try
            {
                var dates = MongoDBService.GetService().GetCollection().Aggregate(pipeline).ToList().ElementAt(0);
                dateTimePickerForDateSelection.MinDate = dates.GetElement("minDate").Value.ToLocalTime();
                dateTimePickerForDateSelection.MaxDate = dates.GetElement("maxDate").Value.ToLocalTime();
                dateTimePickerForDateSelection.Value = dates.GetElement("maxDate").Value.ToLocalTime();

                numericUpDownForNumberOfData.Maximum = dates.GetElement("count").Value.ToInt32();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            panelForDateSelection.BringToFront();
        }

        private void buttonForDateSelectionPrev_Click(object sender, EventArgs e)
        {
            panelForCodeSelection.BringToFront();
        }

        private void buttonForDateSelectionNext_Click(object sender, EventArgs e)
        {
            targetDate = dateTimePickerForDateSelection.Value;
            numberOfData = (int)numericUpDownForNumberOfData.Value;
            panelForIndicatorScreen1.BringToFront();
        }

        private void buttonForIndicatorScreen1Prev_Click(object sender, EventArgs e)
        {
            panelForDateSelection.BringToFront();
        }

        private void buttonForIndicatorScreen1Next_Click(object sender, EventArgs e)
        {
            isSMAChecked = checkBoxForSMA.Checked;
            isWMAChecked = checkBoxForWMA.Checked;
            isEMAChecked = checkBoxForEMA.Checked;
            isMACDChecked = checkBoxForMACD.Checked;
            isStochasticsChecked = checkBoxForStochastics.Checked;
            isWilliamsRChecked = checkBoxForWilliamsR.Checked;
            isRSIChecked = checkBoxForRSI.Checked;

            numericUpDownForSMA.Enabled = isSMAChecked;
            numericUpDownForWMA.Enabled = isWMAChecked;
            numericUpDownForEMA.Enabled = isEMAChecked;
            numericUpDownForMACDFirst.Enabled = isMACDChecked;
            numericUpDownForMACDSecond.Enabled = isMACDChecked;
            numericUpDownForMACDTrigger.Enabled = isMACDChecked;
            numericUpDownForStochasticsFastK.Enabled = isStochasticsChecked;
            numericUpDownForStochasticsSlowK.Enabled = isStochasticsChecked;
            numericUpDownForStochasticsSlowD.Enabled = isStochasticsChecked;
            numericUpDownForWilliamsR.Enabled = isWilliamsRChecked;
            numericUpDownForRSI.Enabled = isRSIChecked;

            panelForIndicatorScreen2.BringToFront();
        }

        private void buttonForIndicatorScreen2Prev_Click(object sender, EventArgs e)
        {
            panelForIndicatorScreen1.BringToFront();
        }

        private void buttonForIndicatorScreen2Next_Click(object sender, EventArgs e)
        {
            smaPeriod = (int)numericUpDownForSMA.Value;
            wmaPeriod = (int)numericUpDownForWMA.Value;
            emaPeriod = (int)numericUpDownForEMA.Value;
            firstPeriod = (int)numericUpDownForMACDFirst.Value;
            secondPeriod = (int)numericUpDownForMACDSecond.Value;
            triggerPeriod = (int)numericUpDownForMACDTrigger.Value;
            fastKPeriod = (int)numericUpDownForStochasticsFastK.Value;
            fastDPeriod = (int)numericUpDownForStochasticsSlowK.Value;
            slowDPeriod = (int)numericUpDownForStochasticsSlowD.Value;
            williamsRPeriod = (int)numericUpDownForWilliamsR.Value;
            rsiPeriod = (int)numericUpDownForRSI.Value;
            panelForDataSplitSettings.BringToFront();
        }

        private void buttonForDataSplitPrev_Click(object sender, EventArgs e)
        {
            panelForIndicatorScreen2.BringToFront();
        }

        private void buttonForDataSplitNext_Click(object sender, EventArgs e)
        {
            trainingSetPercentage = (double)numericUpDownForTrainingSetPercent.Value / 100.0;
            numFolds = (int)numericUpDownForNumFolds.Value;

            double[] smaOut = null;
            double[] wmaOut = null;
            double[] emaOut = null;
            double[] macdOut = null;
            double[] stochasticsOut = null;
            double[] williamsROut = null;
            double[] rsiOut = null;
            double[] closesOut = null;

            var data = IndicatorService.GetData(code, targetDate, new string[] { "Tarih", "Kapanis" }, numberOfData + 1);

            if (isSMAChecked)
                smaOut = IndicatorDataPreprocessor.GetSMAOut(MovingAverage.Simple(code, targetDate, smaPeriod, numberOfData));
            if (isWMAChecked)
                wmaOut = IndicatorDataPreprocessor.GetWMAOut(MovingAverage.Weighted(code, targetDate, wmaPeriod, numberOfData));
            if (isEMAChecked)
                emaOut = IndicatorDataPreprocessor.GetEMAOut(MovingAverage.Exponential(code, targetDate, emaPeriod, numberOfData));
            if (isMACDChecked)
                macdOut = IndicatorDataPreprocessor.GetMACDOut(new MovingAverageConvergenceDivergence(code, targetDate, firstPeriod, secondPeriod, triggerPeriod, numberOfData));
            if (isStochasticsChecked)
                stochasticsOut = IndicatorDataPreprocessor.GetStochasticsOut(new Stochastics(code, targetDate, fastKPeriod, fastDPeriod, slowDPeriod, numberOfData));
            if (isWilliamsRChecked)
                williamsROut = IndicatorDataPreprocessor.GetWilliamsROut(WilliamsR.Wsr(code, targetDate, williamsRPeriod, numberOfData));
            if (isRSIChecked)
                rsiOut = IndicatorDataPreprocessor.GetRSIOut(RelativeStrengthIndex.Rsi(code, targetDate, rsiPeriod, numberOfData));
            closesOut = IndicatorDataPreprocessor.GetClosesOut(numberOfData, data);

            int minRowCount = 1000000;
            if (smaOut != null)
                minRowCount = smaOut.Length;
            if (wmaOut != null)
                minRowCount = minRowCount < wmaOut.Length ? minRowCount : wmaOut.Length;
            if (emaOut != null)
                minRowCount = minRowCount < emaOut.Length ? minRowCount : emaOut.Length;
            if (macdOut != null)
                minRowCount = minRowCount < macdOut.Length ? minRowCount : macdOut.Length;
            if (rsiOut != null)
                minRowCount = minRowCount < rsiOut.Length ? minRowCount : rsiOut.Length;
            if (williamsROut != null)
                minRowCount = minRowCount < williamsROut.Length ? minRowCount : williamsROut.Length;
            if (stochasticsOut != null)
                minRowCount = minRowCount < stochasticsOut.Length ? minRowCount : stochasticsOut.Length;
            if (closesOut != null)
                minRowCount = minRowCount < closesOut.Length ? minRowCount : closesOut.Length;

            var fv = new FeatureVector();
            if (isSMAChecked)
                fv.AddColumn("SMA", smaOut.Select(p => (object)p.ToString(CultureInfo.InvariantCulture)).Take(minRowCount).ToArray());
            if (isWMAChecked)
                fv.AddColumn("WMA", wmaOut.Select(p => (object)p.ToString(CultureInfo.InvariantCulture)).Take(minRowCount).ToArray());
            if (isEMAChecked)
                fv.AddColumn("EMA", emaOut.Select(p => (object)p.ToString(CultureInfo.InvariantCulture)).Take(minRowCount).ToArray());
            if (isMACDChecked)
                fv.AddColumn("MACD", macdOut.Select(p => (object)p.ToString(CultureInfo.InvariantCulture)).Take(minRowCount).ToArray());
            if (isRSIChecked)
                fv.AddColumn("RSI", rsiOut.Select(p => (object)p.ToString(CultureInfo.InvariantCulture)).Take(minRowCount).ToArray());
            if (isWilliamsRChecked)
                fv.AddColumn("WilliamsR", williamsROut.Select(p => (object)p.ToString(CultureInfo.InvariantCulture)).Take(minRowCount).ToArray());
            if (isStochasticsChecked)
                fv.AddColumn("Stochastics", stochasticsOut.Select(p => (object)p.ToString(CultureInfo.InvariantCulture)).Take(minRowCount).ToArray());
            fv.AddColumn("label", closesOut.Select(p => (object)string.Format("{0:0.0}", p).ToString(CultureInfo.InvariantCulture)).Take(minRowCount).ToArray());

            var training = new FeatureVector();
            var test = new FeatureVector();
            int count = fv.Values[0].Length;

            for (int i = 0; i < fv.ColumnName.Count; i++)
            {
                training.AddColumn(fv.ColumnName[i], fv.Values[i].Take((int)(count * trainingSetPercentage)).ToArray());
            }

            for (int i = 0; i < fv.ColumnName.Count; i++)
            {
                test.AddColumn(fv.ColumnName[i], fv.Values[i].Skip((int)(count * trainingSetPercentage)).Take(count).ToArray()); // Take(count) means take the rest of all elements, number of the rest of the elements is smaller than count.
            }

            if (numFolds > 0)
            {
                BinaryClassificationEvaluator bce1 = new BinaryClassificationEvaluator();
                LinearRegression linearRegression = new LinearRegression();
                CrossValidator cvLinReg = new CrossValidator(linearRegression, bce1, numFolds);
                CrossValidatorModel cvLinRegModel = (CrossValidatorModel)cvLinReg.Fit(training);
                FeatureVector linRegPredictions = cvLinRegModel.transform(test);
                bce1.evaluate(linRegPredictions);
                linRegAcc = bce1.Accuracy;

                BinaryClassificationEvaluator bce2 = new BinaryClassificationEvaluator();
                LogisticRegression logisticRegression = new LogisticRegression();
                CrossValidator cvLogReg = new CrossValidator(logisticRegression, bce2, numFolds);
                CrossValidatorModel cvLogRegModel = (CrossValidatorModel)cvLogReg.Fit(training);
                FeatureVector logRegPredictions = cvLogRegModel.transform(test);
                bce2.evaluate(logRegPredictions);
                logRegAcc = bce2.Accuracy;

                BinaryClassificationEvaluator bce3 = new BinaryClassificationEvaluator();
                NaiveBayes naiveBayes = new NaiveBayes();
                CrossValidator cvNaiBay = new CrossValidator(naiveBayes, bce3, numFolds);
                CrossValidatorModel cvNaiBayModel = (CrossValidatorModel)cvNaiBay.Fit(training);
                FeatureVector naiBayPredictions = cvNaiBayModel.transform(test);
                bce3.evaluate(naiBayPredictions);
                naiBayAcc = bce3.Accuracy;
            }
            else
            {
                BinaryClassificationEvaluator bce1 = new BinaryClassificationEvaluator();
                LinearRegression linearRegression = new LinearRegression();
                LinearRegressionModel linearRegressionModel = (LinearRegressionModel)linearRegression.Fit(training);
                FeatureVector linRegPredictions = linearRegressionModel.transform(test);
                bce1.evaluate(linRegPredictions);
                linRegAcc = bce1.Accuracy;

                BinaryClassificationEvaluator bce2 = new BinaryClassificationEvaluator();
                LogisticRegression logicticRegression = new LogisticRegression();
                LogisticRegressionModel logisticRegressionModel = (LogisticRegressionModel)logicticRegression.Fit(training);
                FeatureVector logRegPredictions = logisticRegressionModel.transform(test);
                bce2.evaluate(logRegPredictions);
                logRegAcc = bce2.Accuracy;

                BinaryClassificationEvaluator bce3 = new BinaryClassificationEvaluator();
                NaiveBayes naiveBayes = new NaiveBayes();
                NaiveBayesModel naiveBayesModel = (NaiveBayesModel)naiveBayes.Fit(training);
                FeatureVector naiBayPredictions = naiveBayesModel.transform(test);
                bce3.evaluate(naiBayPredictions);
                naiBayAcc = bce3.Accuracy;
            }

            labelForLinRegAcc.Text = linRegAcc.ToString();
            labelForLogRegAcc.Text = logRegAcc.ToString();
            labelForNaiBayAcc.Text = naiBayAcc.ToString();

            panelForResults.BringToFront();
        }

        private void buttonForResultsPrev_Click(object sender, EventArgs e)
        {
            panelForDataSplitSettings.BringToFront();
        }
    }
}
