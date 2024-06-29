#if UNITY_EDITOR
#define CHANCEGEN_DEBUG
#endif

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;
using Debug = UnityEngine.Debug;
using Random = Unity.Mathematics.Random;


#if UNITY_INCLUDE_TESTS
[assembly: InternalsVisibleTo("ChanceGen.Tests")]
#endif

namespace ChanceGen
{
    public class ChanceGenerator
    {
        // TODO: decide what variables should be protected or private.

        #region property backing fields

        [EditorBrowsable(EditorBrowsableState.Never)]
        private HashSet<NodePosition> _bnps = new();

        [EditorBrowsable(EditorBrowsableState.Never)]
        private HashSet<NodePosition> _gnps = new();

        [EditorBrowsable(EditorBrowsableState.Never)]
        private HashSet<Node> _gns;

        #endregion

        protected HashSet<NodePosition> BlockedPositions
        {
            get
            {
                /*Do Assertion*/
                return _bnps;
            }
            private set
            {
                /*Do Assertion*/
                _bnps = value;
            }
        }

        protected HashSet<NodePosition> GeneratedPositions
        {
            get
            {
                /*Do Assertion*/
                return _gnps;
            }
            private set
            {
                /*Do Assertion*/
                _gnps = value;
            }
        }

        protected HashSet<Node> Generated
        {
            get
            {
                /*Do Assertion*/
                return _gns;
            }
            private set
            {
                /*Do Assertion*/
                _gns = value;
            }
        }

        protected readonly int generateAmount;
        protected readonly int diffuseMinimum;
        protected readonly float diffuseBlockChance;

        /// <summary>
        /// Controls if a cell can be selected by the diffuse generator, uses <see cref="ConwayRule.IfAnd"/>.
        /// </summary>
        protected readonly ConwayRule diffuseSelectionRule;

        protected readonly ConwayRule removeRule;
        protected readonly ConwayRule addRule;

        protected Random random;

        public ChanceGenerator(int generateAmount,
            int diffuseMinimum,
            float diffuseBlockChance,
            uint seed,
            ConwayRule diffuseSelectionRule,
            ConwayRule removeRule,
            ConwayRule addRule)
        {
            this.generateAmount = generateAmount;
            this.diffuseBlockChance = diffuseBlockChance;
            this.diffuseSelectionRule = diffuseSelectionRule;
            this.removeRule = removeRule;
            this.addRule = addRule;
            this.diffuseMinimum = diffuseMinimum;
            random = new Random(seed);

            Generated = new HashSet<Node>(diffuseMinimum, new Node.NodeComparer());
        }

        // TODO: require cancellation token as will most likely be called from MonoBehaviour. 
        public async Task<ReadOnlyMemory<Node>> Generate()
        {
            var startNode = new Node(0, 0);
            GeneratedPositions.Add(startNode.position);

            await Task.Run(() => NeighborDiffuse(in startNode), Application.exitCancellationToken); // diffuse path
            await Task.Run(ConwayPass, Application.exitCancellationToken); // conway pass
            await Task.Run(BridgeIslands, Application.exitCancellationToken); // reconnect islands
            await Task.Run(GenerateNodes, Application.exitCancellationToken); // generate nodes
            BlockedPositions = null;
            GeneratedPositions = null;

            return Generated.ToArray();
        }

        protected void GenerateNodes()
        {
            foreach (var position in GeneratedPositions)
            {
                var node = new Node(position);
                Generated.Add(node);
            }

            GenerateNodesDebug();
        }

        [Conditional("CHANCEGEN_DEBUG")]
        private void GenerateNodesDebug()
        {
            foreach (var position in BlockedPositions)
            {
                var node = new Node(position);
                node.blocked = true;
                Generated.Add(node);
            }
        }

        // diffuses from start node, adding neighbors to GeneratedPositions set.
        protected virtual void NeighborDiffuse(in Node startNode)
        {
            while (GeneratedPositions.Count + BlockedPositions.Count < generateAmount
                   || GeneratedPositions.Count < diffuseMinimum)
            {
                Span<NodePosition> allNeighbors = GetAllAdjacentNeighbors();

                if (allNeighbors.Length == 0)
                {
                    Debug.LogWarning("Was not able to generate minimum number of nodes! All open spaces blocked.");
                    break;
                }

                var index = random.NextInt(0, allNeighbors.Length);
                var neighborsCount = GetFullNeighborCount(allNeighbors[index]);
                var blockedType = BlockedType.None;

                // TODO: only calculate this if Generated.Count > diffuseMinimum
                if (diffuseSelectionRule.IfAnd(neighborsCount, ref random))
                    blockedType = BlockedType.ConwayBlocked;
                else if (random.NextFloat() <= diffuseBlockChance)
                    blockedType = BlockedType.DiffuseBlocked;

                if ((GeneratedPositions.Count > diffuseMinimum
                     && blockedType != BlockedType.None)
                    || blockedType == BlockedType.ConwayBlocked)
                {
                    BlockedPositions.Add(allNeighbors[index]);
                    continue;
                }

                GeneratedPositions.Add(allNeighbors[index]);
            }
        }

        // uses remove and add rules to determine if a node should be removed or added.
        protected virtual void ConwayPass()
        {
            Span<NodePosition> removing = GeneratedPositions.ToArray();

            foreach (var nodePosition in removing)
            {
                var neighborCount = GetFullNeighborCount(nodePosition);

                if (GeneratedPositions.Contains(nodePosition) && removeRule.IfAnd(neighborCount, ref random))
                {
                    GeneratedPositions.Remove(nodePosition);
                    BlockedPositions.Add(nodePosition);
                }
            }

            Span<NodePosition> allNodePositions = GetAllAdjacentNeighbors();
            foreach (var position in allNodePositions)
            {
                var neighborCount = GetFullNeighborCount(position);
                if (addRule.IfAnd(neighborCount, ref random))
                    GeneratedPositions.Add(position);
            }
        }

        protected HashSet<NodePosition> BridgeIslands()
        {
            List<HashSet<NodePosition>> islands = new();

            var positions = new List<NodePosition>(GeneratedPositions);

            while (positions.Count > 0)
            {
                HashSet<NodePosition> island = WalkPositions(positions[random.NextInt(0, positions.Count)]);

                islands.Add(island);
                positions = positions.Where(pos => !island.Contains(pos)).ToList();
            }

            var blocked = new Queue<NodePosition>(BlockedPositions);
            Span<NodePosition> buffer4 = stackalloc NodePosition[4];
            while (blocked.TryDequeue(out var position))
            {
                GetAdjacentNeighbors(position, ref buffer4);

                var diffIslandsCount = 0;
                var islandIndexes = new List<int>();

                for (var i = 0; i < islands.Count; i++)
                {
                    foreach (var p in buffer4)
                    {
                        if (!islands[i].Contains(p) || islandIndexes.Contains(i)) continue;
                        diffIslandsCount++;
                        islandIndexes.Add(i);
                        break;
                    }
                }

                if (diffIslandsCount < 2) continue;

                BlockedPositions.Remove(position);
                GeneratedPositions.Add(position);

                while (islandIndexes.Count > 1)
                {
                    islands[islandIndexes[0]].UnionWith(islands[islandIndexes[1]]);
                    islands.RemoveAt(islandIndexes[1]);
                    islandIndexes.RemoveAt(1);
                }
            }

            return null;
        }

        public HashSet<NodePosition> WalkPositions(NodePosition startPosition)
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
                    if (!GeneratedPositions.Contains(position) || walked.Contains(position)) continue;
                    walkQueue.Enqueue(position);
                    walked.Add(position);
                }
            }

            return walked;
        }

        // TODO: only usable in certain stage, if not in correct stage assert and direct to GetGeneratedAllAdjacentNeighbors (which is slower).
        /// <summary>
        /// Gets all adjacent neighbors for every Generated node.
        /// </summary>
        /// <returns>A span of node positions for every found position.</returns>
        protected Span<NodePosition> GetAllAdjacentNeighbors()
        {
            var neighbors = new HashSet<NodePosition>();

            Span<NodePosition> adj = stackalloc NodePosition[4];
            foreach (var nodePosition in GeneratedPositions)
            {
                GetAdjacentNeighbors(nodePosition, ref adj);
                foreach (var n in adj)
                {
                    if (!GeneratedPositions.Contains(n) && !BlockedPositions.Contains(n))
                        neighbors.Add(n);
                }
            }

            return neighbors.ToArray();
        }

        /// <summary>
        /// buffer4 elements set to: <br/>
        /// [0] -> up <br/>
        /// [1] -> down <br/>
        /// [2] -> left <br/>
        /// [3] -> right
        /// </summary>
        /// <param name="nodePosition">The node position to get adjacent neighbors of.</param>
        /// <param name="buffer4">The buffer to put the neighbors into.</param>
        public void GetAdjacentNeighbors(NodePosition nodePosition, ref Span<NodePosition> buffer4)
        {
            Assert.IsTrue(buffer4.Length == 4, "Buffer length must be 4!");

            buffer4[0] = nodePosition + Node.neighborPositions[0];
            buffer4[1] = nodePosition + Node.neighborPositions[4];
            buffer4[2] = nodePosition + Node.neighborPositions[6];
            buffer4[3] = nodePosition + Node.neighborPositions[2];
        }

        /// <summary>
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
        public void GetFullNeighbors(NodePosition nodePosition, ref Span<NodePosition> buffer8)
        {
            Assert.IsTrue(buffer8.Length == 8, "Buffer length must be 8!");


            for (var i = 0; i < 8; i++)
                buffer8[i] = nodePosition + Node.neighborPositions[i];
        }

        // TODO: only usable in certain stage, if not in correct stage assert and direct to other GetAdjacentNeighborCount
        /// <summary>
        /// Gets the count of the existing neighbors of this node. Depending on the stage of generation,
        /// this can count blocked nodes as well.
        /// </summary>
        /// <param name="position">The node position to find neighbor count of.</param>
        /// <returns>The neighbor count found.</returns>
        public byte GetAdjacentNeighborCount(NodePosition position)
        {
            byte count = 0;

            for (int i = 0, j = 0; i < 4; i++, j += 2)
            {
                var pos = position + Node.neighborPositions[j];
                if (GeneratedPositions.Contains(pos))
                    count++;
            }

            return count;
        }

        // TODO: only usable in certain stage, if not in correct stage assert and direct to other GetFullNeighborCount
        /// <summary>
        /// Gets the full (adjacents and diagonals) count of neighbors of this node. Depending on the stage of generation,
        /// this can count blocked nodes as well.
        /// </summary>
        /// <param name="position">The node position to find neighbor count of.</param>
        /// <returns>The neighbor count found.</returns>
        public byte GetFullNeighborCount(NodePosition position)
        {
            byte count = 0;

            for (var i = 0; i < 8; i++)
            {
                var pos = position + Node.neighborPositions[i];
                if (GeneratedPositions.Contains(pos))
                    count++;
            }

            return count;
        }

        /// <summary>
        /// buffer4 elements set to: <br/>
        /// [0] -> up <br/>
        /// [1] -> down <br/>
        /// [2] -> left <br/>
        /// [3] -> right
        /// </summary>
        /// <param name="node">The node position to get adjacent neighbors of.</param>
        /// <param name="buffer4">The buffer to put the neighbors into.</param>
        public void GetAdjacentNeighbors(Node node, ref Span<Node> buffer4)
        {
            Assert.IsTrue(buffer4.Length == 4, "Buffer length must be 4!");

            for (int i = 0, j = 0; i < 4; i++, j += 2)
            {
                buffer4[i] ??= new Node(0, 0);
                buffer4[i].position = node.position + Node.neighborPositions[j];

                if (Generated.TryGetValue(buffer4[i], out var found))
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
        public void GetFullNeighbors(Node node, ref Span<Node> buffer8)
        {
            Assert.IsTrue(buffer8.Length == 8, "Buffer length must be 8!");

            for (var i = 0; i < 8; i++)
            {
                buffer8[i] ??= new Node(0, 0);
                buffer8[i].position = node.position + Node.neighborPositions[i];

                if (Generated.TryGetValue(buffer8[i], out var found))
                    buffer8[i] = found;
                else
                    buffer8[i] = null;
            }
        }

        /// <summary>
        /// Gets the count of the existing neighbors of this node. Depending on the stage of generation,
        /// this can count blocked nodes as well.
        /// </summary>
        /// <param name="node">The node to find neighbor count of.</param>
        /// <returns>The neighbor count found.</returns>
        public byte GetAdjacentNeighborCount(Node node)
        {
            byte count = 0;

            for (int i = 0, j = 0; i < 4; i++, j += 2)
            {
                var pos = node.position + Node.neighborPositions[j];
                if (GeneratedPositions.Contains(pos))
                    count++;
            }

            return count;
        }

        /// <summary>
        /// Gets the full (adjacents and diagonals) count of neighbors of this node. Depending on the stage of generation,
        /// this can count blocked nodes as well.
        /// </summary>
        /// <param name="node">The node to find neighbor count of.</param>
        /// <returns>The neighbor count found.</returns>
        public byte GetFullNeighborCount(Node node)
        {
            byte count = 0;

            for (var i = 0; i < 8; i++)
            {
                var pos = node.position + Node.neighborPositions[i];
                if (GeneratedPositions.Contains(pos))
                    count++;
            }

            return count;
        }
    }
}