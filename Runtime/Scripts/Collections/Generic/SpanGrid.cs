using System;
using System.ComponentModel;
using System.Linq;
using System.Diagnostics.CodeAnalysis;

namespace ChanceGen.Collections.Generic
{
    public readonly ref struct SpanGrid<T>
    {
        public int Length => _span.Length;

        private readonly Span<T> _span;
        private readonly int _width;

        public SpanGrid(T[,] array)
        {
            _span = array.Cast<T>().ToArray();
            _width = array.GetLength(1);
        }

        public unsafe SpanGrid(void* pointer, int length, int width)
        {
            _span = new Span<T>(pointer, length);
            _width = width;
        }

        internal SpanGrid(Memory<T> memory, int width)
        {
            _span = memory.Span;
            _width = width;
        }

        public T[] ToFlattenedArray() => _span.ToArray();

        public T this[Index x, Index y]
        {
            get => _span[new Index(x.Value + _width * y.Value)];
            set => _span[new Index(x.Value + _width * y.Value)] = value;
        }

#pragma warning disable CS0809
        [Obsolete("Equals() on SpanGrid will always throw an exception. Use == instead.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object obj) => _span.Equals(obj);

        [Obsolete("GetHashCode() on SpanGrid will always throw an exception.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode() => _span.GetHashCode();
#pragma warning restore CS0809

        public static implicit operator SpanGrid<T>([AllowNull] T[,] array) => new SpanGrid<T>(array);
        public static explicit operator Span<T>(SpanGrid<T> grid) => grid._span;
        public static bool operator ==(SpanGrid<T> left, SpanGrid<T> right) => left._span == right._span;
        public static bool operator !=(SpanGrid<T> left, SpanGrid<T> right) => !(left == right);
    }
}