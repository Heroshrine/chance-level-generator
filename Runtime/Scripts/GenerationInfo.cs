using System;
using UnityEngine;

namespace ChanceGen
{
    [Serializable]
    public struct GenerationInfo
    {
        public readonly uint seed;

        public long Area => SideSize * SideSize;

        // number of rooms to make in a grid, this value is one side
        [field: SerializeField] public int SideSize { get; private set; }

        // how much the random chance increases every time a room is placed to skip placing a room.
        [field: SerializeField] public float RandomChanceIncrease { get; private set; }

        // if the floor generates to this number of rooms or less, regenerate the whole thing.
        [field: SerializeField] public float RegenerateLimit { get; private set; }

        // if the floor is larger than this many number of rooms, shrink to this number of rooms
        [field: SerializeField] public int ShrinkLimit { get; private set; }

        public GenerationInfo(uint seed, int sideSize, float randomChanceIncrease, int regenerateLimit, int shrinkLimit)
        {
            Debug.Assert(sideSize % 2 == 0, "sideSize should be odd");

            this.seed = seed;
            SideSize = sideSize;
            RandomChanceIncrease = randomChanceIncrease;
            RegenerateLimit = regenerateLimit;
            ShrinkLimit = shrinkLimit;
        }
    }
}