using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AssignTexture : MonoBehaviour
{
    public ComputeShader shader; 
    public int texResolution = 256;
    
    private Renderer _rend;
    private RenderTexture _outputTexture;
    private int _kernelHandle;
    
    // Start is called before the first frame update
    void Start()
    {
        _outputTexture = new RenderTexture(texResolution, texResolution, 0);
        _outputTexture.enableRandomWrite = true; // compute shader can write to the texture
        _outputTexture.Create();

        _rend = GetComponent<Renderer>();
        _rend.enabled = true;
        
        InitShader();
    }

    private void InitShader()
    {
        _kernelHandle = shader.FindKernel("CSMain");
        shader.SetTexture(_kernelHandle, "Result", _outputTexture);
        _rend.material.SetTexture("_MainTex", _outputTexture);
        
        DispatchShader(texResolution/16, texResolution/16);
    }

    private void DispatchShader(int x, int y)
    {
        // We are dispatching X*Y*Z threads (works)
        // X horizontally = number of works in the X direction 
        // Y vertically = number of works in the Y direction 
        // Z in depth = number of works in the Z direction 
        // The single thread, from the compute shader, has size 8,8,1
        // So if X and Y are 32 each, we have a final computation of all the 256 pixel of the texture 
        shader.Dispatch(_kernelHandle, x, y, 1);
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyUp(KeyCode.U))
        {
            // We are dispatching a Kernel with 32 thread groups for the x, and 32 for the y (and 1 for the z)
            DispatchShader(texResolution/8, texResolution/8);
        }
    }
}
