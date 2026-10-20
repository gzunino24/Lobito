using UnityEngine;

public class FollowPlayer : MonoBehaviour
{
    [Header("Objetivo")]
    public Transform objetivo;

    [Header("Distancia Manual (Edítala desde Unity)")]
    // Modifica estos valores desde el Inspector en Play Mode para ajustar la vista ideal
    public Vector3 offset = new Vector3(0f, 12f, -25f);
    public float suavizado = 5f;

    [Header("Ángulo de la Cámara")]
    public Vector3 rotacionCamara = new Vector3(25f, 0f, 0f);

    void LateUpdate()
    {
        if (objetivo == null) return;

        // Mantiene la posición relativa fija sin importar hacia dónde gire el personaje
        Vector3 posicionDeseada = objetivo.position + offset;
        transform.position = Vector3.Lerp(transform.position, posicionDeseada, suavizado * Time.deltaTime);

        // Mantiene siempre la rotación fija que le definiste
        transform.rotation = Quaternion.Euler(rotacionCamara);
    }
}