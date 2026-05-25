using System.Collections;
using System.IO.Ports;
using System.Threading;
using UnityEngine;
using TMPro;

public class ArduinoManager : MonoBehaviour
{

    [Header("Configuración")]
    public string puerto = "COM3";
    public int baudRate = 9600;

    [Header("Opciones")]
    public bool mostrarMensajeEsperaba = false;

    [Header("UI")]
    public TMP_Text[] textoCajas;
    public TMP_Text textoMensaje;
    public GameObject panelIncorrecto;
    public GameObject panelConexion;      // panel con el input del puerto
    public TMP_InputField inputPuerto;    // campo donde escribe el puerto
    public UnityEngine.UI.Button botonConectar; // botón para conectar

    private SerialPort serial;
    private Thread hilo;
    private string ultimaLinea = "";
    private bool hayDatos = false;
    private readonly object lockObj = new object();

    void Start()
    {
        if (panelIncorrecto != null)
            panelIncorrecto.SetActive(false);

        // Mostrar panel de conexión al inicio
        if (panelConexion != null)
            panelConexion.SetActive(true);

        if (inputPuerto != null)
            inputPuerto.text = puerto;

        if (botonConectar != null)
            botonConectar.onClick.AddListener(Conectar);
    }

    public void Conectar()
    {
        if (inputPuerto != null && inputPuerto.text != "")
            puerto = inputPuerto.text.Trim();

        serial = new SerialPort(puerto, baudRate);
        serial.ReadTimeout = 100;

        try
        {
            serial.Open();
            hilo = new Thread(LeerSerial);
            hilo.IsBackground = true;
            hilo.Start();

            Debug.Log("Arduino conectado en " + puerto);

            if (textoMensaje != null)
                textoMensaje.text = "Conectando...";

            // Ocultar panel de conexión
            if (panelConexion != null)
                panelConexion.SetActive(false);

        }
        catch
        {
            Debug.LogError("No se pudo conectar en " + puerto);
            if (textoMensaje != null)
                textoMensaje.text = "Error: puerto " + puerto + " no encontrado";
        }
    }

    void LeerSerial()
    {
        while (serial != null && serial.IsOpen)
        {
            try
            {
                string linea = serial.ReadLine();
                Debug.Log("Recibido: " + linea);
                if (linea.StartsWith("{"))
                {
                    lock (lockObj)
                    {
                        ultimaLinea = linea;
                        hayDatos = true;
                    }
                }
            }
            catch { }
        }
    }

    void Update()
    {
        if (hayDatos)
        {
            string linea;
            lock (lockObj)
            {
                linea = ultimaLinea;
                hayDatos = false;
            }

            try
            {
                EstadoJuego estado = JsonUtility.FromJson<EstadoJuego>(linea);
                ActualizarUI(estado);
            }
            catch
            {
                Debug.LogWarning("Error parseando JSON: " + linea);
            }
        }
    }

    void ActualizarUI(EstadoJuego estado)
    {
        bool huboIncorrecto = estado.mensaje.StartsWith("Esperaba");

        if (huboIncorrecto)
        {
            StartCoroutine(MostrarPanelIncorrecto());
        }
        else
        {
            if (panelIncorrecto != null)
                panelIncorrecto.SetActive(false);
        }

        for (int i = 0; i < estado.cajas.Length && i < textoCajas.Length; i++)
        {
            if (textoCajas[i] == null) continue;

            if (estado.cajas[i].resuelta)
            {
                int digito = ObtenerDigito(estado.cajas[i].color);
                textoCajas[i].text = digito.ToString();
                textoCajas[i].color = Color.green;
            }
            else
            {
                textoCajas[i].text = "0";
                textoCajas[i].color = Color.white;
            }
        }

        if (textoMensaje != null)
        {
            if (estado.mensaje.StartsWith("Esperaba") && !mostrarMensajeEsperaba)
                textoMensaje.text = "";
            else
                textoMensaje.text = estado.mensaje;
        }
    }

    int ObtenerDigito(string color)
    {
        string[] colores = {
            "NEGRO", "ROJO", "VERDE", "AZUL", "AMARILLO",
            "MORADO", "FUCSIA", "BLANCO", "ROSA", "GRIS"
        };
        for (int i = 0; i < colores.Length; i++)
        {
            if (colores[i] == color) return i;
        }
        return -1;
    }

    IEnumerator MostrarPanelIncorrecto()
    {
        if (panelIncorrecto != null)
        {
            panelIncorrecto.SetActive(true);
            yield return new WaitForSeconds(2f);
            panelIncorrecto.SetActive(false);
        }
    }

    void OnApplicationQuit()
    {
        if (hilo != null) hilo.Abort();
        if (serial != null && serial.IsOpen) serial.Close();
    }
}