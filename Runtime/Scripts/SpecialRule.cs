using UnityEngine;

namespace ChanceGen
{
    [CreateAssetMenu(fileName = "new Special Room Rule", menuName = "ChanceGen/new Special Rule", order = 0)]
    public class SpecialRule : ScriptableObject
    {
        // max setps from 0 on the ordered list, wherever that may be.
        [field: SerializeField] public int MaxSteps { get; private set; }
        // how much the chance to break should increase per step attempt.
        [field: SerializeField] public int ChanceIncreasePerStepAttempt { get; private set; }
        // is this room required to spawn?
        [field: SerializeField] public bool IsMandatory { get; private set; }
        // the room type to place when successfully placing the room
        [field: SerializeField] public RoomType RoomType { get; private set; }
    }
}