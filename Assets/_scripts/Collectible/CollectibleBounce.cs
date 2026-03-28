using UnityEngine;

public class CollectibleBounce : MonoBehaviour
{
    // ─────────────────────────────────────────
    #region Configuração

    [Header("Força de Lançamento")]
    [SerializeField] private float minLaunchForce = 2f;
    [SerializeField] private float maxLaunchForce = 4f;
    [SerializeField] private float horizontalForce = 1.5f;

    [Header("Física Simulada")]
    [SerializeField] private float gravity = 9.8f;
    [SerializeField] private float bounceDamping = 0.55f;
    [SerializeField] private float bounceThreshold = 0.3f;
    [SerializeField] private float groundY = 0f;

    [Header("Sombra")]
    [SerializeField] private Transform shadowTransform;
    [SerializeField] private float shadowMinScale = 0.3f;
    [SerializeField] private float shadowMaxScale = 0.7f;
    [SerializeField] private float shadowMaxHeight = 3f;

    [Header("Referências")]
    [SerializeField] private Transform itemVisual;

    [Header("Obstáculos")]
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float collisionRadius = 0.2f;

    [Header("Efeito Imã")]
    [SerializeField] private float magnetRadius = 2f;
    [SerializeField] private float magnetForce = 8f;
    [SerializeField] private float magnetAcceleration = 3f;

    [Header("Separação entre Itens")]
    [SerializeField] private float separationRadius = 0.3f;
    [SerializeField] private float separationForce = 2f;
    [SerializeField] private LayerMask collectibleLayer;

    #endregion

    // ─────────────────────────────────────────
    #region Estado

    private float _currentHeight;
    private float _verticalVelocity;
    private Vector2 _horizontalVelocity;

    private bool _isBouncing = false;
    private bool _hasLanded = false;
    private bool _isMagneting = false;
    private int _bounceCount = 0;

    private float _currentMagnetSpeed = 0f; 
    private Transform _magnetTarget; // jogador alvo do imã atual

    private SpriteRenderer _shadowRenderer;
    private Collectible _collectible;

    private static readonly Collider2D[] _overlapBuffer = new Collider2D[16];

    #endregion

    // ─────────────────────────────────────────
    #region Unity Callbacks

    private void Awake()
    {
        _collectible = GetComponent<Collectible>();
        _shadowRenderer = shadowTransform?.GetComponent<SpriteRenderer>();

        // PlayerRef substitui FindGameObjectWithTag
        //_playerTransform = PlayerRef.Transform;
    }

    private void Start()
    {
        Launch();
    }

    private void OnDestroy()
    {
        CollectibleManager.Instance?.Unregister(this);
    }

    // Update REMOVIDO — Manager assume o controle ✅

    #endregion

    // ─────────────────────────────────────────
    #region Ticks — chamados pelo Manager

    public void TickBounce()
    {
        if (_isMagneting) return;
        SimulateArc();
    }

    public void TickVisuals()
    {
        UpdateVisuals();
    }

    public void TickLanded()
    {
        if (_isMagneting)
        {
            MagnetUpdate();
            return;
        }

        ApplySeparation();
        CheckMagnetRange();
    }

    #endregion

    // ─────────────────────────────────────────
    #region Launch

    private void Launch()
    {
        Collider2D overlap = Physics2D.OverlapCircle(
            transform.position,
            collisionRadius,
            obstacleLayer
        );

        if (overlap != null)
        {
            _isBouncing = false;
            _hasLanded = true;
            _currentHeight = groundY;
            CollectibleManager.Instance.RegisterLanded(this);
            return;
        }

        _verticalVelocity = Random.Range(minLaunchForce, maxLaunchForce);
        Vector2 randomDir = Random.insideUnitCircle.normalized;
        _horizontalVelocity = randomDir * horizontalForce;

        _currentHeight = 0f;
        _isBouncing = true;
        _hasLanded = false;
        _bounceCount = 0;

        CollectibleManager.Instance.RegisterBouncing(this);
    }

    #endregion

    // ─────────────────────────────────────────
    #region Simulação de Arco

    private void SimulateArc()
    {
        if (_horizontalVelocity.sqrMagnitude > 0.01f)
        {
            Vector2 currentPos = transform.position;
            Vector2 moveDir = _horizontalVelocity.normalized;
            float moveDist = _horizontalVelocity.magnitude * Time.deltaTime + collisionRadius;

            RaycastHit2D hit = Physics2D.CircleCast(
                currentPos,
                collisionRadius,
                moveDir,
                moveDist,
                obstacleLayer
            );

            if (hit.collider != null)
            {
                _horizontalVelocity = Vector2.Reflect(_horizontalVelocity, hit.normal)
                                      * bounceDamping;

                transform.position = (Vector3)hit.centroid +
                                     (Vector3)(hit.normal * (collisionRadius + 0.05f));
            }
        }

        transform.position += new Vector3(
            _horizontalVelocity.x,
            _horizontalVelocity.y,
            0f
        ) * Time.deltaTime;

        _verticalVelocity -= gravity * Time.deltaTime;
        _currentHeight += _verticalVelocity * Time.deltaTime;

        if (_currentHeight <= groundY)
        {
            _currentHeight = groundY;
            _horizontalVelocity *= 0.5f;

            if (Mathf.Abs(_verticalVelocity) > bounceThreshold)
            {
                _verticalVelocity = -_verticalVelocity * bounceDamping;
                _bounceCount++;
                OnBounce();
            }
            else
            {
                _verticalVelocity = 0f;
                _horizontalVelocity = Vector2.zero;
                _currentHeight = groundY;
                _isBouncing = false;
                OnLanded();
            }
        }
    }

    #endregion

    // ─────────────────────────────────────────
    #region Separação

    private void ApplySeparation()
    {
        int count = Physics2D.OverlapCircleNonAlloc(
            transform.position,
            separationRadius,
            _overlapBuffer,
            collectibleLayer
        );

        Vector2 totalPush = Vector2.zero;
        int pushCount = 0;

        for (int i = 0; i < count; i++)
        {
            Collider2D other = _overlapBuffer[i];
            if (other == null || other.gameObject == gameObject) continue;

            CollectibleBounce otherBounce = other.GetComponent<CollectibleBounce>();
            if (otherBounce == null || !otherBounce._hasLanded) continue;

            Vector2 direction = (Vector2)transform.position - (Vector2)other.transform.position;
            float distance = direction.magnitude;

            if (distance < 0.001f)
            {
                float angle = (GetInstanceID() % 360) * Mathf.Deg2Rad;
                direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                distance = 0.001f;
            }
            else
            {
                direction /= distance;
            }

            if (distance >= separationRadius) continue;

            float strength = (1f - (distance / separationRadius)) * separationForce;
            totalPush += direction * strength;
            pushCount++;
        }

        if (pushCount == 0) return;

        Vector2 displacement = totalPush * Time.deltaTime;
        Vector2 newPosition = (Vector2)transform.position + displacement;

        RaycastHit2D hit = Physics2D.CircleCast(
            transform.position,
            collisionRadius,
            displacement.normalized,
            displacement.magnitude,
            obstacleLayer
        );

        if (hit.collider != null)
        {
            Vector2 reflected = Vector2.Reflect(displacement, hit.normal);
            newPosition = (Vector2)transform.position + reflected * 0.5f;
        }

        transform.position = new Vector3(newPosition.x, newPosition.y, transform.position.z);
    }

    #endregion

    // ─────────────────────────────────────────
    #region Imã

    private void CheckMagnetRange()
    {
        // Busca o jogador mais próximo que PODE coletar este item
        PlayerController nearest = GetNearestEligiblePlayer();

        if (nearest != null)
            StartMagnet(nearest.transform);
    }

    private PlayerController GetNearestEligiblePlayer()
    {
        float bestDist = float.MaxValue;
        PlayerController best = null;

        foreach (PlayerController player in PlayerManager.Players)
        {
            if (player == null) continue;

            // Verifica se este jogador pode coletar (distância no raio)
            float dist = Vector2.Distance(transform.position, player.transform.position);
            if (dist > magnetRadius) continue;

            // Verifica elegibilidade via Collectible
            Collectible collectible = GetComponent<Collectible>();
            if (collectible != null && !collectible.CanPlayerCollect(player.gameObject)) continue;

            if (dist < bestDist)
            {
                bestDist = dist;
                best = player;
            }
        }

        return best;
    }

    private void StartMagnet(Transform target)
    {
        _magnetTarget = target;
        _isMagneting = true;
        _hasLanded = false;
        _currentMagnetSpeed = magnetForce * 0.5f;
        _currentHeight = 0.3f;
        _verticalVelocity = 0f;

        if (itemVisual != null)
            itemVisual.localPosition = new Vector3(0f, _currentHeight, 0f);
    }

    private void MagnetUpdate()
    {
        if (_magnetTarget == null) { _isMagneting = false; return; }

        _currentMagnetSpeed = Mathf.MoveTowards(
            _currentMagnetSpeed,
            magnetForce,
            magnetAcceleration * Time.deltaTime
        );

        transform.position = Vector2.MoveTowards(
            transform.position,
            _magnetTarget.position,
            _currentMagnetSpeed * Time.deltaTime
        );

        UpdateShadow();

        float dist = Vector2.Distance(transform.position, _magnetTarget.position);
        if (dist < 0.2f)
            _collectible.Collect(_magnetTarget.gameObject);
    }

    #endregion

    // ─────────────────────────────────────────
    #region Visuals

    private void UpdateVisuals()
    {
        if (itemVisual != null)
            itemVisual.localPosition = new Vector3(0f, _currentHeight, 0f);

        UpdateShadow();
    }

    private void UpdateShadow()
    {
        if (shadowTransform == null) return;

        float heightRatio = Mathf.Clamp01(_currentHeight / shadowMaxHeight);
        float scale = Mathf.Lerp(shadowMaxScale, shadowMinScale, heightRatio);
        float alpha = Mathf.Lerp(0.6f, 0.1f, heightRatio);

        shadowTransform.localScale = new Vector3(scale, scale, 1f);

        if (_shadowRenderer != null)
        {
            Color c = _shadowRenderer.color;
            _shadowRenderer.color = new Color(c.r, c.g, c.b, alpha);
        }
    }

    #endregion

    // ─────────────────────────────────────────
    #region Callbacks

    private void OnBounce()
    {
        //Debug.Log($"[Bounce] Quique #{_bounceCount}");
    }

    private void OnLanded()
    {
        _hasLanded = true;
        CollectibleManager.Instance.RegisterLanded(this);

        if (shadowTransform != null)
        {
            shadowTransform.localScale = new Vector3(shadowMaxScale, shadowMaxScale, 1f);
            if (_shadowRenderer != null)
            {
                Color c = _shadowRenderer.color;
                _shadowRenderer.color = new Color(c.r, c.g, c.b, 0.6f);
            }
        }
    }

    #endregion

    // ─────────────────────────────────────────
    #region Gizmos

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, magnetRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, collisionRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, separationRadius);
    }

    #endregion
}
