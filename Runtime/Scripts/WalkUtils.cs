using System;
using System.Collections.Generic;
using System.Linq;
using Random = Unity.Mathematics.Random;
using static ChanceGen.NeighborUtils;

namespace ChanceGen
{
    public static class WalkUtils
    {
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