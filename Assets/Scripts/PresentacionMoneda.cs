using UnityEngine;

[DisallowMultipleComponent]
public class PresentacionMoneda : MonoBehaviour
{
    public float velocidadRotacion = 70f;
    public float amplitudFlotacion = 0.28f;
    public float velocidadFlotacion = 2f;
    private Vector3 posicionInicial;
    private float fase;

    private void Awake()
    {
        posicionInicial = transform.position;
        fase = Mathf.Abs(transform.position.x + transform.position.z) % Mathf.PI;
        Light punto = GetComponentInChildren<Light>(true);
        if (punto == null)
        {
            GameObject luz = new GameObject("Luz dorada");
            luz.transform.SetParent(transform, false);
            punto = luz.AddComponent<Light>();
        }

        Renderer[] visuales = GetComponentsInChildren<Renderer>();
        Bounds bounds = visuales.Length > 0 ? visuales[0].bounds : new Bounds(transform.position, Vector3.one);
        for (int i = 1; i < visuales.Length; i++) bounds.Encapsulate(visuales[i].bounds);
        punto.transform.position = bounds.center;
        punto.type = LightType.Point;
        punto.color = new Color(1f, 0.55f, 0.08f);
        float diametroVisual = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        punto.range = Mathf.Clamp(diametroVisual * 3.5f, 6f, 9f);
        punto.intensity = Mathf.Lerp(1.85f, 2.25f, Mathf.InverseLerp(6f, 9f, punto.range));
        punto.shadows = LightShadows.None;
        punto.enabled = true;
    }

    private void Update()
    {
        transform.Rotate(Vector3.up, velocidadRotacion * Time.deltaTime, Space.World);
        transform.position = posicionInicial + Vector3.up * (Mathf.Sin(Time.time * velocidadFlotacion + fase) * amplitudFlotacion);
    }
}
