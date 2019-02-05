using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;

namespace FinancialForecastingDSS.indicators
{
    class BollingerBands
    {
        public double[] Lower { get; set; }
        public double[] Upper { get; set; }
        private double[] Middle { get; set; }

        private int Period { get; set; }
        private int NumberOfData { get; set; }
        private double StandardDeviationFactor { get; set; }


        public BollingerBands(string code, DateTime targetDate, int period = 20, int numberOfData = 40, double standardDeviationFactor = 2.0)
        {
            if (period <= 0)
                throw new IndicatorException("Period must be positive.");
            if (standardDeviationFactor <= 0)
                throw new IndicatorException("Standard Deviation must be positive.");

            Period = period;
            NumberOfData = numberOfData;
            StandardDeviationFactor = standardDeviationFactor;

            List<BsonDocument> data = IndicatorService.GetData(code, targetDate, "Kapanis", NumberOfData + period - 1);
            if (data.Count < period)
                period = data.Count;

            CalculateBands(data);
        }

        private void CalculateBands(List<BsonDocument> data)
        {
            Middle = MovingAverage.calculateSMA(Period, NumberOfData, data);
            double[] std = CalculateStandardDeviation(data.Select(p => p.GetElement("Kapanis").Value.ToDouble()).ToArray());

            Lower = new double[NumberOfData];
            Upper = new double[NumberOfData];

            for (int i = 0; i < NumberOfData; i++)
            {
                Lower[i] = Middle[i] - StandardDeviationFactor * std[i];
                Upper[i] = Middle[i] + StandardDeviationFactor * std[i];
            }
        }

        private double[] CalculateStandardDeviation(double[] closes)
        {
            double[] std = new double[NumberOfData];
            for (int i = 0; i < NumberOfData; i++)
            {
                // can be optimized
                std[i] = MathService.StandardDeviation(closes.Skip(i).Take(Period), Middle[i]);
            }

            return std;
        }
    }
}
