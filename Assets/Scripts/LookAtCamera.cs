using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookAtCamera : MonoBehaviour
{
    [SerializeField] private Camera _mainCamera;

    private void Update()
    {
        transform.LookAt(_mainCamera.transform.position);
        transform.Rotate(0, 180, 0);
    }
}
