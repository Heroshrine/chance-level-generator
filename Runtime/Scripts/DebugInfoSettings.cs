using UnityEngine;
using System;

namespace ChanceGen
{
    [Serializable]
    public struct DebugInfoSettings
    {
        [field: SerializeField] public bool ShowWalk { get; private set; }
        [field: SerializeField] public DebugInfo DebuggingInfo { get; private set; }

        public DebugInfoSettings(bool showWalk, DebugInfo debugInfo)
        {
            ShowWalk = showWalk;
            DebuggingInfo = debugInfo;
        }

        public enum DebugInfo
        {
            Minimal = 1,
            Full = 3
        }
    }
}