using System;

namespace ChanceGen
{
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

    public static class ConnectionsUtils
    {
        public static int CountConnections(Connections connections)
        {
            return (int)(connections & Connections.Up)
                   + ((int)(connections & Connections.Down) >> 1)
                   + ((int)(connections & Connections.Left) >> 2)
                   + ((int)(connections & Connections.Right) >> 3);
        }
    }
}