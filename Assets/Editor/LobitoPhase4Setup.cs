using System.Linq;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class LobitoPhase4Setup
{
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";

    [MenuItem("Lobito/Aplicar correcciones de jugabilidad Fase 4")]
    public static void Aplicar()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject jugador = scene.GetRootGameObjects().FirstOrDefault(x => x.name == "Jugador");
        Terrain terrain = Object.FindFirstObjectByType<Terrain>();
        var monedas = scene.GetRootGameObjects().Where(x => x.name.StartsWith("Coin")).OrderBy(x => x.name).ToList();
        GameObject plataforma = scene.GetRootGameObjects().FirstOrDefault(x => x.name == "Plataforma Lobito");
        GameObject rampa = scene.GetRootGameObjects().FirstOrDefault(x => x.name == "Rampa Lobito");
        if (jugador == null || terrain == null || monedas.Count != 10 || plataforma == null || rampa == null)
        {
            Debug.LogError("Lobito Fase 4: se requieren Jugador, Terrain, exactamente 10 monedas, plataforma y rampa.");
            return;
        }

        foreach (GameObject moneda in monedas) moneda.transform.localScale = Vector3.one * 8f;

        Vector3 inicio = jugador.transform.position;
        Vector2[] offsets =
        {
            new(-7,7), new(7,7), new(-7,-7), new(7,-7),
            new(8,8), new(12,11), new(16,8)
        };
        for (int i = 0; i < 7; i++) ColocarSegura(monedas[i], terrain, inicio, offsets[i], 1.05f);

        float px = inicio.x + 16f, pz = inicio.z - 12f;
        float suelo = Altura(terrain, px, pz);
        plataforma.transform.SetPositionAndRotation(new Vector3(px, suelo + 1.4f, pz), Quaternion.identity);
        plataforma.transform.localScale = new Vector3(8f, 2.8f, 8f);
        AsegurarBox(plataforma);

        rampa.transform.SetPositionAndRotation(new Vector3(px, suelo + .45f, pz - 6.2f), Quaternion.Euler(-11f, 0, 0));
        rampa.transform.localScale = new Vector3(5.5f, .75f, 8f);
        AsegurarBox(rampa);

        ColocarSegura(monedas[7], terrain, inicio, new Vector2(12,-8), 1.05f);
        ColocarSegura(monedas[8], terrain, inicio, new Vector2(16,-6), 1.05f);
        monedas[9].transform.position = new Vector3(px, suelo + 4.2f, pz);
        AjustarBase(monedas[9], suelo + 3.05f);

        for (int i = 0; i < monedas.Count; i++)
        {
            monedas[i].name = i == 9 ? "Coin (9) - Plataforma" : $"Coin ({i})";
            ConfigurarTrigger(monedas[i]);
        }

        GameObject sistemas = scene.GetRootGameObjects().FirstOrDefault(x => x.name == "Sistemas de Lobito");
        if (sistemas == null)
        {
            sistemas = new GameObject("Sistemas de Lobito");
            SceneManager.MoveGameObjectToScene(sistemas, scene);
        }
        if (sistemas.GetComponent<NivelMonedas>() == null) sistemas.AddComponent<NivelMonedas>();

        ConfigurarCamaras(jugador.transform, plataforma.transform, terrain);
        Physics.SyncTransforms();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Lobito Fase 4: introduccion interactiva, monedas, plataforma, triggers y camaras corregidos.");
    }

    private static void ColocarSegura(GameObject moneda, Terrain terrain, Vector3 inicio, Vector2 offset, float separacion)
    {
        Vector3 elegida = new(inicio.x + offset.x, 0, inicio.z + offset.y);
        for (int intento = 0; intento < 12; intento++)
        {
            float angulo = intento * 137.5f * Mathf.Deg2Rad;
            float radio = intento == 0 ? 0 : 1.5f + intento * .45f;
            Vector3 candidato = elegida + new Vector3(Mathf.Cos(angulo) * radio, 0, Mathf.Sin(angulo) * radio);
            candidato.y = Altura(terrain, candidato.x, candidato.z) + 1.5f;
            if (!HayObstaculo(candidato, moneda)) { elegida = candidato; break; }
        }
        moneda.transform.position = elegida;
        AjustarBase(moneda, Altura(terrain, elegida.x, elegida.z) + separacion);
    }

    private static bool HayObstaculo(Vector3 centro, GameObject moneda)
    {
        foreach (Collider c in Physics.OverlapSphere(centro, 1.2f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (c is TerrainCollider || c.transform.IsChildOf(moneda.transform) || c.name.StartsWith("Coin")) continue;
            if (c.GetComponentInParent<PlayerMovimiento>() != null) continue;
            return true;
        }
        return false;
    }

    private static float Altura(Terrain t, float x, float z) => t.SampleHeight(new Vector3(x, 0, z)) + t.transform.position.y;

    private static void AjustarBase(GameObject go, float baseY)
    {
        Renderer[] rs = go.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) { Vector3 p = go.transform.position; p.y = baseY; go.transform.position = p; return; }
        Bounds b = rs[0].bounds;
        foreach (Renderer r in rs.Skip(1)) b.Encapsulate(r.bounds);
        go.transform.position += Vector3.up * (baseY - b.min.y);
    }

    private static void ConfigurarTrigger(GameObject moneda)
    {
        CapsuleCollider trigger = moneda.GetComponent<CapsuleCollider>() ?? moneda.AddComponent<CapsuleCollider>();
        Renderer[] rs = moneda.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return;
        Bounds b = rs[0].bounds;
        foreach (Renderer r in rs.Skip(1)) b.Encapsulate(r.bounds);
        Vector3 s = moneda.transform.lossyScale;
        float horizontal = Mathf.Max(.0001f, Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.z)));
        float vertical = Mathf.Max(.0001f, Mathf.Abs(s.y));
        trigger.direction = 1;
        trigger.center = moneda.transform.InverseTransformPoint(b.center);
        trigger.radius = 1.1f / horizontal;
        trigger.height = Mathf.Max(trigger.radius * 2f, 2.4f / vertical);
        trigger.isTrigger = true;
    }

    private static void AsegurarBox(GameObject go)
    {
        BoxCollider box = go.GetComponent<BoxCollider>() ?? go.AddComponent<BoxCollider>();
        box.isTrigger = false;
    }

    private static void ConfigurarCamaras(Transform jugador, Transform plataforma, Terrain terrain)
    {
        CinemachineCamera intro = Object.FindObjectsByType<CinemachineCamera>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(x => x.name == "CM Introduccion" || x.name == "Camara introduccion");
        CinemachineCamera follow = Object.FindObjectsByType<CinemachineCamera>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(x => x.name == "CM Seguimiento" || x.name == "Camara personaje");
        CinemachineCamera camPlataforma = Object.FindObjectsByType<CinemachineCamera>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(x => x.name == "CM Especial" || x.name == "CM Plataforma");
        CinemachineCamera victoria = Object.FindObjectsByType<CinemachineCamera>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(x => x.name == "CM Victoria");
        if (intro != null) { intro.name = "CM Introduccion"; intro.Priority = 150; intro.transform.position = jugador.position + new Vector3(-22, 16, 20); intro.transform.LookAt(jugador.position + Vector3.up * 2); }
        if (follow != null) { follow.name = "CM Seguimiento"; follow.Priority = 20; }
        if (camPlataforma != null) { camPlataforma.name = "CM Especial"; camPlataforma.Priority = 5; camPlataforma.transform.position = plataforma.position + new Vector3(-12, 8, -11); camPlataforma.transform.LookAt(plataforma.position + Vector3.up * 2); }
        if (victoria != null)
        {
            Vector3 pos = jugador.position + new Vector3(7, 4.5f, 8);
            pos.y = Mathf.Max(pos.y, Altura(terrain, pos.x, pos.z) + 2.5f);
            victoria.transform.position = pos;
            victoria.transform.LookAt(jugador.position + Vector3.up * 1.6f);
            victoria.Priority = 5;
        }
    }
}
