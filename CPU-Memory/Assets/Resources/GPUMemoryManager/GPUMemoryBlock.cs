using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Allocates a large memory block(compute buffer) and partition it into smaller blocks.
/// <para />
/// Use Allocate to get a Handle to access a partition.
/// </summary>
public class GPUMemoryBlock
{

    #region HANDLE

    /// <summary>
    /// Handle to manage memory access to block.
    /// Handle is an interface for the developer to access memory partition.
    /// <para />
    /// ONLY ACCESS MEMORY BETWEEN [OFFSET] AND [OFFSET+COUNT].
    /// </summary>
    public class Handle
    {

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="block">Memory block this handle maps to.</param>
        public Handle(GPUMemoryBlock block)
        {
            mBlock = block;
        }

        /// <summary>
        /// The memory block this handle maps to.
        /// </summary>
        private GPUMemoryBlock mBlock;

        /// <summary>
        /// Offset to the first element(Start index) in memory block.
        /// <para />
        /// DO NOT ACCESS MEMORY OUTSIDE HANDLE.
        /// </summary>
        public int Offset
        {
            get
            {
                Debug.Assert(mBlock.mPartitionDictionary.ContainsKey(this), "Error: Handle doesn't map to block.");
                return mBlock.mPartitionDictionary[this].mOffset;
            }
        }

        /// <summary>
        /// Number of elemets in memory block this handle maps to.
        /// <para />
        /// DO NOT ACCESS MEMORY OUTSIDE HANDLE.
        /// </summary>
        public int Count
        {
            get
            {
                Debug.Assert(mBlock.mPartitionDictionary.ContainsKey(this), "Error: Handle doesn't map to block.");
                return mBlock.mPartitionDictionary[this].mCount;
            }
        }

        /// <summary>
        /// Copy array to GPU memory.
        /// </summary>
        /// <param name="swapbuffer">Which buffer to set data in. [WRITE or READ]</param>
        /// <param name="dataArray">Array with data to copy.</param>
        public void SetData<T>(SWAPBUFFER swapbuffer, T[] dataArray)
        {
            Debug.Assert(dataArray.GetLength(0) == Count, "Error: Array not same length as partition.");
            ComputeBuffer buffer = swapbuffer == SWAPBUFFER.WRITE ? mBlock.mSwapBuffer.WriteBuffer : mBlock.mSwapBuffer.ReadBuffer;
            buffer.SetData(dataArray, 0, Offset, Count);
        }

        /// <summary>
        /// Copy array from GPU memory.
        /// Returns array with data from GPU.
        /// </summary>
        /// <param name="swapbuffer">Which buffer to get data from. [WRITE or READ]</param>
        public T[] GetData<T>(SWAPBUFFER swapbuffer)
        {
            T[] dataArray = new T[Count];
            ComputeBuffer buffer = swapbuffer == SWAPBUFFER.WRITE ? mBlock.mSwapBuffer.WriteBuffer : mBlock.mSwapBuffer.ReadBuffer;
            buffer.GetData(dataArray, 0, Offset, Count);
            return dataArray;
        }
    }

    #endregion



    #region PARTITION

    /// <summary>
    /// Private class to track data partitions.
    /// </summary>
    private class Partition
    {
        /// <summary>
        /// Offset from first element(Start index) in memory block.
        /// </summary>
        public int mOffset;

        /// <summary>
        /// Number of elemets in memory block.
        /// </summary>
        public int mCount;

        /// <summary>
        /// Constuctor.
        /// </summary>
        public Partition(int offset, int count)
        {
            mOffset = offset;
            mCount = count;
        }
    }

    #endregion



    #region SWAPBUFFER

    public enum SWAPBUFFER
    {
        READ,
        WRITE
    }

    /// <summary>
    /// Private class contaning several compute buffers.
    /// </summary>
    private class SwapBuffer
    {
        /// <summary>
        /// Buffer array contaning data.
        /// </summary>
        private ComputeBuffer[] mBuffers = null;

        /// <summary>
        /// Buffer array contaning data.
        /// </summary>
        private int mBufferCount;

        /// <summary>
        /// Walker to swap between buffers.
        /// </summary>
        private int mWalker = 0;

        /// <summary>
        /// Buffer to read from.
        /// </summary>
        public ComputeBuffer ReadBuffer
        {
            get { return mBuffers[mWalker]; }
        }

        /// <summary>
        /// Buffer to write to.
        /// </summary>
        public ComputeBuffer WriteBuffer
        {
            get { return mBuffers[(mWalker + 1) % mBufferCount]; }
        }

        /// <summary>
        /// Swap buffers.
        /// </summary>
        public void Swap()
        {
            mWalker = (mWalker + 1) % mBufferCount;
        }

        /// <summary>
        /// Release buffers.
        /// </summary>
        public void Release()
        {
            for (int i = 0; i < mBufferCount; ++i)
            {
                mBuffers[i].Release();
            }
        }

        /// <summary>
        /// Constuctor.
        /// </summary>
        /// <param name="capacity">Capacity(number of elemets) of memory block.</param>
        /// <param name="stride">Stride of memory block.</param>
        /// <param name="type">Type of memory block.</param>
        /// <param name="bufferCount">Number of buffers in swap buffer. Default: 2.</param>
        public SwapBuffer(int capacity, int stride, ComputeBufferType type, int bufferCount = 2)
        {
            mBufferCount = bufferCount;
            mBuffers = new ComputeBuffer[mBufferCount];
            for (int i = 0; i < mBufferCount; ++i)
            {
                mBuffers[i] = new ComputeBuffer(capacity, stride, type);
            }
        }
    }

    #endregion



    #region VARIABLES

    /// <summary>
    /// End index in memory block. Allocates new partitions at this index.
    /// Keeps track of allocated/free memory.
    /// </summary>
    private int mEndIndex = 0;

    /// <summary>
    /// Sorted list keeping tack of allocated partitions.
    /// </summary>
    private SortedList<int, Partition> mAllocatedPartitionList = new SortedList<int, Partition>();

    /// <summary>
    /// Sorted list keeping tack of fragmented partitions.
    /// Fragmented partitions exists between allocated data.
    /// Defragment memory block to remove fragmented partitions.
    /// </summary>
    private SortedList<int, Partition> mFragmentedPartitionList = new SortedList<int, Partition>();

    /// <summary>
    /// Dictionary used to map Handle to Partition.
    /// </summary>
    private Dictionary<Handle, Partition> mPartitionDictionary = new Dictionary<Handle, Partition>();

    /// <summary>
    /// Swap buffer contaning compute buffers.
    /// </summary>
    private SwapBuffer mSwapBuffer = null;

    #endregion



    #region FUNCTIONS

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="capacity">Capacity(number of elemets) of memory block.</param>
    /// <param name="stride">Stride of memory block.</param>
    /// <param name="type">Type of memory block.</param>
    public GPUMemoryBlock(int capacity, int stride, ComputeBufferType type)
    {
        mCapacity = capacity;
        mStride = stride;
        mSwapBuffer = new SwapBuffer(capacity, stride, type);
    }

    /// <summary>
    /// Capacity(number of elemets) of memory block.
    /// </summary>
    private int mCapacity;

    /// <summary>
    /// Capacity(number of elements) of memory block.
    /// </summary>
    public int Capacity
    {
        get { return mCapacity; }
    }

    /// <summary>
    /// Stride of memory block.
    /// </summary>
    private int mStride;

    /// <summary>
    /// Stride of memory block.
    /// </summary>
    public int Stride
    {
        get { return mStride; }
    }

    /// <summary>
    /// Buffer to read from.
    /// </summary>
    public ComputeBuffer ReadBuffer
    {
        get { return mSwapBuffer.ReadBuffer; }
    }

    /// <summary>
    /// Buffer to write to.
    /// </summary>
    public ComputeBuffer WriteBuffer
    {
        get { return mSwapBuffer.WriteBuffer; }
    }

    /// <summary>
    /// Swap to switch read and write buffer.
    /// </summary>
    public void Swap()
    {
        mSwapBuffer.Swap();
    }

    /// <summary>
    /// Release memory block.
    /// </summary>
    public void Release()
    {
        mSwapBuffer.Release();
    }

    /// <summary>
    /// End index, marking the end of allocated memory.
    /// </summary>
    public int EndIndex
    {
        get { return mEndIndex; }
    }

    /// <summary>
    /// Allocate memory.
    /// Returns Handle used to access the memory.
    /// </summary>
    /// <param name="count">Number of elements to allocate.</param>
    public Handle Allocate(int count)
    {
        // Store start index.
        int offset = mEndIndex;

        // Allocate partition.
        Debug.Assert(offset + count <= Capacity, "Error: Out of memory! No allocation can be done.");
        Partition partition = new Partition(offset, count);
        mAllocatedPartitionList.Add(partition.mOffset, partition);

        // Increment end index.
        mEndIndex += count;

        // Create Handle.
        Handle handle = new Handle(this);
        mPartitionDictionary[handle] = partition;
        return handle;
    }

    /// <summary>
    /// Free memory.
    /// Deallocate memory in block.
    /// </summary>
    /// <param name="handle">Handle mapped to partition.</param>
    public void Free(Handle handle)
    {
        // Get partition mapped to handle.
        Debug.Assert(mPartitionDictionary.ContainsKey(handle), "Error: Handle not mapped to this block.");
        Partition partition = mPartitionDictionary[handle];

        Debug.Assert(mAllocatedPartitionList.ContainsValue(partition), "Error: Can't remove partition not allocated.");

        if (partition.mOffset + partition.mCount == mEndIndex)
        {   // Remove last partition by moving end index back.
            mEndIndex = partition.mOffset;
        }
        else
        {   // Fragmented partition, add to fragmented list.
            mFragmentedPartitionList.Add(partition.mOffset, partition);
        }

        // Remove partition from allocated list.
        mAllocatedPartitionList.Remove(partition.mOffset);

        // Remove/Destory Handle.
        Debug.Assert(mPartitionDictionary.ContainsKey(handle), "Error: Handle not i Dictionary.");
        mPartitionDictionary.Remove(handle);
        handle = null;
        partition = null;
    }

    /// <summary>
    /// Defragment memory.
    /// Memory block can become fragmented when removing allocated partitions.
    /// Defragment memory by calling this function.
    /// <para />
    /// Defragmentation can be done over several frames.
    /// </summary>
    /// <param name="steps">Number of steps to make this frame. Default: Defragment whole block this frame(uint.MaxValue).</param>
    public void Defragment(uint steps = uint.MaxValue)
    {
        // Return early if dividing defragmentation over several frames.
        if (steps == 0) return;

        // If no fragmented partitions, memory is defragmented.
        if (mFragmentedPartitionList.Count == 0) return;

        // Get [first] fragmented partition.
        Partition fragmentedPartition = mFragmentedPartitionList.Values[0];
        if (fragmentedPartition.mOffset + fragmentedPartition.mCount == mEndIndex)
        {
            // Assert this is the last fragmeneted chunk. Assert should never occur.
            Debug.Assert(mFragmentedPartitionList.Count == 1, "Error: Not last fragemented partition! Something went wrong!");

            // Move end index and clear fragmented list.
            mEndIndex = fragmentedPartition.mOffset;
            mFragmentedPartitionList.Clear();

            // Memory defragmentation DONE.
            return;
        }

        int nextPartitionOffset = fragmentedPartition.mOffset + fragmentedPartition.mCount;
        if (mAllocatedPartitionList.ContainsKey(nextPartitionOffset))
        {
            // Next partition is allocated and should swap place with fragmented chunk.
            Partition allocatedPartition = mAllocatedPartitionList[nextPartitionOffset];

            // Move allocated partition memory.
            byte[] dataArray = new byte[allocatedPartition.mCount];

            // Write buffer. TODO, CopyResource.
            mSwapBuffer.WriteBuffer.GetData(dataArray, 0, allocatedPartition.mOffset, allocatedPartition.mCount);
            mSwapBuffer.WriteBuffer.SetData(dataArray, 0, fragmentedPartition.mOffset, allocatedPartition.mCount);

            // Read buffer.
            mSwapBuffer.ReadBuffer.GetData(dataArray, 0, allocatedPartition.mOffset, allocatedPartition.mCount);
            mSwapBuffer.ReadBuffer.SetData(dataArray, 0, fragmentedPartition.mOffset, allocatedPartition.mCount);

            // Update allocated partition offset in list.
            mAllocatedPartitionList.Remove(allocatedPartition.mOffset);
            allocatedPartition.mOffset = fragmentedPartition.mOffset;
            fragmentedPartition.mOffset = allocatedPartition.mOffset + allocatedPartition.mCount;
            mAllocatedPartitionList.Add(allocatedPartition.mOffset, allocatedPartition);

            // Update fragmented partition offset in list.
            mFragmentedPartitionList.RemoveAt(0);
            mFragmentedPartitionList.Add(fragmentedPartition.mOffset, fragmentedPartition);
        }
        else if (mFragmentedPartitionList.ContainsKey(nextPartitionOffset))
        {
            // Next partition is free and should be combined with fragmented chunk.
            Partition nextPartition = mFragmentedPartitionList[nextPartitionOffset];

            // Combine partitions.
            fragmentedPartition.mOffset += nextPartition.mOffset;

            // Remove next partition.
            mFragmentedPartitionList.Remove(nextPartitionOffset);
        }
        else
        {
            Debug.Assert(false, "Error: Something went wrong! [nextPartitionOffset] not found in dictionary.");
        }

        // Continue defragmentation.
        Defragment(steps - 1);
    }

    #endregion

}
