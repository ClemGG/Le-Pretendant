

using UnityEngine;

public class MeshDeformerInput : MonoBehaviour
{

    public float force = 10f;
    public float forceOffset = 0.1f;
    public bool automaticRaycast;

    void Update()
    {
        if (Input.GetMouseButton(0) || automaticRaycast)
        {
            HandleInput();
        }
    }


    void HandleInput()
    {
        Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(inputRay, out hit))
        {
            //Debug.Log("The ray hit " + hit.collider.name);
            MeshDeformer deformer = hit.collider.GetComponent<MeshDeformer>();
            if (deformer)
            {
                Vector3 point = hit.point;
                point += hit.normal * forceOffset;
                deformer.AddDeformingForce(point, force);
            }
        }
    }
}
