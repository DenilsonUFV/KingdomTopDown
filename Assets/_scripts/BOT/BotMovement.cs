using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BotMovement : MonoBehaviour
{
    // ─────────────────────────────────────────
    #region Configuração

    [Header("Movimento")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float arriveRadius = 0.2f;

    #endregion

    // ─────────────────────────────────────────
    #region Estado

    private Rigidbody2D _rb;
    private Vector2 _targetPosition;
    private bool _isMoving = false;

    public bool IsMoving => _isMoving;
    public bool HasArrived => Vector2.Distance(transform.position, _targetPosition) <= arriveRadius;
    public Vector2 MoveInput => _isMoving
                                   ? ((Vector2)_targetPosition - (Vector2)transform.position).normalized
                                   : Vector2.zero;

    #endregion

    // ─────────────────────────────────────────
    #region Unity Callbacks

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale = 0f;
        _rb.freezeRotation = true;
    }

    private void FixedUpdate()
    {
        if (!_isMoving) { _rb.linearVelocity = Vector2.zero; return; }
        if (HasArrived) { Stop(); return; }

        Vector2 dir = ((Vector2)_targetPosition - (Vector2)transform.position).normalized;
        _rb.linearVelocity = dir * moveSpeed;
    }

    #endregion

    // ─────────────────────────────────────────
    #region API

    public void MoveTo(Vector2 target)
    {
        _targetPosition = target;
        _isMoving = true;
    }

    public void Stop()
    {
        _isMoving = false;
        _rb.linearVelocity = Vector2.zero;
    }

    #endregion
}
