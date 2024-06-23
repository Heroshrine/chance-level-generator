using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace OLD
{
    public class ChanceGenMono : MonoBehaviour
    {
        public int size;
        public float randomRoomChanceDecrease;
        public int regenerateLimit;
        public int shrinkLimit;

        [Space] public ConwayRules removeRules;
        public ConwayRules addRules;
        public SpecialRoomRules specialRoomRules;
        public DebugInfoSettings debugInfoSettings;

        public string customSeed;

        ChanceGenerator current;

        public bool CurrentFinished => (current == null || !current.isGenerating);

        //public void Stop() => current.Destroy();

        public void Generate()
        {
            if (CurrentFinished)
            {
                //current = ChanceGenerator.GetOrCreate();

                current.Dispose();

                current.Generate(new GenerationInfo(1, size, randomRoomChanceDecrease, regenerateLimit, shrinkLimit),
                    removeRules, addRules, specialRoomRules, debugInfoSettings);
            }
        }
    }
}