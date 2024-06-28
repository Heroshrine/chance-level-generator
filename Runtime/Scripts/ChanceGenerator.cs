using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;
using Random = Unity.Mathematics.Random;

#if UNITY_INCLUDE_TESTS
[assembly: InternalsVisibleTo("ChanceGen.Tests")]
#endif

namespace ChanceGen
{
    public class ChanceGenerator
    {
        protected readonly HashSet<NodePosition> blockedPositions = new();
        protected readonly HashSet<NodePosition> generatedPositions = new();
        protected readonly HashSet<Node> generated;

        protected readonly int generateAmount;
        protected readonly int diffuseMinimum;
        protected readonly float diffuseBlockChance;

        /// <summary>
        /// Controls if a cell can be selected by the diffuse generator, uses <see cref="ConwayRule.IfAnd"/>.
        /// </summary>
        protected readonly ConwayRule diffuseSelectionRule;

        protected Random random;

        public ChanceGenerator(int generateAmount,
            int diffuseMinimum,
            float diffuseBlockChance,
            uint seed,
            ConwayRule diffuseSelectionRule)
        {
            this.generateAmount = generateAmount;
            this.diffuseBlockChance = diffuseBlockChance;
            this.diffuseSelectionRule = diffuseSelectionRule;
            this.diffuseMinimum = diffuseMinimum;
            random = new Random(seed);

            generated = new HashSet<Node>(diffuseMinimum, new Node.NodeComparer());
        }

        public virtual async Task<ReadOnlyMemory<Node>> Generate()
        {
            var startNode = new Node(0, 0);
            await Task.Run(() => NeighborDiffuse(in startNode), Application.exitCancellationToken);
            //await Task.Run(() => generated.RemoveAll(n => n.blocked), Application.exitCancellationToken);
            return generated.ToArray();
        }

        // diffuses from start node, adding neighbors to generated and generatedPositions sets.
        protected virtual void NeighborDiffuse(in Node startNode)
        {
            generated.Add(startNode);
            generatedPositions.Add(startNode.position);

            while (generated.Count < generateAmount)
            {
                Span<NodePosition> allNeighbors = GetAllAdjacentNeighbors();

                if (allNeighbors.Length == 0)
                {
                    Debug.LogWarning("Was not able to generate minimum number of nodes! All open spaces blocked.");
                    break;
                }

                var index = random.NextInt(0, allNeighbors.Length);
                var neighborsCount = GetFullNeighborsCount(allNeighbors[index]);
                var blockedType = BlockedType.None;
                Node node;

                // TODO: only calculate this if generated.Count > diffuseMinimum
                if (diffuseSelectionRule.IfAnd(neighborsCount, ref random))
                    blockedType = BlockedType.ConwayBlocked;
                else if (random.NextFloat() <= diffuseBlockChance)
                    blockedType = BlockedType.DiffuseBlocked;

                if ((generated.Count > diffuseMinimum
                     && blockedType != BlockedType.None)
                    || blockedType == BlockedType.ConwayBlocked)
                {
                    node = new Node(allNeighbors[index])
                    {
                        blocked = blockedType
                    };
                    generated.Add(node);
                    blockedPositions.Add(node.position);
                    continue;
                }

                node = new Node(allNeighbors[index]);
                generated.Add(node);
                generatedPositions.Add(node.position);
            }
        }

        // TODO: only usable in certain stage, if not in correct stage assert and direct to GetGeneratedAllAdjacentNeighbors (which is slower).
        /// <summary>
        /// Gets all adjacent neighbors for every generated node.
        /// </summary>
        /// <returns>A span of node positions for every generated node.</returns>
        protected Span<NodePosition> GetAllAdjacentNeighbors()
        {
            var neighbors = new HashSet<NodePosition>();

            Span<NodePosition> adj = stackalloc NodePosition[4];
            foreach (var node in generated.Where(node => node.blocked == BlockedType.None))
            {
                GetAdjacentNeighbors(in node.position, ref adj);
                foreach (var n in adj)
                {
                    if (!generatedPositions.Contains(n) && !blockedPositions.Contains(n))
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
        public void GetAdjacentNeighbors(in NodePosition nodePosition, ref Span<NodePosition> buffer4)
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
        public void GetFullNeighbors(in NodePosition nodePosition, ref Span<NodePosition> buffer8)
        {
            Assert.IsTrue(buffer8.Length == 8, "Buffer length must be 8!");


            for (var i = 0; i < 8; i++)
                buffer8[i] = nodePosition + Node.neighborPositions[i];
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

                if (generated.TryGetValue(buffer4[i], out var found))
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

                if (generated.TryGetValue(buffer8[i], out var found))
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
        public byte GetAdjacentNeighborsCount(Node node)
        {
            byte count = 0;

            for (int i = 0, j = 0; i < 4; i++, j += 2)
            {
                var pos = node.position + Node.neighborPositions[j];
                if (generatedPositions.Contains(pos))
                    count++;
            }

            return count;
        }

        // TODO: only usable in certain stage, if not in correct stage assert and direct to other GetAdjacentNeighborsCount
        /// <summary>
        /// Gets the count of the existing neighbors of this node. Depending on the stage of generation,
        /// this can count blocked nodes as well.
        /// </summary>
        /// <param name="position">The node position to find neighbor count of.</param>
        /// <returns>The neighbor count found.</returns>
        public byte GetAdjacentNeighborsCount(NodePosition position)
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
        /// Gets the full (adjacents and diagonals) count of neighbors of this node. Depending on the stage of generation,
        /// this can count blocked nodes as well.
        /// </summary>
        /// <param name="node">The node to find neighbor count of.</param>
        /// <returns>The neighbor count found.</returns>
        public byte GetFullNeighborsCount(Node node)
        {
            byte count = 0;

            for (var i = 0; i < 8; i++)
            {
                var pos = node.position + Node.neighborPositions[i];
                if (generatedPositions.Contains(pos))
                    count++;
            }

            return count;
        }

        // TODO: only usable in certain stage, if not in correct stage assert and direct to other GetFullNeighborsCount
        /// <summary>
        /// Gets the full (adjacents and diagonals) count of neighbors of this node. Depending on the stage of generation,
        /// this can count blocked nodes as well.
        /// </summary>
        /// <param name="position">The node position to find neighbor count of.</param>
        /// <returns>The neighbor count found.</returns>
        public byte GetFullNeighborsCount(NodePosition position)
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
    }
}