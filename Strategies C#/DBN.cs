using System;
using System.Linq;
using Accord.Math;
using Accord.Neuro;
using Accord.Neuro.Learning;
using QuantConnect;
using QuantConnect.Algorithm;

namespace Strategies
{
    class DBN : QCAlgorithm
    {
        public override void Initialize()
        {
            var history = History("SPY", TimeSpan.FromDays(1000), Resolution.Daily);

            var highestClose = history.Max(h => h.Close);
            var lowestClose = history.Min(h => h.Close);
            var highestVolume = history.Max(h => h.Volume);
            var lowestVolume = history.Min(h => h.Volume);

            var inputs = history.Select(h =>
                new[]
                {
                    (double) ((h.Close - lowestClose) / (highestClose - lowestClose)),
                    (double) (h.Volume- lowestVolume) / (highestVolume - lowestVolume)
                }).ToArray();

            var classes = inputs.Take(inputs.Length - 1).Zip(inputs.Skip(1), (a, b) => b[0] < a[0] ? 0 : b[0] > a[0] ? 2 : 1).ToArray();

            var outputs = Jagged.OneHot(classes);

            var network = new ActivationNetwork(new SigmoidFunction(), 2, 3, 1);

            new NguyenWidrow(network).Randomize();

            var teacher2 = new ResilientBackpropagationLearning(network);
            var maxError = double.MaxValue;
            var error = 0d;

            // Run supervised learning.
            while (error < maxError)
            {
                error = teacher2.RunEpoch(inputs, outputs);
                if (error < maxError) maxError = error;
            }

            // Checks if the network has learned
            for (var i = 0; i < inputs.Length; i++)
            {
                var answer = network.Compute(inputs[i]);

                var expected = classes[i];
                int actual;
                answer.Max(out actual);
                // actual should be equal to expected
            }
        }
    }
}