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
            [ValueSource(typeof(ChanceGeneratorDataProvider), nameof(ChanceGeneratorDataProvider.GetBlockChance))]
            float blockChance,
            [ValueSource(typeof(ChanceGeneratorDataProvider), nameof(ChanceGeneratorDataProvider.GetSeed))]
            uint seed)
        {
            var generator = new ChanceGenerator(genInfo.genAmount, genInfo.nonInvalidMin, diffuseAddChance, blockChance,
                seed);
            Task<ReadOnlyMemory<Node>> task = Task.Run(generator.Generate, Application.exitCancellationToken);

            while (!task.IsCompleted) { }

            ReadOnlySpan<Node> result = task.Result.Span;

            Assert.IsTrue(result.Length >= genInfo.nonInvalidMin,
                $"minimum invalid length was: {genInfo.nonInvalidMin}, length was: {result.Length}");
        }
    }

    public static class ChanceGeneratorDataProvider
    {
        public static IEnumerable<(int genAmount, int nonInvalidMin)> GetGenerateAmount()
        {
            yield return (30, 5);
            yield return (80, 20);
            yield return (200, 60);
        }

        public static IEnumerable<float> GetGenerateAddChance()
        {
            yield return 0.4f;
            yield return 0.6f;
            yield return 0.8f;
            yield return 0.99f;
        }

        public static IEnumerable<float> GetBlockChance()
        {
            yield return 0.05f;
            yield return 0.1f;
            yield return 0.2f;
            yield return 0.5f;
        }

        public static IEnumerable<uint> GetSeed()
        {
            Unity.Mathematics.Random random = new(uint.Parse($"{DateTime.Now: mfffhss}"));
            for (var i = 0; i <= 5; i++)
            {
                yield return random.NextUInt() + 1u;
            }
        }
    }
}