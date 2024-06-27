using Unity.Mathematics;

public record Node
{
    public int2 position;
    public bool invalid;
    public uint walkCount;

    public Node(int x, int y)
    {
        position = new int2(x, y);
        invalid = false;
        walkCount = 0;
    }
}