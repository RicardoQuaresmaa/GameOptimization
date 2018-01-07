using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GPUMemoryManager
{

    #region SINGLETON

    /// <summary>
    /// Singleton instance.
    /// </summary>
    private static GPUMemoryManager mInstance = null;

    /// <summary>
    /// Instance of singleton.
    /// </summary>
    public static GPUMemoryManager Instance
    {
        get
        {
            if (mInstance == null)
            {
                mInstance = new GPUMemoryManager();
            }
            return mInstance;
        }
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    private GPUMemoryManager() { }

    #endregion


    #region MEMBER

    bool mInitlized = false;

    Dictionary<int, GPUMemoryBlock> mGPUMemoryBlockDictionary = null;
    Dictionary<GPUMemoryBlock, int> mINVGPUMemoryBlockDictionary = null;

    /// <summary>
    /// Call to initialize singleton.
    /// </summary>
    public void StartUp()
    {
        if (mInitlized) return;

        mGPUMemoryBlockDictionary = new Dictionary<int, GPUMemoryBlock>();
        mINVGPUMemoryBlockDictionary = new Dictionary<GPUMemoryBlock, int>();

        mInitlized = true;
    }

    /// <summary>
    /// Call to deinitialize singleton.
    /// </summary>
    public void Shutdown()
    {
        if (!mInitlized) return;

        foreach (KeyValuePair<int, GPUMemoryBlock> it in mGPUMemoryBlockDictionary)
            it.Value.Release();
        mGPUMemoryBlockDictionary.Clear();
        mGPUMemoryBlockDictionary = null;

        mINVGPUMemoryBlockDictionary.Clear();
        mINVGPUMemoryBlockDictionary = null;

        mInitlized = false;
    }

    /// <summary>
    /// Create GPUMemoryBlock.
    /// </summary>
    /// <param name="name">Name of memory block. Enables access to block if reference is lost. Name must be unique.</param>
    /// <param name="capacity">Capacity(number of elements) of memory block.</param>
    /// <param name="stride">Stride or each element in memory block.</param>
    /// <param name="type">Type of memory block.</param>
    public GPUMemoryBlock CreateGPUMemoryBlock(string name, int capacity, int stride, ComputeBufferType type)
    {
        int hash = name.GetHashCode();
        Debug.Assert(!mGPUMemoryBlockDictionary.ContainsKey(hash), "Name(hash) already exist.");
        GPUMemoryBlock block = new GPUMemoryBlock(capacity, stride, type);
        mGPUMemoryBlockDictionary.Add(hash, block);
        mINVGPUMemoryBlockDictionary.Add(block, hash);
        return block;
    }

    /// <summary>
    /// Get GPUMemoryBlock.
    /// </summary>
    /// <param name="name">Name of memory block to get.</param>
    public GPUMemoryBlock GetGPUMemoryBlock(string name)
    {
        int hash = name.GetHashCode();
        Debug.Assert(mGPUMemoryBlockDictionary.ContainsKey(hash), "Name(hash) doesn't exist.");
        return mGPUMemoryBlockDictionary[hash];
    }

    /// <summary>
    /// Remove GPUMemoryBlock.
    /// </summary>
    /// <param name="name">Name of memory block to remove.</param>
    public void RemoveGPUMemoryBlock(string name)
    {
        RemoveGPUMemoryBlock(name.GetHashCode());
    }

    /// <summary>
    /// Remove GPUMemoryBlock.
    /// </summary>
    /// <param name="block">Block to remove.</param>
    public void RemoveGPUMemoryBlock(GPUMemoryBlock block)
    {
        Debug.Assert(mINVGPUMemoryBlockDictionary.ContainsKey(block), "Block doesn't exist.");
        RemoveGPUMemoryBlock(mINVGPUMemoryBlockDictionary[block]);
    }

    /// <summary>
    /// Remove GPUMemoryBlock.
    /// </summary>
    /// <param name="hash">Hash of memory block to remove.</param>
    public void RemoveGPUMemoryBlock(int hash)
    {
        Debug.Assert(mGPUMemoryBlockDictionary.ContainsKey(hash), "Name(hash) doesn't exist.");
        GPUMemoryBlock block = mGPUMemoryBlockDictionary[hash];
        block.Release();
        mGPUMemoryBlockDictionary.Remove(hash);
        mINVGPUMemoryBlockDictionary.Remove(block);
    }

    #endregion

}
