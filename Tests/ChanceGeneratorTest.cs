using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using Assert = UnityEngine.Assertions.Assert;


namespace ChanceGen.Tests
{
    [TestFixture]
    public static class ChanceGeneratorTest
    {
        [Test, Timeout(120000)]
        public static void RoomCountGenerationTest(
            [ValueSource(typeof(ChanceGeneratorDataProvider), nameof(ChanceGeneratorDataProvider.GetGenerateAmount))]
            (int genAmount, int nonInvalidMin) genInfo,
            [ValueSource(typeof(ChanceGeneratorDataProvider), nameof(ChanceGeneratorDataProvider.GetGenerateAddChance))]
            float diffuseAddChance,
            [ValueSource(typeof(ChanceGeneratorDataProvider), nameof(ChanceGeneratorDataProvider.GetSeed))]
            uint seed)
        {
            var generator = new ChanceGenerator(genInfo.genAmount, genInfo.nonInvalidMin, diffuseAddChance, seed);
            Task<ReadOnlyMemory<Node>> task = Task.Run(generator.Generate, Application.exitCancellationToken);

            while (!task.IsCompleted) { }

            ReadOnlySpan<Node> result = task.Result.Span;

            Assert.IsTrue(result.Length >= genInfo.nonInvalidMin,
                $"minimum invalid length was: {genInfo.nonInvalidMin}, length was: {result.Length}");
        }
    }

    public static class ChanceGeneratorDataProvider
    {
        public const uint SEED = 1237508;

        public static IEnumerable<(int genAmount, int nonInvalidMin)> GetGenerateAmount()
        {
            for (int i = 30, j = 8; i <= 120; i += 10, j += 3)
            {
                yield return (i, j);
            }
        }

        public static IEnumerable<float> GetGenerateAddChance()
        {
            for (var i = 0.4f; i <= 0.9f; i += 0.1f)
            {
                yield return i;
            }
        }

        public static IEnumerable<uint> GetSeed()
        {
            Unity.Mathematics.Random random = new(SEED);
            for (var i = 0; i <= 5; i++)
            {
                yield return random.NextUInt() + 1u;
            }
        }
    }
}