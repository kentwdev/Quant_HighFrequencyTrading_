using System;
using System.Collections.Generic;
using System.Linq;
using Accord.MachineLearning;
using Accord.Math;
using MathNet.Numerics.Statistics;

namespace Strategies.VxvPricePredictionStrategy
{
    public class KNeighborsRegressor
    {
        private int _k;
        private double[][] _input;
        private int[] _output;
        private int _classes;

        private KNearestNeighbors _predictor;

        public KNeighborsRegressor(int k, int classes, double[][] input, int[] output)
        {
            //_k = (int) Math.Round(Math.Sqrt(classes));
            _input = input;
            _output = output;
            _classes = classes;

            _k = OptimizeK();

            _predictor = new KNearestNeighbors(_k, _classes, _input, _output);
        }

        private double RMSE(IEnumerable<double> actual, IEnumerable<double> forecast)
        {
            return Math.Sqrt(actual.Zip(forecast, (a, f) => Math.Pow(a - f, 2.0)).Sum() / 2d);
        }

        private int OptimizeK()
        {
            Tuple<KNearestNeighbors, double> bestModel = null;

            for (int k = 50; k <= 200; k++)
            {
                var crossvalidation = new CrossValidation<KNearestNeighbors>(_input.Length, 5);

                crossvalidation.Fitting = delegate(int fold, int[] indicesTrain, int[] indicesValidation)
                {
                    var trainingInputs = _input.Submatrix(indicesTrain);
                    var trainingOutputs = _output.Submatrix(indicesTrain);

                    var validationInputs = _input.Submatrix(indicesValidation);
                    var validationOutputs = _output.Submatrix(indicesValidation);

                    var predictor = new KNearestNeighbors(k, _classes, trainingInputs, trainingOutputs);

                    // Create a training algorithm and learn the training data

                    var trainingError = 0.0;

                    for (int i = 0; i < trainingInputs.Length; i++)
                    {
                        int[] nearest;
                        predictor.GetNearestNeighbors(trainingInputs[i], out nearest);

                        var prediction = InverseDistanceWeightedAverage(nearest);

                        if (prediction > 0 && trainingOutputs[i] > 0
                            || prediction < 0 && trainingOutputs[i] < 0
                            || prediction.Equals(trainingOutputs[i]))
                        {
                            continue;
                        }

                        trainingError++;
                    }

                    double validationError = 0.0;

                    for (int i = 0; i < validationInputs.Length; i++)
                    {
                        int[] nearest;
                        predictor.GetNearestNeighbors(validationInputs[i], out nearest);

                        var prediction = InverseDistanceWeightedAverage(nearest);

                        if (prediction > 0 && validationOutputs[i] > 0
                            || prediction < 0 && validationOutputs[i] < 0
                            || prediction.Equals(validationOutputs[i]))
                        {
                            continue;
                        }

                        validationError++;
                    }

                    trainingError /= trainingInputs.Length;
                    validationError /= validationInputs.Length;

                    return new CrossValidationValues<KNearestNeighbors>(predictor, trainingError, validationError);
                };

                var result = crossvalidation.Compute();

                //var minError = result.Models.Select(y => y.ValidationValue).Min();
                var minError = result.Models.Select(y => Math.Sqrt(Math.Pow(y.TrainingValue + y.ValidationValue, 2.0))).Min();

                if (bestModel == null || minError < bestModel.Item2)
                {
                    var bestFit = result.Models.FirstOrDefault(x => minError.Equals(x.ValidationValue))?.Model;
                    bestModel = bestFit == null ? bestModel : new Tuple<KNearestNeighbors, double>(bestFit, minError);
                }
            }

            return bestModel?.Item1.K ?? 80;
        }

        public double Predict(double[] query)
        {
            int[] nearest;
            _predictor.GetNearestNeighbors(query, out nearest);

            return InverseDistanceWeightedAverage(nearest);
        }

        private static double InverseDistanceWeightedAverage(int[] nearest)
        {
            var weightedValueSum = 0.0;
            var weightSum = 0.0;

            for (int i = 0; i < nearest.Length; i++)
            {
                weightedValueSum += nearest[i] * (nearest.Length - i);
                weightSum += i;
            }

            return weightedValueSum / weightSum;
        }
    }
}