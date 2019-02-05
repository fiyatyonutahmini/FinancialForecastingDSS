using System.IO;
using System.Linq;
using FinancialForecastingDSS.ml;

namespace FinancialForecastingDSS
{
    class LabeledPointExporter : Exporter
    {
        private FeatureVector featureVector { get; set; }

        public LabeledPointExporter(FeatureVector featureVector)
        {
            this.featureVector = featureVector;
        }

        public void Export(string filePath)
        {
            int minRowCount = featureVector.Values.Min(p => p.Length); // if length of values are different, choose the smallest.

            StreamWriter writer = new StreamWriter(filePath);
            for (int i = 1; i < minRowCount; i++)
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
