using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GPUMemoryManagerMainScript : MonoBehaviour
{

	void Start ()
    {
        GPUMemoryManager.Instance.StartUp();

        GPUMemoryBlock positionBlock = GPUMemoryManager.Instance.CreateGPUMemoryBlock("PositionBlock", 16, System.Runtime.InteropServices.Marshal.SizeOf(typeof(Vector4)), ComputeBufferType.Default);


        Debug.Log("EndIndex: " + positionBlock.EndIndex);

        GPUMemoryBlock.Handle emitter1 = positionBlock.Allocate(2);
        emitter1.SetData(GPUMemoryBlock.SWAPBUFFER.WRITE, new Vector4[] { new Vector4(1,1,1,1), new Vector4(2,2,2,2) });
        Debug.Log("EndIndex: " + positionBlock.EndIndex);
        {
            Vector4[] dataArray = emitter1.GetData<Vector4>(GPUMemoryBlock.SWAPBUFFER.WRITE);
            for (int i = 0; i < dataArray.GetLength(0); ++i)
                Debug.Log(dataArray[i]);
        }

        GPUMemoryBlock.Handle emitter2 = positionBlock.Allocate(2);
        emitter2.SetData(GPUMemoryBlock.SWAPBUFFER.WRITE, new Vector4[] { new Vector4(3, 3, 3, 3), new Vector4(4, 4, 4, 4) });
        Debug.Log("EndIndex: " + positionBlock.EndIndex);
        {
            Vector4[] dataArray = emitter2.GetData<Vector4>(GPUMemoryBlock.SWAPBUFFER.WRITE);
            for (int i = 0; i < dataArray.GetLength(0); ++i)
                Debug.Log(dataArray[i]);
        }


        positionBlock.Free(emitter1);
        Debug.Log("EndIndex: " + positionBlock.EndIndex);
        positionBlock.Defragment();
        Debug.Log("EndIndex: " + positionBlock.EndIndex); ;
        {
            Vector4[] dataArray = emitter2.GetData<Vector4>(GPUMemoryBlock.SWAPBUFFER.WRITE);
            for (int i = 0; i < dataArray.GetLength(0); ++i)
                Debug.Log(dataArray[i]);
        }
    }
	
	void Update ()
    {
		
	}

    void OnDestroy()
    {
        GPUMemoryManager.Instance.Shutdown();
    }
}
