using System;
using System.Collections.Generic;
using System.Linq;

namespace FinancialForecastingDSS.indicators
{
    class IndicatorDataPreprocessor
    {
        public static double[] GetClosesOut(int numberOfData, List<MongoDB.Bson.BsonDocument> data)
        {
            double[] closesOut = new double[numberOfData];
            for (int i = 1; i < numberOfData; i++)
            {
                if (data.ElementAt(i - 1).GetElement("Kapanis").Value.ToDouble() >= data.ElementAt(i).GetElement("Kapanis").Value.ToDouble())
                    closesOut[i] = 1;
                else
                    closesOut[i] = 0;
            }

            return closesOut;
        }

        public static double[] GetSMAOut(double[] sma)
        {
            double[] smaOut = new double[sma.Length];
            for (int i = 1; i < sma.Length; i++)
            {
                smaOut[i] = sma[i - 1] >= sma[i] ? 1 : 0;
            }

            return smaOut;
        }

        public static double[] GetWMAOut(double[] wma)
        {
            double[] wmaOut = new double[wma.Length];
            for (int i = 1; i < wma.Length; i++)
            {
                wmaOut[i] = wma[i - 1] >= wma[i] ? 1 : 0;
            }

            return wmaOut;
        }

        public static double[] GetEMAOut(double[] ema)
        {
            double[] emaOut = new double[ema.Length];
            for (int i = 1; i < ema.Length; i++)
            {
                emaOut[i] = ema[i - 1] >= ema[i] ? 1 : 0;
            }

            return emaOut;
        }

        public static double[] GetMACDOut(MovingAverageConvergenceDivergence macd)
        {
            double[] macdOut = new double[macd.TriggerLine.Length];
            for (int i = 0; i < macd.TriggerLine.Length; i++)
            {
                if (macd.MacdLine[i] < macd.TriggerLine[i] || macd.MacdLine[i] < 0)
                    macdOut[i] = 0;
                else
                    macdOut[i] = 1;
            }

            return macdOut;
        }

        public static double[] GetRSIOut(double[] rsi, bool discretized = true)
        {
            if (discretized)
            {
                double[] rsiOut = new double[rsi.Length - 1];
                for (int i = 0; i < rsi.Length - 1; i++)
                {
                    if (rsi[i] > 70)
                        rsiOut[i] = 0;
                    else if (rsi[i] < 30)
                        rsiOut[i] = 1;
                    else
                    {
                        if (rsi[i] > rsi[i + 1])
                            rsiOut[i] = 1;
                        else
                            rsiOut[i] = 0;
                    }
                }
                return rsiOut;
            }
            else
            {
                return MathService.Normalize(rsi);
            }
        }

        public static double[] GetWilliamsROut(double[] williamsR, bool discretized = true)
        {
            if (discretized)
            {
                double[] williamsOut = new double[williamsR.Length - 1];
                for (int i = 0; i < williamsR.Length - 1; i++)
                {
                    if (williamsR[i] >= -20)
                        williamsOut[i] = 0;
                    else if (williamsR[i] <= -80)
                        williamsOut[i] = 1;
                    else
                    {
                        if (williamsR[i] > williamsR[i + 1])
                            williamsOut[i] = 1;
                        else
                            williamsOut[i] = 0;
                    }
                }
                return williamsOut;
            }
            else
            {
                return williamsR.Select(e => e / -100.0).ToArray();
            }
        }

        public static double[] GetStochasticsOut(Stochastics stochastics, bool discretized = true)
        {
            if (discretized)
            {
                double[] stochasticsOut = new double[stochastics.SlowK.Length - 1];
                for (int i = 0; i < stochastics.SlowK.Length - 1; i++)
                {
                    if (stochastics.SlowK[i] >= 80)
                        stochasticsOut[i] = 0;
                    else if (stochastics.SlowK[i] <= 20)
                        stochasticsOut[i] = 1;
                    else
                    {
                        if (stochastics.SlowK[i] > stochastics.SlowK[i + 1])
                            stochasticsOut[i] = 1;
                        else
                            stochasticsOut[i] = 0;
                    }
                }
                return stochasticsOut;
            }
            else
            {
                return MathService.Normalize(stochastics.FastD);
            }
        }

        public static double[] GetBollingerBandsOut(BollingerBands bollinger)
        {
            throw new NotImplementedException();
        }
    }
}
