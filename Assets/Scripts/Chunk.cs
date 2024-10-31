using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

public struct Chunk
{
    private UnsafeList<int> sphereIndexes;

    public int Length => sphereIndexes.Length;

    public Chunk(Allocator allocator)
    {
        this.sphereIndexes = new UnsafeList<int>(1, allocator);
    }

    public void Add(int sphereId)
    {
        sphereIndexes.Add(sphereId);
    }

    public void Clear()
    {
        sphereIndexes.Clear();
    }

    public int GetSphereId(int index) => sphereIndexes[index];

    public void Dispose()
    {
        sphereIndexes.Dispose();
    }
}
