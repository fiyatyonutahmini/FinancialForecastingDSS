using System;
using System.Globalization;

namespace FinancialForecastingDSS.ml.algorithms
{
    class LinearRegression : Estimator
    {
        private double learningRate;
        private int maxIterations;
        private double[] weights;

        public LinearRegression(double learningRate = .1, int maxIterations = 500)
        {
            this.learningRate = learningRate;
            this.maxIterations = maxIterations;
        }

        public Transformer Fit(FeatureVector featureVector)
        {
            weights = new double[featureVector.ColumnName.Count];
            for (int i = 0; i < weights.Length; i++)
            {
                weights[i] = 1; // not necessarily to be one, just to give an initial value.
            }

            double[][] features = new double[featureVector.Values[0].Length][];
            for (int i = 0; i < features.Length; i++)
            {
                features[i] = new double[weights.Length];
                features[i][0] = 1;
                for (int j = 1; j < weights.Length; j++)
                {
                    features[i][j] = Convert.ToDouble(featureVector.Values[j - 1][i], CultureInfo.InvariantCulture);
                }
            }
            for (int i = 0; i < maxIterations; i++)
            {
                UpdateWeights(featureVector, features);
            }
            double threshold = SetThreshold(features);

            return new LinearRegressionModel(weights, threshold);
        }

        private double SetThreshold(double[][] features)
        {
            double threshold = 0;
            for (int i = 0; i < weights.Length; i++)
            {
                double prediction = Hypothesis(weights, features[i]);
                threshold += prediction;
            }

            return threshold / weights.Length;
        }

        private void UpdateWeights(FeatureVector featureVector, double[][] features)
        {
            int m = features.Length;
            int n = weights.Length;
            double[] predMinusLab = new double[m]; // prediction minus label
            for (int i = 0; i < m; i++)
            {
                double prediction = Hypothesis(weights, features[i]);
                predMinusLab[i] = prediction - Convert.ToDouble(featureVector.Values[featureVector.Values.Count - 1][i], CultureInfo.InvariantCulture);
            }

            double[] featuresXPredMinusLabel = new double[n]; // multiplication of transpose of "feature" and "prediction minus label" vectors.
            for (int i = 0; i < n; i++)
            {
                double product = 0;
                for (int j = 0; j < m; j++)
                {
                    product += features[j][i] * predMinusLab[j];
                }
                featuresXPredMinusLabel[i] = product;
            }

            double coeff = learningRate / m;
            for (int i = 0; i < n; i++)
            {
                weights[i] = weights[i] - coeff * featuresXPredMinusLabel[i];
            }
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
