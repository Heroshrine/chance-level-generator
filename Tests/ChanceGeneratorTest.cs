using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Random = Unity.Mathematics.Random;

namespace ChanceGen.Tests
{
    [TestFixture]
    public class ChanceGeneratorTest
    {
        public const int GENERATOR_SMALL_COUNT = 32;
        public const int GENERATOR_MEDIUM_COUNT = 32;
        public const int GENERATOR_LARGE_COUNT = 32;

        [Test, Combinatorial, Timeout(40000)]
        public void GenericTestsNoAsync(
            [ValueSource(nameof(GetGenerators))] ChanceGenerator generator)
        {
            generator.GenerateNoAsync();
        }

        public static List<ChanceGenerator> GetGenerators
        {
            get
            {
                Random random = new(uint.Parse($"{DateTime.Now: fffsshhmm}"));
                var results = new List<ChanceGenerator>();

                results.AddRange(GetSmallGenerators(ref random));
                results.AddRange(GetMediumGenerators(ref random));
                results.AddRange(GetLargeGenerators(ref random));

                return results;
            }
        }

        private static ChanceGenerator[] GetSmallGenerators(ref Random random)
        {
            var results = new ChanceGenerator[GENERATOR_SMALL_COUNT];

            for (var i = 0; i < ChanceGeneratorTest.GENERATOR_SMALL_COUNT; i++)
            {
                var generationInfo = new GenerationInfo(random.NextUInt() + 1u, 31, 0.00975f,
                    3, 16);
                var removeRule = new ConwayRule(0, 5, 0.43f);
                var addRule = new ConwayRule(4, 3, 0.33f);
                var spawnRoomRule = ScriptableObject.CreateInstance<SpecialStepRule>();
                {
                    spawnRoomRule.RoomType = ScriptableObject.CreateInstance<RoomType>();
                    spawnRoomRule.RoomType.name = "RoomType_Spawn";
                    spawnRoomRule.MinSteps = 0;
                    spawnRoomRule.MaxSteps = 0;
                    spawnRoomRule.IsUnique = true;
                }
                var bossRoomRule = ScriptableObject.CreateInstance<SpecialStepRule>();
                {
                    bossRoomRule.RoomType = ScriptableObject.CreateInstance<RoomType>();
                    bossRoomRule.RoomType.name = "RoomType_Boss";
                    bossRoomRule.MinSteps = 5;
                    bossRoomRule.MaxSteps = int.MaxValue;
                    bossRoomRule.IsUnique = true;
                }
                results[i] = new ChanceGenerator(generationInfo, removeRule, addRule, spawnRoomRule, bossRoomRule, null,
                    DebugInfo.Full);
            }

            return results;
        }

        private static ChanceGenerator[] GetMediumGenerators(ref Random random)
        {
            var results = new ChanceGenerator[GENERATOR_MEDIUM_COUNT];

            for (var i = 0; i < ChanceGeneratorTest.GENERATOR_MEDIUM_COUNT; i++)
            {
                var generationInfo = new GenerationInfo(random.NextUInt() + 1u, 137, 0.00878f,
                    16, 42);
                var removeRule = new ConwayRule(0, 5, 0.43f);
                var addRule = new ConwayRule(4, 2, 0.33f);
                var spawnRoomRule = ScriptableObject.CreateInstance<SpecialStepRule>();
                {
                    spawnRoomRule.RoomType = ScriptableObject.CreateInstance<RoomType>();
                    spawnRoomRule.RoomType.name = "RoomType_Spawn";
                    spawnRoomRule.MinSteps = 0;
                    spawnRoomRule.MaxSteps = 1;
                    spawnRoomRule.IsUnique = true;
                }
                var bossRoomRule = ScriptableObject.CreateInstance<SpecialStepRule>();
                {
                    bossRoomRule.RoomType = ScriptableObject.CreateInstance<RoomType>();
                    bossRoomRule.RoomType.name = "RoomType_Boss";
                    bossRoomRule.MinSteps = 18;
                    bossRoomRule.MaxSteps = int.MaxValue;
                    bossRoomRule.IsUnique = true;
                }
                results[i] = new ChanceGenerator(generationInfo, removeRule, addRule, spawnRoomRule, bossRoomRule, null,
                    DebugInfo.Full);
            }

            return results;
        }

        private static ChanceGenerator[] GetLargeGenerators(ref Random random)
        {
            var results = new ChanceGenerator[GENERATOR_LARGE_COUNT];

            for (var i = 0; i < ChanceGeneratorTest.GENERATOR_LARGE_COUNT; i++)
            {
                var generationInfo = new GenerationInfo(random.NextUInt() + 1u, 303, 0.00425f,
                    70, 120);
                var removeRule = new ConwayRule(0, 6, 0.64f);
                var addRule = new ConwayRule(5, 2, 0.34f);
                var spawnRoomRule = ScriptableObject.CreateInstance<SpecialStepRule>();
                {
                    spawnRoomRule.RoomType = ScriptableObject.CreateInstance<RoomType>();
                    spawnRoomRule.RoomType.name = "RoomType_Spawn";
                    spawnRoomRule.MinSteps = 0;
                    spawnRoomRule.MaxSteps = 4;
                    spawnRoomRule.IsUnique = true;
                }
                var bossRoomRule = ScriptableObject.CreateInstance<SpecialStepRule>();
                {
                    bossRoomRule.RoomType = ScriptableObject.CreateInstance<RoomType>();
                    bossRoomRule.RoomType.name = "RoomType_Boss";
                    bossRoomRule.MinSteps = 42;
                    bossRoomRule.MaxSteps = int.MaxValue;
                    bossRoomRule.IsUnique = true;
                }
                results[i] = new ChanceGenerator(generationInfo, removeRule, addRule, spawnRoomRule, bossRoomRule, null,
                    DebugInfo.Full);
            }

            return results;
        }
    }
}