using System;
using System.Collections.Generic;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace ChanceGen
{
    public abstract class RoomRule : ScriptableObject
    {
        [SerializeField, SerializeReference, Tooltip("The rooms that will be chosen from when generating.")]
        protected Room[] roomPrefabs;

        public abstract void PlaceRoom(ReadOnlySpan<Node> nodes,
            HashSet<Node> generatedNodes,
            int maxWalkValue,
            float nodeSideSize,
            ref Random random);
    }
}