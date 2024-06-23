using UnityEngine;
using System;

namespace ChanceGen
{
    [System.Serializable]
    public struct DebugInfoSettings
    {
        [field: SerializeField, Range(0, 0.25f)]
        public float GenerationSpeed { get; private set; }

        [field: SerializeField] public bool ShowWalk { get; private set; }
        [field: SerializeField] public DebugInfo DebuggingInfo { get; private set; }

        public DebugInfoSettings(float generationSpeed, bool showWalk, DebugInfo debugInfo)
        {
            GenerationSpeed = generationSpeed;
            ShowWalk = showWalk;
            DebuggingInfo = debugInfo;
        }

        [Flags]
        public enum DebugInfo
        {
            Minimal = 1,
            Full = 3
        }
    }
}