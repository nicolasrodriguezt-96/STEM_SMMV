using System;
using System.Collections.Generic;

[Serializable]
public class StemPackageModel
{
    public int version;

    public string id;
    public string nombre;
    public string descripcion;
    public string portada;

    // Cuestionarios de conocimientos
    public List<string> cuestionarios;

    // Historias
    public List<string> historias;

    // Videos
    public List<string> videos;

    // NUEVO: único test vocacional
    public string testVocacional;
}