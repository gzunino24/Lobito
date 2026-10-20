using UnityEngine;
using UnityEngine.Splines;

public class FollowPath : MonoBehaviour
{
    public float speed = 1f;
    private float t = 0f;
    public SplineContainer splineContainer;

    [Header("Configuración de Rotación")]
    public bool rotarHaciaElCamino = true;
    public float offsetAngulo = -90f; // Si se ve de costado, probá cambiarlo a 0, 90 u 180 desde el Inspector

    void Update()
    {
        if (splineContainer == null) return;

        // 1. Avanzar el tiempo en bucle
        t += Time.deltaTime * speed;
        t %= 1f;

        // 2. Obtener posición local de la spline y convertirla a posición global en la escena
        var spline = splineContainer.Spline;
        Vector3 localPosition = spline.EvaluatePosition(t);
        Vector3 worldPosition = splineContainer.transform.TransformPoint(localPosition);

        transform.position = worldPosition;

        // 3. Rotar hacia la dirección del camino (opcional)
        if (rotarHaciaElCamino)
        {
            Vector3 localTangent = spline.EvaluateTangent(t);
            Vector3 worldTangent = splineContainer.transform.TransformDirection(localTangent);

            float angle = Mathf.Atan2(worldTangent.y, worldTangent.x) * Mathf.Rad2Deg + offsetAngulo;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }
}