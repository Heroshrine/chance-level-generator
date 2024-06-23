using System;
using System.Linq;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace ChanceGen.Collections.Generic
{
    public readonly struct MemoryGrid<T> : IEquatable<MemoryGrid<T>>
    {
        public SpanGrid<T> SpanGrid => new(_memory, _width);

        private readonly Memory<T> _memory;
        private readonly int _width;

        public MemoryGrid(T[,] array)
        {
            _memory = new Memory<T>(array.Cast<T>().ToArray());
            _width = array.GetLength(1);
        }

        public bool Equals(MemoryGrid<T> other) => _memory.Equals(other._memory);

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object obj)
        {
            return obj switch
            {
                MemoryGrid<T> other => _memory.Equals(other._memory),
                Memory<T> memory => memory.Equals(memory),
                _ => false
            };
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode() => _memory.GetHashCode();

        public static implicit operator MemoryGrid<T>([AllowNull] T[,] array) => new MemoryGrid<T>(array);
        public static explicit operator Memory<T>(MemoryGrid<T> grid) => grid._memory;
    }
}