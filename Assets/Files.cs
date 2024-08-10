using System;
using System.Collections.Generic;

public class Files : ICloneable
{
    public string Name;
    public int Status;
    public double Value;
    private static List<string> _predeterminedFiles = new List<string>()
    {
        "ktane.exe", "11one1notone11.txt", "nudes.png", "two2twotwo322.txt",
        "killbot.exe", "CompromEvidence.png", "system32.dll",
        "NukesCodes.txt", "dictation.mp3", "PlayVsPause.bmp", "BHr34.com",
        "ExecutionList.txt", "Veto.json", "LEGOsInteractive.exe", "KarnaughMap.cs", "homework.rar",
        "video.mp4.exe",  "youtube.exe",  "KeepTalking.mp3", "AndNobodyExplodes.mp3",  "SMILEYFACE.mp3"
    };
    private static List<string> _possibleFileNames = new List<string>(){
        "paSsword", "txt", "exe", "", "None",
        "data", "woman", "man", "notepad", "phone", "phOto", "(delete)",
        "TODO", "fIle", "Name",
        "reddit", "manual", "ksdfkmfp",
        "BSOD", "100101-10"
    };
    private static List<string> _possibleFileExtensions = new List<string>(){
        "exe", "txt", "py", "cs", "png", "html", "pdf", "zip"
    };
    private static List<string> _fileModifiers = new List<string>(){
        "addDoubleExtension",
        "addRandomNumbers",
        "addDoubleNames",
        "changeLetterRegister"
    };
    private static List<string> _possibleFiles = new List<string>();
    
    public Files()
    {
        Name = CreateFileName();
        Status = UnityEngine.Random.Range(0, 3);
    }
    
    private void СreateFilesNames()
    {
        // creating all possible filenames
        foreach (string fileName in _possibleFileNames)
            foreach(string fileExtension in _possibleFileExtensions)
                _possibleFiles.Add(fileName + "." + fileExtension);
    
        foreach (string fileName in _predeterminedFiles)
            _possibleFiles.Add(fileName);
    }
    
    public string CreateFileName()
    {
        if (_possibleFiles.Count == 0)
            СreateFilesNames();
    
        int countModifiers = UnityEngine.Random.Range(0, 3);
        string fileName = _possibleFiles[UnityEngine.Random.Range(0, _possibleFiles.Count)];
        
        for (int index = 0; index < countModifiers; index++)
        {
            string modifier = _fileModifiers[UnityEngine.Random.Range(0, _fileModifiers.Count)];
            if (modifier == "addDoubleExtension")
                fileName = fileName + "." + _possibleFileExtensions[UnityEngine.Random.Range(0, _possibleFileExtensions.Count)];
            if (modifier == "addDoubleNames")
                fileName = _possibleFileNames[UnityEngine.Random.Range(0, _possibleFileNames.Count)] + fileName;
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
