using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class RifleController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AudioSource audioSource;

    [SerializeField] private AudioClip shootSound;
    [SerializeField] private ParticleSystem[] shootParticles;
    
    public Transform puntaDelCañon;
    public float intervaloDisparo = 0.3f;
    
    private float tiempoSiguienteDisparo;
    private bool estaApretandoTrigger = false;
    private XRBaseInteractable interactable;
    
    // Guardamos la referencia al interactor (mano) que activó el arma
    private IXRActivateInteractor activadorActual;

    void Awake()
    {
        interactable = GetComponent<XRBaseInteractable>();

        // Al presionar el gatillo, el evento nos pasa 'args' con toda la info
        interactable.activated.AddListener(args => {
            estaApretandoTrigger = true;
            activadorActual = args.interactorObject; // Guardamos quién disparó
        });

        interactable.deactivated.AddListener(args => {
            estaApretandoTrigger = false;
            activadorActual = null; // Limpiamos la referencia
        });
    }

    void Update()
    {
        if (estaApretandoTrigger && Time.time >= tiempoSiguienteDisparo)
        {
            Disparar();
            tiempoSiguienteDisparo = Time.time + intervaloDisparo;
        }
    }

    void Disparar()
    {
        audioSource.PlayOneShot(shootSound);
        foreach (var ps in shootParticles) {
            ps.Play();
        }
        // 1. Raycast
        RaycastHit hit;
        if (Physics.Raycast(puntaDelCañon.position, puntaDelCañon.forward, out hit, float.PositiveInfinity))
        {
            Debug.Log($"Impacto en: {hit.collider.name}");
        }

        if (activadorActual != null)
        {
            // Intentamos convertir el activador a un interactor que acepte inputs
            if (activadorActual is XRBaseInputInteractor inputInteractor)
            {
                // Ahora sí te debería aparecer el método
                inputInteractor.SendHapticImpulse(0.5f, 0.1f);
            }
        }
    }
}