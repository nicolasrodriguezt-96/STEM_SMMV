using System;
using System.Collections.Generic;

[Serializable]
public class StoryModel
{
    public string id;
    public string nombre;
    public List<StorySceneModel> escenas;
}