using System.Collections.Generic;
using System.Linq;

namespace FinancialForecastingDSS.ml.algorithms
{
    class CrossValidatorModel : Transformer
    {
        private Transformer[] transformers;

        public CrossValidatorModel(Transformer[] transformers)
        {
            this.transformers = transformers;
        }

        public FeatureVector transform(FeatureVector featureVector)
        {
            FeatureVector[] predictionVectors = new FeatureVector[transformers.Length];
            for (int i = 0; i < transformers.Length; i++)
            {
                predictionVectors[i] = ObjectCopier.Clone(featureVector);
                predictionVectors[i] = transformers[i].transform(predictionVectors[i]);
            }

            // it's just to copy all the columns, not necessarily to be predictionsVectors[0]
            FeatureVector predictionVector = ObjectCopier.Clone(predictionVectors[0]);

            int predictionColumnIndex = predictionVectors[0].ColumnName.Count - 1;
            for (int i = 0; i < predictionVectors[0].Values[0].Length; i++)
            {
                // keeps how much times each prediction exist.
                Dictionary<object, int> predictionExistedNumber = new Dictionary<object, int>();

                for (int j = 0; j < predictionVectors.Length; j++)
                {
                    if (predictionExistedNumber.ContainsKey(predictionVectors[j].Values[predictionColumnIndex][i]))
                        predictionExistedNumber[predictionVectors[j].Values[predictionColumnIndex][i]]++;
                    else
                        predictionExistedNumber[predictionVectors[j].Values[predictionColumnIndex][i]] = 1;
                }

                predictionVector.Values[predictionColumnIndex][i] = predictionExistedNumber.OrderByDescending(p => p.Value).Select(p => p.Key).Take(1).ToArray()[0];
            }

            return predictionVector;
        }
    }
}
