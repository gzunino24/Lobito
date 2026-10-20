using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Splines;

/// <summary>Crea recorridos ambientales adicionales sin alterar las cuatro Splines academicas.</summary>
public static class AmbientacionVoladora
{
    private const string NombreGrupo = "AMBIENTACION_VOLADORA";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Registrar()
    {
        SceneManager.sceneLoaded -= AlCargarEscena;
        SceneManager.sceneLoaded += AlCargarEscena;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CrearEnEscenaInicial()
    {
        CrearSiCorresponde(SceneManager.GetActiveScene());
    }

    private static void AlCargarEscena(Scene escena, LoadSceneMode modo) => CrearSiCorresponde(escena);

    private static void CrearSiCorresponde(Scene escena)
    {
        if (escena.name != "SampleScene" || GameObject.Find(NombreGrupo) != null) return;
        GameObject original = GameObject.Find("Animacion Spline 3 - Mariposa");
        GameObject jugador = GameObject.Find("Jugador");
        if (original == null || original.transform.childCount == 0 || jugador == null) return;

        GameObject grupo = new GameObject(NombreGrupo);
        SceneManager.MoveGameObjectToScene(grupo, escena);
        GameObject visualOriginal = original.transform.GetChild(0).gameObject;
        Vector3 origen = jugador.transform.position;
        Vector3[] centros =
        {
            origen + new Vector3(-13, 9, 12), origen + new Vector3(10, 11, 14),
            origen + new Vector3(20, 8, -7), origen + new Vector3(-18, 12, -10),
            origen + new Vector3(5, 14, -18)
        };
        float[] duraciones = { 8.5f, 10.2f, 7.8f, 11.4f, 9.3f };

        for (int i = 0; i < centros.Length; i++)
            CrearRecorrido(grupo.transform, visualOriginal, centros[i], duraciones[i], i);
    }

    private static void CrearRecorrido(Transform padre, GameObject visualOriginal, Vector3 centro, float duracion, int indice)
    {
        GameObject camino = new GameObject($"Spline ambiental mariposa {indice + 1}");
        camino.transform.SetParent(padre, false);
        camino.transform.position = centro;
        SplineContainer container = camino.AddComponent<SplineContainer>();
        float signo = indice % 2 == 0 ? 1f : -1f;
        Vector3[] puntos =
        {
            new(-5f, 0f, -2f * signo), new(-1.5f, 2.2f, 4.5f * signo),
            new(5f, .8f, 2f * signo), new(2f, -1.1f, -4.5f * signo)
        };
        container.Splines = new[] { new Spline(puntos.Select(p => (float3)p), TangentMode.AutoSmooth, true) };

        GameObject animado = new GameObject($"Mariposa ambiental {indice + 1}");
        animado.transform.SetParent(camino.transform, false);
        GameObject visual = Object.Instantiate(visualOriginal, animado.transform);
        visual.name = "Mariposa reutilizada";
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale *= .75f + indice * .05f;

        SplineAnimate animate = animado.AddComponent<SplineAnimate>();
        animate.Container = container;
        animate.PlayOnAwake = true;
        animate.Loop = SplineAnimate.LoopMode.Loop;
        animate.AnimationMethod = SplineAnimate.Method.Time;
        animate.Duration = duracion;
        animate.Alignment = SplineAnimate.AlignmentMode.SplineElement;
        animate.Easing = SplineAnimate.EasingMode.None;
        animate.StartOffset = indice * .19f;
    }
}
