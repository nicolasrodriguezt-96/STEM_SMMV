using System;
using System.Collections.Generic;

[Serializable]
public class QuestionModel
{
    public string id;
    public string texto;
    public string imagen;
    public List<AnswerModel> respuestas;
}