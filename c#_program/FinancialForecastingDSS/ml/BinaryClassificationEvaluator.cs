namespace FinancialForecastingDSS.ml
{
    class BinaryClassificationEvaluator : Evaluator
    {
        public double Accuracy { get; set; }
        public ConfusionMatrix confusionMatrix { get; set; }

        public BinaryClassificationEvaluator()
        {
            confusionMatrix = new ConfusionMatrix();
            confusionMatrix.TP = 0;
            confusionMatrix.TN = 0;
            confusionMatrix.FP = 0;
            confusionMatrix.FN = 0;
        }

        public void evaluate(FeatureVector predictionVector)
        {
            confusionMatrix.TP = 0;
            confusionMatrix.TN = 0;
            confusionMatrix.FP = 0;
            confusionMatrix.FN = 0;

            object label, prediction;

            for (int i = 0; i < predictionVector.Values[0].Length; i++)
            {
                label = predictionVector.Values[predictionVector.Values.Count - 2][i];
                prediction = predictionVector.Values[predictionVector.Values.Count - 1][i];
                if (label.Equals(prediction))
                {
                    Accuracy += 1;
                    if (label.ToString() == "1.0")
                        confusionMatrix.TP++;
                    else
                        confusionMatrix.TN++;
                }
                else
                {
                    if (label.ToString() == "1.0")
                        confusionMatrix.FN++;
                    else
                        confusionMatrix.FP++;
                }
            }

            Accuracy /= predictionVector.Values[0].Length;
        }
    }
}
