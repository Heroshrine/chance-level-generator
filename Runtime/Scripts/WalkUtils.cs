using System;
using System.Collections.Generic;
using System.Linq;
using Random = Unity.Mathematics.Random;
using static ChanceGen.NeighborUtils;

namespace ChanceGen
{
    /// <summary>
    /// A collection of utility methods relating to walking through generated positions and nodes.
    /// </summary>
    public static class WalkUtils
    {
        /// <summary>
        /// Returns a hash set of contiguous positions from the supplied start position. Assumes that the supplied
        /// start positions exists. Supplying a position that does not exist in the hash set results in undefined behaviour.
        /// </summary>
        /// <param name="startPosition">The position to walk from.</param>
        /// <param name="generatedPositions">The currently generated positions.</param>
        /// <returns>A hash set of <see cref="NodePosition"/>s, contiguous with the supplied
        ///     <see cref="startPosition"/>.</returns>
        public static HashSet<NodePosition> WalkPositions(NodePosition startPosition,
            HashSet<NodePosition> generatedPositions)
        {
            HashSet<NodePosition> walked = new();
            Queue<NodePosition> walkQueue = new();
            Span<NodePosition> buffer4 = stackalloc NodePosition[4];
            walkQueue.Enqueue(startPosition);
            walked.Add(startPosition);

            while (walkQueue.TryDequeue(out var working))
            {
                GetAdjacentNeighbors(working, ref buffer4);
                foreach (var position in buffer4)
                {
                    if (!generatedPositions.Contains(position) || walked.Contains(position)) continue;
                    walkQueue.Enqueue(position);
                    walked.Add(position);
                }
            }

            return walked;
        }

        /// <summary>
        /// Walks the supplied nodes hash set from the supplied start node - assuming that the start node is
        /// in the hash set - and applies the walk count and walk from last branch values to each node. This
        /// overrides existing values. Supplying a node that does not exist in the hash set results in undefined behaviour.
        /// </summary>
        /// <param name="startNode">The node to walk from.</param>
        /// <param name="generatedNodes">The nodes to walk.</param>
        /// <param name="branchRequirement">How many nodes a node need to connect to to reset the walk branch value.</param>
        /// <returns>The highest walk value.</returns>
        public static int WalkNodesAndApplyValues(Node startNode,
            HashSet<Node> generatedNodes,
            int branchRequirement)
        {
            HashSet<Node> walked = new();
            Queue<Node> walkQueue = new();
            Span<Node> buffer4 = new Node[4];

            startNode.nodeData.walkCount = 0;
            startNode.nodeData.walkFromLastBranch = 0;
            walkQueue.Enqueue(startNode);

            var _highestWalk = int.MinValue;

            while (walkQueue.TryDequeue(out var working))
            {
                GetAdjacentNeighbors(working, ref buffer4, generatedNodes);
                for (var i = 0; i < buffer4.Length; i++)
                {
                    if (buffer4[i] is null
                        || !generatedNodes.TryGetValue(buffer4[i], out var found))
                        continue;

                    working.nodeData.connections |= i switch
                    {
                        0 => Connections.Down,
                        1 => Connections.Up,
                        2 => Connections.Right,
                        3 => Connections.Left,
                        _ => throw new ArgumentOutOfRangeException()
                    };

                    if (found.nodeData.walkCount == -1
                        || found.nodeData.walkCount > working.nodeData.walkCount + 1)
                        found.nodeData.walkCount = working.nodeData.walkCount + 1;

                    if (walked.Contains(found)) continue;

                    walkQueue.Enqueue(found);
                    walked.Add(working);
                }

                if (ConnectionsUtils.CountConnections(working.nodeData.connections) >= branchRequirement)
                    working.nodeData.walkFromLastBranch = 0;

                foreach (var node in buffer4)
                {
                    if (node is null
                        || !generatedNodes.TryGetValue(node, out var found))
                        continue;

                    if (found.nodeData.walkFromLastBranch == -1
                        || found.nodeData.walkFromLastBranch > working.nodeData.walkFromLastBranch + 1)
                        found.nodeData.walkFromLastBranch = working.nodeData.walkFromLastBranch + 1;
                }

                if (working.nodeData.walkCount > _highestWalk)
                    _highestWalk = working.nodeData.walkCount;
            }

            return _highestWalk;
        }

        // connects islands to make the entire generated level contiguous
        internal static void BridgeIslands(HashSet<NodePosition> generatedPositions,
            HashSet<NodePosition> blockedPositions,
            ref Random random)
        {
            List<HashSet<NodePosition>> islands = GetIslands(generatedPositions, ref random);

            var blocked = new Queue<NodePosition>(blockedPositions);
            Span<NodePosition> buffer4 = stackalloc NodePosition[4];
            while (blocked.TryDequeue(out var position))
            {
                var islandIndexes = new List<int>();
                GetAdjacentNeighbors(position, ref buffer4);

                if (CountAdjacentIslands(buffer4, islands, islandIndexes) < 2) continue;

                blockedPositions.Remove(position);
                generatedPositions.Add(position);

                while (islandIndexes.Count > 1)
                {
                    islands[islandIndexes[0]].UnionWith(islands[islandIndexes[1]]);
                    islands.RemoveAt(islandIndexes[1]);
                    islandIndexes.RemoveAt(1);
                }
            }
        }

        // finds and returns islands from the supplied hash set of generated positions
        private static List<HashSet<NodePosition>> GetIslands(HashSet<NodePosition> generatedPositions,
            ref Random random)
        {
            List<HashSet<NodePosition>> islands = new();

            var positions = new List<NodePosition>(generatedPositions);

            while (positions.Count > 0)
            {
                HashSet<NodePosition> island =
                    WalkPositions(positions[random.NextInt(0, positions.Count)], generatedPositions);

                islands.Add(island);
                positions = positions.Where(pos => !island.Contains(pos)).ToList();
            }

            return islands;
        }

        // counts islands in the supplied list
        private static byte CountAdjacentIslands(ReadOnlySpan<NodePosition> positionNeighbors,
            List<HashSet<NodePosition>> islands,
            List<int> islandIndexes)
        {
            byte adjacentIslands = 0;

            for (var i = 0; i < islands.Count; i++)
            {
                foreach (var p in positionNeighbors)
                {
                    if (!islands[i].Contains(p) || islandIndexes.Contains(i)) continue;
                    adjacentIslands++;
                    islandIndexes.Add(i);
                    break;
                }
            }

            return adjacentIslands;
        }
    }
}