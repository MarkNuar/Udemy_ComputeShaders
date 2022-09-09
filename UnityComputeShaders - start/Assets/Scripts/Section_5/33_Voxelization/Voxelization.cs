using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Voxelization : MonoBehaviour
{
    
    // Start is called before the first frame update
    void Start()
    {
        VoxelizeMesh voxelizeMesh = GetComponent<VoxelizeMesh>();
        voxelizeMesh.Voxelize(voxelizeMesh.meshToVoxelize);

        float pS = voxelizeMesh.ParticleSize;
        Vector3 scale = new Vector3(pS, pS, pS);

        for (int i = 0; i < voxelizeMesh.PositionList.Count; i++)
        {
            Vector3 pos = voxelizeMesh.PositionList[i];
            GameObject particle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            particle.transform.position = pos;
            particle.transform.localScale = scale;
            particle.transform.parent = gameObject.transform;
        }
    }
}
