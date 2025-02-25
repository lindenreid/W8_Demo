using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Assertions;

public class Duck : MonoBehaviour
{
    private enum DuckState 
    {
        Wandering, Pursuing
    }

    [SerializeField] private LayerMask _lineOfSightLayers;
    [SerializeField] private float _wanderTimeMax = 5.0f;
    [SerializeField] private float _obstacleCheckDistance = 1.0f;
    [SerializeField] private float _obstacleCheckRadius = 1.0f;
    [SerializeField] private float _stopDistance = 0.5f;
    [SerializeField] private float _rotateSpeed;
    [SerializeField] private float _walkSpeed;
    [SerializeField] private float _lineOfSightMaxDistance;
    [SerializeField] private Vector3 _raycastStartOffset;
    [SerializeField] private Rigidbody _rigidBody;
    [SerializeField] private MeshRenderer _renderer;

    private string _playerTag = "Player";

    private DuckState _state;
    private float _wanderTime;
    private Vector3 _wanderDirection;

    // our position + raycast offset, in world space
    // _raycastStartOffset is used to make sure the raycast starts a little above the ground
    // TransformPoint is used to take the offset from local to world space
    private Vector3 _raycastStart {
        get {
            return transform.TransformPoint(_raycastStartOffset);
        }
    } 
    
    // a vector pointing from _raycastStart to the player's center
    private Vector3 _raycastDir {
        get {
            return (Player.Instance.PlayerCenter - _raycastStart).normalized;
        }
    }

    // variables used for drawing Gizmos
    private Vector3 _raycastHitLocation;
    private Vector3 _spherecastHitLocation;
    private bool _hasLineOfSightToPlayer;
    private Vector3 _meToPlayer;

    private void Update ()
    {
        UpdateState();
        RunState();
    }

    private void UpdateState ()
    {
        if(HasLineOfSightToPlayer())
        {
            _state = DuckState.Pursuing;
        }
        else 
        {
            _state = DuckState.Wandering;
        }
    }

    // suggested improvement: 
    // this state machine is curretly overkill because there's only 2 states
    // but if we were to want to implement state transitions,
    // like maybe if the duck wanted to finish turning before it started walking in a new direction,
    // then changing states would matter and this state machine would help with that!
    private void RunState ()
    {
        switch(_state) 
        {
            case DuckState.Wandering: RunWanderState(); break;
            case DuckState.Pursuing: RunPursueState(); break;
            default: Debug.LogError("unhandled state " + _state); break;
        }
    }

    private void RunWanderState ()
    {
        _renderer.material.color = Color.white;

        // switches to a new random direction every [_wanderTimeMax] seconds
        _wanderTime -= Time.deltaTime;
        if(_wanderTime <= 0.0f)
        {
            _wanderTime = _wanderTimeMax;
            GetNewWanderDirection();
        }

        // checks for obstacles, and gets a new direction if there are any
        // limit attempts per frame so we don't crash program if duck gets stuck
        int attempts = 0;
        while(HasCloseObstacles() && attempts < 3)
        {
            GetNewWanderDirection();
            attempts ++;
        }

        // actually rotate towards and move in wander direction
        RotateTowards(_wanderDirection);
        transform.Translate(_wanderDirection * _walkSpeed * Time.deltaTime, Space.World);
    }

    private void GetNewWanderDirection ()
    {
        // get a random 2d location inside a circle and treat it as a direction
        Vector3 randomDir = UnityEngine.Random.insideUnitCircle;
        _wanderDirection = new Vector3(randomDir.x, 0.0f, randomDir.y);
        _wanderDirection = _wanderDirection.normalized;
    }

    private bool HasCloseObstacles ()
    {
        // do a spherecast in the direction we want to move in
        // if we hit anything, we'll check a new direction
        RaycastHit hitInfo;
        bool hasObstacle = Physics.SphereCast(
            _raycastStart,
            _obstacleCheckRadius,
            _wanderDirection,
            out hitInfo,
            _obstacleCheckDistance
        );
        
        if(hasObstacle) 
        {
            _spherecastHitLocation = hitInfo.point;
        }

        return hasObstacle;
    }

    private void RunPursueState ()
    {
        _renderer.material.color = Color.red;

        // zero out y-axis because we only care about moving on x/z plane (ground)
        Vector3 playerPos = Player.Instance.transform.position;
        playerPos = new Vector3(playerPos.x, 0, playerPos.z);

        // get vector pointing from duck to target point
        Vector3 me = new Vector3(transform.position.x, 0, transform.position.z);
        _meToPlayer = (playerPos - me).normalized;

        RotateTowards(_meToPlayer);
        WalkTowards(playerPos);
    }

    private void RotateTowards(Vector3 direction)
    {
        Vector3 currentForward = new Vector3(transform.forward.x, 0, transform.forward.z);
        Vector3 newForward = Vector3.RotateTowards(currentForward, direction, _rotateSpeed * Time.deltaTime, 0.0f);
        transform.forward = newForward;
    }

    private void WalkTowards(Vector3 point)
    {
        Vector3 me = new Vector3(transform.position.x, 0, transform.position.z);

        if(Vector3.Distance(me, point) <= _stopDistance)
        {
            // exit early if i'm already close to player
            return;
        }

        // create a vector pointing from our position to the target position
        Vector3 meToTarget = point - me;
        meToTarget = meToTarget.normalized;

        // move in that direction
        transform.Translate(meToTarget * _walkSpeed * Time.deltaTime, Space.World);
    }

    private bool HasLineOfSightToPlayer ()
    {
        _hasLineOfSightToPlayer = false;
        RaycastHit hitInfo;
        // fire a raycast pointing from the duck (_raycastStart) in the direction of the player (_raycastDir)
        // and only going as far as _lineOfSightMaxDistance
        if(Physics.Raycast(_raycastStart, _raycastDir, out hitInfo, _lineOfSightMaxDistance, _lineOfSightLayers.value))
        {
            _raycastHitLocation = hitInfo.point;
            // check if the object we hit was actually the player
            if(hitInfo.collider.gameObject.tag.Equals(_playerTag))
            {
                _hasLineOfSightToPlayer = true;
            }
        }

        return _hasLineOfSightToPlayer;
    }

    private void OnDrawGizmos ()
    {
        // don't draw these gizmos unless game is running
        if(!Application.isPlaying) return;

        // draw player raycast stuff
        if(_hasLineOfSightToPlayer) {
            Gizmos.color = Color.green;
        } else {
            Gizmos.color = Color.red;
        }
        Gizmos.DrawRay(_raycastStart, _raycastDir * _lineOfSightMaxDistance);
        if(Player.Instance != null) Gizmos.DrawSphere(Player.Instance.PlayerCenter, 0.1f);
        Gizmos.DrawSphere(_raycastHitLocation, 0.1f);

        // draw direction we want to move in based on state we're in 
        if(_state == DuckState.Wandering)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position, _wanderDirection);

            // also visualize spherecast checking for obstacles
            Gizmos.DrawWireSphere(_raycastStart, _obstacleCheckRadius);
            Gizmos.DrawWireSphere(_raycastStart + _wanderDirection * _obstacleCheckDistance, _obstacleCheckRadius);

            // draw spherecast hit location
            Gizmos.DrawSphere(_spherecastHitLocation, 0.1f);
        }
        else if(_state == DuckState.Pursuing)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position, _meToPlayer);
        }
    }
}
