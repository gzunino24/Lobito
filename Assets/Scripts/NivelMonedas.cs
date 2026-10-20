using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Unity.Cinemachine;

public class NivelMonedas : MonoBehaviour
{
    public enum EstadoNivel { Introduccion, Jugando, Celebracion, Victoria }
    public static NivelMonedas Instancia { get; private set; }
    public static event Action NivelCompletado;
    public static event Action ContinuarSolicitado;

    private int recogidas;
    private int total;
    private bool terminado;
    private PlayerMovimiento jugador;
    private CinemachineCamera camaraIntroduccion;
    private CinemachineCamera camaraSeguimiento;
    private CinemachineCamera camaraVictoria;
    private Light luzVictoria;
    private Text contador;
    private GameObject panelIntroduccion;
    private GameObject panelVictoria;
    private ActivadorCamaraPlataforma activadorPlataforma;
    private bool introduccionActiva;
    private Coroutine movimientoIntroduccion;
    private Vector3 posicionFinalIntroduccion;
    private Quaternion rotacionFinalIntroduccion;
    private CanvasGroup grupoVictoria;
    private RectTransform rectVictoria;
    private Vector3 inicioOrbitaVictoria;
    private Vector3 finOrbitaVictoria;
    public EstadoNivel Estado { get; private set; } = EstadoNivel.Introduccion;
    public bool PuedeRecoger => Estado == EstadoNivel.Jugando && !terminado;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ReiniciarEstaticos()
    {
        Instancia = null;
        NivelCompletado = null;
        ContinuarSolicitado = null;
        SceneManager.sceneLoaded -= AlCargarEscena;
        SceneManager.sceneLoaded += AlCargarEscena;
    }

    private static void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        if (escena.name != "SampleScene") return;
        NivelMonedas existente = FindFirstObjectByType<NivelMonedas>();
        if (existente == null) new GameObject("Sistemas de Lobito").AddComponent<NivelMonedas>();
    }

    private void Awake()
    {
        if (Instancia != null && Instancia != this) { Destroy(gameObject); return; }
        Instancia = this;
        recogidas = 0;
        total = 0;
        terminado = false;
        Estado = EstadoNivel.Introduccion;
        Time.timeScale = 1f;
        ConfigurarEscena();
        CrearInterfaz();
        MostrarIntroduccion();
    }

    private void OnDestroy() { if (Instancia == this) Instancia = null; }

    private void Update()
    {
        if (terminado && Input.GetKeyDown(KeyCode.R)) ReiniciarNivel();
    }

    public void RegistrarMoneda()
    {
        if (terminado) return;
        recogidas = Mathf.Min(recogidas + 1, total);
        ActualizarContador();
        if (recogidas == 1 && activadorPlataforma != null && jugador != null)
            activadorPlataforma.MostrarPrimeraMoneda(jugador.transform);
        if (recogidas < total) return;

        terminado = true;
        Estado = EstadoNivel.Celebracion;
        StartCoroutine(ReproducirVictoria());
    }

    public void ReiniciarNivel() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    public void ContinuarDespuesDeLaVictoria()
    {
        if (!terminado) return;
        ContinuarSolicitado?.Invoke();
        SceneManager.LoadScene("Creditos");
    }

    private void ConfigurarEscena()
    {
        foreach (GameObject raiz in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (raiz.name == "Jugador") jugador = raiz.GetComponent<PlayerMovimiento>();
            if (raiz.name == "CM Introduccion" || raiz.name == "Camara introduccion")
            {
                camaraIntroduccion = raiz.GetComponent<CinemachineCamera>();
                cambioIntro legado = raiz.GetComponent<cambioIntro>();
                if (legado != null) legado.enabled = false;
            }
            if (raiz.name == "CM Seguimiento" || raiz.name == "Camara personaje") camaraSeguimiento = raiz.GetComponent<CinemachineCamera>();
            if (raiz.name == "CM Victoria") camaraVictoria = raiz.GetComponent<CinemachineCamera>();
            if (raiz.name == "ILUMINACION_ACADEMICA")
            {
                Transform t = raiz.transform.Find("Spot Victoria");
                if (t != null) luzVictoria = t.GetComponent<Light>();
            }
            if (!raiz.name.StartsWith("Coin", StringComparison.Ordinal)) continue;
            total++;
            CapsuleCollider trigger = raiz.GetComponent<CapsuleCollider>();
            if (trigger == null)
            {
                trigger = raiz.AddComponent<CapsuleCollider>();
            }
            ConfigurarTriggerMoneda(raiz, trigger);
            trigger.isTrigger = true;
            Moneda legadoMoneda = raiz.GetComponent<Moneda>();
            if (legadoMoneda != null) legadoMoneda.enabled = false;
            if (raiz.GetComponent<RecolectorMoneda>() == null) raiz.AddComponent<RecolectorMoneda>();
            if (raiz.GetComponent<PresentacionMoneda>() == null) raiz.AddComponent<PresentacionMoneda>();
        }
        activadorPlataforma = FindFirstObjectByType<ActivadorCamaraPlataforma>();
    }

    private void MostrarIntroduccion()
    {
        introduccionActiva = true;
        Estado = EstadoNivel.Introduccion;
        if (jugador != null) jugador.PuedeMoverse = false;
        if (camaraIntroduccion != null) camaraIntroduccion.Priority = 150;
        if (camaraSeguimiento != null) camaraSeguimiento.Priority = 20;
        panelIntroduccion.SetActive(true);
        if (camaraIntroduccion != null && jugador != null)
            movimientoIntroduccion = StartCoroutine(AnimarCamaraIntroduccion());
    }

    private IEnumerator AnimarCamaraIntroduccion()
    {
        Transform cam = camaraIntroduccion.transform;
        Vector3 objetivo = jugador.transform.position + Vector3.up * 1.6f;
        posicionFinalIntroduccion = jugador.transform.position + new Vector3(-10f, 6.5f, 10f);
        posicionFinalIntroduccion.y = Mathf.Max(posicionFinalIntroduccion.y, AlturaTerreno(posicionFinalIntroduccion) + 2.2f);
        Vector3 inicio = jugador.transform.position + new Vector3(-22f, 25f, 24f);
        inicio.y = Mathf.Max(inicio.y, AlturaTerreno(inicio) + 4f);
        Quaternion rotacionInicio = Quaternion.LookRotation(objetivo - inicio);
        rotacionFinalIntroduccion = Quaternion.LookRotation(objetivo - posicionFinalIntroduccion);
        cam.SetPositionAndRotation(inicio, rotacionInicio);

        const float duracion = 5f;
        for (float tiempo = 0f; tiempo < duracion && introduccionActiva; tiempo += Time.unscaledDeltaTime)
        {
            float suave = Mathf.SmoothStep(0f, 1f, tiempo / duracion);
            cam.position = Vector3.LerpUnclamped(inicio, posicionFinalIntroduccion, suave);
            cam.rotation = Quaternion.Slerp(rotacionInicio, rotacionFinalIntroduccion, suave);
            yield return null;
        }
        if (introduccionActiva) cam.SetPositionAndRotation(posicionFinalIntroduccion, rotacionFinalIntroduccion);
        movimientoIntroduccion = null;
    }

    public void ComenzarPartida()
    {
        if (!introduccionActiva) return;
        introduccionActiva = false;
        if (movimientoIntroduccion != null)
        {
            StopCoroutine(movimientoIntroduccion);
            movimientoIntroduccion = null;
            if (camaraIntroduccion != null)
                camaraIntroduccion.transform.SetPositionAndRotation(posicionFinalIntroduccion, rotacionFinalIntroduccion);
        }
        panelIntroduccion.SetActive(false);
        if (camaraIntroduccion != null) camaraIntroduccion.Priority = 5;
        StartCoroutine(HabilitarTrasBlend());
    }

    private IEnumerator HabilitarTrasBlend()
    {
        yield return new WaitForSecondsRealtime(1.4f);
        Estado = EstadoNivel.Jugando;
        if (jugador != null) jugador.PuedeMoverse = true;
    }

    private void ActivarCamaraVictoria()
    {
        if (camaraIntroduccion != null) camaraIntroduccion.Priority = 5;
        if (camaraSeguimiento != null) camaraSeguimiento.Priority = 20;
        if (camaraVictoria != null) camaraVictoria.Priority = 200;
        if (luzVictoria != null) luzVictoria.enabled = true;
    }

    private IEnumerator ReproducirVictoria()
    {
        if (jugador != null) { jugador.PuedeMoverse = false; jugador.DetenerMovimiento(); }
        if (activadorPlataforma != null) activadorPlataforma.CancelarPorVictoria();
        PosicionarCamaraVictoria();
        ActivarCamaraVictoria();
        if (jugador != null && camaraVictoria != null)
        {
            Vector3 direccion = camaraVictoria.transform.position - jugador.transform.position;
            direccion.y = 0f;
            Quaternion destino = direccion.sqrMagnitude > .01f ? Quaternion.LookRotation(direccion) : jugador.transform.rotation;
            for (float t = 0; t < .65f; t += Time.deltaTime)
            {
                jugador.Cuerpo.MoveRotation(Quaternion.Slerp(jugador.Cuerpo.rotation, destino, t / .65f));
                yield return null;
            }
            jugador.Celebrar();
            CrearParticulasVictoria();
            const float duracionCelebracion = 2.2f;
            for (float t = 0; t < duracionCelebracion; t += Time.unscaledDeltaTime)
            {
                ObtenerEncuadreFox(out Vector3 foco, out _, out _);
                float suave = Mathf.SmoothStep(0f, 1f, t / duracionCelebracion);
                Vector3 posicion = Vector3.LerpUnclamped(inicioOrbitaVictoria, finOrbitaVictoria, suave);
                camaraVictoria.transform.position = posicion;
                camaraVictoria.transform.rotation = Quaternion.Slerp(camaraVictoria.transform.rotation,
                    Quaternion.LookRotation(foco - posicion), 9f * Time.unscaledDeltaTime);
                ActualizarLuzVictoria();
                yield return null;
            }
        }
        yield return AnimarPanelVictoria();
        Estado = EstadoNivel.Victoria;
        NivelCompletado?.Invoke();
    }

    private void PosicionarCamaraVictoria()
    {
        if (jugador == null || camaraVictoria == null) return;
        Transform fox = jugador.transform;
        ObtenerEncuadreFox(out Vector3 foco, out float distancia, out float altura);
        camaraVictoria.Lens.FieldOfView = 50f;
        Vector3 frente = Vector3.ProjectOnPlane(fox.forward, Vector3.up).normalized;
        if (frente.sqrMagnitude < .01f) frente = Vector3.forward;
        Vector3 derecha = Vector3.Cross(Vector3.up, frente).normalized;
        inicioOrbitaVictoria = foco + frente * distancia + derecha * (distancia * .28f) + Vector3.up * altura;
        finOrbitaVictoria = foco + frente * distancia - derecha * (distancia * .18f) + Vector3.up * (altura + .12f);
        inicioOrbitaVictoria = AsegurarCamaraDespejada(foco, inicioOrbitaVictoria, frente, distancia, altura);
        finOrbitaVictoria = AsegurarCamaraDespejada(foco, finOrbitaVictoria, frente, distancia, altura);
        camaraVictoria.transform.SetPositionAndRotation(inicioOrbitaVictoria, Quaternion.LookRotation(foco - inicioOrbitaVictoria));
    }

    private void ObtenerEncuadreFox(out Vector3 foco, out float distancia, out float altura)
    {
        Renderer[] renderers = jugador.GetComponentsInChildren<Renderer>();
        Bounds bounds = renderers.Length > 0 ? renderers[0].bounds : new Bounds(jugador.transform.position + Vector3.up, new Vector3(1f, 2f, 1f));
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        float alto = Mathf.Max(1.5f, bounds.size.y);
        foco = new Vector3(bounds.center.x, bounds.min.y + alto * .52f, bounds.center.z);
        distancia = Mathf.Max(6f, (alto * .65f) / Mathf.Tan(25f * Mathf.Deg2Rad));
        altura = alto * .28f;
    }

    private Vector3 AsegurarCamaraDespejada(Vector3 foco, Vector3 candidato, Vector3 frente, float distancia, float altura)
    {
        candidato.y = Mathf.Max(candidato.y, AlturaTerreno(candidato) + 2f);
        if (!Physics.Linecast(foco, candidato, out RaycastHit golpe, ~0, QueryTriggerInteraction.Ignore) ||
            golpe.transform.IsChildOf(jugador.transform)) return candidato;
        Vector3 alternativo = foco + frente * distancia + Vector3.up * (altura + 1.2f);
        alternativo.y = Mathf.Max(alternativo.y, AlturaTerreno(alternativo) + 2f);
        return alternativo;
    }

    private float AlturaTerreno(Vector3 posicion)
    {
        Terrain terreno = Terrain.activeTerrain;
        return terreno == null ? posicion.y : terreno.SampleHeight(posicion) + terreno.transform.position.y;
    }

    private void ActualizarLuzVictoria()
    {
        if (luzVictoria == null || jugador == null) return;
        luzVictoria.transform.position = jugador.transform.position + Vector3.up * 5f - jugador.transform.forward * 2f;
        luzVictoria.transform.rotation = Quaternion.LookRotation(jugador.transform.position + Vector3.up - luzVictoria.transform.position);
    }

    private void CrearParticulasVictoria()
    {
        if (jugador == null) return;
        GameObject go = new GameObject("Particulas doradas de victoria");
        go.transform.position = jugador.transform.position + Vector3.up * .7f;
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main; main.duration = 1.5f; main.startLifetime = 1.8f; main.startSpeed = 3.2f;
        main.startSize = .18f; main.startColor = new Color(1f, .65f, .08f, 1f); main.loop = false;
        var emission = ps.emission; emission.rateOverTime = 0f;
        var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = 1.1f;
        ps.Emit(42); ps.Play(); Destroy(go, 3.5f);
    }

    private IEnumerator AnimarPanelVictoria()
    {
        panelVictoria.SetActive(true);
        Vector2 destino = rectVictoria.anchoredPosition;
        Vector2 inicio = destino + Vector2.right * 180f;
        grupoVictoria.alpha = 0f;
        rectVictoria.anchoredPosition = inicio;
        for (float t = 0f; t < .55f; t += Time.unscaledDeltaTime)
        {
            float suave = Mathf.SmoothStep(0f, 1f, t / .55f);
            grupoVictoria.alpha = suave;
            rectVictoria.anchoredPosition = Vector2.LerpUnclamped(inicio, destino, suave);
            yield return null;
        }
        grupoVictoria.alpha = 1f;
        rectVictoria.anchoredPosition = destino;
    }

    private static void ConfigurarTriggerMoneda(GameObject moneda, CapsuleCollider trigger)
    {
        Renderer[] renders = moneda.GetComponentsInChildren<Renderer>();
        if (renders.Length == 0) { trigger.center = Vector3.up; trigger.radius = 1f; trigger.height = 2f; return; }
        Bounds bounds = renders[0].bounds;
        for (int i = 1; i < renders.Length; i++) bounds.Encapsulate(renders[i].bounds);
        Vector3 escala = moneda.transform.lossyScale;
        float horizontal = Mathf.Max(.0001f, Mathf.Max(Mathf.Abs(escala.x), Mathf.Abs(escala.z)));
        float vertical = Mathf.Max(.0001f, Mathf.Abs(escala.y));
        trigger.direction = 1;
        trigger.center = moneda.transform.InverseTransformPoint(bounds.center);
        trigger.radius = 1.1f / horizontal;
        trigger.height = Mathf.Max(trigger.radius * 2f, 2.4f / vertical);
        trigger.isTrigger = true;
    }

    private void CrearInterfaz()
    {
        Font fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        GameObject lienzoGo = new GameObject("Canvas Lobito", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas lienzo = lienzoGo.GetComponent<Canvas>();
        lienzo.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = lienzoGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        contador = CrearTexto(lienzoGo.transform, "Contador", fuente, 32, TextAnchor.MiddleCenter, new Vector2(25, -25), new Vector2(310, 65), new Vector2(0, 1));
        ActualizarContador();

        panelIntroduccion = CrearPanel(lienzoGo.transform, "Introduccion", new Color(0.03f, 0.11f, 0.05f, 0.78f), new Vector2(760, 300));
        CrearTexto(panelIntroduccion.transform, "Titulo", fuente, 64, TextAnchor.MiddleCenter, new Vector2(0, 75), new Vector2(700, 85), new Vector2(.5f, .5f)).text = "LOBITO";
        CrearTexto(panelIntroduccion.transform, "Mision", fuente, 30, TextAnchor.MiddleCenter, new Vector2(0, 5), new Vector2(700, 60), new Vector2(.5f, .5f)).text = $"¡Recolectá las {total} monedas para ganar!";
        CrearTexto(panelIntroduccion.transform, "Controles", fuente, 24, TextAnchor.MiddleCenter, new Vector2(0, -62), new Vector2(700, 85), new Vector2(.5f, .5f)).text = "WASD: Moverse   •   Espacio: Saltar   •   Shift: Correr";
        CrearBoton(panelIntroduccion.transform, "COMENZAR", fuente, new Vector2(0, -125), ComenzarPartida);

        if (FindFirstObjectByType<EventSystem>() == null)
            new GameObject("EventSystem Lobito", typeof(EventSystem), typeof(StandaloneInputModule));

        panelVictoria = CrearPanel(lienzoGo.transform, "Victoria", new Color(0.04f, 0.14f, 0.06f, 0.9f), new Vector2(650, 390));
        rectVictoria = panelVictoria.GetComponent<RectTransform>();
        rectVictoria.anchorMin = rectVictoria.anchorMax = new Vector2(.76f, .5f);
        rectVictoria.anchoredPosition = Vector2.zero;
        grupoVictoria = panelVictoria.AddComponent<CanvasGroup>();
        CrearTexto(panelVictoria.transform, "Felicitaciones", fuente, 52, TextAnchor.MiddleCenter, new Vector2(0, 110), new Vector2(660, 75), new Vector2(.5f, .5f)).text = "¡FELICITACIONES!";
        CrearTexto(panelVictoria.transform, "Mensaje", fuente, 28, TextAnchor.MiddleCenter, new Vector2(0, 35), new Vector2(610, 55), new Vector2(.5f, .5f)).text = "¡Recolectaste las 10 monedas!";
        CrearBoton(panelVictoria.transform, "VOLVER A JUGAR", fuente, new Vector2(-145, -90), ReiniciarNivel);
        CrearBoton(panelVictoria.transform, "CONTINUAR A CRÉDITOS", fuente, new Vector2(145, -90), ContinuarDespuesDeLaVictoria);
        CrearTexto(panelVictoria.transform, "Atajo", fuente, 19, TextAnchor.MiddleCenter, new Vector2(0, -155), new Vector2(500, 35), new Vector2(.5f, .5f)).text = "También podés presionar R para reiniciar";
        panelVictoria.SetActive(false);
    }

    private void ActualizarContador() { if (contador != null) contador.text = $"Monedas: {recogidas}/{total}"; }

    private static GameObject CrearPanel(Transform padre, string nombre, Color color, Vector2 tamano)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(padre, false);
        RectTransform r = go.GetComponent<RectTransform>();
        r.anchorMin = r.anchorMax = new Vector2(.5f, .5f); r.sizeDelta = tamano;
        go.GetComponent<Image>().color = color;
        return go;
    }

    private static Text CrearTexto(Transform padre, string nombre, Font fuente, int tamano, TextAnchor alineacion, Vector2 posicion, Vector2 medidas, Vector2 ancla)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(padre, false);
        RectTransform r = go.GetComponent<RectTransform>();
        r.anchorMin = r.anchorMax = ancla; r.pivot = ancla; r.anchoredPosition = posicion; r.sizeDelta = medidas;
        Text t = go.GetComponent<Text>(); t.font = fuente; t.fontSize = tamano; t.alignment = alineacion; t.color = Color.white;
        t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }

    private static void CrearBoton(Transform padre, string texto, Font fuente, Vector2 posicion, UnityEngine.Events.UnityAction accion)
    {
        GameObject go = new GameObject(texto, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(padre, false);
        RectTransform r = go.GetComponent<RectTransform>(); r.anchorMin = r.anchorMax = new Vector2(.5f, .5f); r.anchoredPosition = posicion; r.sizeDelta = new Vector2(245, 58);
        go.GetComponent<Image>().color = new Color(.72f, .48f, .12f, 1f);
        go.GetComponent<Button>().onClick.AddListener(accion);
        Text etiqueta = CrearTexto(go.transform, "Texto", fuente, 23, TextAnchor.MiddleCenter, Vector2.zero, r.sizeDelta, new Vector2(.5f, .5f));
        etiqueta.text = texto;
    }
}
