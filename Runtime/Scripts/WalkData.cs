namespace ChanceGen
{
    public struct WalkData
    {
        public int walkValue;
        public bool queued;

        public WalkData(int walkValue, bool queued)
        {
            this.walkValue = walkValue;
            this.queued = queued;
        }
    }
}