using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class LobitoLevelSetup
{
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";
    private const string CoinPath = "Assets/RPG Pack/Prefabs/Coin.prefab";
    private const string ControllerPath = "Assets/Fox/Animaciones Elegidas/Anim.controller";
    private const string FallPath = "Assets/Fox/Animaciones Elegidas/Fox_Falling.fbx";
    private const string AudioPath = "Assets/SUPER Character Controller/Demo Assets/Sounds/GemPickup.wav";

    [MenuItem("Lobito/Aplicar nivel de aventura")]
    public static void Apply()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject jugador = scene.GetRootGameObjects().FirstOrDefault(x => x.name == "Jugador");
        Terrain terrain = Object.FindFirstObjectByType<Terrain>();
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CoinPath);
        AudioClip sonido = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioPath);
        if (jugador == null || terrain == null || prefab == null)
        {
            Debug.LogError("Lobito: no se encontro Jugador, Terrain o prefab Coin. No se aplicaron cambios.");
            return;
        }

        Vector3 basePos = jugador.transform.position;
        Vector2[] offsets =
        {
            new(-18,-10), new(-8,-18), new(8,-18), new(18,-8), new(16,12), new(-14,14),
            new(28,18), new(38,26), new(48,18)
        };

        var monedas = scene.GetRootGameObjects().Where(x => x.name.StartsWith("Coin")).OrderBy(x => x.name).ToList();
        while (monedas.Count < 10)
        {
            GameObject nueva = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            nueva.name = $"Coin ({monedas.Count})";
            monedas.Add(nueva);
        }

        for (int i = 0; i < 9; i++)
        {
            ColocarSobreTerreno(monedas[i], terrain, basePos.x + offsets[i].x, basePos.z + offsets[i].y, 1.2f);
            ConfigurarMoneda(monedas[i], sonido);
        }

        GameObject plataforma = ObtenerOCrearCubo(scene, "Plataforma Lobito");
        float px = basePos.x + 26f, pz = basePos.z - 28f;
        float suelo = terrain.SampleHeight(new Vector3(px, 0, pz)) + terrain.transform.position.y;
        plataforma.transform.SetPositionAndRotation(new Vector3(px, suelo + 1.5f, pz), Quaternion.identity);
        plataforma.transform.localScale = new Vector3(11f, 3f, 11f);
        AplicarMaterialBosque(plataforma);

        GameObject rampa = ObtenerOCrearCubo(scene, "Rampa Lobito");
        rampa.transform.SetPositionAndRotation(new Vector3(px, suelo + .8f, pz - 8f), Quaternion.Euler(-12f, 0, 0));
        rampa.transform.localScale = new Vector3(6f, 1.2f, 10f);
        AplicarMaterialBosque(rampa);

        monedas[9].transform.position = new Vector3(px, suelo + 5f, pz);
        AjustarBaseVisual(monedas[9], suelo + 3.8f);
        ConfigurarMoneda(monedas[9], sonido);
        monedas[9].name = "Coin (9) - Plataforma";

        ConfigurarJugador(jugador);
        ConfigurarAnimator();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Lobito: nivel de aventura aplicado. 10 monedas, plataforma, rampa y Animator configurados.");
    }

    private static void ConfigurarMoneda(GameObject moneda, AudioClip sonido)
    {
        Moneda viejo = moneda.GetComponent<Moneda>();
        if (viejo != null) viejo.enabled = false;
        CapsuleCollider trigger = moneda.GetComponent<CapsuleCollider>();
        if (trigger == null) trigger = Undo.AddComponent<CapsuleCollider>(moneda);
        trigger.isTrigger = true;
        trigger.center = new Vector3(.003962f, .9442f, -.00816f);
        trigger.radius = .1217f; trigger.height = .2839f;
        RecolectorMoneda recolector = moneda.GetComponent<RecolectorMoneda>();
        if (recolector == null) recolector = Undo.AddComponent<RecolectorMoneda>(moneda);
        recolector.sonidoRecoleccion = sonido;
        if (moneda.GetComponent<PresentacionMoneda>() == null) Undo.AddComponent<PresentacionMoneda>(moneda);
        EditorUtility.SetDirty(moneda);
    }

    private static void ColocarSobreTerreno(GameObject go, Terrain terrain, float x, float z, float separacion)
    {
        go.transform.position = new Vector3(x, 0, z);
        float suelo = terrain.SampleHeight(go.transform.position) + terrain.transform.position.y;
        AjustarBaseVisual(go, suelo + separacion);
    }

    private static void AjustarBaseVisual(GameObject go, float alturaBase)
    {
        Renderer[] rs = go.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) { go.transform.position = new Vector3(go.transform.position.x, alturaBase, go.transform.position.z); return; }
        Bounds b = rs[0].bounds;
        foreach (Renderer r in rs.Skip(1)) b.Encapsulate(r.bounds);
        go.transform.position += Vector3.up * (alturaBase - b.min.y);
    }

    private static GameObject ObtenerOCrearCubo(Scene scene, string nombre)
    {
        GameObject existente = scene.GetRootGameObjects().FirstOrDefault(x => x.name == nombre);
        if (existente != null) return existente;
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = nombre;
        SceneManager.MoveGameObjectToScene(go, scene);
        Undo.RegisterCreatedObjectUndo(go, "Crear " + nombre);
        return go;
    }

    private static void AplicarMaterialBosque(GameObject go)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Fantasy Forest Environment Free Sample/Materials/dirt01.mat");
        if (material != null) go.GetComponent<Renderer>().sharedMaterial = material;
    }

    private static void ConfigurarJugador(GameObject jugador)
    {
        PlayerMovimiento movimiento = jugador.GetComponent<PlayerMovimiento>();
        if (movimiento == null) return;
        movimiento.velocidad = 8f;
        movimiento.multiplicadorCarrera = 1.7f;
        movimiento.velocidadRotacion = 10f;
        movimiento.distanciaSuelo = .25f;
        if (movimiento.animator != null) movimiento.animator.applyRootMotion = false;
        EditorUtility.SetDirty(movimiento);
    }

    private static void ConfigurarAnimator()
    {
        AnimatorController c = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (c == null) return;
        if (!c.parameters.Any(p => p.name == "enSuelo")) c.AddParameter("enSuelo", AnimatorControllerParameterType.Bool);
        if (!c.parameters.Any(p => p.name == "velocidadVertical")) c.AddParameter("velocidadVertical", AnimatorControllerParameterType.Float);
        AnimatorStateMachine sm = c.layers[0].stateMachine;
        AnimatorState movimiento = sm.states.Select(s => s.state).FirstOrDefault(s => s.name == "Movimiento");
        AnimatorState salto = sm.states.Select(s => s.state).FirstOrDefault(s => s.name == "Fox_Jump");
        AnimatorState caida = sm.states.Select(s => s.state).FirstOrDefault(s => s.name == "Fox_Falling");
        if (caida == null)
        {
            caida = sm.AddState("Fox_Falling", new Vector3(520, 20));
            caida.motion = AssetDatabase.LoadAllAssetsAtPath(FallPath).OfType<AnimationClip>().FirstOrDefault(x => !x.name.StartsWith("__preview__"));
        }
        if (salto != null && movimiento != null && caida != null)
        {
            foreach (AnimatorStateTransition t in salto.transitions.ToArray()) salto.RemoveTransition(t);
            AnimatorStateTransition aCaida = salto.AddTransition(caida);
            aCaida.hasExitTime = true; aCaida.exitTime = .62f; aCaida.duration = .12f;
            aCaida.AddCondition(AnimatorConditionMode.Less, -.1f, "velocidadVertical");
            if (!caida.transitions.Any(t => t.destinationState == movimiento))
            {
                AnimatorStateTransition aterrizar = caida.AddTransition(movimiento);
                aterrizar.hasExitTime = false; aterrizar.duration = .15f;
                aterrizar.AddCondition(AnimatorConditionMode.If, 0, "enSuelo");
            }
        }
        EditorUtility.SetDirty(c);
    }
}
