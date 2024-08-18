using System;
using System.Collections.Generic;
[System.Serializable]
public class WordParagraph 
{
    public string hint;
    public string question;
    public string answer;
    public string choice_0;
    public string choice_1;
    public string choice_2;
}

[System.Serializable]
public class WordObject 
{
    public string preface;
    public List<WordParagraph> paragraphs;
}
