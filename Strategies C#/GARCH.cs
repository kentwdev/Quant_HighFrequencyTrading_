using System;
using System.Collections.Generic;
using Accord.Math.Convergence;
using Accord.Math.Optimization;

namespace Strategies
{
    public class GARCH
    {
        private double[] _values;
        private int _windowIndexStart;
        private int _windowIndexEnd;
        private int _maxIterations = 1000000;

        public GARCH(double[] values)
        {
            _values = values;
            _windowIndexStart = 0;
            _windowIndexEnd = values.Length - 1;
        }

        public double GetLogLikelihoodForParameters(double omega, double alpha, double beta)
        {
            var logLikelihood = 0.0;

            var volScaling = 1.0;
            var h = omega/(1.0 - alpha - beta);
            for (var i = _windowIndexStart + 1; i <= _windowIndexEnd - 1; i++)
            {
                var eval = volScaling*Math.Log(_values[i]/_values[i - 1]);
                h = omega + alpha*eval*eval + beta*h;
                var evalNext = volScaling*Math.Log(_values[i + 1]/_values[i]);

                logLikelihood += -Math.Log(h) - evalNext*evalNext/h;
            }
            logLikelihood += -Math.Log(2*Math.PI)*(_windowIndexEnd - _windowIndexStart);
            logLikelihood *= 0.5;

            return logLikelihood;
        }

        public double GetLastResidualForParameters(double omega, double alpha, double beta)
        {
            var volScaling = 1.0;
            var h = omega/(1.0 - alpha - beta);
            for (var i = _windowIndexStart + 1; i <= _windowIndexEnd; i++)
            {
                var eval = volScaling*Math.Log(_values[i]/_values[i - 1]);
                h = omega + alpha*eval*eval + beta*h;
            }

            return h;
        }

        public double[] GetSzenarios(double omega, double alpha, double beta)
        {
            var szenarios = new double[_windowIndexEnd - _windowIndexStart + 1 - 1];

            var volScaling = 1.0;
            var h = omega/(1.0 - alpha - beta);
            var vol = Math.Sqrt(h)*volScaling;
            for (var i = _windowIndexStart + 1; i <= _windowIndexEnd; i++)
            {
                szenarios[i - _windowIndexStart - 1] = Math.Log(_values[i]/_values[i - 1])/vol;

                var eval = volScaling*Math.Log(_values[i]/_values[i - 1]);
                h = omega + alpha*eval*eval + beta*h;
                vol = Math.Sqrt(h)*volScaling;
            }

            Array.Sort(szenarios);
            return szenarios;
        }

        public double[] GetQuantilPredictionsForParameters(double omega, double alpha, double beta, double[] quantiles)
        {
            var szenarios = GetSzenarios(omega, alpha, beta);

            var volScaling = 1.0;
            var h = omega/(1.0 - alpha - beta);
            var vol = Math.Sqrt(h)*volScaling;

            var quantileValues = new double[quantiles.Length];
            for (var i = 0; i < quantiles.Length; i++)
            {
                var quantile = quantiles[i];
                var quantileIndex = szenarios.Length*quantile - 1;
                var quantileIndexLo = (int) quantileIndex;
                var quantileIndexHi = quantileIndexLo + 1;

                var szenarioRelativeChange =
                    (quantileIndexHi - quantileIndex)*Math.Exp(szenarios[Math.Max(quantileIndexLo, 0)]*vol)
                    +
                    (quantileIndex - quantileIndexLo)*
                    Math.Exp(szenarios[Math.Min(quantileIndexHi, szenarios.Length)]*vol);

                var quantileValue = _values[_windowIndexEnd]*szenarioRelativeChange;
                quantileValues[i] = quantileValue;
            }

            return quantileValues;
        }

        public Dictionary<string, object> GetBestParameters(Dictionary<string, object> guess)
        {
            // Create a guess for the solver
            var guessOmega = 1.0;
            var guessAlpha = 0.2;
            var guessBeta = 0.2;
            if (guess != null)
            {
                guessOmega = (double) guess["Omega"];
                guessAlpha = (double) guess["Alpha"];
                guessBeta = (double) guess["Beta"];
            }

            // Constrain guess to admissible range
            guessOmega = RestrictToOpenSet(guessOmega, 0.0, double.MaxValue);
            guessAlpha = RestrictToOpenSet(guessAlpha, 0.0, 1.0);
            guessBeta = RestrictToOpenSet(guessBeta, 0.0, 1.0 - guessAlpha);

            var guessMucorr = guessAlpha + guessBeta;
            var guessMuema = guessBeta/(guessAlpha + guessBeta);

            // Transform guess to solver coordinates
            var guessParameters = new double[3];
            guessParameters[0] = Math.Log(guessOmega);
            guessParameters[1] = -Math.Log(-Math.Log(guessMucorr));
            guessParameters[2] = -Math.Log(-Math.Log(guessMuema));

            var f = new GARCHMaxLikelihoodFunction(this);

            var nm = new NelderMead(3);

            nm.Function = solution => f.Value(solution);
            nm.Convergence.MaximumEvaluations = _maxIterations;

            nm.Maximize(guessParameters);
            var bestParameters = nm.Solution;

            // Transform parameters to GARCH parameters
            var omega = Math.Exp(bestParameters[0]);
            var mucorr = Math.Exp(-Math.Exp(-bestParameters[1]));
            var muema = Math.Exp(-Math.Exp(-bestParameters[2]));
            var beta = mucorr*muema;
            var alpha = mucorr - beta;

            double[] quantiles = {0.01, 0.05, 0.5};
            var quantileValues = GetQuantilPredictionsForParameters(omega, alpha, beta, quantiles);

            var results = new Dictionary<string, object>();
            results.Add("Omega", omega);
            results.Add("Alpha", alpha);
            results.Add("Beta", beta);
            results.Add("Szenarios", GetSzenarios(omega, alpha, beta));
            //results.Add("Likelihood", GetLogLikelihoodForParameters(omega, alpha, beta));
            results.Add("Vol", Math.Sqrt(GetLastResidualForParameters(omega, alpha, beta)));
            results.Add("Quantile=1%", quantileValues[0]);
            results.Add("Quantile=5%", quantileValues[1]);
            results.Add("Quantile=50%", quantileValues[2]);

            return results;
        }

        private static double RestrictToOpenSet(double value, double lowerBond, double upperBound)
        {
            value = Math.Max(value, lowerBond*(1.0 + Math.Sign(lowerBond)*1E-15) + 1E-15);
            value = Math.Min(value, upperBound*(1.0 - Math.Sign(upperBound)*1E-15) - 1E-15);
            return value;
        }
    }

    class GARCHMaxLikelihoodFunction
    {
        private GARCH _model;

        public GARCHMaxLikelihoodFunction(GARCH model)
        {
            _model = model;
        }

        public double Value(double[] variables)
        {
            /*
             * Transform variables: The solver variables are in (-\infty, \infty).
             * We transform the variable to the admissible domain for GARCH, that is
             * omega > 0, 0 < alpha < 1, 0 < beta < (1-alpha), displacement > lowerBoundDisplacement
             */
            var omega = Math.Exp(variables[0]);
            var mucorr = Math.Exp(-Math.Exp(-variables[1]));
            var muema = Math.Exp(-Math.Exp(-variables[2]));
            var beta = mucorr*muema;
            var alpha = mucorr - beta;

            var logLikelihood = _model.GetLogLikelihoodForParameters(omega, alpha, beta);

            // Penalty to prevent solver from hitting the bounds
            logLikelihood -= Math.Max(1E-30 - omega, 0)/1E-30;
            logLikelihood -= Math.Max(1E-30 - alpha, 0)/1E-30;
            logLikelihood -= Math.Max(alpha - 1 + 1E-30, 0)/1E-30;
            logLikelihood -= Math.Max(1E-30 - beta, 0)/1E-30;
            logLikelihood -= Math.Max(beta - 1 + 1E-30, 0)/1E-30;

            return logLikelihood;
        }
    }
}