using System;
using System.Globalization;

namespace FinancialForecastingDSS.ml.algorithms
{
    class LogisticRegressionModel : Transformer
    {
        private double[] weights;

        public LogisticRegressionModel(double[] weights)
        {
            this.weights = weights;
        }

        public FeatureVector transform(FeatureVector vectorToBePredicted)
        {
            object[] predictions = new object[vectorToBePredicted.Values[0].Length];

            for (int i = 0; i < vectorToBePredicted.Values[0].Length; i++)
            {
                double[] features = new double[weights.Length];
                features[0] = 1;
                for (int j = 1; j < weights.Length; j++)
                {
                    features[j] = Convert.ToDouble(vectorToBePredicted.Values[j - 1][i], CultureInfo.InvariantCulture);
                    predictions[i] = (object)string.Format("{0:0.0}", Classify(weights, features)).ToString(CultureInfo.InvariantCulture);
                }
            }

            vectorToBePredicted.AddColumn("prediction", predictions);
            return vectorToBePredicted;
        }

        private double Classify(double[] weights, double[] features)
        {
            double hypothesis = Logistic.Hypothesis(weights, features);
            return hypothesis >= .5 ? 1.0 : 0.0;
        }
    }
}
