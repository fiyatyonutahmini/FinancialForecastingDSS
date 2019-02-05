using System.IO;
using System.Linq;
using FinancialForecastingDSS.ml;

namespace FinancialForecastingDSS
{
    class LabeledPointSplittedExporter : Exporter
    {
        private int numberOfSplits { get; set; }
        private FeatureVector featureVector { get; set; }
        private int minRowCount { get; set; }
        private int splitSize { get; set; } // number of rows in each split
        private int i { get; set; }

        public LabeledPointSplittedExporter(FeatureVector featureVector, int numberOfSplits)
        {
            this.numberOfSplits = numberOfSplits;
            this.featureVector = featureVector;
            minRowCount = featureVector.Values.Min(p => p.Length); // if length of values are different, choose the smallest.
            splitSize = minRowCount / numberOfSplits;
            i = 1;
        }

        public void InitialWrite(string filePath, int numberOfRows)
        {
            StreamWriter writer = new StreamWriter(filePath);
            for (int k = 0; k < numberOfRows && i < minRowCount; i++, k++)
            {
                string row = "(";
                row += featureVector.Values[featureVector.Values.Count - 1][i] + ",[";
                int j;
                for (j = 0; j < featureVector.Values.Count - 2; j++)
                {
                    row += featureVector.Values[j][i] + ",";
                }
                row += featureVector.Values[j][i] + "])";
                writer.WriteLine(row);
            }
            writer.Close();
        }

        public void Export(string filePath)
        {
            StreamWriter writer = new StreamWriter(filePath);
            for (int k = 0; k < 1 && i < minRowCount; i++, k++)
            {
                string row = "(";
                row += featureVector.Values[featureVector.Values.Count - 1][i] + ",[";
                int j;
                for (j = 0; j < featureVector.Values.Count - 2; j++)
                {
                    row += featureVector.Values[j][i] + ",";
                }
                row += featureVector.Values[j][i] + "])";
                writer.WriteLine(row);
            }
            writer.Close();
        }
    }
}
