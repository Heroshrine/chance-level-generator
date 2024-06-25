using ChanceGen.Attributes;
using UnityEngine;
using System;
using Unity.Mathematics;

namespace ChanceGen
{
    // DONE: needs to be monobehaviour? probably not.
    [Serializable]
    public class RoomInfo
    {
        [field: SerializeField, ReadOnlyInInspector]
        public bool Contiguous { get; internal set; }

        [field: SerializeField, ReadOnlyInInspector]
        public bool Invalid { get; internal set; }

        [HideInInspector] public bool markedForDeletion;
        public readonly WalkData[] walkData = new WalkData[2]; // TODO: look into using pointers

        public int2 GridPosition { get; internal set; }

        public RoomConnections connections;
        public RoomType roomType;

        internal RoomInfo(int2 gridPosition, RoomType roomType)
        {
            this.GridPosition = gridPosition;
            this.roomType = roomType;
        }

        internal RoomInfo(int2 gridPosition, RoomType roomType, RoomConnections connections)
        {
            this.GridPosition = gridPosition;
            this.roomType = roomType;
            this.connections = connections;
        }

        public override string ToString() => GridPosition.ToString();
    }
}