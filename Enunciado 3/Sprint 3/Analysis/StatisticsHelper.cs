using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.Statistics;
using MathNet.Numerics.Distributions;

namespace Lab03S03.Analysis
{
    public static class StatisticsHelper
    {
        public static double Median(IEnumerable<double> values)
        {
            var arr = values.ToArray();
            if (arr.Length == 0) return 0;
            return Statistics.Median(arr);
        }

        public static (double rho, double pValue) Spearman(double[] x, double[] y)
        {
            if (x.Length != y.Length || x.Length < 3)
                return (0, 1);

            double rho = Correlation.Spearman(x, y);

            int n = x.Length;
            // t = r * sqrt((n-2)/(1-r^2))
            if (Math.Abs(rho) == 1.0)
                return (rho, 0.0);

            double t = rho * Math.Sqrt((n - 2) / (1 - rho * rho));
            var dist = new StudentT(0, 1, n - 2);
            double pValue = 2.0 * (1.0 - dist.CumulativeDistribution(Math.Abs(t)));

            return (rho, pValue);
        }

        public static string InterpretRho(double rho)
        {
            double absRho = Math.Abs(rho);
            string direction = rho < 0 ? "negativa" : "positiva";

            if (absRho >= 0.7) return $"forte {direction}";
            if (absRho >= 0.4) return $"moderada {direction}";
            if (absRho >= 0.1) return $"fraca {direction}";
            return "sem correlação";
        }
    }
}