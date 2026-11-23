using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
using System.IO.Ports;
#endif

/// <summary>
/// Define qué hace cada pulsador físico mientras esta pantalla está visible.
/// Si 'usarTarjetasDinamicas' está activado, A/B/C/D pulsan las tarjetas
/// 1/2/3/4 del contenedor, incluso si Unity las crea durante la ejecución.
/// </summary>
[Serializable]
public class StemScreenButtonMap
{
    [Tooltip("Ej.: Cuestionarios, Historias, Pregunta, Inicio, Resultado")]
    public string nombrePantalla;

    [Tooltip("Panel que está activo únicamente cuando esta pantalla es visible")]
    public GameObject panel;

    [Header("Selección de tarjetas generadas por Unity")]
    [Tooltip("Actívalo en TestsPanel y StoriesPanel")]
    public bool usarTarjetasDinamicas;

    [Tooltip("TestsContainer o StoriesContainer, según corresponda")]
    public Transform contenedorTarjetas;

    [Header("Botones manuales A / B / C / D")]
    public Button botonA;
    public Button botonB;
    public Button botonC;
    public Button botonD;

    public Button GetButton(int index)
    {
        if (usarTarjetasDinamicas)
        {
            if (contenedorTarjetas == null || index < 0)
                return null;

            // Se consultan los objetos en el momento de la pulsación.
            // No se guardan referencias a tarjetas destruidas/recreadas.
            int visibles = 0;
            for (int i = 0; i < contenedorTarjetas.childCount; i++)
            {
                Transform tarjeta = contenedorTarjetas.GetChild(i);
                if (tarjeta == null || !tarjeta.gameObject.activeInHierarchy)
                    continue;

                Button button = tarjeta.GetComponentInChildren<Button>(false);
                if (button == null || !button.gameObject.activeInHierarchy)
                    continue;

                if (visibles == index)
                    return button;

                visibles++;
            }

            return null;
        }

        switch (index)
        {
            case 0: return botonA;
            case 1: return botonB;
            case 2: return botonC;
            case 3: return botonD;
            default: return null;
        }
    }
}

/// <summary>
/// Entrada híbrida Windows: botones normales de Unity + Arduino USB.
/// NO modifica los OnClick existentes y no depende de que haya Arduino.
/// El puerto se detecta en un hilo aparte; la UI se maneja en Update().
/// </summary>
public class StemArduinoHybridInput : MonoBehaviour
{
    [Header("Pantallas (de la más específica a la más general)")]
    [SerializeField] private List<StemScreenButtonMap> pantallas =
        new List<StemScreenButtonMap>();

    [Header("Detección serial en Windows")]
    [Tooltip("Vacío = buscar Arduino en los COM automáticamente. Opcional: COM3")]
    [SerializeField] private string puertoPreferido = "";
    [SerializeField, Min(0.5f)] private float segundosEntreBusquedas = 1.5f;
    [SerializeField] private bool registrarPulsaciones = false;

    private readonly Queue<char> pulsaciones = new Queue<char>();
    private readonly object colaLock = new object();
    private volatile bool detener;
    private volatile bool conectado;
    private volatile string nombrePuertoConectado = "";
    private bool ultimoEstadoMostrado;

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
    private Thread hiloSerial;
    private SerialPort puertoActual;
    private readonly object puertoLock = new object();
#endif

    private void OnEnable()
    {
        detener = false;
        conectado = false;
        ultimoEstadoMostrado = false;

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        hiloSerial = new Thread(BuscarYLeerArduino);
        hiloSerial.IsBackground = true;
        hiloSerial.Name = "STEM-Arduino-Serial";
        hiloSerial.Start();
#else
        Debug.LogWarning(
            "StemArduinoHybridInput: esta conexión USB serial está implementada para Windows. " +
            "La interfaz de Unity sigue funcionando sin Arduino.", this);
#endif
    }

    private void Update()
    {
        if (ultimoEstadoMostrado != conectado)
        {
            ultimoEstadoMostrado = conectado;
            if (conectado)
                Debug.Log("STEM Arduino conectado automáticamente en " + nombrePuertoConectado, this);
            else
                Debug.LogWarning("STEM Arduino desconectado. Se mantiene la interfaz normal.", this);
        }

        // Se ejecuta siempre en el hilo principal de Unity.
        for (int i = 0; i < 16; i++)
        {
            char tecla;
            lock (colaLock)
            {
                if (pulsaciones.Count == 0)
                    break;
                tecla = pulsaciones.Dequeue();
            }
            PulsarBoton(tecla);
        }
    }

    /// <summary>
    /// Se puede llamar desde pruebas o botones de depuración sin conectar Arduino.
    /// </summary>
    public void SimularA() { PulsarBoton('A'); }
    public void SimularB() { PulsarBoton('B'); }
    public void SimularC() { PulsarBoton('C'); }
    public void SimularD() { PulsarBoton('D'); }

    private void PulsarBoton(char letra)
    {
        int indice = letra - 'A';
        if (indice < 0 || indice >= 4)
            return;

        foreach (StemScreenButtonMap mapa in pantallas)
        {
            if (mapa == null || mapa.panel == null || !mapa.panel.activeInHierarchy)
                continue;

            Button boton = mapa.GetButton(indice);
            if (boton == null)
            {
                if (registrarPulsaciones)
                    Debug.LogWarning("No hay botón " + letra + " en " + mapa.nombrePantalla, this);
                return;
            }

            // Evita pulsar botones ocultos, inactivos o deshabilitados.
            if (!boton.gameObject.activeInHierarchy || !boton.IsActive() || !boton.IsInteractable())
                return;

            if (registrarPulsaciones)
                Debug.Log("Arduino " + letra + " => " + mapa.nombrePantalla + " / " + boton.name, this);

            // Dispara EXACTAMENTE los mismos eventos del clic de Unity.
            boton.onClick.Invoke();
            return;
        }

        if (registrarPulsaciones)
            Debug.LogWarning("No hay pantalla activa configurada para Arduino.", this);
    }

    private void OnDisable()
    {
        detener = true;
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        // Cerrar el puerto interrumpe ReadLine si está esperando datos.
        lock (puertoLock)
        {
            if (puertoActual != null)
            {
                try { puertoActual.Close(); } catch { }
            }
        }

        if (hiloSerial != null && hiloSerial.IsAlive)
            hiloSerial.Join(700);

        hiloSerial = null;
#endif
        conectado = false;
        nombrePuertoConectado = "";
        lock (colaLock) { pulsaciones.Clear(); }
    }

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
    private void BuscarYLeerArduino()
    {
        while (!detener)
        {
            string[] puertos;
            try
            {
                if (!string.IsNullOrWhiteSpace(puertoPreferido))
                    puertos = new[] { puertoPreferido.Trim() };
                else
                    puertos = SerialPort.GetPortNames();

                Array.Sort(puertos, StringComparer.OrdinalIgnoreCase);
            }
            catch
            {
                puertos = new string[0];
            }

            foreach (string nombre in puertos)
            {
                if (detener) break;
                ProbarPuerto(nombre);
            }

            int espera = Math.Max(500, (int)(segundosEntreBusquedas * 1000));
            for (int transcurrido = 0; !detener && transcurrido < espera; transcurrido += 100)
                Thread.Sleep(100);
        }
    }

    private void ProbarPuerto(string nombre)
    {
        SerialPort puerto = null;
        try
        {
            puerto = new SerialPort(nombre, 9600);
            puerto.NewLine = "\n";
            puerto.ReadTimeout = 150;
            puerto.WriteTimeout = 200;
            puerto.DtrEnable = true;
            puerto.Open();

            lock (puertoLock) { puertoActual = puerto; }

            // Un Arduino UNO puede reiniciarse al abrir el COM.
            // Por eso intentamos identificarlo durante varios segundos.
            DateTime limite = DateTime.UtcNow.AddSeconds(4.0);
            DateTime proximoPing = DateTime.MinValue;
            bool identificado = false;

            while (!detener && DateTime.UtcNow < limite)
            {
                if (DateTime.UtcNow >= proximoPing)
                {
                    puerto.WriteLine("STEM_PING");
                    proximoPing = DateTime.UtcNow.AddMilliseconds(350);
                }

                try
                {
                    string linea = puerto.ReadLine().Trim();
                    if (linea == "STEM_READY_V1")
                    {
                        identificado = true;
                        break;
                    }
                }
                catch (TimeoutException) { }
            }

            if (!identificado || detener)
                return;

            nombrePuertoConectado = nombre;
            conectado = true;

            while (!detener)
            {
                try
                {
                    string linea = puerto.ReadLine().Trim();
                    if (linea.Length == 5 && linea.StartsWith("BTN:", StringComparison.Ordinal))
                    {
                        char tecla = linea[4];
                        if (tecla >= 'A' && tecla <= 'D')
                        {
                            lock (colaLock)
                            {
                                // Protección frente a una cola de pulsaciones excesiva.
                                if (pulsaciones.Count < 32)
                                    pulsaciones.Enqueue(tecla);
                            }
                        }
                    }
                }
                catch (TimeoutException) { }
            }
        }
        catch (Exception)
        {
            // Puerto ajeno, ocupado o desconectado. Sin errores en la interfaz.
        }
        finally
        {
            conectado = false;
            nombrePuertoConectado = "";
            lock (puertoLock)
            {
                if (ReferenceEquals(puertoActual, puerto))
                    puertoActual = null;
            }
            if (puerto != null)
            {
                try { puerto.Close(); } catch { }
                try { puerto.Dispose(); } catch { }
            }
        }
    }
#endif
}
