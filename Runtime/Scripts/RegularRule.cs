using System;
using Unity.Mathematics;
using UnityEngine;

namespace ChanceGen
{
    [CreateAssetMenu(fileName = "new Regular Special Rule", menuName = "ChanceGen/new Regular Special Rule", order = 0)]
    public class RegularRule : SpecialRule
    {
        public override bool ShouldGenerate(ReadOnlySpan<RoomInfo> neighborBuffer4, int walkIndex, int2 gridPosition) =>
            true;
    }
}