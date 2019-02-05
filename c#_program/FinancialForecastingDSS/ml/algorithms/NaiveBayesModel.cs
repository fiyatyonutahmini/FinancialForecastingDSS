using System;
using System.Collections.Generic;

namespace FinancialForecastingDSS.ml.algorithms
{
    class NaiveBayesModel : Transformer
    {
        private Dictionary<object, int> featureCounts;
        private Dictionary<object, int> labelCounts;
        private int numberOfTrainingData;

        public NaiveBayesModel(Dictionary<object, int> labelCounts, Dictionary<object, int> featureCounts, int numberOfTrainingData)
        {
            this.labelCounts = labelCounts;
            this.featureCounts = featureCounts;
            this.numberOfTrainingData = numberOfTrainingData;
        }

        public FeatureVector transform(FeatureVector vectorToBePredicted)
        {
            object[] predictions = new object[vectorToBePredicted.Values[0].Length];

            for (int i = 0; i < vectorToBePredicted.Values[0].Length; i++)
            {
                double mostProbableProbability = 0;
                object mostProbableKey = null;

                foreach (var label in labelCounts)
                {
                    double probability = 1;

                    for (int j = 0; j < vectorToBePredicted.Values.Count - 1; j++)
                    {
                        int featureIndex = j;
                        object feature = vectorToBePredicted.Values[j][i];
                        object key = Tuple.Create(featureIndex, feature, label.Key);

                        if (!featureCounts.ContainsKey(key))
                            featureCounts[key] = 1;
                        probability *= (double)featureCounts[key] / label.Value;
                    }

                    probability *= (double)label.Value / numberOfTrainingData;

                    if (probability > mostProbableProbability)
                    {
                        mostProbableProbability = probability;
                        mostProbableKey = label.Key;
                    }
                }

                predictions[i] = mostProbableKey;
            }

            vectorToBePredicted.AddColumn("prediction", predictions);
            return vectorToBePredicted;
        }
    }
}
