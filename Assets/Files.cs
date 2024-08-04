using System;
using System.Collections.Generic;

public class Files : ICloneable
{
    public string Name;
    public int Status;
    public double Value;
    protected static List<string> PredeterminedFiles = new List<string>()
    {
        "ktane.exe", "11one1notone11.txt", "nudes.png", "two2twotwo322.txt",
        "killbot.exe", "CompromEvidence.png", "system32.dll",
        "NukesCodes.txt", "dictation.mp3", "PlayVsPause.bmp", "BHr34.com",
        "ExecutionList.txt", "Veto.json", "LEGOsInteractive.exe", "KarnaughMap.cs", "homework.rar"
    };
    protected static List<string> PossibleFileNames = new List<string>(){
        "paSsword", "txt", "exe", "", "None",
        "data", "woman", "man", "notepad", "phone", "phOto", "(delete)",
        "TODO", "fIle", "Name",
        "reddit", "manual", "ksdfkmfp",
        "BSOD", "100101-10"
    };
    protected static List<string> PossibleFileExtensions = new List<string>(){
        "exe", "txt", "py", "cs", "png", "html", "pdf", "zip"
    };
    protected static List<string> FileModifiers = new List<string>(){
        "addDoubleExtension",
        "addRandomNumbers",
        "addDoubleNames",
        "changeLetterRegister"
    };
    protected static List<string> PossibleFiles = new List<string>();
    
    public Files()
    {
        Name = CreateFileName();
        Status = UnityEngine.Random.Range(0, 3);
    }
    
    private void СreateFilesNames()
    {
        // creating all possible filenames
        foreach (string fileName in PossibleFileNames)
            foreach(string fileExtension in PossibleFileExtensions)
                PossibleFiles.Add(fileName + "." + fileExtension);
    
        foreach (string fileName in PredeterminedFiles)
            PossibleFiles.Add(fileName);
    }
    
    public string CreateFileName()
    {
        if (PossibleFiles.Count == 0)
            СreateFilesNames();
    
        int countModifiers = UnityEngine.Random.Range(0, 3);
        string fileName = PossibleFiles[UnityEngine.Random.Range(0, PossibleFiles.Count)];
        
        for (int index = 0; index < countModifiers; index++)
        {
            string modifier = FileModifiers[UnityEngine.Random.Range(0, FileModifiers.Count)];
            if (modifier == "addDoubleExtension")
                fileName = fileName + "." + PossibleFileExtensions[UnityEngine.Random.Range(0, PossibleFileExtensions.Count)];
            if (modifier == "addDoubleNames")
                fileName = PossibleFileNames[UnityEngine.Random.Range(0, PossibleFileNames.Count)] + fileName;
            if (modifier == "addRandomNumbers")
            {
                int countNumbers = UnityEngine.Random.Range(2, 5);
                string numbers = "";
                for (int iteration = 0; iteration < countNumbers; iteration++)
                    numbers += UnityEngine.Random.Range(0, 10).ToString();
                int extensionPos = fileName.IndexOf('.');
                fileName = fileName.Insert(extensionPos, numbers);
            }
            if (modifier == "changeLetterRegister")
            {
                int extensionPos = fileName.IndexOf('.');
                int countChangedLetters = UnityEngine.Random.Range(1, extensionPos-1);
                for (int iteration = 0; iteration < countChangedLetters; iteration++)
                {
                    int letterIndex = UnityEngine.Random.Range(0, extensionPos);
                    char letter = fileName[letterIndex];
                    fileName = fileName.Remove(letterIndex, 1).Insert(letterIndex, Char.ToString(Char.IsUpper(letter) ? Char.ToLower(letter) : Char.ToUpper(letter)));
                }
            }
        }
        return fileName;
        }
    
        public object Clone()
        {
            return MemberwiseClone();
        }
}
