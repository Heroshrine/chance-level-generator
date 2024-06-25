using System;
using Unity.Mathematics;
using UnityEngine;

namespace ChanceGen
{
    public abstract class SpecialRule : ScriptableObject
    {
        // the room type to place when successfully placing the room
        [field: SerializeField] public RoomType RoomType { get; protected set; }

        public abstract (int min, int max) GetWalkValueRange(in ReadOnlySpan<RoomInfo> orderedWalk, int walkDataIndex);

        // if the rule can generate multiple times or not. If unique, must spawn.
        [field: SerializeField, SerializeReference]
        public virtual bool IsUnique { get; protected set; }

        // // if this rule must be successful
        // [field: SerializeField, SerializeReference]
        // public virtual bool MustSpawn { get; protected set; }

        public abstract bool ShouldGenerate(in ReadOnlySpan<RoomInfo> orderedWalk,
            in ReadOnlySpan<RoomInfo> adjacentBuffer4,
            int walkIndex,
            int walkDataIndex,
            byte fullNeighborCount,
            int2 gridPosition,
            (int start, int end) generateIndexRange);
    }
}