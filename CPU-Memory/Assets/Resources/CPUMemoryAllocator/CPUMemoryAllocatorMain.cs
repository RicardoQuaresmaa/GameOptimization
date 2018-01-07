using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CPUMemoryAllocatorMain : MonoBehaviour
{

    public class Chunk
    {
        public int mStartIndex;
        public int mSize;

        public Chunk(int startIndex, int size)
        {
            mStartIndex = startIndex;
            mSize = size;
        }
    }

    public class MemoryHandle
    {
        public int GetStartIndex()
        {
            return sMemoryHandleDic[this].mStartIndex;
        }

        public int GetSize()
        {
            return sMemoryHandleDic[this].mSize;
        }
    }

    const int mMaxSize = 16;
    int[] mMemory = new int[mMaxSize];
    int mEndIndex = 0;

    SortedList<int, Chunk> mAllocatedList = new SortedList<int, Chunk>();
    SortedList<int, Chunk> mFragmentedFreeList = new SortedList<int, Chunk>();

    static Dictionary<MemoryHandle, Chunk> sMemoryHandleDic = new Dictionary<MemoryHandle, Chunk>();

    void Init()
    {
        for (int i = 0; i < mMaxSize; ++i)
            mMemory[i] = -1;
    }

    //void Print()
    //{
    //    for (int i = 0; i < mEndIndex; ++i)
    //        Debug.Log(i + ": " + mMemory[i]);
    //}

    //void PrintAll()
    //{
    //    for (int i = 0; i < mMaxSize; ++i)
    //        Debug.Log(i + ": " + mMemory[i]);
    //}

    // Returns start index.
    MemoryHandle Allocate(int size)
    {
        // Store start index.
        int startIndex = mEndIndex;

        // Allocate chunk.
        Debug.Assert(startIndex + size <= mMaxSize, "Size to big, out of memory.");
        Chunk chunk = new Chunk(startIndex, size);
        mAllocatedList.Add(chunk.mStartIndex, chunk);

        // Insert default values.
        for (int i = startIndex; i < startIndex + size; ++i)
            mMemory[i] = -1;

        // Increment end index.
        mEndIndex += size;

        // Create memory handle.
        MemoryHandle memoryHandle = new MemoryHandle();
        sMemoryHandleDic[memoryHandle] = chunk;
        return memoryHandle;
    }

    void Free(MemoryHandle memoryHandle)
    {
        // Get chunk from memory handle.
        Debug.Assert(sMemoryHandleDic.ContainsKey(memoryHandle));
        Chunk chunk = sMemoryHandleDic[memoryHandle];

        Debug.Assert(mAllocatedList.ContainsValue(chunk), "Trying to remove chunk not allocated.");

        if (chunk.mStartIndex + chunk.mSize == mEndIndex)
        {   // Removing last chunk, move end index back.
            mEndIndex = chunk.mStartIndex;

            // Insert "null" values to flag unallocated data.
            for (int i = chunk.mStartIndex; i < chunk.mStartIndex + chunk.mSize; ++i)
                mMemory[i] = -1;
        }
        else
        {   // Not removing last chunk, add to fragmented list.
            mFragmentedFreeList.Add(chunk.mStartIndex, chunk);

            // Insert "null" values to flag unallocated data.
            for (int i = chunk.mStartIndex; i < chunk.mStartIndex + chunk.mSize; ++i)
                mMemory[i] = -1;
        }

        // Remove chunk from allocated list.
        mAllocatedList.Remove(chunk.mStartIndex);

        // Deinit memory handle.
        Debug.Assert(sMemoryHandleDic.ContainsKey(memoryHandle));

        sMemoryHandleDic.Remove(memoryHandle);

        memoryHandle = null;
        chunk = null;
    }

    void Defragment(uint steps = uint.MaxValue)
    {
        // Return early if dividing defragmentation over several frames.
        if (steps == 0) return;

        // If no fragmented chunks, memory is defragmented.
        if (mFragmentedFreeList.Count == 0) return;
        
        Chunk fragmentedChunk = mFragmentedFreeList.Values[0];
        if (fragmentedChunk.mStartIndex + fragmentedChunk.mSize == mEndIndex)
        {
            // Assert this is the last fragmeneted chunk. Assert should never occur.
            Debug.Assert(mFragmentedFreeList.Count == 1, "Not last fragemented chunk!");

            // Move end index and clear fragmented list.
            mEndIndex = fragmentedChunk.mStartIndex;
            mFragmentedFreeList.Clear();

            // Memory is now defragmented.
            return;
        }

        int nextChunkStartIndex = fragmentedChunk.mStartIndex + fragmentedChunk.mSize;
        if (mAllocatedList.ContainsKey(nextChunkStartIndex))
        {
            // Next chunk is allocated and should swap place with fragmented chunk.
            Chunk allocatedChunk = mAllocatedList[nextChunkStartIndex];

            // Move allocated chunk memory.
            for (int i = 0; i < allocatedChunk.mSize; ++i)
            {
                mMemory[fragmentedChunk.mStartIndex + i] = mMemory[allocatedChunk.mStartIndex + i];

                // Set "null" value to deallocated memory.
                mMemory[allocatedChunk.mStartIndex + i] = -1;
            }

            mAllocatedList.Remove(allocatedChunk.mStartIndex);

            // Update chunk start index.
            allocatedChunk.mStartIndex = fragmentedChunk.mStartIndex;
            fragmentedChunk.mStartIndex = allocatedChunk.mStartIndex + allocatedChunk.mSize;

            mAllocatedList.Add(allocatedChunk.mStartIndex, allocatedChunk);

            mFragmentedFreeList.RemoveAt(0);
            mFragmentedFreeList.Add(fragmentedChunk.mStartIndex, fragmentedChunk);
        }
        else if (mFragmentedFreeList.ContainsKey(nextChunkStartIndex))
        {
            // Next chunk is free and should be combined with fragmented chunk.
            Chunk nextChunk = mFragmentedFreeList[nextChunkStartIndex];

            // Combine chunks.
            fragmentedChunk.mSize += nextChunk.mSize;

            // Remove next chunk.
            mFragmentedFreeList.Remove(nextChunkStartIndex);
        }
        else
        {
            Debug.Assert(false, "Something went wrong!");
        }

        // Continue defragmentation.
        Defragment(steps - 1);
    }

    List<MemoryHandle> testList = new List<MemoryHandle>();

    int tmpCount = 0;

    void Start ()
    {
        Init();

        MemoryHandle memoryHandle;

        memoryHandle = Allocate(2);
        for (int i = memoryHandle.GetStartIndex(); i < memoryHandle.GetStartIndex() + memoryHandle.GetSize(); ++i)
        {
            mMemory[i] = tmpCount;
        }
        tmpCount++;
        testList.Add(memoryHandle);

        memoryHandle = Allocate(2);
        for (int i = memoryHandle.GetStartIndex(); i < memoryHandle.GetStartIndex() + memoryHandle.GetSize(); ++i)
        {
            mMemory[i] = tmpCount;
        }
        tmpCount++;
        testList.Add(memoryHandle);

        memoryHandle = Allocate(2);
        for (int i = memoryHandle.GetStartIndex(); i < memoryHandle.GetStartIndex() + memoryHandle.GetSize(); ++i)
        {
            mMemory[i] = tmpCount;
        }
        tmpCount++;
        testList.Add(memoryHandle);

        // PRINT.
        Debug.Log("---");
        foreach (MemoryHandle mh in testList)
        {
            for (int i = mh.GetStartIndex(); i < mh.GetStartIndex() + mh.GetSize(); ++i)
                Debug.Log(i + ": " + mMemory[i]);
        }


        memoryHandle = testList[0];
        testList.Remove(memoryHandle);
        Free(memoryHandle);


        // PRINT.
        Debug.Log("---");
        foreach (MemoryHandle mh in testList)
        {
            for (int i = mh.GetStartIndex(); i < mh.GetStartIndex() + mh.GetSize(); ++i)
                Debug.Log(i + ": " + mMemory[i]);
        }

        Defragment();

        // PRINT.
        Debug.Log("---");
        foreach (MemoryHandle mh in testList)
        {
            for (int i = mh.GetStartIndex(); i < mh.GetStartIndex() + mh.GetSize(); ++i)
                Debug.Log(i + ": " + mMemory[i]);
        }
    }

	void Update ()
    {
        MemoryHandle memoryHandle;

        memoryHandle = Allocate(2);
        for (int i = memoryHandle.GetStartIndex(); i < memoryHandle.GetStartIndex() + memoryHandle.GetSize(); ++i)
        {
            mMemory[i] = tmpCount;
        }
        tmpCount++;
        testList.Add(memoryHandle);


        memoryHandle = testList[Random.Range(0, testList.Count-1)];
        testList.Remove(memoryHandle);
        Free(memoryHandle);

        Defragment();

        // PRINT.
        Debug.Log("---" + mEndIndex);
        for (int j = 0; j < testList.Count; ++j)
        {
            MemoryHandle mh = testList[j];
            for (int i = mh.GetStartIndex(); i < mh.GetStartIndex() + mh.GetSize(); ++i)
                Debug.Log(j + ": " + mMemory[i]);
        }
    }
}
