using UnityEngine;

public class Moneda : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // Comprueba si lo que tocó la moneda es el zorro
        if (other.CompareTag("Player") || other.GetComponent<PlayerMovimiento>() != null)
        {
            // Busca el Animator en el personaje (o en sus hijos si está en el modelo)
            Animator animator = other.GetComponent<Animator>();
            if (animator == null)
            {
                animator = other.GetComponentInChildren<Animator>();
            }

            // Activa el Trigger "ataque" en el Animator Controller
            if (animator != null)
            {
                animator.SetTrigger("ataque");
            }

            // Destruye la moneda
            Destroy(gameObject);
        }
    }
}