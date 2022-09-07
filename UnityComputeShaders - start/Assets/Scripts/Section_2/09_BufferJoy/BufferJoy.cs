using UnityEngine;
using System.Collections;

public class BufferJoy : MonoBehaviour
{

    public ComputeShader shader;
    public int texResolution = 1024;

    Renderer rend;
    RenderTexture outputTexture;

    int circlesHandle;
    int clearHandle;

    struct Circle
    {
        public Vector2 origin;
        public Vector2 velocity;
        public float radius;
    }

    private int count = 10;
    private Circle[] circleData;
    private ComputeBuffer buffer;

    public Color clearColor = new Color();
    public Color circleColor = new Color();

    // Use this for initialization
    void Start()
    {
        outputTexture = new RenderTexture(texResolution, texResolution, 0);
        outputTexture.enableRandomWrite = true;
        outputTexture.Create();

        rend = GetComponent<Renderer>();
        rend.enabled = true;

        InitData();

        InitShader();
    }

    private void InitData()
    {
        circlesHandle = shader.FindKernel("Circles");

        uint threadGroupSizeX;
        // This methos gives the numthreads values (dimensions of the single group)
        // In this case we are only interested in the X value, since the Y and Z are set to 1 
        shader.GetKernelThreadGroupSizes(circlesHandle, out threadGroupSizeX, out _, out _);

        // The total amount of circle is the X of numthread * the count of works (count)
        int total = (int)threadGroupSizeX * count;
        circleData = new Circle[total];

        // variables to setup the Circle array
        float speed = 100;
        float halfSpeed = speed * 0.5f;
        float minRadius = 10.0f;
        float maxRadius = 30.0f;
        float radiusRange = maxRadius - minRadius;

        for (int i = 0; i < total; i++)
        {
            // create circle 
            Circle circle = circleData[i];
            circle.origin.x = Random.value * texResolution;
            circle.origin.y = Random.value * texResolution;
            circle.velocity.x = (Random.value * speed) - halfSpeed;
            circle.velocity.y = (Random.value * speed) - halfSpeed;
            circle.radius = Random.value * radiusRange + minRadius;
            circleData[i] = circle;
        }
        
    }

    private void InitShader()
    {
    	clearHandle = shader.FindKernel("Clear");
    	
        shader.SetVector( "clearColor", clearColor );
        shader.SetVector( "circleColor", circleColor );
        shader.SetInt( "texResolution", texResolution );
		
		shader.SetTexture( clearHandle, "Result", outputTexture );
        shader.SetTexture( circlesHandle, "Result", outputTexture );

        int stride = (2 + 2 + 1) * sizeof(float); // we have 2 vector2 and a float -> (2 + 2 + 1) * size of float 
        buffer = new ComputeBuffer(circleData.Length, stride);
        buffer.SetData(circleData); // set data in the buffer 
        shader.SetBuffer(circlesHandle, "circlesBuffer", buffer);
        
        rend.material.SetTexture("_MainTex", outputTexture);
    }
 
    private void DispatchKernels(int count)
    {
    	shader.Dispatch(clearHandle, texResolution/8, texResolution/8, 1);
        shader.SetFloat("time", Time.time);
        shader.Dispatch(circlesHandle, count, 1, 1);
    }

    void Update()
    {
        DispatchKernels(count);
    }
}

