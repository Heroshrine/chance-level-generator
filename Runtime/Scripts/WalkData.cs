namespace ChanceGen
{
    public struct WalkData
    {
        public int walkValue;
        public int queued;

        public WalkData(int walkValue, int queued)
        {
            this.walkValue = walkValue;
            this.queued = queued;
        }
    }
}