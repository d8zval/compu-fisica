using UnityEngine;

public class BotonConectar : MonoBehaviour
{
    public ArduinoManager arduinoManager;

    public void OnClick()
    {
        if (arduinoManager != null)
            arduinoManager.Conectar();
    }
}