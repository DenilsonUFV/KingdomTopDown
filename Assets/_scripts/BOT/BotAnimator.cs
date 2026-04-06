using UnityEngine;

[RequireComponent(typeof(BotMovement))]
public class BotAnimator : MonoBehaviour
{
    // ─────────────────────────────────────────
    #region Hashes

    private static readonly int VelocityX = Animator.StringToHash("VelocityX");
    private static readonly int VelocityY = Animator.StringToHash("VelocityY");
    private static readonly int IsMoving = Animator.StringToHash("IsMoving");
    private static readonly int ActionDirection = Animator.StringToHash("ActionDirection");
    private static readonly int IsBuildingHash = Animator.StringToHash("IsBuilding");
    private static readonly int DeathHash = Animator.StringToHash("Death");

    private const int DIR_DOWN = 0;
    private const int DIR_UP = 1;
    private const int DIR_SIDE = 2;

    #endregion

    // ─────────────────────────────────────────
    #region Referências

    private Animator _animator;
    private SpriteRenderer _sr;
    private BotMovement _movement;

    #endregion

    // ─────────────────────────────────────────
    #region Estado

    private Vector2 _lastDirection = Vector2.down;

    #endregion

    // ─────────────────────────────────────────
    #region Unity Callbacks

    private void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
        _sr = GetComponentInChildren<SpriteRenderer>();
        _movement = GetComponent<BotMovement>();
    }

    private void Update()
    {
        UpdateMovementAnimation();
    }

    #endregion

    // ─────────────────────────────────────────
    #region Animações

    private void UpdateMovementAnimation()
    {
        Vector2 input = _movement.MoveInput;
        bool isMoving = input.sqrMagnitude > 0.01f;

        _animator.SetBool(IsMoving, isMoving);

        if (isMoving)
        {
            _animator.SetFloat(VelocityX, Mathf.Abs(input.x));
            _animator.SetFloat(VelocityY, input.y);

            if (input.x != 0)
                _sr.flipX = input.x < 0;

            _lastDirection = input.normalized;
        }
    }

    public void PlayBuildAnimation(Vector3 targetPosition)
    {
        Debug.Log("PlayBuildAnimation");
        Vector2 dir = ((Vector2)targetPosition - (Vector2)transform.position).normalized;
        ApplyDirectionAndFlip(dir);
        _animator.SetBool(IsBuildingHash, true);
    }

    public void StopBuildAnimation()
    {
        Debug.Log("StopBuildAnimation");
        _animator.SetBool(IsBuildingHash, false);
    }

    public void PlayDeathAnimation()
    {
        _animator.SetTrigger(DeathHash);
    }

    private void ApplyDirectionAndFlip(Vector2 direction)
    {
        bool isVertical = Mathf.Abs(direction.y) >= Mathf.Abs(direction.x);

        if (isVertical)
        {
            _animator.SetInteger(ActionDirection, direction.y >= 0 ? DIR_UP : DIR_DOWN);
        }
        else
        {
            _animator.SetInteger(ActionDirection, DIR_SIDE);
            _sr.flipX = direction.x < 0;
        }

        _lastDirection = direction;
    }

    #endregion
}
