using System.Collections.Generic;
using UnityEngine;

public class StarGlow : MonoBehaviour
{
    #region Field

    [Range(0, 1)]
    public float threshold = 1;

    [Range(0, 10)]
    public float intensity = 1;

    [Range(1, 20)]
    public int divide = 3;

    [Range(1, 5)]
    public int iteration = 5;

    [Range(0, 1)]
    public float attenuation = 1;

    [Range(0, 360)]
    public float angleOfStreak = 0;

    [Range(1, 16)]
    public int numOfStreaks = 4;

    public Material material;

    public Color color = Color.white;

    private int compositeTexID   = 0;
    private int compositeColorID = 0;
    private int brightnessSettingsID   = 0;
    private int iterationID      = 0;
    private int offsetID         = 0;

    #endregion Field

    #region Method

    void Start()
    {
        compositeTexID   = Shader.PropertyToID("_CompositeTex");
        compositeColorID = Shader.PropertyToID("_CompositeColor");
        brightnessSettingsID   = Shader.PropertyToID("_BrightnessSettings");
        iterationID      = Shader.PropertyToID("_Iteration");
        offsetID         = Shader.PropertyToID("_Offset");
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        RenderTexture brightnessTex = RenderTexture.GetTemporary(source.width/divide, source.height/divide, source.depth, source.format);
        RenderTexture blurredTex1 = RenderTexture.GetTemporary(brightnessTex.descriptor);
        RenderTexture blurredTex2 = RenderTexture.GetTemporary(brightnessTex.descriptor);
        RenderTexture compositeTex = RenderTexture.GetTemporary(brightnessTex.descriptor);
        
        material.SetVector(brightnessSettingsID, new Vector3(threshold, intensity, attenuation));
        
        Graphics.Blit(source, brightnessTex, material, 1);

        float angle = 360 / numOfStreaks;

        for (int x = 1; x < numOfStreaks; x++)
        {
            Vector2 offset = (Quaternion.AngleAxis(angle * x + angleOfStreak, Vector3.forward) * Vector2.down)
                .normalized;
            
            material.SetVector(offsetID, offset);
            material.SetInt(iterationID, 1);
            
            Graphics.Blit(brightnessTex, blurredTex1, material, 2);

            for (int i = 2; i < iteration; i++)
            {
                material.SetInt(iterationID, i);
                Graphics.Blit(blurredTex1, blurredTex2, material, 2);

                (blurredTex1, blurredTex2) = (blurredTex2, blurredTex1);
                
            }
            
            Graphics.Blit(blurredTex1, compositeTex, material, 3);
            
        }
        
        material.SetColor(compositeColorID, color);
        material.SetTexture(compositeTexID, compositeTex);
        
        Graphics.Blit(source, destination, material, 4);
        
        RenderTexture.ReleaseTemporary(brightnessTex);
        RenderTexture.ReleaseTemporary(blurredTex1);
        RenderTexture.ReleaseTemporary(blurredTex2);
        RenderTexture.ReleaseTemporary(compositeTex);
    }

    #endregion Method
}