using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace ChanceGen
{
    /// <summary>
    /// A class that describes a relationship of <see cref="NodeData"/> to a <see cref="NodePosition"/> generated
    /// by a <see cref="ChanceGenerator"/> instance.
    /// </summary>
    public sealed class Node
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

        /// <summary>
        /// The node position of this node.
        /// </summary>
        public NodePosition position;

        /// <summary>
        /// If the node is blocked. Is only ever true when #CHANCEGEN_DEBUG is defined.
        /// </summary>
        public bool blocked;

        /// <summary>
        /// The data the node holds in relation to its <see cref="position"/>.
        /// </summary>
        public NodeData nodeData;

        public Node(int x, int y)
        {
            position = new NodePosition(x, y);
            nodeData = new NodeData { walkCount = -1, walkFromLastBranch = -1, connections = Connections.None };
        }

        public Node(NodePosition position)
        {
            this.position = position;
            nodeData = new NodeData { walkCount = -1, walkFromLastBranch = -1, connections = Connections.None };
        }

        public override string ToString() =>
            $"position: {position}, blocked: {blocked}, walkCount: {nodeData.walkCount}, walkFromLastBranch: "
            + $"{nodeData.walkFromLastBranch}, connections: {nodeData.connections}";

        public static explicit operator NodePosition(Node node) => node.position;

        public class NodeComparer : IEqualityComparer<Node>
        {
            public bool Equals(Node x, Node y) => x?.position == y?.position;
            public int GetHashCode(Node obj) => obj.position.GetHashCode();
        }
    }

    /// <summary>
    /// Information held by a node.
    /// </summary>
    [Serializable]
    public struct NodeData
    {
        /// <summary>
        /// The walk count of the node, counting up from the last chosen walk location.
        /// </summary>
        public int walkCount;

        /// <summary>
        /// The walk count from the last branch, counting up from the last chosen walk location.
        /// The first chosen walk location counts as having branches, even if it doesn't.
        /// </summary>
        public int walkFromLastBranch;

        /// <summary>
        /// The connections the node has.
        /// </summary>
        public Connections connections;

        public void Deconstruct(out int walkCount, out int walkFromLastBranch, out Connections connections)
        {
            walkCount = this.walkCount;
            walkFromLastBranch = this.walkFromLastBranch;
            connections = this.connections;
        }

        public NodeData(int walkCount, int walkFromLastBranch, Connections connections)
        {
            this.walkCount = walkCount;
            this.walkFromLastBranch = walkFromLastBranch;
            this.connections = connections;
        }
    }

    /// <summary>
    /// Describes a node position.
    /// <br/><br/>
    /// Can be implicitly converted to <see cref="int2"/> and <see cref="Vector2Int"/>.<br/>
    /// Can be explicitly converted from <see cref="int2"/> and <see cref="Vector2Int"/>.
    /// </summary>
    public readonly struct NodePosition
    {
        /// <summary>
        /// The x coordinate of the position.
        /// </summary>
        public readonly int x;

        /// <summary>
        /// The y coordinate of the position.
        /// </summary>
        public readonly int y;

        public NodePosition(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public readonly void Deconstruct(out int x, out int y)
        {
            x = this.x;
            y = this.y;
        }

        public override string ToString() => $"({x}, {y})";
        private bool Equals(NodePosition other) => x == other.x && y == other.y;
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