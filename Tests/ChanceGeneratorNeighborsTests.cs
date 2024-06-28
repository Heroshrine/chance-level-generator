using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using Assert = UnityEngine.Assertions.Assert;

namespace ChanceGen.Tests
{
    [TestFixture]
    public static class ChanceGeneratorNeighborsTests
    {
        [Test]
        public static void GetAdjacentNeighborsByPositionTest(
            [ValueSource(typeof(ChanceGeneratorNeighborsDataProvider),
                nameof(ChanceGeneratorNeighborsDataProvider.NodePositions))]
            NodePosition position,
            [ValueSource(typeof(ChanceGeneratorNeighborsDataProvider),
                nameof(ChanceGeneratorNeighborsDataProvider.Seeds))]
            uint seed)
        {
            var generator = new ChanceGenerator(40, 12, 0.06f, seed, new ConwayRule(8, 4, 0.45f));
            Task<ReadOnlyMemory<Node>> task = Task.Run(generator.Generate, Application.exitCancellationToken);

            while (!task.IsCompleted) { }

            Debug.Log($"Checking from: {position}");

            Span<NodePosition> neighbors = stackalloc NodePosition[4];
            generator.GetAdjacentNeighbors(position, ref neighbors);

            Assert.IsTrue(neighbors[0].x == position.x && neighbors[0].y == position.y + 1);
            Assert.IsTrue(neighbors[1].x == position.x && neighbors[1].y == position.y - 1);
            Assert.IsTrue(neighbors[2].x == position.x - 1 && neighbors[2].y == position.y);
            Assert.IsTrue(neighbors[3].x == position.x + 1 && neighbors[3].y == position.y);

            foreach (var n in neighbors)
                Debug.Log(n.ToString());
        }

        [Test]
        public static void GetFullNeighborsByPositionTest(
            [ValueSource(typeof(ChanceGeneratorNeighborsDataProvider),
                nameof(ChanceGeneratorNeighborsDataProvider.NodePositions))]
            NodePosition position,
            [ValueSource(typeof(ChanceGeneratorNeighborsDataProvider),
                nameof(ChanceGeneratorNeighborsDataProvider.Seeds))]
            uint seed)
        {
            var generator = new ChanceGenerator(40, 12, 0.06f, seed, new ConwayRule(8, 5, 0.78f));
            Task<ReadOnlyMemory<Node>> task = Task.Run(generator.Generate, Application.exitCancellationToken);

            while (!task.IsCompleted) { }

            Debug.Log($"Checking from: {position}");

            Span<NodePosition> neighbors = stackalloc NodePosition[8];
            generator.GetFullNeighbors(position, ref neighbors);

            Assert.IsTrue(neighbors[0].x == position.x && neighbors[0].y == position.y + 1);
            Assert.IsTrue(neighbors[1].x == position.x + 1 && neighbors[1].y == position.y + 1);
            Assert.IsTrue(neighbors[2].x == position.x + 1 && neighbors[2].y == position.y);
            Assert.IsTrue(neighbors[3].x == position.x + 1 && neighbors[3].y == position.y - 1);
            Assert.IsTrue(neighbors[4].x == position.x && neighbors[4].y == position.y - 1);
            Assert.IsTrue(neighbors[5].x == position.x - 1 && neighbors[5].y == position.y - 1);
            Assert.IsTrue(neighbors[6].x == position.x - 1 && neighbors[6].y == position.y);
            Assert.IsTrue(neighbors[7].x == position.x - 1 && neighbors[7].y == position.y + 1);

            foreach (var n in neighbors)
                Debug.Log(n.ToString());
        }

        [Test]
        public static void GetAdjacentNeighborsTest(
            [ValueSource(typeof(ChanceGeneratorNeighborsDataProvider),
                nameof(ChanceGeneratorNeighborsDataProvider.Nodes))]
            Node node,
            [ValueSource(typeof(ChanceGeneratorNeighborsDataProvider),
                nameof(ChanceGeneratorNeighborsDataProvider.Seeds))]
            uint seed)
        {
            var generator = new ChanceGenerator(40, 12, 0.06f, seed, new ConwayRule(8, 5, 0.78f));
            Task<ReadOnlyMemory<Node>> task = Task.Run(generator.Generate, Application.exitCancellationToken);

            while (!task.IsCompleted) { }

            Debug.Log($"Checking from: {node}");

            Span<Node> neighbors = new Node[4];
            generator.GetAdjacentNeighbors(node, ref neighbors);

            Assert.IsTrue(neighbors[0] == null
                          || (neighbors[0].position.x == node.position.x
                              && neighbors[0].position.y == node.position.y + 1));
            Assert.IsTrue(neighbors[1] == null
                          || (neighbors[1].position.x == node.position.x
                              && neighbors[1].position.y == node.position.y - 1));
            Assert.IsTrue(neighbors[2] == null
                          || (neighbors[2].position.x == node.position.x - 1
                              && neighbors[2].position.y == node.position.y));
            Assert.IsTrue(neighbors[3] == null
                          || (neighbors[3].position.x == node.position.x + 1
                              && neighbors[3].position.y == node.position.y));

            foreach (var n in neighbors)
                Debug.Log(n?.ToString());
        }

        [Test]
        public static void GetFullNeighborsest(
            [ValueSource(typeof(ChanceGeneratorNeighborsDataProvider),
                nameof(ChanceGeneratorNeighborsDataProvider.Nodes))]
            Node node,
            [ValueSource(typeof(ChanceGeneratorNeighborsDataProvider),
                nameof(ChanceGeneratorNeighborsDataProvider.Seeds))]
            uint seed)
        {
            var generator = new ChanceGenerator(40, 12, 0.06f, seed, new ConwayRule(8, 5, 0.78f));
            Task<ReadOnlyMemory<Node>> task = Task.Run(generator.Generate, Application.exitCancellationToken);

            while (!task.IsCompleted) { }

            Debug.Log($"Checking from: {node}");

            Span<Node> neighbors = new Node[8];
            generator.GetFullNeighbors(node, ref neighbors);

            Assert.IsTrue(neighbors[0] == null
                          || (neighbors[0].position.x == node.position.x
                              && neighbors[0].position.y == node.position.y + 1));
            Assert.IsTrue(neighbors[1] == null
                          || (neighbors[1].position.x == node.position.x + 1
                              && neighbors[1].position.y == node.position.y + 1));
            Assert.IsTrue(neighbors[2] == null
                          || (neighbors[2].position.x == node.position.x + 1
                              && neighbors[2].position.y == node.position.y));
            Assert.IsTrue(neighbors[3] == null
                          || (neighbors[3].position.x == node.position.x + 1
                              && neighbors[3].position.y == node.position.y - 1));
            Assert.IsTrue(neighbors[4] == null
                          || (neighbors[4].position.x == node.position.x
                              && neighbors[4].position.y == node.position.y - 1));
            Assert.IsTrue(neighbors[5] == null
                          || (neighbors[5].position.x == node.position.x - 1
                              && neighbors[5].position.y == node.position.y - 1));
            Assert.IsTrue(neighbors[6] == null
                          || (neighbors[6].position.x == node.position.x - 1
                              && neighbors[6].position.y == node.position.y));
            Assert.IsTrue(neighbors[7] == null
                          || (neighbors[7].position.x == node.position.x - 1
                              && neighbors[7].position.y == node.position.y + 1));

            foreach (var n in neighbors)
                Debug.Log(n?.ToString());
        }
    }

    public static class ChanceGeneratorNeighborsDataProvider
    {
        public static IEnumerable<NodePosition> NodePositions
        {
            get
            {
                yield return new NodePosition(0, 0);
                yield return new NodePosition(3, 0);
                yield return new NodePosition(3, 3);
                yield return new NodePosition(0, 3);
                yield return new NodePosition(-3, 3);
                yield return new NodePosition(-3, 0);
                yield return new NodePosition(-3, -3);
                yield return new NodePosition(0, -3);
                yield return new NodePosition(3, -3);
            }
        }

        public static IEnumerable<Node> Nodes => NodePositions.Select(nodePos => new Node(nodePos));

        public static IEnumerable<uint> Seeds
        {
            get
            {
                yield return 138929u;
                yield return 98123u;
                yield return 701920u;
                yield return 301909273u;
            }
        }
    }
}