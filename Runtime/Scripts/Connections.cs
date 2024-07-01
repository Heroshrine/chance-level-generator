using System;

namespace ChanceGen
{
    /// <summary>
    /// Describes the connections a node has.
    /// </summary>
    [Flags]
    public enum Connections : byte
    {
        None = 0,
        Up = 1,
        Down = 1 << 1,
        Left = 1 << 2,
        Right = 1 << 3,
        All = Up | Down | Left | Right
    }

    /// <summary>
    /// A small class of utilities for <see cref="Connections"/>.
    /// </summary>
    public static class ConnectionsUtils
    {
        /// <summary>
        /// Checks how many connections a <see cref="Connections"/> value has.
        /// </summary>
        /// <param name="connections">The value to check.</param>
        /// <returns>The number of connections counted.</returns>
        public static int CountConnections(Connections connections)
        {
            return (int)(connections & Connections.Up)
                   + ((int)(connections & Connections.Down) >> 1)
                   + ((int)(connections & Connections.Left) >> 2)
                   + ((int)(connections & Connections.Right) >> 3);
        }
    }
}