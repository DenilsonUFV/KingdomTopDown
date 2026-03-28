using System;
using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    // ─────────────────────────────────────────
    #region Hashes

    private static readonly int VelocityX = Animator.StringToHash("VelocityX");
    private static readonly int VelocityY = Animator.StringToHash("VelocityY");
    private static readonly int IsMoving = Animator.StringToHash("IsMoving");
    private static readonly int ActionDirection = Animator.StringToHash("ActionDirection");

    private static readonly int ChopHash = Animator.StringToHash("Chop");
    private static readonly int MineHash = Animator.StringToHash("Mine");
    private static readonly int FishHash = Animator.StringToHash("Fish");
    private static readonly int InteractHash = Animator.StringToHash("Interact");

    private const int DIR_DOWN = 0;
    private const int DIR_UP = 1;
    private const int DIR_SIDE = 2;

    #endregion

    // ─────────────────────────────────────────
    #region Referências

    private Animator _animator;
    private SpriteRenderer _spriteRenderer;
    private PlayerMovement _playerMovement;

    #endregion

    // ─────────────────────────────────────────
    #region Estado

    private Vector2 _lastDirection = Vector2.down;

    // Evento disparado quando animação de ação termina
    public event Action OnActionAnimationEnd;

    #endregion

    // ─────────────────────────────────────────
    #region Unity Callbacks

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _playerMovement = GetComponentInParent<PlayerMovement>();
    }

    private void Update()
    {
        UpdateMovementAnimation();
    }

    #endregion

    // ─────────────────────────────────────────
    #region Movement Animation

    private void UpdateMovementAnimation()
    {
        Vector2 velocity = _playerMovement.MoveInput;
        bool isMoving = velocity.sqrMagnitude > 0.01f;

        _animator.SetBool(IsMoving, isMoving);

        if (isMoving)
        {
            _animator.SetFloat(VelocityX, Mathf.Abs(velocity.x));
            _animator.SetFloat(VelocityY, velocity.y);

            if (velocity.x != 0)
                _spriteRenderer.flipX = velocity.x < 0;

            _lastDirection = velocity.normalized;
        }
    }

    #endregion

    // ─────────────────────────────────────────
    #region Action Animation

    public void PlayActionAnimation(ToolType tool, Vector3 targetPosition)
    {
        Debug.Log("AQUIIIIIIIIIIII  "+GetTriggerHash(tool));
        Vector2 dirToTarget = ((Vector2)targetPosition - (Vector2)transform.position).normalized;
        ApplyDirectionAndFlip(dirToTarget);
        _animator.SetTrigger(GetTriggerHash(tool));
    }

    public void PlayActionAnimation(ToolType tool)
    {
        ApplyDirectionAndFlip(_lastDirection);
        _animator.SetTrigger(GetTriggerHash(tool));
    }

    private int GetTriggerHash(ToolType tool) => tool switch
    {
        ToolType.Axe => ChopHash,
        ToolType.Pickaxe => MineHash,
        ToolType.FishingRod => FishHash,
        _ => InteractHash
    };

    /// <summary>
    /// Chamado via Animation Event no último frame de cada animação de ação.
    /// Notifica que a animação terminou para liberar o movimento.
    /// </summary>
    public void OnActionEnd()
    {
        OnActionAnimationEnd?.Invoke();
    }

    #endregion

    // ─────────────────────────────────────────
    #region Direction Helper

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
            _spriteRenderer.flipX = direction.x < 0;
        }

        _lastDirection = direction;
    }

    #endregion
}
