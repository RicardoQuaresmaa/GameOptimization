using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VSInstanceMain : MonoBehaviour
{
    const int width = 8192;
    const int height = 4096;
    const float spacing = 0.05f;

    ComputeBuffer mPositionBuffer = null;
    ComputeBuffer mArgsBuffer = null;

    Material mRenderMaterial = null;

	void Start ()
    {
        Camera.main.transform.position = new Vector3(width / 2.0f * spacing, height / 2.0f * spacing, -150);

        mPositionBuffer = new ComputeBuffer(width * height, sizeof(float) * 4, ComputeBufferType.Default);
        mArgsBuffer = new ComputeBuffer(1, sizeof(int) * 4, ComputeBufferType.IndirectArguments);

        Shader renderShader = Resources.Load<Shader>("VSInstance/VSInstanceRenderShader");
        Debug.Assert(renderShader, "Failed loading render shader.");
        mRenderMaterial = new Material(renderShader);

        Vector4[] positionArray = new Vector4[width * height];
        for (int i = 0; i < width * height; ++i)
        {
            positionArray[i] = new Vector4((i % width) * spacing, (i / width) * spacing, 0, 0);
        }
        mPositionBuffer.SetData(positionArray);

        mArgsBuffer.SetData(new int[] { 6, width * height, 0, 0 });
    }
	
	void Update ()
    {

    }

    void OnRenderObject()
    {
        mRenderMaterial.SetPass(0);

        mRenderMaterial.SetBuffer("gPosition", mPositionBuffer);

        Graphics.DrawProceduralIndirect(MeshTopology.Triangles, mArgsBuffer);
    }

    void OnDestroy()
    {
        mPositionBuffer.Release();
        mArgsBuffer.Release();
    }

}
