using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class PlayerMovimiento : MonoBehaviour
{
    public float velocidad = 8f;
    public float fuerzaSalto = 7f;
    public float velocidadRotacion = 10f;
    [Min(1f)] public float multiplicadorCarrera = 1.7f;
    public float offsetOrigenRayo = 0.5f;
    public float distanciaSuelo = 0.25f;
    public LayerMask capasDeSuelo = ~0;
    public Animator animator;

    public bool PuedeMoverse { get; set; } = true;
    public bool EnSuelo => isGrounded;
    public Rigidbody Cuerpo => rbd;

    private readonly RaycastHit[] resultadosSuelo = new RaycastHit[8];
    private Rigidbody rbd;
    private CapsuleCollider capsula;
    private Vector2 entrada;
    private Vector2 mezclaAnimacion;
    private bool saltoSolicitado;
    [SerializeField] private bool isGrounded;

    private void Awake()
    {
        rbd = GetComponent<Rigidbody>();
        capsula = GetComponent<CapsuleCollider>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (animator != null) animator.applyRootMotion = false;
    }

    private void Update()
    {
        entrada = PuedeMoverse
            ? Vector2.ClampMagnitude(new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")), 1f)
            : Vector2.zero;
        if (PuedeMoverse && Input.GetKeyDown(KeyCode.Space)) saltoSolicitado = true;
        ActualizarAnimacion();
    }

    private void FixedUpdate()
    {
        isGrounded = ComprobarSuelo();
        Vector3 direccion = new Vector3(entrada.x, 0f, entrada.y);
        bool corriendo = PuedeMoverse && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
        Vector3 vertical = Vector3.Project(rbd.linearVelocity, Vector3.up);
        rbd.linearVelocity = direccion * velocidad * (corriendo ? multiplicadorCarrera : 1f) + vertical;

        if (direccion.sqrMagnitude > 0.01f)
        {
            Quaternion destino = Quaternion.LookRotation(direccion, Vector3.up);
            rbd.MoveRotation(Quaternion.Slerp(rbd.rotation, destino, velocidadRotacion * Time.fixedDeltaTime));
        }

        if (saltoSolicitado && isGrounded)
        {
            rbd.AddForce(Vector3.up * fuerzaSalto, ForceMode.VelocityChange);
            isGrounded = false;
            if (animator != null) animator.SetTrigger("fuerzaSalto");
        }
        saltoSolicitado = false;
    }

    private bool ComprobarSuelo()
    {
        Bounds b = capsula.bounds;
        float radio = Mathf.Max(0.05f, Mathf.Min(b.extents.x, b.extents.z) * 0.82f);
        float distancia = b.extents.y + Mathf.Max(0.05f, distanciaSuelo);
        int cantidad = Physics.SphereCastNonAlloc(b.center, radio, Vector3.down, resultadosSuelo, distancia, capasDeSuelo, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < cantidad; i++)
        {
            Collider c = resultadosSuelo[i].collider;
            if (c != null && c.attachedRigidbody != rbd && !c.transform.IsChildOf(transform)) return true;
        }
        return false;
    }

    private void ActualizarAnimacion()
    {
        if (animator == null) return;
        bool corriendo = PuedeMoverse && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
        Vector3 mundo = new Vector3(entrada.x, 0f, entrada.y);
        Vector3 local = transform.InverseTransformDirection(mundo);
        Vector2 objetivo = new Vector2(local.x, local.z) * (corriendo ? 2f : 1f);
        mezclaAnimacion = Vector2.MoveTowards(mezclaAnimacion, objetivo, 5f * Time.deltaTime);
        animator.SetFloat("xSpeed", mezclaAnimacion.x, 0.12f, Time.deltaTime);
        animator.SetFloat("ySpeed", mezclaAnimacion.y, 0.12f, Time.deltaTime);
        animator.SetBool("enSuelo", isGrounded);
        animator.SetFloat("velocidadVertical", rbd.linearVelocity.y);
    }

    public void DetenerMovimiento()
    {
        entrada = Vector2.zero;
        saltoSolicitado = false;
        if (rbd != null) rbd.linearVelocity = Vector3.zero;
    }

    public void Celebrar()
    {
        DetenerMovimiento();
        if (animator == null) return;
        animator.ResetTrigger("ataque");
        animator.SetBool("enSuelo", true);
        animator.SetFloat("velocidadVertical", 0f);
        animator.SetTrigger("fuerzaSalto");
    }
}
