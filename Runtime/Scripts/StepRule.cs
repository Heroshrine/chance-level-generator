using System;
using System.Data;
using ChanceGen.Collections;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace ChanceGen
{
    [CreateAssetMenu(fileName = "new Special Step Rule", menuName = "ChanceGen/new Special Step Rule", order = 0)]
    public class StepRule : SpecialRule
    {
        // max steps from 0 on the ordered list (from spawn room)
        [field: SerializeField, Min(0)] public int MinSteps { get; protected set; }

        // min steps from 0 on the ordered list (from spawn room)
        [field: SerializeField, Min(0)] public int MaxSteps { get; protected set; }

        // set when GetWalkValueRange is called
        protected int realMin;

        // set when GetWalkValueRange is called
        protected int realMax;

        public override (int min, int max) GetWalkValueRange(in ReadOnlySpan<RoomInfo> orderedWalk,
            int walkDataIndex)
        {
            // clamps minimum to maximum number of steps
            // + 1 because spawn room has walk value of 1
            realMin = math.min(orderedWalk[0].walkData[walkDataIndex].walkValue, MinSteps + 1);
            // clamp max to min number of steps and to max walk value
            realMax = math.clamp(MaxSteps, realMin, orderedWalk[0].walkData[walkDataIndex].walkValue);

            return (realMin - 1, realMax);
        }

        public override bool ShouldGenerate(in ReadOnlySpan<RoomInfo> orderedWalk,
            in ReadOnlySpan<RoomInfo> adjacentBuffer4,
            int walkIndex,
            int walkDataIndex,
            byte fullNeighborCount,
            int2 gridPosition,
            (int start, int end) generateIndexRange,
            ref Random random)
        {
            var chance = 1f / (generateIndexRange.end - generateIndexRange.start + 1f);

            Debug.Log($"walkIndex: {walkIndex}");
            Debug.Log($"generateIndexRange.start: {generateIndexRange.start}");
            Debug.Log($"generateIndexRange.end: {generateIndexRange.end}");
            Debug.Log($"chance: {chance}");

            Debug.LogWarning((walkIndex - generateIndexRange.start + 1) * chance);

            /* If unique, guarantee spawn by the last index. ChanceGenerator will always force spawn if at the end
             * of range if not overriding boss room.
             */
            if (IsUnique)
                return random.NextFloat() <= (walkIndex - generateIndexRange.start + 1) * chance;
            else
                return random.NextFloat() <= chance;
        }
    }
}