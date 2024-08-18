using System;
using System.Collections.Generic;
[System.Serializable]
public class StoryParagraph 
{
    public string text;
    public string question;
    public string answer;
    public string choice_0;
    public string choice_1;
    public string choice_2;
}

[System.Serializable]
public class StoryObject 
{
    public string preface;
    public List<StoryParagraph> paragraphs;
}
