using System;

namespace FinancialForecastingDSS.ml.algorithms
{
    class Logistic
    {
        private static double Sigmoid(double z)
        {
            return (1.0 / (1 + Math.Exp(-z)));
        }

        public static double Hypothesis(double[] weights, double[] features)
        {
            double z = 0;
            for (int i = 0; i < weights.Length; i++)
            {
                z += weights[i] * features[i];
            }
            return Sigmoid(z);
        }
    }
}
