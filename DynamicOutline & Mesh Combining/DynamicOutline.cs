using UnityEngine;
using System.Collections;

public class DynamicOutline : MonoBehaviour {

    [HideInInspector]
    public bool active = false;

    [HideInInspector]
    public GameObject root = null;

    [HideInInspector]
    public Material material = null;

    public void ShowOutline(bool show)
    {
        this.active = show;
        this.root.SetActive(show);
    }
}
