using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Assertions;

namespace ChanceGen
{
    public static class NeighborUtils
    {
        /// <summary>
        /// Gets all adjacent neighbors that have not yet been generated for every generated position.
        /// Can only be used during position generation.
        /// </summary>
        /// <param name="generatedPositions">The generated positions hash set.</param>
        /// <param name="blockedPositions">The blocked positions hash set.</param>
        /// <returns>A span of node positions for every found position.</returns>
        public static Span<NodePosition> GetAllAdjacentNeighbors(HashSet<NodePosition> generatedPositions,
            HashSet<NodePosition> blockedPositions)
        {
            var neighbors = new HashSet<NodePosition>();

            Span<NodePosition> adj = stackalloc NodePosition[4];
            foreach (var nodePosition in generatedPositions)
            {
                GetAdjacentNeighbors(nodePosition, ref adj);
                foreach (var n in adj)
                {
                    if (!generatedPositions.Contains(n) && !blockedPositions.Contains(n))
                        neighbors.Add(n);
                }
            }

            return neighbors.ToArray();
        }

        /// <summary>
        /// Always returns all 4 positions, even if they don't actually exist. <br/>
        /// buffer4 elements set to: <br/>
        /// [0] -> up <br/>
        /// [1] -> down <br/>
        /// [2] -> left <br/>
        /// [3] -> right
        /// </summary>
        /// <param name="nodePosition">The node position to get adjacent neighbors of.</param>
        /// <param name="buffer4">The buffer to put the neighbors into.</param>
        public static void GetAdjacentNeighbors(NodePosition nodePosition, ref Span<NodePosition> buffer4)
        {
            Assert.IsTrue(buffer4.Length == 4, "Buffer length must be 4!");

            buffer4[0] = nodePosition + Node.neighborPositions[0];
            buffer4[1] = nodePosition + Node.neighborPositions[4];
            buffer4[2] = nodePosition + Node.neighborPositions[6];
            buffer4[3] = nodePosition + Node.neighborPositions[2];
        }

        /// <summary>
        /// Always returns all 8 positions, even if they don't actually exist. <br/>
        /// buffer8 elements set to: <br/>
        /// [0] -> up <br/>
        /// [1] -> up-right <br/>
        /// [2] -> right <br/>
        /// [3] -> down-right <br/>
        /// [4] -> down <br/>
        /// [5] -> down-left <br/>
        /// [6] -> left <br/>
        /// [7] -> up-left
        /// </summary>
        /// <param name="nodePosition">The node position to get the neighbors of.</param>
        /// <param name="buffer8">The buffer to put the neighbors into.</param>
        public static void GetFullNeighbors(NodePosition nodePosition, ref Span<NodePosition> buffer8)
        {
            Assert.IsTrue(buffer8.Length == 8, "Buffer length must be 8!");


            for (var i = 0; i < 8; i++)
                buffer8[i] = nodePosition + Node.neighborPositions[i];
        }

        /// <summary>
        /// Gets the count of the existing neighbors of this node. If generating nodes,
        /// use <see cref="GetAdjacentNeighborCount(Node, HashSet{Node})"/> instead.
        /// </summary>
        /// <param name="position">The node position to find neighbor count of.</param>
        /// <param name="generatedPositions">The generated positions hash set.</param>
        /// <returns>The neighbor count found.</returns>
        public static byte GetAdjacentNeighborCount(NodePosition position, HashSet<NodePosition> generatedPositions)
        {
            byte count = 0;

            for (int i = 0, j = 0; i < 4; i++, j += 2)
            {
                var pos = position + Node.neighborPositions[j];
                if (generatedPositions.Contains(pos))
                    count++;
            }

            return count;
        }

        /// <summary>
        /// Gets the full (adjacents and diagonals) count of neighbors of this node. If generating nodes,
        /// use <see cref="GetFullNeighborCount(Node, HashSet{Node})"/> instead.
        /// </summary>
        /// <param name="position">The node position to find neighbor count of.</param>
        /// <param name="generatedPositions">The generated positions hash set.</param>
        /// <returns>The neighbor count found.</returns>
        public static byte GetFullNeighborCount(NodePosition position, HashSet<NodePosition> generatedPositions)
        {
            byte count = 0;

            for (var i = 0; i < 8; i++)
            {
                var pos = position + Node.neighborPositions[i];
                if (generatedPositions.Contains(pos))
                    count++;
            }

            return count;
        }

        /// <summary>
        /// Gets the 4 adjacent neighbors of the supplied node. Some elements may be null if there is no neighbor.
        /// If generating positions, use <see cref="GetAdjacentNeighbors(NodePosition, ref Span{NodePosition}"/> instead. <br/>
        /// buffer4 elements set to: <br/>
        /// [0] -> up <br/>
        /// [1] -> down <br/>
        /// [2] -> left <br/>
        /// [3] -> right
        /// </summary>
        /// <param name="node">The node position to get adjacent neighbors of.</param>
        /// <param name="buffer4">The buffer to put the neighbors into.</param>
        /// <param name="generatedNodes">The generated nodes hash set.</param>
        public static void GetAdjacentNeighbors(Node node, ref Span<Node> buffer4, HashSet<Node> generatedNodes)
        {
            Assert.IsTrue(buffer4.Length == 4, "Buffer length must be 4!");

            for (int i = 0, j = 0; i < 4; i++, j += 2)
            {
                buffer4[i] ??= new Node(0, 0);
                buffer4[i].position = node.position + Node.neighborPositions[j];

                if (generatedNodes.TryGetValue(buffer4[i], out var found))
                    buffer4[i] = found;
                else
                    buffer4[i] = null;
            }

            var n = buffer4[1];
            buffer4[1] = buffer4[2];
            buffer4[2] = buffer4[3];
            buffer4[3] = n;
        }

        /// <summary>
        /// Gets all 8 neighbors of the supplied node. Some elements may be null if there is no neighbor.
        /// If generating positions, use <see cref="GetFullNeighbors(NodePosition, ref Span{NodePosition})"/> instead. <br/>
        /// buffer8 elements set to: <br/>
        /// [0] -> up <br/>
        /// [1] -> up-right <br/>
        /// [2] -> right <br/>
        /// [3] -> down-right <br/>
        /// [4] -> down <br/>
        /// [5] -> down-left <br/>
        /// [6] -> left <br/>
        /// [7] -> up-left
        /// </summary>
        /// <param name="node">The node position to get the neighbors of.</param>
        /// <param name="buffer8">The buffer to put the neighbors into.</param>
        /// <param name="generatedNodes">The generated nodes hash set.</param>
        public static void GetFullNeighbors(Node node, ref Span<Node> buffer8, HashSet<Node> generatedNodes)
        {
            Assert.IsTrue(buffer8.Length == 8, "Buffer length must be 8!");

            for (var i = 0; i < 8; i++)
            {
                buffer8[i] ??= new Node(0, 0);
                buffer8[i].position = node.position + Node.neighborPositions[i];

                if (generatedNodes.TryGetValue(buffer8[i], out var found))
                    buffer8[i] = found;
                else
                    buffer8[i] = null;
            }
        }

        /// <summary>
        /// Gets the count of the existing neighbors of this node. If generating positions,
        /// use <see cref="GetAdjacentNeighborCount(NodePosition, HashSet{NodePosition})"/> instead.
        /// </summary>
        /// <param name="node">The node to find neighbor count of.</param>
        /// <param name="generatedNodes">The generated nodes hash set.</param>
        /// <returns>The neighbor count found.</returns>
        public static byte GetAdjacentNeighborCount(Node node, HashSet<Node> generatedNodes)
        {
            byte count = 0;

            for (int i = 0, j = 0; i < 4; i++, j += 2)
            {
                var pos = node.position + Node.neighborPositions[j];
                if (generatedNodes.Contains(new Node(pos)))
                    count++;
            }

            return count;
        }

        /// <summary>
        /// Gets the full (adjacents and diagonals) count of neighbors of this node. If generating positions,
        /// use <see cref="GetFullNeighborCount(NodePosition, HashSet{NodePosition})"/> instead.
        /// </summary>
        /// <param name="node">The node to find neighbor count of.</param>
        /// <param name="generatedNodes">The generated nodes hash set.</param>
        /// <returns>The neighbor count found.</returns>
        public static byte GetFullNeighborCount(Node node, HashSet<Node> generatedNodes)
        {
            byte count = 0;

            for (var i = 0; i < 8; i++)
            {
                var pos = node.position + Node.neighborPositions[i];
                if (generatedNodes.Contains(new Node(pos)))
                    count++;
            }

            return count;
        }
    }
}