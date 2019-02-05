using System;
using System.Linq;
using System.Threading;

namespace FinancialForecastingDSS.ml.algorithms
{
    class CrossValidator : Estimator
    {
        private Estimator estimator { get; set; }
        private Evaluator evaluator { get; set; }
        private int numFolds { get; set; }

        public double Accuracy { get; set; }

        public CrossValidator(Estimator estimator, Evaluator evaluator, int numFolds)
        {
            this.estimator = estimator;
            this.evaluator = evaluator;
            this.numFolds = numFolds;
        }

        public Transformer Fit(FeatureVector featureVector)
        {
            var vector = ObjectCopier.Clone(featureVector);
            Shuffle(vector);
            FeatureVector[] folds = Partition(vector);
            Transformer[] transformers = GetTransformersAndAccuracy(folds);
            return new CrossValidatorModel(transformers);
        }

        /**
         * @see https://stackoverflow.com/a/1262619
        */
        private static class ThreadSafeRandom
        {
            [ThreadStatic]
            private static Random Local;

            public static Random ThisThreadsRandom
            {
                //get { return Local ?? (Local = new Random(0)); } // to get the same values everytime.
                get { return Local ?? (Local = new Random(unchecked(Environment.TickCount * 31 + Thread.CurrentThread.ManagedThreadId))); }
            }
        }

        private void Shuffle(FeatureVector featureVector)
        {
            int n = featureVector.Values[0].Length;
            while (n > 1)
            {
                n--;
                int k = ThreadSafeRandom.ThisThreadsRandom.Next(n + 1);
                FeatureVector.Swap(featureVector, k, n);
            }
        }

        private FeatureVector[] Partition(FeatureVector featureVector)
        {
            FeatureVector[] folds = new FeatureVector[numFolds];

            int foldLength = featureVector.Values[0].Length / numFolds;

            for (int i = 0; i < numFolds; i++)
            {
                folds[i] = new FeatureVector();

                for (int j = 0; j < featureVector.Values.Count; j++)
                {
                    object[] values = featureVector.Values[j].Skip(i * foldLength).Take(foldLength).ToArray();
                    folds[i].AddColumn(featureVector.ColumnName[j], values);
                }
            }
            return folds;
        }

        private Transformer[] GetTransformersAndAccuracy(FeatureVector[] folds)
        {
            Accuracy = 0;
            Transformer[] transformers = new Transformer[folds.Length];

            for (int i = 0; i < transformers.Length; i++)
            {
                FeatureVector trainingFeatures = MergeVectors(i, folds);
                FeatureVector testFeatures = folds[i];

                transformers[i] = estimator.Fit(trainingFeatures);
                evaluator.evaluate(transformers[i].transform(ObjectCopier.Clone(testFeatures)));
                Accuracy += evaluator.Accuracy;
            }

            Accuracy /= transformers.Length;
            return transformers;
        }

        private FeatureVector MergeVectors(int excludedFoldIndex, FeatureVector[] folds)
        {
            FeatureVector mergedVector = null;

            int i = 0;
            if (i == excludedFoldIndex)
            {
                i++;
                mergedVector = folds[i];

                for (; i < folds.Length; i++)
                {
                    for (int j = 0; j < folds[0].ColumnName.Count; j++)
                    {
                        mergedVector.Values[j].Concat(folds[i].Values[j]);
                    }
                }

                return mergedVector;
            }

            mergedVector = folds[i++];
            for (; i < folds.Length; i++)
            {
                if (i == excludedFoldIndex)
                    continue;

                for (int j = 0; j < folds[0].ColumnName.Count; j++)
                {
                    mergedVector.Values[j].Concat(folds[i].Values[j]);
                }
            }

            return mergedVector;
        }
    }
}
