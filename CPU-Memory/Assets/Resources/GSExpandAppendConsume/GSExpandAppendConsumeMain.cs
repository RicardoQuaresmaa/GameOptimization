using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GSExpandAppendConsumeMain : MonoBehaviour
{
    const int width = 8192;
    const int height = 4096;
    const float spacing = 0.05f;

    ComputeBuffer mPositionBuffer = null;
    ComputeBuffer mArgsBuffer = null;

    ComputeShader mComputeShader = null;

    Material mRenderMaterial = null;

	void Start ()
    {
        Camera.main.transform.position = new Vector3(width / 2.0f * spacing, height / 2.0f * spacing, -150);

        mPositionBuffer = new ComputeBuffer(width * height, sizeof(float) * 4, ComputeBufferType.Append);
        mArgsBuffer = new ComputeBuffer(1, sizeof(int) * 4, ComputeBufferType.IndirectArguments);
        mArgsBuffer.SetData(new int[] { width * height, 1, 0, 0 });

        Shader renderShader = Resources.Load<Shader>("GSExpandAppendConsume/GSExpandAppendConsumeRenderShader");
        Debug.Assert(renderShader, "Failed loading render shader.");
        mRenderMaterial = new Material(renderShader);

        mComputeShader = Resources.Load<ComputeShader>("GSExpandAppendConsume/GSExpandAppendConsumeComputeShader");
        Debug.Assert(mComputeShader, "Failed loading compute shader.");
    }
	
	void Update ()
    {
        mComputeShader.SetBuffer(0, "gPosition", mPositionBuffer);

        mComputeShader.SetInt("gCount", width * height);

        mComputeShader.SetFloat("gSpacing", spacing);
        mComputeShader.SetInt("gWidth", width);

        mComputeShader.Dispatch(0, (int)Mathf.Ceil(width * height / 64.0f), 1, 1);
    }

    void OnRenderObject()
    {
        Graphics.SetRandomWriteTarget(1, mPositionBuffer, true);
        
        mRenderMaterial.SetPass(0);

        ComputeBuffer.CopyCount(mPositionBuffer, mArgsBuffer, 0);

        Graphics.DrawProceduralIndirect(MeshTopology.Points, mArgsBuffer);

        Graphics.ClearRandomWriteTargets();
    }

    void OnDestroy()
    {
        mPositionBuffer.Release();
        mArgsBuffer.Release();
    }

}
