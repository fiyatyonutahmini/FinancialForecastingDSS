package financialForecastWithSparkStreaming;

import java.io.Serializable;

public class LabelPrediction implements Serializable {
	private static final long serialVersionUID = -5341498835427500903L;
	private Integer label, prediction;
	private String isItAccurate;

	public Integer getLabel() {
		return label;
	}

	public void setLabel(Integer label) {
		this.label = label;
	}

	public Integer getPrediction() {
		return prediction;
	}

	public void setPrediction(Integer prediction) {
		this.prediction = prediction;
	}

	public String getIsItAccurate() {
		return isItAccurate;
	}

	public void setIsItAccurate(String isItAccurate) {
		this.isItAccurate = isItAccurate;
	}

	public static long getSerialversionuid() {
		return serialVersionUID;
	}

	public LabelPrediction(int label, int prediction) {
		this.label = label;
		this.prediction = prediction;
		this.isItAccurate = label == prediction ? "T" : "F";
	}
	
	@Override
	public String toString() {
		return label + "\t" + prediction + "\t" + isItAccurate;
	}

}
