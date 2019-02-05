using System.IO;
using System.Linq;
using FinancialForecastingDSS.ml;

namespace FinancialForecastingDSS
{
    class CSVExporter : Exporter
    {
        private FeatureVector Vector { get; set; }

        public CSVExporter(FeatureVector featureVector)
        {
            Vector = featureVector;
        }

        public void Export(string filePath)
        {
            int minRowCount = Vector.Values.Min(p => p.Length); // if length of values are different, choose the smallest.

            StreamWriter writer = new StreamWriter(filePath);
            writer.WriteLine(string.Join(",", Vector.ColumnName)); // writes the headerline.
            for (int i = 1; i < minRowCount; i++) // last days label is unknown, so index is started from 1.
            {
                int j = 0;
                for (; j < Vector.Values.Count - 1; j++)
                    writer.Write(Vector.Values[j][i] + ",");

                writer.WriteLine(Vector.Values[j][i]);
            }
            writer.Close();
        }
    }
}
