using System;
using Unity.Mathematics;
using UnityEngine;

namespace ChanceGen
{
    public abstract class SpecialRule : ScriptableObject
    {
        // max steps from 0 on the ordered list, wherever that may be.
        [field: SerializeField] public int MinSteps { get; private set; }

        // how much the chance to break should increase per step attempt.
        [field: SerializeField, Min(0)]
        public virtual float ChancePlaceEarly { get; private set; } = float.Epsilon;

        // the room type to place when successfully placing the room
        [field: SerializeField] public RoomType RoomType { get; private set; }

        public abstract bool ShouldGenerate(ReadOnlySpan<RoomInfo> neighborBuffer4, int walkIndex, int2 gridPosition);
    }
}