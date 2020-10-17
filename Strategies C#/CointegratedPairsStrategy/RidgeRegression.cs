using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Factorization;

namespace Strategies.CointegratedPairsStrategy
{
    public class RidgeRegression
    {
        public Matrix<double> X { get; }
        public double[] Y { get; }

        private Svd<double> X_svd;
        private double _l2Penalty;
        private double[] _coefficients;
        private double[] _standarderrors;

        private double[] _fitted;
        private double[] _residuals;

        public double[] StandardErrors => _standarderrors;
        public double[] Coefficients => _coefficients;

        public RidgeRegression(double[,] x, double[] y)
        {
            X = Matrix<double>.Build.DenseOfArray(x);
            Y = y;
            _fitted = new double[y.Length];
            _residuals = new double[y.Length];
        }

        public void UpdateCoefficients(double l2Penalty)
        {
            if (X_svd == null)
            {
                X_svd = X.Svd();
            }

            Matrix<double> V = X_svd.VT.Transpose();
            double[] s = X_svd.W.ToRowWiseArray();
            Matrix<double> U = X_svd.U;

            for (int i = 0; i < s.Length; i++)
            {
                s[i] = s[i] / (s[i] * s[i] + l2Penalty);
            }

            Matrix<double> S = Matrix<double>.Build.DenseOfDiagonalArray(s);
            Matrix<double> Z = V.Multiply(S).Multiply(U.Transpose());

            _coefficients = Z.Multiply(Vector<double>.Build.DenseOfArray(Y)).ToArray();

            _fitted = X.Multiply(Vector<double>.Build.DenseOfArray(_coefficients)).ToArray();
            double errorVariance = 0;
            for (int i = 0; i < _residuals.Length; i++)
            {
                _residuals[i] = Y[i] - _fitted[i];
                errorVariance += _residuals[i] * _residuals[i];
            }
            errorVariance = errorVariance / (X.RowCount - X.ColumnCount);

            Matrix<double> errorVarianceMatrix = Matrix<double>.Build.DenseIdentity(Y.Length).Multiply(errorVariance);
            Matrix<double> coefficientsCovarianceMatrix = Z.Multiply(errorVarianceMatrix).Multiply(Z.Transpose());
            _standarderrors = GetDiagonal(coefficientsCovarianceMatrix);
        }

        private double[] GetDiagonal(Matrix<double> x)
        {
            double[] diag = new double[x.ColumnCount];
            for (int i = 0; i < diag.Length; i++)
            {
                diag[i] = x[i, i];
            }
            return diag;
        }
    }
}