using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InfiniteBackground : MonoBehaviour
{
    public float backgroundWidth; // Ancho de cada segmento del fondo (configurado manualmente)

    private List<GameObject> backgrounds = new List<GameObject>(); // Lista para almacenar los segmentos del fondo
    private Vector3 lastBackgroundEndPosition; // Posición del final del último segmento creado

    void Start()
    {
        // Inicializar el primer segmento de fondo
        GameObject initialBackground = this.gameObject;

        // Configurar la posición final del primer segmento
        lastBackgroundEndPosition = initialBackground.transform.position + new Vector3(backgroundWidth, 0, 0);

        // Agregar el primer segmento de fondo a la lista
        backgrounds.Add(initialBackground);
        
        while(true) 
        {
            CreateNewBackgroundSegment();
        }
    }

    private void CreateNewBackgroundSegment()
    {
        // Clonar el objeto de fondo actual y posicionarlo al final del último segmento
        GameObject newBackground = Instantiate(this.gameObject, lastBackgroundEndPosition, Quaternion.identity);

        // Agregar el nuevo segmento a la lista
        backgrounds.Add(newBackground);

        // Actualizar la posición final del último segmento
        lastBackgroundEndPosition = newBackground.transform.position + new Vector3(backgroundWidth, 0, 0);
    }
}