# Financial Forecasting Decision Support System

## Description

This repository contains a decision support system that runs on streaming stock market data. The system aims to predict the price movement direction. In order to achive this, it uses Spark Streaming's Linear Regression and Logistic Regression algorithms and the technical indicators which are implemented in [yasinuygun/TechnicalIndicators](https://github.com/yasinuygun/TechnicalIndicators) as features.

## Repository Contents and Starting the Application

`c#_program` is the decision support system that is coded in C#. It uses the `spark_streaming_program` that is coded in Java. In order to start the application, first run the Java program and enter the number of features and machine learning algorithm you want to use. Then run the C# program, and set the features you want to use. Once you started both of the programs, the system will start to run.

## Members

- [Yasin Uygun](https://github.com/yasinuygun)
- [Ramazan Faruk Oğuz](https://github.com/farukoguz)
