namespace FinancialForecastingDSS.ml
{
    interface Evaluator
    {
        double Accuracy { get; set; }
        void evaluate(FeatureVector predictionVector);
    }
}
