package financialForecastWithSparkStreaming;

import java.util.Arrays;
import java.util.Scanner;

import org.apache.spark.api.java.JavaSparkContext;
import org.apache.spark.api.java.function.Function2;
import org.apache.spark.mllib.classification.StreamingLogisticRegressionWithSGD;
import org.apache.spark.mllib.linalg.Vector;
import org.apache.spark.mllib.linalg.Vectors;
import org.apache.spark.mllib.regression.LabeledPoint;
import org.apache.spark.mllib.regression.StreamingLinearRegressionWithSGD;
import org.apache.spark.streaming.Durations;
import org.apache.spark.streaming.api.java.JavaDStream;
import org.apache.spark.streaming.api.java.JavaPairDStream;
import org.apache.spark.streaming.api.java.JavaStreamingContext;

import scala.Tuple2;

public class App {
	private final static String homePath = System.getProperty("user.home");
	private final static String trainingDir = homePath + "\\training";
	private final static String testDir = homePath + "\\test";
	public static volatile String fileNamePrefix = "";
	private static int numOfFeatures;

	public static void main(String[] args) throws InterruptedException {
		JavaSparkContext javaSparkContext = new JavaSparkContext("local[4]", "StreamingLogisticRegressionPrediction");
		javaSparkContext.setLogLevel("ERROR");
		JavaStreamingContext javaStreamingContext = new JavaStreamingContext(javaSparkContext, Durations.seconds(5));

		JavaDStream<LabeledPoint> trainingData = javaStreamingContext.textFileStream(trainingDir).map(line -> {
			return parse(line);
		});
		JavaDStream<LabeledPoint> testData = javaStreamingContext.textFileStream(testDir).map(line -> {
			return parse(line);
		});

		Scanner input = new Scanner(System.in);
		System.out.print("How many features exist in feature vector: ");
		numOfFeatures = Integer.parseInt(input.nextLine());
		
		double[] initialWeights = new double[numOfFeatures];
		Arrays.fill(initialWeights, 0.0);
		
		System.out.print("1 for Logistic Regression, 2 for Linear Regression: ");
		int selection = Integer.parseInt(input.nextLine());
		if (selection == 1) {
			StreamingLogisticRegressionWithSGD model = new StreamingLogisticRegressionWithSGD()
					.setInitialWeights(Vectors.dense(initialWeights));

			model.trainOn(trainingData);
			JavaPairDStream<Double, Double> predictions = model
					.predictOnValues(testData.mapToPair(lp -> new Tuple2<>(lp.label(), lp.features())));

			JavaDStream<Integer6> evaluationResults = predictions.map(e -> {
				if (e._1.equals(e._2))
					if (e._1 == 1.0)
						return new Integer6(1, 1, 1, 0, 0, 0);
					else
						return new Integer6(1, 1, 0, 1, 0, 0);
				else if (e._2 == 1.0)
					return new Integer6(0, 1, 0, 0, 1, 0);
				else
					return new Integer6(0, 1, 0, 0, 0, 1);
			}).reduce(new Function2<Integer6, Integer6, Integer6>() {

				@Override
				public Integer6 call(Integer6 v1, Integer6 v2) throws Exception {
					Integer6 ret = new Integer6(v1.getAccurates() + v2.getAccurates(), v1.getTotal() + v2.getTotal(),
							v1.getTP() + v2.getTP(), v1.getTN() + v2.getTN(), v1.getFP() + v2.getFP(),
							v1.getFN() + v2.getFN());
					return ret;
				}
			});

			JavaDStream<LabelPrediction> labelPrediction = predictions.map(e -> {
				return new LabelPrediction(e._1.intValue(), e._2.intValue());
			});

			evaluationResults.dstream().saveAsTextFiles(homePath + "\\spark_output\\" + App.fileNamePrefix + "evaluation",
					"e");
			labelPrediction.dstream().saveAsTextFiles(homePath + "\\spark_output\\" + App.fileNamePrefix + "predictions",
					"lp");
			labelPrediction.print();
			App.fileNamePrefix = "";
		} else {
			StreamingLinearRegressionWithSGD model = new StreamingLinearRegressionWithSGD()
					.setInitialWeights(Vectors.dense(initialWeights));

			model.trainOn(trainingData);
			JavaPairDStream<Double, Double> predictions = model
					.predictOnValues(testData.mapToPair(lp -> new Tuple2<>(lp.label(), lp.features())));

			JavaDStream<Integer6> evaluationResults = predictions.map(e -> {
				if (e._1.equals(e._2))
					if (e._1 == 1.0)
						return new Integer6(1, 1, 1, 0, 0, 0);
					else
						return new Integer6(1, 1, 0, 1, 0, 0);
				else if (e._2 == 1.0)
					return new Integer6(0, 1, 0, 0, 1, 0);
				else
					return new Integer6(0, 1, 0, 0, 0, 1);
			}).reduce(new Function2<Integer6, Integer6, Integer6>() {

				@Override
				public Integer6 call(Integer6 v1, Integer6 v2) throws Exception {
					Integer6 ret = new Integer6(v1.getAccurates() + v2.getAccurates(), v1.getTotal() + v2.getTotal(),
							v1.getTP() + v2.getTP(), v1.getTN() + v2.getTN(), v1.getFP() + v2.getFP(),
							v1.getFN() + v2.getFN());
					return ret;
				}
			});

			JavaDStream<LabelPrediction> labelPrediction = predictions.map(e -> {
				if (e._2 > .5)
					return new LabelPrediction(e._1.intValue(), 1);
				else
					return new LabelPrediction(e._1.intValue(), 0);
			});

			evaluationResults.dstream().saveAsTextFiles(homePath + "\\spark_output\\" + App.fileNamePrefix + "evaluation",
					"e");
			labelPrediction.dstream().saveAsTextFiles(homePath + "\\spark_output\\" + App.fileNamePrefix + "predictions",
					"lp");
			labelPrediction.print();
			App.fileNamePrefix = "";
		}
		
		javaStreamingContext.start();
		javaStreamingContext.awaitTermination();
	}

	private static LabeledPoint parse(String line) {
		line = line.substring(1, line.length() - 2);
		int firstComma = line.indexOf(",");
		double label = Double.parseDouble(line.substring(0, firstComma));
		String[] featuresStr = line.substring(firstComma + 2).split(",");
		double[] featuresDouble = new double[featuresStr.length];
		for (int i = 0; i < featuresDouble.length; i++) {
			featuresDouble[i] = Double.parseDouble(featuresStr[i]);
		}
		Vector features = Vectors.dense(featuresDouble);
		return new LabeledPoint(label, features);
	}
}
