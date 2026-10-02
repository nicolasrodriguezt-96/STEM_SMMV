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

    public List<string> cuestionarios;
    public List<string> historias;
    public List<string> videos;
}