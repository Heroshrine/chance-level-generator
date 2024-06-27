using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using Random = Unity.Mathematics.Random;

namespace ChanceGen
{
    public class ChanceGenerator
    {
        private readonly List<Node> _generated = new();

        private readonly int _generateAmount;
        private readonly int _generateMin;
        private readonly float _diffuseAddChance;

        private Random _random;

        public ChanceGenerator(int generateAmount, int generateMin, float diffuseAddChance, uint seed)
        {
            _generateAmount = generateAmount;
            _diffuseAddChance = diffuseAddChance;

            _random = new Random(seed);
        }

        public virtual async Task<ReadOnlyMemory<Node>> Generate()
        {
            var startNode = new Node(0, 0);
            await Task.Run(() => NeighborDiffuse(in startNode));
            //await Task.Run(() => );
            return _generated.ToArray();
        }

        protected virtual void NeighborDiffuse(in Node startNode)
        {
            _generated.Add(startNode);

            while (_generated.Count < _generateAmount)
            {
                Span<Node> allNeighbors = GetAllAdjacentNeighbors();

                var index = _random.NextInt(0, allNeighbors.Length);

                if (_generated.Count > _generateMin && _random.NextFloat() > _diffuseAddChance)
                {
                    allNeighbors[index].invalid = true;
                    _generated.Add(allNeighbors[index]);
                    continue;
                }

                _generated.Add(allNeighbors[index]);
            }
        }

        protected Span<Node> GetAllAdjacentNeighbors()
        {
            var neighbors = new ConcurrentBag<Node>();

            var parallel = Parallel.ForEach(_generated,
                new ParallelOptions { CancellationToken = Application.exitCancellationToken }, node =>
                {
                    if (node.invalid) return;

                    Span<Node> adj = GetAdjacentNeighbors(node);
                    foreach (var n in adj)
                    {
                        if (!_generated.Contains(n) && !neighbors.Contains(n))
                            neighbors.Add(n);
                    }
                });

            Assert.IsTrue(parallel.IsCompleted);

            return neighbors.ToArray();
        }

        protected virtual Span<Node> GetAdjacentNeighbors(Node node) =>
            new[]
            {
                new Node(node.position.x, node.position.y - 1),
                new Node(node.position.x - 1, node.position.y),
                new Node(node.position.x + 1, node.position.y),
                new Node(node.position.x, node.position.y + 1)
            };
    }
}