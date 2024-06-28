using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace ChanceGen
{
    public class Node
    {
        #region static

        /// <summary>
        /// [0] -> up <br/>
        /// [1] -> up-right <br/>
        /// [2] -> right <br/>
        /// [3] -> down-right <br/>
        /// [4] -> down <br/>
        /// [5] -> down-left <br/>
        /// [6] -> left <br/>
        /// [7] -> up-left
        /// </summary>
        public static readonly NodePosition[] neighborPositions =
        {
            new NodePosition(0, 1),
            new NodePosition(1, 1),
            new NodePosition(1, 0),
            new NodePosition(1, -1),
            new NodePosition(0, -1),
            new NodePosition(-1, -1),
            new NodePosition(-1, 0),
            new NodePosition(-1, 1),
        };

        #endregion

        public NodePosition position;
        public bool blocked;
        public uint walkCount;
        public uint walkFromLastBranch;
        public Connections connections;

        public Node(int x, int y)
        {
            position = new NodePosition(x, y);
            blocked = false;
            walkCount = 0;
        }

        public Node(NodePosition position)
        {
            this.position = position;
            blocked = false;
            walkCount = 0;
        }

        public override string ToString() =>
            $"position: {position}, blocked: {blocked}, walkCount: {walkCount}, walkFromLastBranch: "
            + $"{walkFromLastBranch}, connections: {connections}";

        public static explicit operator NodePosition(Node node) => node.position;

        public class NodeComparer : IEqualityComparer<Node>
        {
            public bool Equals(Node x, Node y) => x?.position == y?.position;
            public int GetHashCode(Node obj) => obj.position.GetHashCode();
        }
    }

    public readonly struct NodePosition
    {
        public readonly int x;
        public readonly int y;

        public NodePosition(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public override string ToString() => $"({x}, {y})";
        public bool Equals(NodePosition other) => x == other.x && y == other.y;
        public override bool Equals(object obj) => obj is NodePosition other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(x, y);

        public static NodePosition operator +(NodePosition lhs, NodePosition rhs) =>
            new NodePosition(lhs.x + rhs.x, lhs.y + rhs.y);

        public static NodePosition operator -(NodePosition lhs, NodePosition rhs) =>
            new NodePosition(lhs.x - rhs.x, lhs.y - rhs.y);

        public static bool operator ==(NodePosition lhs, NodePosition rhs) => lhs.x == rhs.x && lhs.y == rhs.y;
        public static bool operator !=(NodePosition lhs, NodePosition rhs) => !(lhs == rhs);

        public static explicit operator NodePosition(int2 pos) => new NodePosition(pos.x, pos.y);
        public static explicit operator NodePosition(Vector2Int pos) => new NodePosition(pos.x, pos.y);
        public static implicit operator int2(NodePosition pos) => new int2(pos.x, pos.y);
        public static implicit operator Vector2Int(NodePosition pos) => new Vector2Int(pos.x, pos.y);
    }
}