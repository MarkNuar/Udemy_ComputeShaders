using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class BasePP : MonoBehaviour
{
    public ComputeShader shader = null;

    protected string kernelName = "CSMain";

    protected Vector2Int texSize = new Vector2Int(0,0);
    protected Vector2Int groupSize = new Vector2Int(); // for the dispatch method
    protected Camera thisCamera;

    protected RenderTexture output = null;
    protected RenderTexture renderedSource = null;

    protected int kernelHandle = -1;
    protected bool init = false;

    protected virtual void Init()
    {
        if (!SystemInfo.supportsComputeShaders)
        {
            Debug.LogError("It seems your target Hardware does not support Compute Shaders.");
            return;
        }

        if (!shader)
        {
            Debug.LogError("No shader");
            return;
        }

        kernelHandle = shader.FindKernel(kernelName);

        thisCamera = GetComponent<Camera>();

        if (!thisCamera)
        {
            Debug.LogError("Object has no Camera");
            return;
        }

        CreateTextures();

        init = true;
    }

    protected void ClearTexture(ref RenderTexture textureToClear)
    {
        if (null != textureToClear)
        {
            textureToClear.Release();
            textureToClear = null;
        }
    }

    protected virtual void ClearTextures()
    {
        ClearTexture(ref output);
        ClearTexture(ref renderedSource);
    }

    protected void CreateTexture(ref RenderTexture textureToMake, int divide=1)
    {
        textureToMake = new RenderTexture(texSize.x/divide, texSize.y/divide, 0);
        textureToMake.enableRandomWrite = true;
        textureToMake.Create();
    }


    protected virtual void CreateTextures()
    {
        // the textures will have the size of the camera 
        texSize.x = thisCamera.pixelWidth;
        texSize.y = thisCamera.pixelHeight;

        if (shader)
        {
            uint x, y;
            // get kernel group size (dimensions of the single group), numthreads
            shader.GetKernelThreadGroupSizes(kernelHandle, out x, out y, out _); // by using out we pass by reference
            // get the group count for the dispatch method by dividing the textures H and W by the size of the single group
            groupSize.x = Mathf.CeilToInt((float)texSize.x / (float)x);
            groupSize.y = Mathf.CeilToInt((float)texSize.y / (float)y);
        }
        
        CreateTexture(ref output);
        CreateTexture(ref renderedSource);
        
        shader.SetTexture(kernelHandle, "source", renderedSource);
        shader.SetTexture(kernelHandle, "output", output);
    }

    // use in edit mode!
    protected virtual void OnEnable()
    {
        Init();
    }

    protected virtual void OnDisable()
    {
        ClearTextures();
        init = false;
    }

    protected virtual void OnDestroy()
    {
        ClearTextures();
        init = false;
    }

    protected virtual void DispatchWithSource(ref RenderTexture source, ref RenderTexture destination)
    {
        // copy the source into the compute shader texture renderedSource
        Graphics.Blit(source, renderedSource);
        
        shader.Dispatch(kernelHandle, groupSize.x, groupSize.y, 1);
        
        // copy the output of the compute shader into the destination render texture (so on camera)
        Graphics.Blit(output, destination);
    }

    protected void CheckResolution(out bool resChange )
    {
        resChange = false;

        if (texSize.x == thisCamera.pixelWidth && texSize.y == thisCamera.pixelHeight) return;
        
        resChange = true;
        // regenerate the textures
        CreateTextures();
    }
    
    // Standard Monobehavior callback
    // Blit copies the source to the destination
    protected virtual void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        // if not initialized or non shader, simply blit source into dest
        if (!init || shader == null)
        {
            Graphics.Blit(source, destination);
        }
        // if shader ready, apply it to source
        else
        {
            CheckResolution(out _);
            DispatchWithSource(ref source, ref destination);
        }
    }

}
