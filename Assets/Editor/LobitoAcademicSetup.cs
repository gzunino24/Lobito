using System.Collections.Generic;
using System.Linq;
using Unity.Cinemachine;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.Splines;
using UnityEngine.UI;

public static class LobitoAcademicSetup
{
    const string Level = "Assets/Scenes/SampleScene.unity";
    const string Menu = "Assets/Scenes/MenuPrincipal.unity";
    const string Credits = "Assets/Scenes/Creditos.unity";

    [MenuItem("Lobito/Aplicar requisitos academicos Fase 3")]
    public static void Apply()
    {
        CrearMenu();
        CrearCreditos();
        ConfigurarNivel();
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(Menu, true),
            new EditorBuildSettingsScene(Level, true),
            new EditorBuildSettingsScene(Credits, true)
        };
        AssetDatabase.SaveAssets();
        Debug.Log("Lobito Fase 3: escenas, 4 splines, iluminacion y 4 camaras configuradas.");
    }

    static void ConfigurarNivel()
    {
        Scene s = EditorSceneManager.OpenScene(Level, OpenSceneMode.Single);
        GameObject jugador = Raiz(s, "Jugador");
        GameObject plataforma = Raiz(s, "Plataforma Lobito");
        if (jugador == null || plataforma == null) { Debug.LogError("Fase 3: falta Jugador o Plataforma Lobito."); return; }
        ConfigurarSplines(s, jugador.transform.position);
        ConfigurarLuces(s, jugador.transform, plataforma.transform);
        ConfigurarCamaras(s, jugador.transform, plataforma.transform);
        EditorSceneManager.MarkSceneDirty(s);
        EditorSceneManager.SaveScene(s);
    }

    static void ConfigurarSplines(Scene s, Vector3 origen)
    {
        GameObject grupo = ObtenerRaiz(s, "SPLINES_ACADEMICAS");
        GameObject splineOriginal = Raiz(s, "Spline");
        GameObject conejoOriginal = Raiz(s, "conejo");
        if (splineOriginal != null) splineOriginal.transform.SetParent(grupo.transform, true);
        if (conejoOriginal != null) conejoOriginal.transform.SetParent(grupo.transform, true);
        if (splineOriginal != null) splineOriginal.name = "Spline 1 - Conejo existente";
        if (conejoOriginal != null) conejoOriginal.name = "Animacion Spline 1 - Conejo";

        CrearSplineAnimada(grupo.transform, "Spline 2 - Segundo animal", "Animacion Spline 2 - Conejo blanco",
            "Assets/Rabbits/Prefabs/Rabbit 1.prefab", origen + new Vector3(12, 2, 10),
            new[] { new Vector3(-8,0,-5), new Vector3(5,0,-9), new Vector3(10,0,3), new Vector3(-4,0,8) }, 12f, false);

        GameObject mariposa = CrearMariposa();
        CrearSplineAnimada(grupo.transform, "Spline 3 - Trayectoria aerea", "Animacion Spline 3 - Mariposa",
            null, origen + new Vector3(-10, 13, 6),
            new[] { new Vector3(-8,0,0), new Vector3(-2,5,7), new Vector3(7,1,5), new Vector3(5,4,-6) }, 9f, true, mariposa);

        CrearSplineAnimada(grupo.transform, "Spline 4 - Recorrido ambiental", "Animacion Spline 4 - Espiritu",
            "Assets/RPG Pack/Prefabs/Bottle_Mana.prefab", origen + new Vector3(25, 7, 16),
            new[] { new Vector3(-10,0,-6), new Vector3(3,2,-10), new Vector3(12,0,2), new Vector3(0,3,11) }, 15f, true);
    }

    static void CrearSplineAnimada(Transform padre, string nombreSpline, string nombreAnimacion, string prefabPath,
        Vector3 centro, Vector3[] puntos, float duracion, bool volador, GameObject objetoPropio = null)
    {
        Transform anterior = padre.Find(nombreSpline);
        if (anterior != null) Object.DestroyImmediate(anterior.gameObject);
        GameObject camino = new GameObject(nombreSpline);
        camino.transform.SetParent(padre, false);
        camino.transform.position = centro;
        SplineContainer container = camino.AddComponent<SplineContainer>();
        container.Splines = new[] { new Spline(puntos.Select(p => (float3)p), TangentMode.AutoSmooth, true) };

        GameObject animado = new GameObject(nombreAnimacion);
        animado.transform.SetParent(camino.transform, false);
        GameObject visual = objetoPropio;
        if (visual == null)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab != null) visual = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        }
        if (visual != null) { visual.transform.SetParent(animado.transform, false); visual.transform.localScale *= volador ? .7f : 1f; }
        SplineAnimate animate = animado.AddComponent<SplineAnimate>();
        animate.Container = container;
        animate.PlayOnAwake = true;
        animate.Loop = SplineAnimate.LoopMode.Loop;
        animate.AnimationMethod = SplineAnimate.Method.Time;
        animate.Duration = duracion;
        animate.Alignment = SplineAnimate.AlignmentMode.SplineElement;
    }

    static GameObject CrearMariposa()
    {
        GameObject raiz = new GameObject("Mariposa propia Lobito");
        GameObject cuerpo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        cuerpo.transform.SetParent(raiz.transform, false); cuerpo.transform.localScale = new Vector3(.15f,.35f,.15f);
        Object.DestroyImmediate(cuerpo.GetComponent<Collider>());
        Material dorado = AssetDatabase.LoadAssetAtPath<Material>("Assets/RPG Pack/Materials/Gold.mat");
        if (dorado != null) cuerpo.GetComponent<Renderer>().sharedMaterial = dorado;
        for (int lado = -1; lado <= 1; lado += 2)
        {
            GameObject ala = GameObject.CreatePrimitive(PrimitiveType.Quad);
            ala.name = lado < 0 ? "Ala izquierda" : "Ala derecha";
            ala.transform.SetParent(raiz.transform, false);
            ala.transform.localPosition = new Vector3(.35f * lado, 0, 0);
            ala.transform.localRotation = Quaternion.Euler(90, 0, 18 * lado);
            ala.transform.localScale = new Vector3(.7f,.45f,.7f);
            Object.DestroyImmediate(ala.GetComponent<Collider>());
            if (dorado != null) ala.GetComponent<Renderer>().sharedMaterial = dorado;
        }
        return raiz;
    }

    static void ConfigurarLuces(Scene s, Transform jugador, Transform plataforma)
    {
        GameObject grupo = ObtenerRaiz(s, "ILUMINACION_ACADEMICA");
        foreach (Transform h in grupo.transform.Cast<Transform>().ToArray()) Object.DestroyImmediate(h.gameObject);
        CrearLuz(grupo.transform, "Spot Plataforma", LightType.Spot, plataforma.position + new Vector3(0,14,0), new Color(1f,.72f,.32f), 2.7f, 32, plataforma.position);
        CrearLuz(grupo.transform, "Spot Punto de interes", LightType.Spot, jugador.position + new Vector3(34,13,18), new Color(.55f,.8f,1f), 1.8f, 24, jugador.position + new Vector3(34,0,18));
        Light victoria = CrearLuz(grupo.transform, "Spot Victoria", LightType.Spot, jugador.position + new Vector3(0,12,-5), new Color(1f,.45f,.2f), 2.5f, 22, jugador.position);
        victoria.enabled = false;
        CrearLuz(grupo.transform, "Point Plataforma", LightType.Point, plataforma.position + Vector3.up * 4, new Color(.12f,.72f,1f), 2.6f, 18, Vector3.zero);
        CrearLuz(grupo.transform, "Point Zona especial", LightType.Point, jugador.position + new Vector3(-24,5,22), new Color(.25f,.55f,1f), 1.1f, 10, Vector3.zero);
    }

    static Light CrearLuz(Transform padre, string nombre, LightType tipo, Vector3 posicion, Color color, float intensidad, float rango, Vector3 objetivo)
    {
        GameObject go = new GameObject(nombre, typeof(Light)); go.transform.SetParent(padre, false); go.transform.position = posicion;
        if (tipo == LightType.Spot) go.transform.LookAt(objetivo);
        Light l = go.GetComponent<Light>(); l.type = tipo; l.color = color; l.intensity = intensidad; l.range = rango; l.shadows = LightShadows.None;
        if (tipo == LightType.Spot) { l.spotAngle = 52; l.innerSpotAngle = 30; }
        return l;
    }

    static void ConfigurarCamaras(Scene s, Transform jugador, Transform plataforma)
    {
        var existentes = Object.FindObjectsByType<CinemachineCamera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        CinemachineCamera intro = existentes.FirstOrDefault(x => x.name == "CM Introduccion" || x.name == "Camara introduccion");
        CinemachineCamera follow = existentes.FirstOrDefault(x => x.name == "CM Seguimiento" || x.name == "Camara personaje");
        if (intro != null) intro.Priority = 100;
        if (follow != null) follow.Priority = 20;
        CinemachineCamera camPlataforma = existentes.FirstOrDefault(x => x.name == "CM Especial" || x.name == "CM Plataforma");
        if (camPlataforma == null) camPlataforma = ObtenerCamara(s, "CM Especial", plataforma.position + new Vector3(-18,12,-16), plataforma.position + Vector3.up * 2, 5);
        CinemachineCamera camVictoria = ObtenerCamara(s, "CM Victoria", jugador.position + new Vector3(-18,12,-18), jugador.position + Vector3.up * 4, 5);

        GameObject zonaExistente = Raiz(s, "Trigger Camara Plataforma");
        if (zonaExistente != null) Object.DestroyImmediate(zonaExistente);
        GameObject zona = new GameObject("Trigger Camara Plataforma");
        SceneManager.MoveGameObjectToScene(zona, s);
        zona.transform.position = plataforma.position;
        BoxCollider box = zona.AddComponent<BoxCollider>();
        box.isTrigger = true; box.size = new Vector3(20,10,20);
        ActivadorCamaraPlataforma act = zona.AddComponent<ActivadorCamaraPlataforma>();
        act.camaraPlataforma = camPlataforma; act.camaraSeguimiento = follow; act.duracion = 3;
    }

    static CinemachineCamera ObtenerCamara(Scene s, string nombre, Vector3 pos, Vector3 mirar, int prioridad)
    {
        GameObject go = Raiz(s, nombre) ?? new GameObject(nombre);
        if (!go.scene.IsValid()) SceneManager.MoveGameObjectToScene(go, s);
        go.transform.position = pos; go.transform.LookAt(mirar);
        CinemachineCamera c = go.GetComponent<CinemachineCamera>();
        if (c == null) c = go.AddComponent<CinemachineCamera>();
        c.Priority = prioridad;
        return c;
    }

    static void CrearMenu()
    {
        Scene s = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); s.name = "MenuPrincipal";
        CrearCamaraFondo(new Color(.035f,.13f,.07f));
        NavegacionEscenas nav = new GameObject("Navegacion").AddComponent<NavegacionEscenas>();
        Canvas canvas = CrearCanvas("Canvas Menu Principal");
        Panel(canvas.transform, new Color(.02f,.09f,.04f,.88f), new Vector2(760,650));
        Texto(canvas.transform,"LOBITO",72,new Vector2(0,220),new Vector2(700,100));
        Texto(canvas.transform,"El bosque de las monedas",32,new Vector2(0,145),new Vector2(700,60));
        Boton(canvas.transform,"JUGAR",new Vector2(0,35),nav.Jugar);
        Boton(canvas.transform,"CRÉDITOS",new Vector2(0,-55),nav.VerCreditos);
        Boton(canvas.transform,"SALIR",new Vector2(0,-145),nav.Salir);
        new GameObject("EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));
        EditorSceneManager.SaveScene(s, Menu);
    }

    static void CrearCreditos()
    {
        Scene s = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); s.name = "Creditos";
        CrearCamaraFondo(new Color(.025f,.08f,.06f));
        NavegacionEscenas nav = new GameObject("Navegacion").AddComponent<NavegacionEscenas>();
        Canvas canvas = CrearCanvas("Canvas Creditos");
        Texto(canvas.transform,"CRÉDITOS",58,new Vector2(0,410),new Vector2(1000,80));
        string contenido = "Universidad del Salvador\nLicenciatura en Arte y Diseño Digital\nArte Interactivo 2 — 2026\n\nAlumna: [COMPLETAR]\nDocentes: [COMPLETAR]\n\nRECURSOS EXTERNOS PRESENTES EN EL PROYECTO\nFox — paquete Fox (autor y URL: completar desde la ficha original)\nRabbits — paquete Rabbits (autor y URL: completar desde la ficha original)\nFantasy Forest Environment Free Sample — fuente local verificable en Assets\nAllSkyFree — fuente local verificable en Assets\nRPG Pack — fuente local verificable en Assets\nPolytope Studio Lowpoly Environments — fuente local verificable en Assets\nSUPER Character Controller — fuente local verificable en Assets\n\nNo se consignan autores ni URLs no verificadas.";
        Text t=Texto(canvas.transform,contenido,23,new Vector2(0,40),new Vector2(1100,680)); t.alignment=TextAnchor.UpperCenter;
        Boton(canvas.transform,"VOLVER AL MENÚ",new Vector2(0,-420),nav.VolverAlMenu);
        new GameObject("EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));
        EditorSceneManager.SaveScene(s, Credits);
    }

    static void CrearCamaraFondo(Color color) { GameObject go=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener)); go.tag="MainCamera"; Camera c=go.GetComponent<Camera>(); c.clearFlags=CameraClearFlags.SolidColor;c.backgroundColor=color; }
    static Canvas CrearCanvas(string nombre) { GameObject go=new GameObject(nombre,typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster)); Canvas c=go.GetComponent<Canvas>();c.renderMode=RenderMode.ScreenSpaceOverlay;CanvasScaler sc=go.GetComponent<CanvasScaler>();sc.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;sc.referenceResolution=new Vector2(1920,1080);return c; }
    static GameObject Panel(Transform p,Color color,Vector2 size){GameObject go=new GameObject("Fondo bosque estilizado",typeof(RectTransform),typeof(Image));go.transform.SetParent(p,false);go.GetComponent<RectTransform>().sizeDelta=size;go.GetComponent<Image>().color=color;return go;}
    static Text Texto(Transform p,string texto,int size,Vector2 pos,Vector2 dims){GameObject go=new GameObject("Texto",typeof(RectTransform),typeof(Text));go.transform.SetParent(p,false);RectTransform r=go.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);r.anchoredPosition=pos;r.sizeDelta=dims;Text t=go.GetComponent<Text>();t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");t.text=texto;t.fontSize=size;t.color=Color.white;t.alignment=TextAnchor.MiddleCenter;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Overflow;return t;}
    static void Boton(Transform p,string texto,Vector2 pos,UnityEngine.Events.UnityAction accion){GameObject go=new GameObject(texto,typeof(RectTransform),typeof(Image),typeof(Button));go.transform.SetParent(p,false);RectTransform r=go.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);r.anchoredPosition=pos;r.sizeDelta=new Vector2(340,70);go.GetComponent<Image>().color=new Color(.55f,.34f,.1f,1);Button b=go.GetComponent<Button>();UnityEventTools.AddPersistentListener(b.onClick,accion);Texto(go.transform,texto,28,Vector2.zero,r.sizeDelta);}
    static GameObject Raiz(Scene s,string nombre)=>s.GetRootGameObjects().FirstOrDefault(x=>x.name==nombre);
    static GameObject ObtenerRaiz(Scene s,string nombre){GameObject go=Raiz(s,nombre);if(go!=null)return go;go=new GameObject(nombre);SceneManager.MoveGameObjectToScene(go,s);return go;}
}
