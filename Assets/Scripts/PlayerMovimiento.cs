using UnityEngine;

public class PlayerMovimiento : MonoBehaviour
{
    public float velocidad = 5f;
    public float fuerzaSalto = 7f;
    public float velocidadRotacion = 12f;

    public float offsetOrigenRayo = 0.5f;
    public float distanciaSuelo = 0.8f;

    public Animator animator;

    private Rigidbody rbd;
    private Vector2 posAnimacionActual = Vector2.zero;
    [SerializeField] private bool isGrounded;

    void Start()
    {
        rbd = GetComponent<Rigidbody>();
       // if (animator == null) animator = GetComponent<Animator>();
    }

    void Update()
    {
        Vector3 origenRayo = transform.position + Vector3.up * offsetOrigenRayo;
        isGrounded = Physics.Raycast(origenRayo, Vector3.down, distanciaSuelo);
        Debug.DrawRay(origenRayo, Vector3.down * distanciaSuelo, isGrounded ? Color.green : Color.red);

        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        Vector3 inputDir = new Vector3(x, 0f, z).normalized;

        if (inputDir.magnitude >= 0.1f)
        {
            Quaternion rotacionObjetivo = Quaternion.LookRotation(inputDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotacionObjetivo, velocidadRotacion * Time.deltaTime);

            Vector3 velActual = rbd.linearVelocity;
            Vector3 velDeseada = inputDir * velocidad;
            rbd.linearVelocity = new Vector3(velDeseada.x, velActual.y, velDeseada.z);

            posAnimacionActual = Vector2.MoveTowards(posAnimacionActual, new Vector2(0f, 1f), 6f * Time.deltaTime);
        }
        else
        {
            Vector3 velActual = rbd.linearVelocity;
            rbd.linearVelocity = new Vector3(0f, velActual.y, 0f);

            posAnimacionActual = Vector2.MoveTowards(posAnimacionActual, Vector2.zero, 6f * Time.deltaTime);
        }

        if (animator != null)
        {
            animator.SetFloat("xSpeed", posAnimacionActual.x);
            animator.SetFloat("ySpeed", posAnimacionActual.y);
        }

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rbd.AddForce(Vector3.up * fuerzaSalto, ForceMode.VelocityChange);

            if (animator != null)
            {
                animator.SetTrigger("fuerzaSalto");
            }
        }
    }
}