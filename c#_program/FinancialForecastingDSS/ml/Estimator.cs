namespace FinancialForecastingDSS.ml
{
    interface Estimator
    {
        Transformer Fit(FeatureVector featureVector);
    }
}
