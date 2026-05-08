using UnityEngine;

/// <summary>
/// Controla as animações dos inimigos (melee e ranged).
/// Lê o movimento de EnemyMovement e recebe chamadas diretas do EnemyBrain.
///
/// Parâmetros esperados no Animator Controller:
///   - IsMoving        (bool)    — caminhando ou parado
///   - VelocityX       (float)   — |velocidade horizontal| (para blend tree)
///   - VelocityY       (float)   — velocidade vertical (para blend tree)
///   - ActionDirection (int)     — 0=Down, 1=Up, 2=Side (direção do ataque)
///   - Attack          (trigger) — disparado a cada ataque
///   - Death           (trigger) — disparado ao morrer
/// </summary>
[RequireComponent(typeof(EnemyMovement))]
public class EnemyAnimator : MonoBehaviour
{
    // ─────────────────────────────────────────
    #region Hashes

    private static readonly int VelocityX      = Animator.StringToHash("VelocityX");
    private static readonly int VelocityY      = Animator.StringToHash("VelocityY");
    private static readonly int IsMovingHash   = Animator.StringToHash("IsMoving");
    private static readonly int ActionDir      = Animator.StringToHash("ActionDirection");
    private static readonly int AttackTrigger  = Animator.StringToHash("Attack");
    private static readonly int DeathTrigger   = Animator.StringToHash("Death");

    private const int DIR_DOWN = 0;
    private const int DIR_UP   = 1;
    private const int DIR_SIDE = 2;

    #endregion

    // ─────────────────────────────────────────
    #region Referências

    private Animator       _animator;
    private SpriteRenderer _sr;
    private EnemyMovement  _movement;

    #endregion

    // ─────────────────────────────────────────
    #region Estado

    private Vector2 _lastDirection = Vector2.down;

    #endregion

    // ─────────────────────────────────────────
    #region Unity Callbacks

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _sr       = GetComponent<SpriteRenderer>();
        _movement = GetComponent<EnemyMovement>();
    }

    private void Update()
    {
        UpdateMovementAnimation();
    }

    #endregion

    // ─────────────────────────────────────────
    #region Animação de Movimento

    private void UpdateMovementAnimation()
    {
        Vector2 input    = _movement.MoveInput;
        bool    isMoving = input.sqrMagnitude > 0.01f;

        _animator.SetBool(IsMovingHash, isMoving);

        if (isMoving)
        {
            _animator.SetFloat(VelocityX, Mathf.Abs(input.x));
            _animator.SetFloat(VelocityY, input.y);

            if (Mathf.Abs(input.x) > 0.01f)
                _sr.flipX = input.x < 0;

            _lastDirection = input.normalized;
        }
    }

    #endregion

    // ─────────────────────────────────────────
    #region API — chamada pelo EnemyBrain

    /// <summary>
    /// Dispara o trigger de ataque e orienta o sprite para o alvo.
    /// Chamado pelo Brain imediatamente antes de PerformAttack.
    /// </summary>
    public void PlayAttackAnimation(Vector3 targetPosition)
    {
        Vector2 dir = ((Vector2)targetPosition - (Vector2)transform.position).normalized;
        ApplyDirectionAndFlip(dir);
        _animator.SetTrigger(AttackTrigger);
    }

    /// <summary>Dispara o trigger de morte.</summary>
    public void PlayDeathAnimation()
    {
        _animator.SetTrigger(DeathTrigger);
    }

    #endregion

    // ─────────────────────────────────────────
    #region Helpers

    private void ApplyDirectionAndFlip(Vector2 direction)
    {
        bool isVertical = Mathf.Abs(direction.y) >= Mathf.Abs(direction.x);

        if (isVertical)
        {
            _animator.SetInteger(ActionDir, direction.y >= 0 ? DIR_UP : DIR_DOWN);
        }
        else
        {
            _animator.SetInteger(ActionDir, DIR_SIDE);
            _sr.flipX = direction.x < 0;
        }

        _lastDirection = direction;
    }

    #endregion
}
