using System;
using Accord.Math;
using MathNet.Numerics.LinearAlgebra;

namespace Strategies.CointegratedPairsStrategy
{
    public class AugmentedDickeyFuller
    {
        private double[] _ts;
        private int _lag;
        private bool _needsDiff = true;
        private double[] _zeroPaddedDiff;

        private const double PVALUE_THRESHOLD = -3.45;

        public bool NeedsDiff => _needsDiff;

        public double[] ZeroPaddedDiff => _zeroPaddedDiff;

        public AugmentedDickeyFuller(double[] ts) : this(ts, (int) Math.Floor(Math.Pow(ts.Length - 1, 1.0 / 3.0)))
        {
        }

        public AugmentedDickeyFuller(double[] ts, int lag)
        {
            _ts = ts;
            _lag = lag;
            ComputeAdfStatistics();
        }

        private void ComputeAdfStatistics()
        {
            double[] y = Diff(_ts);
            Matrix<double> designMatrix = null;
            int k = _lag + 1;
            int n = _ts.Length - 1;

            Matrix<double> z = Matrix<double>.Build.DenseOfArray(LaggedMatrix(y, k));
            Vector<double> zcol1 = z.Column(0); //has length length(ts) - 1 - k + 1
            double[] xt1 = SubsetArray(_ts, k - 1, n - 1);  //ts[k:(length(ts) - 1)], has length length(ts) - 1 - k + 1
            double[] trend = Sequence(k, n); //trend k:n, has length length(ts) - 1 - k + 1
            if (k > 1)
            {
                Matrix<double> yt1 = z.SubMatrix(0, _ts.Length - 1 - k, 1, k - 1); //same as z but skips first column
                //build design matrix as cbind(xt1, 1, trend, yt1)
                designMatrix = Matrix<double>.Build.Dense(_ts.Length - 1 - k + 1, 3 + k - 1);
                designMatrix.SetColumn(0, xt1);
                designMatrix.SetColumn(1, Ones(_ts.Length - 1 - k + 1));
                designMatrix.SetColumn(2, trend);
                designMatrix.SetSubMatrix(0, 3, yt1);
            }
            else
            {
                //build design matrix as cbind(xt1, 1, tt)
                designMatrix = Matrix<double>.Build.Dense(_ts.Length - 1 - k + 1, 3);
                designMatrix.SetColumn(0, xt1);
                designMatrix.SetColumn(1, Ones(_ts.Length - 1 - k + 1));
                designMatrix.SetColumn(2, trend);
            }
            /*OLSMultipleLinearRegression regression = new OLSMultipleLinearRegression();
            regression.setNoIntercept(true);
            regression.newSampleData(zcol1.toArray(), designMatrix.getData());
            double[] beta = regression.estimateRegressionParameters();
            double[] sd = regression.estimateRegressionParametersStandardErrors();
            */
            RidgeRegression regression = new RidgeRegression(designMatrix.ToArray(), zcol1.ToArray());
            regression.UpdateCoefficients(0.0001);
            double[] beta = regression.Coefficients;
            double[] sd = regression.StandardErrors;

            double t = beta[0] / sd[0];
            _needsDiff = t <= PVALUE_THRESHOLD;
        }

        private double[] Diff(double[] x)
        {
            double[] diff = new double[x.Length - 1];
            double[] zeroPaddedDiff = new double[x.Length];
            zeroPaddedDiff[0] = 0;
            for (int i = 0; i < diff.Length; i++)
            {
                double diff_i = x[i + 1] - x[i];
                diff[i] = diff_i;
                zeroPaddedDiff[i + 1] = diff_i;
            }
            _zeroPaddedDiff = zeroPaddedDiff;
            return diff;
        }

        private static double[] Ones(int n)
        {
            double[] ones = new double[n];
            for (int i = 0; i < n; i++)
            {
                ones[i] = 1;
            }
            return ones;
        }

        private static double[,] LaggedMatrix(double[] x, int lag)
        {
            double[,] laggedMatrix = new double[x.Length - lag + 1, lag];
            for (int j = 0; j < lag; j++)
            {
                for (int i = 0; i < laggedMatrix.Rows(); i++)
                {
                    laggedMatrix[i, j] = x[lag - j - 1 + i];
                }
            }
            return laggedMatrix;
        }

        private static double[] SubsetArray(double[] x, int start, int end)
        {
            double[] subset = new double[end - start + 1];
            Array.Copy(x, start, subset, 0, end - start + 1);
            return subset;
        }

        private double[] Sequence(int start, int end)
        {
            double[] sequence = new double[end - start + 1];
            for (int i = start; i <= end; i++)
            {
                sequence[i - start] = i;
            }
            return sequence;
        }
    }
}