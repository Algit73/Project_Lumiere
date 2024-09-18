using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;


public class Method_1_Obj
{
    public string feedback { get; set; }
    public bool A { get; set; }
}

// Class to hold each question-answer pair
public class QuestionAnswer
{
    public string Q { get; set; }
    public string A { get; set; }
}

// Class to hold the list of QuestionAnswer pairs for each method
public class Method
{
    public List<QuestionAnswer> QAndAs { get; set; }
}

// Class to hold all the methods, providing indexed access
public class Finding_Items_Obj
{
    private List<Method> methodsList;

    // Constructor to initialize the list
    public Finding_Items_Obj(Dictionary<string, List<Dictionary<string, string>>> methodsDict)
    {
        methodsList = new List<Method>();

        // Convert each method's dictionary of question-answer pairs to a Method object
        foreach (var methodPair in methodsDict)
        {
            Method method = new Method { QAndAs = new List<QuestionAnswer>() };

            foreach (var questionAnswerDict in methodPair.Value)
            {
                method.QAndAs.Add(new QuestionAnswer
                {
                    Q = questionAnswerDict.Values.ElementAt(0),
                    A = questionAnswerDict.Values.ElementAt(1)
                });
                // foreach (var kvp in questionAnswerDict)
                // {
                //     method.QuestionsAndAnswers.Add(new QuestionAnswer
                //     {
                //         Question = kvp.Key,
                //         Answer = kvp.Value
                //     });
                // }
            }

            methodsList.Add(method);
        }
    }

    // Indexer to access methods by index
    public Method this[int index]
    {
        get => methodsList[index];
    }

    // Method to deserialize the JSON into a Finding_Items_Obj object
    public static Finding_Items_Obj FromJson(string json)
    {
        // Deserialize the JSON string into a dictionary
        var methodsDict = JsonConvert.DeserializeObject<Dictionary<string, List<Dictionary<string, string>>>>(json);

        // Convert dictionary to MethodsContainer object
        return new Finding_Items_Obj(methodsDict);
    }
}
