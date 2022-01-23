using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class fixRotatedModel : MonoBehaviour
{
    [ContextMenu("Fix Shit")]
    public void correct()
    {
        // save the parent GO-s pos+rot
        Vector3 position = transform.position;
        Quaternion rotation = transform.rotation;

        // move to the origin for combining
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;

        int children = transform.childCount;
        for (int lod = 0; lod < children; lod++)
        {
            List<MeshFilter> filters = new List<MeshFilter>();
            List<Matrix4x4> transforms = new List<Matrix4x4>();
            foreach (MeshFilter filt in transform.GetChild(lod).GetComponentsInChildren<MeshFilter>())
            {

                filters.Add(filt);
                transforms.Add(filt.transform.localToWorldMatrix);
            }
            MeshFilter[] meshFilters = filters.ToArray();
            CombineInstance[] combine = new CombineInstance[meshFilters.Length];
            int c = 0;
            while (c < meshFilters.Length)
            {
                combine[c].mesh = meshFilters[c].sharedMesh;
                combine[c].transform = transforms[c];

                c++;
            }

            Mesh lodMesh = new Mesh();
            lodMesh.CombineMeshes(combine);
            GameObject bruh = new GameObject("test");
            bruh.AddComponent<MeshFilter>();
            bruh.AddComponent<MeshRenderer>();
            bruh.GetComponent<MeshFilter>().mesh = lodMesh;
        }


        // restore the parent GO-s pos+rot
        transform.position = position;
        transform.rotation = rotation;
    }
    [ContextMenu("Fix Shit New")]
    public void correctNew()
    {
        // save the parent GO-s pos+rot
        Vector3 position = transform.position;
        Quaternion rotation = transform.rotation;

        // move to the origin for combining
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;

        int children = transform.childCount;
        for (int lod = 0; lod < children; lod++)
        {
            MeshFilter[] filters = transform.GetChild(lod).GetComponentsInChildren<MeshFilter>();
            List<CombineInstance> combine = new List<CombineInstance>();

            for (int i = 0; i < filters.Length; i++)
            {
                // skip the empty parent GO
                if (filters[i].sharedMesh == null)
                    continue;

                // combine submeshes
                for (int j = 0; j < filters[i].sharedMesh.subMeshCount; j++)
                {
                    CombineInstance ci = new CombineInstance();

                    ci.mesh = filters[i].sharedMesh;
                    ci.subMeshIndex = j;
                    ci.transform = filters[i].transform.localToWorldMatrix;

                    combine.Add(ci);
                }
            }
            Mesh lodMesh = new Mesh();
            lodMesh.CombineMeshes(combine.ToArray(), true, true);


            GameObject bruh = new GameObject("test");
            bruh.AddComponent<MeshFilter>();
            bruh.AddComponent<MeshRenderer>();
            bruh.GetComponent<MeshFilter>().mesh = lodMesh;
        }

        // restore the parent GO-s pos+rot
        transform.position = position;
        transform.rotation = rotation;
    }
}
