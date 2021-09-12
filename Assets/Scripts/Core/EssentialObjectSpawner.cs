using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EssentialObjectSpawner : MonoBehaviour
{
    [SerializeField] GameObject essentialObjectsPrefab;

    void Awake()
    {
        var existingObjects = FindObjectsOfType<EssentialObjects>();
        //If there are not any essential objects in the scene, then instantiate
        if (existingObjects.Length == 0)
            Instantiate(essentialObjectsPrefab, new Vector3(0,0,0), Quaternion.identity);
    }
}
