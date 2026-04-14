using UnityEngine;
using Pathfinding;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Seeker))]
public class BotMovement : MonoBehaviour
{
    // ─────────────────────────────────────────
    #region Configuração

    [Header("Movimento")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float arriveRadius = 0.3f;
    [SerializeField] private float nextWaypointDist = 0.5f;  // distância para avançar waypoint
    [SerializeField] private float repathInterval = 0.5f;   // recalcula path a cada X segundos

    #endregion

    // ─────────────────────────────────────────
    #region Estado

    private Rigidbody2D _rb;
    private Seeker _seeker;

    private Path _path;
    private int _waypointIndex = 0;
    private bool _isMoving = false;
    private Vector2 _targetPos;
    private float _repathTimer = 0f;

    public bool IsMoving => _isMoving;
    public bool HasArrived => Vector2.Distance(transform.position, _targetPos) <= arriveRadius;
    public Vector2 MoveInput => _currentVelocity.normalized;

    private Vector2 _currentVelocity;

    #endregion

    // ─────────────────────────────────────────
    #region Unity Callbacks

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _seeker = GetComponent<Seeker>();
        _rb.gravityScale = 0f;
        _rb.freezeRotation = true;
    }

    private void FixedUpdate()
    {
        if (!_isMoving) { _rb.linearVelocity = Vector2.zero; _currentVelocity = Vector2.zero; return; }
        if (HasArrived) { Stop(); return; }

        FollowPath();

        // Recalcula path periodicamente (desvia se obstáculo mudou)
        _repathTimer += Time.fixedDeltaTime;
        if (_repathTimer >= repathInterval)
        {
            _repathTimer = 0f;
            RequestPath(_targetPos);
        }
    }

    #endregion

    // ─────────────────────────────────────────
    #region Pathfinding

    public void MoveTo(Vector2 target)
    {
        _targetPos = target;
        _isMoving = true;
        _repathTimer = 0f;
        RequestPath(target);
    }

    private void RequestPath(Vector2 target)
    {
        if (_seeker.IsDone())
            _seeker.StartPath(transform.position, target, OnPathComplete);
    }

    private void OnPathComplete(Path p)
    {
        if (p.error) { Debug.LogWarning($"[BotMovement] Path error: {p.errorLog}"); return; }

        _path = p;
        _waypointIndex = 0;
    }

    private void FollowPath()
    {
        if (_path == null || _waypointIndex >= _path.vectorPath.Count)
        {
            _rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 waypoint = _path.vectorPath[_waypointIndex];
        Vector2 direction = (waypoint - (Vector2)transform.position).normalized;

        _currentVelocity = direction * moveSpeed;
        _rb.linearVelocity = _currentVelocity;

        // Avança para o próximo waypoint se chegou perto o suficiente
        if (Vector2.Distance(transform.position, waypoint) < nextWaypointDist)
            _waypointIndex++;
    }

    public void Stop()
    {
        _isMoving = false;
        _path = null;
        _rb.linearVelocity = Vector2.zero;
        _currentVelocity = Vector2.zero;
    }

    #endregion
}