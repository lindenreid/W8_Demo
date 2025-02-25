using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Assertions;

public class Duck : MonoBehaviour
{
    private enum DuckState 
    {
        Wandering, WalkingToPlayer
    }

    [SerializeField] private float _lineOfSightMaxDistance;
    [SerializeField] private Vector3 _raycastStartOffset;

    private string _playerTag = "Player";

    private DuckState _state;

    // gizmos stuff
    private Vector3 _raycastHitLocation;
    private Vector3 _raycastStart;
    private Vector3 _raycastDir;
    private bool _hasLineOfSightToPlayer;

    private void Update ()
    {
        UpdateState();
        RunState();
    }

    private void UpdateState ()
    {
        if(HasLineOfSightToPlayer())
        {
            _state = DuckState.WalkingToPlayer;
        }
        else 
        {
            _state = DuckState.Wandering;
        }
    }

    private void RunState ()
    {
        switch(_state) 
        {
            case DuckState.Wandering: RunWanderState(); break;
            case DuckState.WalkingToPlayer: RunWalkingToPlayerState(); break;
            default: Debug.LogError("unhandled state " + _state); break;
        }
    }

    private void RunWanderState ()
    {

    }

    private void RunWalkingToPlayerState ()
    {

    }

    private bool HasLineOfSightToPlayer ()
    {
        _raycastStart = transform.TransformPoint(_raycastStartOffset);
        _raycastDir = (Player.Instance.PlayerCenter - _raycastStart).normalized;

        _hasLineOfSightToPlayer = false;
        RaycastHit hitInfo;
        if(Physics.Raycast(_raycastStart, _raycastDir, out hitInfo, _lineOfSightMaxDistance))
        {
            _raycastHitLocation = hitInfo.point;
            if(hitInfo.collider.gameObject.tag.Equals(_playerTag))
            {
                _hasLineOfSightToPlayer = true;
            }
        }

        return _hasLineOfSightToPlayer;
    }

    private void OnDrawGizmos ()
    {
        if(_hasLineOfSightToPlayer) {
            Gizmos.color = Color.green;
        } else {
            Gizmos.color = Color.red;
        }

        Gizmos.DrawRay(_raycastStart, _raycastDir * _lineOfSightMaxDistance);
        if(Player.Instance != null) Gizmos.DrawSphere(Player.Instance.PlayerCenter, 0.1f);
        Gizmos.DrawSphere(_raycastHitLocation, 0.1f);
    }
}
