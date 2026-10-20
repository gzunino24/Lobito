using Unity.Cinemachine;
using UnityEngine;


public class cambioIntro : MonoBehaviour
{
    public CinemachineCamera camaraBase;
    public float tiempo;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Invoke("delay",tiempo);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    void delay()
    {
        camaraBase.gameObject.SetActive(false);
    }

}
