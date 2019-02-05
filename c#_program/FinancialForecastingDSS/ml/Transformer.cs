namespace FinancialForecastingDSS.ml
{
    interface Transformer
    {
        FeatureVector transform(FeatureVector featureVector);
    }
}
