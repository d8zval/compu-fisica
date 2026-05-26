using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    [Header("Paneles")]
    public GameObject panel;

    [Header("Audio")]
    public AudioSource musicaFondo;
    public AudioClip sonidoCorrecto;
    public AudioClip sonidoIncorrecto;
    public AudioClip sonidoBoton;

    private AudioSource sfxSource;

    public static UIManager Instance;

    void Awake()
    {
        Instance = this;
        sfxSource = gameObject.AddComponent<AudioSource>();
    }

    void Start()
    {
        if (musicaFondo != null)
        {
            musicaFondo.loop = true;
            musicaFondo.Play();
        }
    }

    //  PANELES 
    public void AbrirPanel()
    {
        if (panel != null)
            panel.SetActive(true);
    }

    public void CerrarPanel()
    {
        if (panel != null)
            panel.SetActive(false);
    }

    //  NAVEGACIÓN 
    public void IrAJugar()
    {
        SceneManager.LoadScene("SampleScene");
    }

    public void VolverAlMenu()
    {
        // Cerrar conexión Arduino antes de cambiar escena
        ArduinoManager arduino = FindObjectOfType<ArduinoManager>();
        if (arduino != null)
            arduino.CerrarConexion();

        SceneManager.LoadScene("Menu");
    }
    public void CerrarJuego()
    {
        Application.Quit();
    }

    //  AUDIO 
    public void ReproducirCorrecto()
    {
        if (sonidoCorrecto != null)
            sfxSource.PlayOneShot(sonidoCorrecto);
    }

    public void ReproducirIncorrecto()
    {
        if (sonidoIncorrecto != null)
            sfxSource.PlayOneShot(sonidoIncorrecto);
    }

    public void ReproducirBoton()
    {
        if (sonidoBoton != null)
            sfxSource.PlayOneShot(sonidoBoton);
    }
}