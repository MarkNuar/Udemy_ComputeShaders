using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class GPUPhysics : MonoBehaviour {
	struct RigidBody
    {
	    // properties of the whole rigidbody 
		public Vector3 position;
		public Quaternion quaternion;
		public Vector3 velocity;
		public Vector3 angularVelocity;
		public int particleIndex; // first index of the particle in the particle buffer 
		public int particleCount; // the next particles in the buffer, that belongs to the rigidbody 

		public RigidBody(Vector3 pos, int pIndex, int pCount)
        {
			position = pos;
			quaternion = Random.rotation;//Quaternion.identity;
			velocity = angularVelocity = Vector3.zero;
			particleIndex = pIndex;
			particleCount = pCount;
        }
    };

	int SIZE_RIGIDBODY = 13 * sizeof(float) + 2 * sizeof(int);

	struct Particle
    {
		public Vector3 position; 
		public Vector3 velocity;
		public Vector3 force; // forces on the particle 
		public Vector3 localPosition; // position wrt parent rigidbody 
		public Vector3 offsetPosition; // position rotate by parent rigidbody quaternion 

		public Particle(Vector3 pos)
        {
			position = velocity = force = offsetPosition = Vector3.zero;
			localPosition = pos;
        }
    };

	int SIZE_PARTICLE = 15 * sizeof(float);

	public Mesh cubeMesh {
		get {
			return CjLib.PrimitiveMeshFactory.BoxFlatShaded();
		}
	}

	public ComputeShader shader;
	public Material cubeMaterial;
	public Bounds bounds;
	public float cubeMass;
	public float scale;
	public int particlesPerEdge;
	public float springCoefficient;
	public float dampingCoefficient;
	public float tangentialCoefficient;
	public float gravityCoefficient;
	public float frictionCoefficient;
	public float angularFrictionCoefficient;
	public float angularForceScalar;
	public float linearForceScalar;
	public int rigidBodyCount = 1000;
	[Range(1, 20)]
	public int stepsPerUpdate = 10;
	
	// calculated
	private Vector3 cubeScale;
	
	int particlesPerBody;
	float particleDiameter;

	RigidBody[] rigidBodiesArray;
	Particle[] particlesArray;
	uint[] argsArray;
	
	ComputeBuffer rigidBodiesBuffer;
	ComputeBuffer particlesBuffer;
	private ComputeBuffer argsBuffer;
	private ComputeBuffer voxelGridBuffer;               
	
	private int kernelGenerateParticleValues;
	private int kernelCollisionDetection;
	private int kernelComputeMomenta;
	private int kernelComputePositionAndRotation;
	
	private int groupsPerRigidBody;
	private int groupsPerParticle;

	private int deltaTimeID;

	int activeCount = 0;

	private int frameCounter;

	void Start() {

		InitArrays();

		InitRigidBodies();

		InitParticles();

		InitBuffers();

		InitShader();
		
		InitInstancing();
		
	}

	void InitArrays()
	{
		particlesPerBody = particlesPerEdge * particlesPerEdge * particlesPerEdge;

		rigidBodiesArray = new RigidBody[rigidBodyCount];
		particlesArray = new Particle[rigidBodyCount * particlesPerBody];
		argsArray = new uint[] { 0, 0, 0, 0, 0 };
	}

	void InitRigidBodies()
	{
		int pIndex = 0;

		for (int i = 0; i < rigidBodyCount; i++)
		{
			Vector3 pos = Random.insideUnitSphere * 5.0f;
			pos.y += 15;
			rigidBodiesArray[i] = new RigidBody(pos, pIndex, particlesPerBody);
			pIndex += particlesPerBody;
		}
	}

	void InitParticles()
	{
		particleDiameter = scale / particlesPerEdge;

		int index = 0;
		
		float centerer = (particleDiameter - scale) * 0.5f;
		Vector3 offset = new Vector3(centerer, centerer, centerer);

		for (int x = 0; x < particlesPerEdge; x++)
		{
			for (int y = 0; y < particlesPerEdge; y++)
			{
				for (int z = 0; z < particlesPerEdge; z++)
				{
					Vector3 pos = offset + new Vector3(x, y, z) * particleDiameter;

					for (int i = 0; i < rigidBodyCount; i++)
					{
						RigidBody body = rigidBodiesArray[i];
						particlesArray[body.particleIndex + index] = new Particle(pos);
					}

					index++;
				}
			}
		}
	}

	void InitBuffers()
	{
		rigidBodiesBuffer = new ComputeBuffer(rigidBodyCount, SIZE_RIGIDBODY);
		rigidBodiesBuffer.SetData(rigidBodiesArray);

		int numOfParticles = rigidBodyCount * particlesPerBody;
		particlesBuffer = new ComputeBuffer(numOfParticles, SIZE_PARTICLE);
		particlesBuffer.SetData(particlesArray);
	}

	void InitShader()
	{
		deltaTimeID = Shader.PropertyToID("deltaTime");
		
		shader.SetInt("particlesPerRigidBody", particlesPerBody);
		shader.SetFloat("particleDiameter", particleDiameter);
		shader.SetFloat("springCoefficient", springCoefficient);
		shader.SetFloat("dampingCoefficient", dampingCoefficient);
		shader.SetFloat("frictionCoefficient", frictionCoefficient);
		shader.SetFloat("angularFrictionCoefficient", angularFrictionCoefficient);
		shader.SetFloat("gravityCoefficient", gravityCoefficient);
		shader.SetFloat("tangentialCoefficient", tangentialCoefficient);
		shader.SetFloat("angularForceScalar", angularForceScalar);
		shader.SetFloat("linearForceScalar", linearForceScalar);
		shader.SetFloat("particleMass", cubeMass/particlesPerBody);
		int particleCount = rigidBodyCount * particlesPerBody;
		shader.SetInt("particleCount", particleCount);


		kernelGenerateParticleValues = shader.FindKernel("GenerateParticleValues");
		kernelCollisionDetection = shader.FindKernel("CollisionDetection");
		kernelComputeMomenta = shader.FindKernel("ComputeMomenta");
		kernelComputePositionAndRotation = shader.FindKernel("ComputePositionAndRotation");

		groupsPerRigidBody = Mathf.CeilToInt(rigidBodyCount / 8.0f);
		groupsPerParticle = Mathf.CeilToInt(particleCount / 8.0f);
		
		shader.SetBuffer(kernelGenerateParticleValues, "rigidBodiesBuffer", rigidBodiesBuffer);
		shader.SetBuffer(kernelGenerateParticleValues, "particlesBuffer", particlesBuffer);
		shader.SetBuffer(kernelCollisionDetection, "particlesBuffer", particlesBuffer);
		shader.SetBuffer(kernelComputeMomenta, "rigidBodiesBuffer", rigidBodiesBuffer);
		shader.SetBuffer(kernelComputeMomenta, "particlesBuffer", particlesBuffer);
		shader.SetBuffer(kernelComputePositionAndRotation, "rigidBodiesBuffer", rigidBodiesBuffer);
	}

	void InitInstancing() {
		cubeMaterial.SetBuffer("rigidBodiesBuffer", rigidBodiesBuffer);

		argsArray[0] = cubeMesh.GetIndexCount(0);
		argsBuffer = new ComputeBuffer(1, argsArray.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
		argsBuffer.SetData(argsArray);
	}

	void Update()
	{
		if (activeCount < rigidBodyCount && frameCounter++ > 5)
		{
			activeCount++;
			frameCounter = 0;
			shader.SetInt("activeCount", activeCount);
			argsArray[1] = (uint)activeCount;

			argsBuffer.SetData(argsArray);
		}

		float dt = Time.deltaTime / stepsPerUpdate;
		shader.SetFloat(deltaTimeID, dt);

		for (int i = 0; i < stepsPerUpdate; i++)
		{
			shader.Dispatch(kernelGenerateParticleValues, groupsPerRigidBody, 1,1);
			shader.Dispatch(kernelCollisionDetection, groupsPerParticle, 1,1);
			shader.Dispatch(kernelComputeMomenta, groupsPerRigidBody, 1, 1);
			shader.Dispatch(kernelComputePositionAndRotation, groupsPerRigidBody, 1,1);
		}

	Graphics.DrawMeshInstancedIndirect(cubeMesh, 0, cubeMaterial, bounds, argsBuffer);
	}

	void OnDestroy() {
		rigidBodiesBuffer.Release();
		particlesBuffer.Release();

		if (argsBuffer != null) {
			argsBuffer.Release();
		}
	}
}