using System;
using System.Globalization;

namespace FinancialForecastingDSS.ml.algorithms
{
    class LinearRegressionModel : Transformer
    {

        private double[] weights;
        private double threshold;

        public LinearRegressionModel(double[] weights, double threshold)
        {
            this.weights = weights;
            this.threshold = threshold;
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
            double hypothesis = Hypothesis(weights, features);
            return hypothesis >= threshold ? 1.0 : 0.0;//it is important beacuse it convergences with the value of stock
        }

        public static double Hypothesis(double[] weights, double[] features)
        {
            double z = 0;
            for (int i = 0; i < weights.Length; i++)
            {
                z += weights[i] * features[i];
            }
            return z;
        }
    }
}
