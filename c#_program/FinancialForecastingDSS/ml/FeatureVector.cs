using System;
using System.Collections.Generic;

namespace FinancialForecastingDSS.ml
{
    [Serializable]
    class FeatureVector
    {
        public List<string> ColumnName { get; set; }
        public List<object[]> Values { get; set; }

        public FeatureVector()
        {
            ColumnName = new List<string>();
            Values = new List<object[]>();
        }

        public void AddColumn(string columnName, object[] values)
        {
            ColumnName.Add(columnName);
            Values.Add(values);
        }

        public static void Swap(FeatureVector featureVector, int i, int j)
        {
            for (int k = 0; k < featureVector.ColumnName.Count; k++)
            {
                object tmp = featureVector.Values[k][i];
                featureVector.Values[k][i] = featureVector.Values[k][j];
                featureVector.Values[k][j] = tmp;
            }
        }
    }
}
