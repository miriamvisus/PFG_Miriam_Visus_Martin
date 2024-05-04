using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using System.Threading;


public class DataReceiver : MonoBehaviour
{
    Thread thread;
    public string serverIP = "127.0.0.1"; 
    public int port = 8001;
    TcpListener server;
    bool running;

    public GameObject ModelManager;

    public static event Action<float, float[]> OnDataReceived; // Evento para notificar cuando se reciben datos

    void Start()
    {
        // Receive on a separate thread so Unity doesn't freeze waiting for data
        thread = new Thread(ReceiveData);
        thread.Start();
    }

    void ReceiveData()
    {
        try 
        {
            // Establece la conexión
            server = new TcpListener(IPAddress.Parse(serverIP), port);
            server.Start();

            // Start listening
            running = true;
            while (running)
            {
                TcpClient client = server.AcceptTcpClient();
                // Imprime por pantalla el mensaje indicando que el servidor está escuchando
                Debug.Log($"Servidor escuchando en {serverIP}:{port}");
                Connection(client);
                client.Close();
            }

            server.Stop();
        }
        catch(Exception ex)
        {
            Debug.LogError("Error al recibir datos de audio: " + ex.Message);
        }
    }

    void Connection(TcpClient client)
    {
        try
        {
            NetworkStream stream = client.GetStream();

            // Recibe los datos de tempo
            byte[] tempoBytes = new byte[4];
            stream.Read(tempoBytes, 0, tempoBytes.Length);
            float tempo = BitConverter.ToSingle(tempoBytes, 0);

            // Recibe la longitud del array de energía
            byte[] energyLengthBytes = new byte[4];
            stream.Read(energyLengthBytes, 0, energyLengthBytes.Length);
            int energyLength = BitConverter.ToInt32(energyLengthBytes, 0);

            // Recibe los datos de energía
            byte[] energyBytes = new byte[sizeof(float) * energyLength];
            stream.Read(energyBytes, 0, energyBytes.Length);
            float[] energy = new float[energyLength];
            Buffer.BlockCopy(energyBytes, 0, energy, 0, energyBytes.Length);

            Debug.Log("Tempo recibido: " + tempo);
            Debug.Log("Energía recibida: " + string.Join(", ", energy));

            // Disparar el evento con los datos recibidos
            OnDataReceived?.Invoke(tempo, energy);

            ModelRunner modelScript = ModelManager.GetComponent<ModelRunner>();

            // Después de recibir los datos, pasa los datos al ModelRunner
            modelScript.ProcessData(tempo, energy);
        }
        catch (Exception ex)
        {
            Debug.LogError("Error al recibir datos de audio: " + ex.Message);
        }
    }

    void OnDestroy()
    {
        running = false;
        thread.Join(); // Espera a que el hilo termine antes de destruir el objeto
    }
}