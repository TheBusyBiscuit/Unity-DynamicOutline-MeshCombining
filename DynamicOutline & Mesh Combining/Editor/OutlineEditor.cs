using UnityEngine;
using System.Collections;
using UnityEditor;
using System;

[CustomEditor(typeof(DynamicOutline))]
public class OutlineEditor : Editor {

    DynamicOutline m_target;

    public override void OnInspectorGUI()
    {
        m_target = (DynamicOutline) target;

        DrawDefaultInspector();
        
        EditorGUILayout.Separator();
        GUILayout.BeginHorizontal();

        GUILayout.Label("Neither of these Options currently support SkinnedMeshRenderers");

        GUILayout.EndHorizontal();

        EditorGUILayout.Separator();

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Combine Meshes", GUILayout.Height(22)))
        {

            GameObject copy = new GameObject();
            copy.name = "Combined Mesh of " + m_target.gameObject.name;
            copy.transform.position = m_target.transform.position;
            copy.transform.eulerAngles = m_target.transform.eulerAngles;
            copy.transform.localScale = new Vector3(1, 1, 1);

            copy.AddComponent<MeshFilter>();
            copy.GetComponent<MeshFilter>().mesh = CombineMeshes(copy, m_target.gameObject, false);
            copy.AddComponent<MeshRenderer>();
        }

        GUILayout.EndHorizontal();

        DrawOutlineInspector();
    }

    void DrawOutlineInspector()
    {
        EditorGUILayout.Separator();

        if (m_target.root == null)
        {
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Create Outline", GUILayout.Height(22)))
                Scan();

            GUILayout.EndHorizontal();
        }
        else
        {
            GUILayout.Label("Outline Settings", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();

            GUILayout.BeginHorizontal();

            bool visible = EditorGUILayout.Toggle("Visible", m_target.active);

            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();

            Color color = EditorGUILayout.ColorField("Color", m_target.material.GetColor("_OutlineColor"));

            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();

            float thickness = EditorGUILayout.Slider("Thickness", m_target.material.GetFloat("_Thickness"), 0.002F, 0.05F);

            GUILayout.EndHorizontal();

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(m_target, "Modify Outline");

                m_target.active = visible;
                m_target.root.SetActive(visible);
                m_target.material.SetColor("_OutlineColor", color);
                m_target.material.SetFloat("_Thickness", thickness);

                EditorUtility.SetDirty(m_target);
            }

            EditorGUILayout.Separator();
            EditorGUILayout.Separator();

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Rescan Mesh", GUILayout.Height(20)))
            {
                DestroyImmediate(m_target.root);

                Scan();
            }


            GUILayout.EndHorizontal();

            /*
            EditorGUILayout.Separator();

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Reset Material", GUILayout.Height(20)))
            {
                Material material = new Material(Shader.Find("DynamicOutline/Outline Only"));

                SaveToAssets(material, "Assets/DynamicOutline & Mesh Combining/materials/" + m_target.gameObject.name + ".mat");

                m_target.material = material;
                m_target.root.GetComponent<MeshRenderer>().materials = new Material[] {material};
            }

            GUILayout.EndHorizontal();
            */
        }
    }

    private void SaveToAssets(Material material, string path)
    {
        Material original = AssetDatabase.LoadMainAssetAtPath(path) as Material;
        if (original == null)
        {
            AssetDatabase.CreateAsset(material, path);
            AssetDatabase.SaveAssets();
        }
        else
        {
            EditorUtility.CopySerialized(material, original);
            AssetDatabase.SaveAssets();
        }
    }

    private void SaveToAssets(Mesh mesh, string path)
    {
        Mesh original = AssetDatabase.LoadMainAssetAtPath(path) as Mesh;
        if (original == null)
        {
            AssetDatabase.CreateAsset(mesh, path);
            AssetDatabase.SaveAssets();
        }
        else
        {
            EditorUtility.CopySerialized(mesh, original);
            AssetDatabase.SaveAssets();
        }
    }

    private void Scan()
    {
        GameObject copy = new GameObject();

        copy.name = m_target.gameObject.name + " Outline";
        
        Material material = m_target.material != null ? m_target.material: new Material(Shader.Find("DynamicOutline/Outline Only"));
        material.name = m_target.gameObject.name;
        SaveToAssets(material, "Assets/DynamicOutline & Mesh Combining/materials/" + m_target.gameObject.name + ".mat");
        m_target.material = material;

        copy.AddComponent<MeshFilter>();
        copy.GetComponent<MeshFilter>().mesh = CombineMeshes(copy, m_target.gameObject, true);
        copy.AddComponent<MeshRenderer>();
        copy.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        copy.GetComponent<MeshRenderer>().receiveShadows = false;
        copy.GetComponent<MeshRenderer>().materials = new Material[] { material };

        AddSkin(m_target.gameObject, copy);
        
        copy.transform.SetParent(m_target.transform);
        copy.transform.localPosition = new Vector3(0, 0, 0);
        copy.transform.localEulerAngles = new Vector3(0, 0, 0);
        copy.transform.localScale = new Vector3(1, 1, 1);

        m_target.root = copy;

        copy.SetActive(m_target.active);

        EditorUtility.SetDirty(m_target);
        EditorUtility.SetDirty(copy);
    }

    private Mesh CombineMeshes(GameObject copy, GameObject obj, bool outline)
    {
        Mesh mesh = new Mesh();
        mesh.name = "Combined Mesh (" + obj.name + ")";

        Vector3 position = new Vector3(obj.transform.position.x, obj.transform.position.y, obj.transform.position.z);

        obj.transform.position = new Vector3(0, 0, 0);

        MeshFilter[] filters = obj.GetComponentsInChildren<MeshFilter>();

        CombineInstance[] combine = new CombineInstance[filters.Length];

        for (int i = 0; i < filters.Length; i++)
        {
            if (filters[i].sharedMesh == null) continue;
            combine[i].mesh = filters[i].sharedMesh;
            combine[i].transform = filters[i].transform.localToWorldMatrix;
        }

        mesh.CombineMeshes(combine, true, true);

        SaveToAssets(mesh, "Assets/DynamicOutline & Mesh Combining/meshes/" + mesh.name + ".asset");

        AddSkins(obj.transform, copy);

        obj.transform.position = position;

        return mesh;
    }

    
    private void AddSkins(Transform transform, GameObject copy)
    {
        foreach (Transform child in transform)
        {
            SkinnedMeshRenderer skin = child.gameObject.GetComponent<SkinnedMeshRenderer>();
            if (skin != null)
            {
                GameObject grandchild = new GameObject();
                grandchild.name = "Skinned Mesh (" + child.gameObject.name + ")";
                grandchild.transform.SetParent(copy.transform);

                AddSkin(child.gameObject, grandchild);

                AddSkins(child, grandchild);
            }
            else
            {
                AddSkins(child, copy);
            }
        }
    }

    private bool AddSkin(GameObject original, GameObject copy) {
        SkinnedMeshRenderer skin = original.GetComponent<SkinnedMeshRenderer>();
        if (skin != null)
        {
            copy.AddComponent<SkinnedMeshRenderer>();
            EditorUtility.CopySerialized(skin, copy.GetComponent<SkinnedMeshRenderer>());

            Material[] materials = new Material[copy.GetComponent<SkinnedMeshRenderer>().materials.Length];

            for (int i = 0; i < materials.Length; i++)
            {
                materials[i] = m_target.material;
            }

            copy.GetComponent<SkinnedMeshRenderer>().materials = materials;

            return true;
        }
        else
        {
            return false;
        }
    }
    
}
