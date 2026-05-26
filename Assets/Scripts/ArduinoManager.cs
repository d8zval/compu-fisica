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
    public TMP_Text textoConexion;
    public GameObject panelIncorrecto;
    public GameObject panelConexion;
    public TMP_InputField inputPuerto;
    public UnityEngine.UI.Button botonConectar;

    private SerialPort serial;
    private Thread hilo;
    private string ultimaLinea = "";
    private bool hayDatos = false;
    private readonly object lockObj = new object();

    private string mensajePendiente = "";
    private bool conexionExitosa = false;
    private bool hayMensajePendiente = false;

    void Start()
    {
        if (panelIncorrecto != null)
            panelIncorrecto.SetActive(false);

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

        if (textoConexion != null)
            textoConexion.text = "Conectando...";

        Thread hiloConexion = new Thread(() =>
        {
            try
            {
                SerialPort temp = new SerialPort(puerto, baudRate);
                temp.ReadTimeout = 100;
                temp.Open();

                serial = temp;
                hilo = new Thread(LeerSerial);
                hilo.IsBackground = true;
                hilo.Start();

                mensajePendiente = "¡Conectado!";
                conexionExitosa = true;
                hayMensajePendiente = true;
            }
            catch
            {
                mensajePendiente = "Error: puerto " + puerto + " no encontrado. Si nada esta conectado, cierra el juego, conecta e intenta de nuevo.";
                conexionExitosa = false;
                hayMensajePendiente = true;
            }
        });
        hiloConexion.IsBackground = true;
        hiloConexion.Start();
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
        if (hayMensajePendiente)
        {
            hayMensajePendiente = false;

            if (textoConexion != null)
                textoConexion.text = mensajePendiente;

            if (conexionExitosa && panelConexion != null)
                panelConexion.SetActive(false);
        }

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
        bool huboCorrecto = estado.mensaje == "Correcto!";

        if (huboIncorrecto)
        {
            StartCoroutine(MostrarPanelIncorrecto());
            if (UIManager.Instance != null)
                UIManager.Instance.ReproducirIncorrecto();
        }
        else
        {
            if (panelIncorrecto != null)
                panelIncorrecto.SetActive(false);
        }

        if (huboCorrecto)
        {
            if (UIManager.Instance != null)
                UIManager.Instance.ReproducirCorrecto();
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

    public void CerrarConexion()
    {
        if (hilo != null) hilo.Abort();
        if (serial != null && serial.IsOpen) serial.Close();
    }

    void OnApplicationQuit()
    {
        if (hilo != null) hilo.Abort();
        if (serial != null && serial.IsOpen) serial.Close();
    }
}