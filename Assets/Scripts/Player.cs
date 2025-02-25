using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] private float _forwardSpeed = 1.0f;
    [SerializeField] private float _turnSpeed = 1.0f;
    [SerializeField] private TerrainCollider _groundCollider;
    [SerializeField] private float _maxGroundDistance = 2.0f;
    [SerializeField] private float _maxGroundAngleDeg = 45;
    [SerializeField] private Vector3 _slopeRaycastOrigin;
    [SerializeField] private Vector3 _playerCenter;

    public Vector3 PlayerCenter {
        get {
            return transform.TransformPoint(_playerCenter);
        }
    }

    private RaycastHit _raycastHit;
    private Vector3 _surfaceNormal;
    private Vector3 _surfaceLocation;

    public static Player Instance { get; private set; } // singleton stuff

    // singleton stuff
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    // movement
    private void Update ()
    {
        float translation = Input.GetAxis("Vertical") * _forwardSpeed * Time.deltaTime;
        float rotation = Input.GetAxis("Horizontal") * _turnSpeed * Time.deltaTime;

        bool isGrounded = false;

        // determine origin point for raycast
        _slopeRaycastOrigin = transform.position + new Vector3(0, 1, 0);
        Ray straightDownRay = new Ray(_slopeRaycastOrigin, Vector3.down);

        // after learning about coordinate spaces - comment out above 2 lines and uncomment this to improve raycast
        //Ray straightDownRay = new Ray(transform.position + _slopeRaycastOrigin, Vector3.down); // wrong
        //Ray straightDownRay = new Ray(transform.TransformPoint(_slopeRaycastOrigin), Vector3.down); // right
        
        // cast a ray into the scene
        // Collider.Raycast returns TRUE if it hits anything, and stores results in _raycastHit
        if(_groundCollider.Raycast(straightDownRay, out _raycastHit, _maxGroundDistance))
        {
            _surfaceLocation = _raycastHit.point; // used for gizmos only

            // get the surface normal at the point we hit
            _surfaceNormal = _raycastHit.normal;
            // check the angle between the surface and the up vector, which tells us the slope of the surface
            float slopeAngle = Vector3.Angle(_surfaceNormal, Vector3.up);
            //Debug.LogFormat("slope: {0} deg", slopeAngle);

            // only set grounded to true if the surface slope is small enough (steeper = larger angle)
            if(slopeAngle <= _maxGroundAngleDeg) {
                isGrounded = true;
            }
        }

        // won't allow player to move forward if they're (1) above the ground or (2) facing a steep slope
        if(isGrounded) {
            transform.Translate(0, 0, translation);
        }
        
        transform.Rotate(0, rotation, 0);
    }

    private void OnDrawGizmos ()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawRay(_surfaceLocation, _surfaceNormal);

        Gizmos.DrawSphere(_slopeRaycastOrigin, 0.1f);
        // after learning about coordinate spaces - uncomment and use instead of above line
        // Gizmos.DrawSphere(transform.position + _slopeRaycastOrigin, 0.1f); // wrong
        // Gizmos.DrawSphere(transform.TransformPoint(_slopeRaycastOrigin), 0.1f); // right
    }
}