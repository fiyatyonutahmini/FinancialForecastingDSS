using System;
using System.Collections.Generic;
using System.Linq;

namespace FinancialForecastingDSS
{
    class MathService
    {
        public static double StandardDeviation(IEnumerable<double> values, double avg)
        {
            return Math.Sqrt(values.Average(v => Math.Pow(v - avg, 2)));
        }

        public static double[] Normalize(double[] values)
        {
            double min = values.Min();
            double max = values.Max();
            double range = max - min;
            return values.Select(p => (p - min) / range).ToArray();
        }

        public static IEnumerable<double> Normalize(IEnumerable<double> values)
        {
            double min = values.Min();
            double max = values.Max();
            double range = max - min;
            return values.Select(p => (p - min) / range);
        }
    }
}
