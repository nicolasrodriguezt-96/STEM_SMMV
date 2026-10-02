using System;
using System.Collections.Generic;

[Serializable]
public class QuizModel
{
    public string id;
    public string nombre;
    public string descripcion;
    public string imagen;

    public List<QuestionModel> preguntas;
    public List<SequenceItemModel> secuencia;
}