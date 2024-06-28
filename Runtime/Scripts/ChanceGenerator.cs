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
        protected readonly HashSet<NodePosition> generatedPositions = new();
        protected readonly HashSet<Node> generated;

        protected readonly int generateAmount;
        protected readonly int diffuseMinimum;
        protected readonly float diffuseAddChance;
        protected readonly float diffuseBlockChance;

        private Random _random;

        public ChanceGenerator(int generateAmount,
            int diffuseMinimum,
            float diffuseAddChance,
            float diffuseBlockChance,
            uint seed)
        {
            this.generateAmount = generateAmount;
            this.diffuseAddChance = diffuseAddChance;
            this.diffuseBlockChance = diffuseBlockChance;
            this.diffuseMinimum = diffuseMinimum;
            _random = new Random(seed);

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

                var index = _random.NextInt(0, allNeighbors.Length);
                Node node;

                if (generated.Count > diffuseMinimum && _random.NextFloat() > diffuseAddChance)
                {
                    if (_random.NextFloat() > diffuseBlockChance) continue;

                    node = new Node(allNeighbors[index])
                    {
                        blocked = true
                    };
                    generated.Add(node);
                    generatedPositions.Add(node.position);
                    continue;
                }

                node = new Node(allNeighbors[index]);
                generated.Add(node);
                generatedPositions.Add(node.position);
            }
        }

        // only usable in certain stage, if not in correct stage assert and direct to GetGeneratedAllAdjacentNeighbors (which is slower).
        /// <summary>
        /// Gets all adjacent neighbors for every generated node.
        /// </summary>
        /// <returns>A span of node positions for every generated node.</returns>
        protected Span<NodePosition> GetAllAdjacentNeighbors()
        {
            var neighbors = new HashSet<NodePosition>();

            Span<NodePosition> adj = stackalloc NodePosition[4];
            foreach (var node in generated.Where(node => !node.blocked))
            {
                GetAdjacentNeighbors(in node.position, ref adj);
                foreach (var n in adj)
                {
                    if (!generatedPositions.Contains(n))
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
    }
}