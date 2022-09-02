using UnityEngine;
using System.Collections;

public class Challenge1 : MonoBehaviour
{

    public ComputeShader shader;
    public int texResolution = 1024;
    [Range(0,1)] public float xCentre = 0.5f;
    [Range(0,1)] public float yCentre = 0.5f;
    [Range(0,1)] public float xSide = 0.5f;
    [Range(0,1)] public float ySide = 0.5f;
    
    
    Renderer rend;
    RenderTexture outputTexture;

    int kernelHandle;

    // Use this for initialization
    void Start()
    {
        outputTexture = new RenderTexture(texResolution, texResolution, 0);
        outputTexture.enableRandomWrite = true;
        outputTexture.Create();

        rend = GetComponent<Renderer>();
        rend.enabled = true;

        InitShader();
    }

    private void InitShader()
    {
        kernelHandle = shader.FindKernel("Square");

		//Create a Vector4 with parameters x, y, width, height
        //Pass this to the shader using SetVector
        shader.SetVector("rect", texResolution * new Vector4(xCentre, yCentre, xSide/2, ySide/2));
        shader.SetTexture(kernelHandle, "Result", outputTexture);
       
        rend.material.SetTexture("_MainTex", outputTexture);

        DispatchShader(texResolution / 8, texResolution / 8);
    }

    private void DispatchShader(int x, int y)
    {
        shader.Dispatch(kernelHandle, x, y, 1);
    }

    void Update()
    {
        shader.SetVector("rect", texResolution * new Vector4(xCentre, yCentre, xSide/2, ySide/2));
        DispatchShader(texResolution / 8, texResolution / 8);
    }
}

