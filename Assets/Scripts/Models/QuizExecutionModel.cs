using System;

[Serializable]
public class QuizExecutionModel
{
    public string quizId;
    public string quizNombre;

    public string fechaInicio;
    public string fechaFin;

    public float tiempoSegundos;

    public int puntosObtenidos;
    public int puntosMaximos;

    public float porcentaje;
}