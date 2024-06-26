using System;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace ChanceGen
{
    [CreateAssetMenu(fileName = "new Special Conway Rule", menuName = "ChanceGen/new Special Conway Rule")]
    public class SpecialConwayRule : SpecialRule
    {
        [SerializeField] protected ConwayRule.IfRule ifMode;
        [field: SerializeField] public ConwayRule ConwayRule { get; protected set; }

        public override (int min, int max)
            GetWalkValueRange(in ReadOnlySpan<RoomInfo> orderedWalk, int walkDataIndex) =>
            (0, orderedWalk[0].walkData[walkDataIndex].walkValue);

        public override bool ShouldGenerate(in ReadOnlySpan<RoomInfo> orderedWalk,
            in ReadOnlySpan<RoomInfo> adjacentBuffer4,
            int walkIndex,
            int walkDataIndex,
            byte fullNeighborCount,
            int2 gridPosition,
            (int start, int end) generateIndexRange,
            ref Random random)
        {
            return ifMode switch
                   {
                       ConwayRule.IfRule.And => ConwayRule.IfAnd(fullNeighborCount),
                       ConwayRule.IfRule.Or => ConwayRule.IfOr(fullNeighborCount),
                       ConwayRule.IfRule.Xor => ConwayRule.IfXor(fullNeighborCount),
                       _ => throw new ArgumentException()
                   }
                   && random.NextFloat() <= ConwayRule.ActionChance;
        }
    }
}