using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class ActivadorCamaraPlataforma : MonoBehaviour
{
    public CinemachineCamera camaraPlataforma;
    public CinemachineCamera camaraSeguimiento;
    public float duracion = 3f;
    private bool usada;
    private Coroutine retorno;
    private Vector3 posicionPlataforma;
    private Quaternion rotacionPlataforma;
    private PlayerMovimiento jugadorEnToma;
    private bool movimientoPrevio;
    private CinemachineBrain brain;
    private CinemachineBlendDefinition blendOriginal;
    private bool blendTemporalActivo;

    private void Awake()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        if (box != null) box.isTrigger = true;
        if (camaraPlataforma != null)
        {
            posicionPlataforma = camaraPlataforma.transform.position;
            rotacionPlataforma = camaraPlataforma.transform.rotation;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (usada || other.GetComponentInParent<PlayerMovimiento>() == null || camaraPlataforma == null ||
            NivelMonedas.Instancia == null || NivelMonedas.Instancia.Estado != NivelMonedas.EstadoNivel.Jugando) return;
        usada = true;
        if (retorno != null) StopCoroutine(retorno);
        camaraPlataforma.transform.SetPositionAndRotation(posicionPlataforma, rotacionPlataforma);
        camaraPlataforma.Priority = 120;
        retorno = StartCoroutine(VolverLuego());
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponentInParent<PlayerMovimiento>() == null) return;
        if (retorno != null) StopCoroutine(retorno);
        VolverASeguimiento();
        RestaurarBlend();
        RestaurarMovimiento();
    }

    private IEnumerator VolverLuego()
    {
        yield return new WaitForSeconds(duracion);
        VolverASeguimiento();
    }

    public void MostrarPrimeraMoneda(Transform jugador)
    {
        if (jugador == null || camaraPlataforma == null || NivelMonedas.Instancia == null ||
            NivelMonedas.Instancia.Estado != NivelMonedas.EstadoNivel.Jugando) return;
        if (retorno != null) StopCoroutine(retorno);
        retorno = StartCoroutine(OrbitarPrimeraMoneda(jugador));
    }

    private IEnumerator OrbitarPrimeraMoneda(Transform jugador)
    {
        jugadorEnToma = jugador.GetComponent<PlayerMovimiento>();
        if (jugadorEnToma != null)
        {
            movimientoPrevio = jugadorEnToma.PuedeMoverse;
            jugadorEnToma.PuedeMoverse = false;
            jugadorEnToma.DetenerMovimiento();
        }

        ObtenerEncuadre(jugador, out Vector3 foco, out float distancia, out float altura);
        camaraPlataforma.Lens.FieldOfView = 52f;
        ConfigurarBlendTemporal();
        // La orientación se captura una sola vez: durante la toma no depende de giros posteriores del Fox.
        Vector3 frente = Vector3.ProjectOnPlane(jugador.forward, Vector3.up).normalized;
        if (frente.sqrMagnitude < .01f) frente = Vector3.forward;
        Vector3 derecha = Vector3.Cross(Vector3.up, frente).normalized;
        Vector3 radioInicial = (frente + derecha * .12f).normalized * distancia;
        Vector3 radioFinal = Quaternion.AngleAxis(-7f, Vector3.up) * radioInicial;
        Vector3 inicio = foco + radioInicial + Vector3.up * altura;
        camaraPlataforma.transform.SetPositionAndRotation(inicio, Quaternion.LookRotation(foco - inicio));
        camaraPlataforma.Priority = 80;

        const float duracion = 1.3f;
        for (float tiempo = 0f; tiempo < duracion; tiempo += Time.unscaledDeltaTime)
        {
            if (NivelMonedas.Instancia == null || NivelMonedas.Instancia.Estado != NivelMonedas.EstadoNivel.Jugando) yield break;
            float suave = Mathf.SmoothStep(0f, 1f, tiempo / duracion);
            Vector3 radio = Vector3.Slerp(radioInicial, radioFinal, suave);
            Vector3 posicion = foco + radio + Vector3.up * altura;
            camaraPlataforma.transform.position = posicion;
            camaraPlataforma.transform.rotation = Quaternion.LookRotation(foco - posicion, Vector3.up);
            yield return null;
        }
        VolverASeguimiento();
        yield return new WaitForSecondsRealtime(.55f);
        camaraPlataforma.transform.SetPositionAndRotation(posicionPlataforma, rotacionPlataforma);
        RestaurarBlend();
        RestaurarMovimiento();
        retorno = null;
    }

    private static void ObtenerEncuadre(Transform jugador, out Vector3 foco, out float distancia, out float altura)
    {
        Renderer[] renderers = jugador.GetComponentsInChildren<Renderer>();
        Bounds bounds = renderers.Length > 0 ? renderers[0].bounds : new Bounds(jugador.position + Vector3.up, new Vector3(1f, 2f, 1f));
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        float alto = Mathf.Max(1.5f, bounds.size.y);
        foco = new Vector3(bounds.center.x, bounds.min.y + alto * .54f, bounds.center.z);
        // El cuerpo ocupa aproximadamente un 68 % de la altura del encuadre a FOV 52.
        distancia = Mathf.Max(7f, (alto * .5f) / (Mathf.Tan(26f * Mathf.Deg2Rad) * .68f));
        altura = alto * .2f;
    }

    private void VolverASeguimiento()
    {
        if (camaraPlataforma != null) camaraPlataforma.Priority = 5;
        if (camaraSeguimiento != null) camaraSeguimiento.Priority = 20;
    }

    private void RestaurarMovimiento()
    {
        if (jugadorEnToma != null && NivelMonedas.Instancia != null &&
            NivelMonedas.Instancia.Estado == NivelMonedas.EstadoNivel.Jugando)
            jugadorEnToma.PuedeMoverse = movimientoPrevio;
        jugadorEnToma = null;
    }

    private void ConfigurarBlendTemporal()
    {
        brain = Camera.main != null ? Camera.main.GetComponent<CinemachineBrain>() : null;
        if (brain == null || blendTemporalActivo) return;
        blendOriginal = brain.DefaultBlend;
        brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, .5f);
        blendTemporalActivo = true;
    }

    private void RestaurarBlend()
    {
        if (brain != null && blendTemporalActivo) brain.DefaultBlend = blendOriginal;
        blendTemporalActivo = false;
    }

    public void CancelarPorVictoria()
    {
        usada = true;
        if (retorno != null) StopCoroutine(retorno);
        jugadorEnToma = null;
        RestaurarBlend();
        if (camaraPlataforma != null) camaraPlataforma.Priority = 5;
    }
}
