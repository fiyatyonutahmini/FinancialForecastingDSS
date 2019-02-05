using System;
using System.Collections.Generic;

namespace FinancialForecastingDSS.ml.algorithms
{
    class NaiveBayes : Estimator
    {
        Dictionary<object, int> labelCounts;
        Dictionary<object, int> featureCounts;

        public Transformer Fit(FeatureVector featureVector)
        {
            labelCounts = new Dictionary<object, int>();
            featureCounts = new Dictionary<object, int>();

            for (int i = 0; i < featureVector.Values[0].Length; i++)
            {
                if (labelCounts.ContainsKey(featureVector.Values[featureVector.Values.Count - 1][i]))
                    labelCounts[featureVector.Values[featureVector.Values.Count - 1][i]]++;
                else
                    labelCounts[featureVector.Values[featureVector.Values.Count - 1][i]] = 2;

                for (int j = 0; j < featureVector.Values.Count - 1; j++)
                {
                    int featureIndex = j;
                    object feature = featureVector.Values[j][i];
                    object label = featureVector.Values[featureVector.Values.Count - 1][i];
                    object key = Tuple.Create(featureIndex, feature, label);
                    if (featureCounts.ContainsKey(key))
                        featureCounts[key]++;
                    else
                        featureCounts[key] = 2;
                }
            }

            return new NaiveBayesModel(labelCounts, featureCounts, featureVector.Values[0].Length);
        }
    }
}
