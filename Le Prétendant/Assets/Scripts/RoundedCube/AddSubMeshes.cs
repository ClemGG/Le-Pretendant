using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter))]
public class AddSubMeshes : MonoBehaviour {

    [SerializeField] int subMeshCount = 1;

	// Use this for initialization
	void OnEnable () {
        GetComponent<MeshFilter>().sharedMesh.subMeshCount = subMeshCount;

    }

    // Update is called once per frame
    void Update () {
        GetComponent<MeshFilter>().sharedMesh.subMeshCount = subMeshCount;

    }
}
