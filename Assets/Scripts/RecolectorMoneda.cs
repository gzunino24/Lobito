using UnityEngine;

[DisallowMultipleComponent]
public class RecolectorMoneda : MonoBehaviour
{
    public AudioClip sonidoRecoleccion;
    private bool recogida;

    private void OnTriggerEnter(Collider other)
    {
        if (recogida) return;
        if (NivelMonedas.Instancia != null && !NivelMonedas.Instancia.PuedeRecoger) return;

        PlayerMovimiento jugador = other.GetComponentInParent<PlayerMovimiento>();
        if (jugador == null && other.attachedRigidbody != null)
            jugador = other.attachedRigidbody.GetComponent<PlayerMovimiento>();
        if (jugador == null && !other.CompareTag("Player")) return;

        recogida = true;
        Collider trigger = GetComponent<Collider>();
        if (trigger != null) trigger.enabled = false;

        Animator animatorJugador = jugador != null ? jugador.animator : other.GetComponentInChildren<Animator>();
        if (animatorJugador != null) animatorJugador.SetTrigger("ataque");

        CrearEfecto();
        if (sonidoRecoleccion != null) AudioSource.PlayClipAtPoint(sonidoRecoleccion, transform.position, 0.65f);
        if (NivelMonedas.Instancia != null) NivelMonedas.Instancia.RegistrarMoneda();
        Destroy(gameObject);
    }

    private void CrearEfecto()
    {
        GameObject efecto = new GameObject("Efecto moneda");
        efecto.transform.position = transform.position + Vector3.up;
        ParticleSystem particulas = efecto.AddComponent<ParticleSystem>();
        particulas.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = particulas.main;
        main.duration = 0.45f;
        main.startLifetime = 0.65f;
        main.startSpeed = 3f;
        main.startSize = 0.35f;
        main.startColor = new Color(1f, 0.75f, 0.15f);
        main.loop = false;
        var emission = particulas.emission;
        emission.rateOverTime = 0f;
        particulas.Emit(18);
        Destroy(efecto, 1.5f);
    }
}
