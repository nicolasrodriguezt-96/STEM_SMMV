using System;
using System.Collections.Generic;

[Serializable]
public class VocationalTestModel
{
    public string id;
    public string nombre;
    public string descripcion;

    public List<VocationalCareerModel> carreras;
    public List<VocationalQuestionModel> preguntas;
}

[Serializable]
public class VocationalCareerModel
{
    public string id;
    public string nombre;
    public string descripcion;
}

[Serializable]
public class VocationalQuestionModel
{
    public string id;
    public string texto;

    public List<VocationalAnswerModel> respuestas;
}

[Serializable]
public class VocationalAnswerModel
{
    public string texto;
    public string carreraId;
}