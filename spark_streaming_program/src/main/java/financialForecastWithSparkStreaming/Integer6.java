package financialForecastWithSparkStreaming;

import java.io.File;
import java.io.IOException;
import java.io.Serializable;
import java.nio.file.Files;
import java.nio.file.Paths;
import java.nio.file.StandardOpenOption;

public class Integer6 implements Serializable {
	private static final long serialVersionUID = 8369642305165414298L;
	private Integer accuratePredictions;
	private Integer totalPredictions;
	private Integer tp;
	private Integer tn;
	private Integer fp;
	private Integer fn;

	public Integer6(Integer accuratePredictions, Integer totalPredictions, Integer tp, Integer tn, Integer fp,
			Integer fn) {
		this.accuratePredictions = accuratePredictions;
		this.totalPredictions = totalPredictions;
		this.tp = tp;
		this.tn = tn;
		this.fp = fp;
		this.fn = fn;
	}

	public Integer getAccurates() {
		return accuratePredictions;
	}

	public Integer getTotal() {
		return totalPredictions;
	}

	public Integer getTP() {
		return tp;
	}

	public Integer getTN() {
		return tn;
	}

	public Integer getFP() {
		return fp;
	}

	public Integer getFN() {
		return fn;
	}

	@Override
	public String toString() {
		double accuracy = accuratePredictions / (double) totalPredictions;
		double precision = tp / ((double) (tp + fp));
		double recall = tp / ((double) (tp + fn));
		double f1Score = 2 * precision * recall / (precision + recall);
		String ret = accuracy + "\t" + precision + "\t" + recall + "\t" + f1Score;
		String filePath = System.getProperty("user.home") + "\\spark_evaluation.txt";
		try {
			File file = new File(filePath);
			if (!file.exists())
				file.createNewFile();
			Files.write(Paths.get(filePath), (ret + "\r\n").getBytes(), StandardOpenOption.APPEND);
		} catch (IOException e) {
		}
		return ret;
	}
}