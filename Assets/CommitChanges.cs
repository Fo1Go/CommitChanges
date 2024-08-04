using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using Random = UnityEngine.Random;

public class CommitChanges : MonoBehaviour
{
    public KMAudio Audio;
    public KMBombModule Module;
    public KMBombInfo Bomb;
    public KMColorblindMode colourblind;
    public KMSelectable BtnPrev, BtnNext, BtnSubmit;
    public AudioClip keyboardSound;
    public AudioClip buttonPressedSound;
    public AudioClip consoleOpensSound;
    public TextMesh screen, consoleTM, repository, contributor, dayOfWeek, colorblindText;
    public GameObject indicator;

    protected int MaxLengthInput;
    protected int MaxEntersLength;
    protected bool IsSolved;
    protected bool Typing;
    
    protected readonly string Alphabet = "abcdefghijklmnopqrstuvwxyz";

    protected int RepoCoef;
    protected string RepoName;

    protected readonly List<string> Repositories = new List<string>()
    {
        "KtaneContent",
    };

    protected string ContributorName;

    protected readonly List<string> Contributes = new List<string>()
    {
        "_Play_",
        "poisongreen",
        "Megum",
        "timwi",
        "samfundev",
        "Emik",
        "Blananas2",
        "eXish",
        "Speakingevil",
        "Royal_Flu$h",
        "rand06",
        "Hexicube",
        "SL7205"
    };

    protected int DoWCoef;
    protected string DayOfWeekName;

    protected readonly List<string> DaysOfWeek = new List<string>()
    {
        "Monday",
        "Tuesday",
        "Wednesday",
        "Thursday",
        "Friday",
        "Saturday",
        "Sunday"
    };

    protected bool ColourblindActive;

    protected readonly List<Files> AllFiles = new List<Files>();
    protected Files CurrentFile;
    protected int FileIndex;
    protected int CountEnters;

    protected int ModuleId;
    protected static int ModuleCounter = 1;

    protected float AnimationDuration;
    protected string UserInput;
    protected string ConsoleText;
    protected bool ModuleSelected;
    protected bool IsAnimating;
    protected bool IsConsoleOpen;
    protected string Commands;
    protected List<Files> FilesAnswer = new List<Files>();
    protected List<Files> FilesInput = new List<Files>();
    protected Files CurrentCommittingFile;

    protected List<MeshRenderer> AllObjects;

    protected Color32 RedColor = new Color32(200, 0, 0, 255);
    protected Color32 GreenColor = new Color32(0, 200, 0, 255);
    protected Color32 YellowColor = new Color32(200, 200, 0, 255);

    // 0 - red, 1 - yellow, 2 - green
    protected readonly List<MeshRenderer> IndicatorElements = new List<MeshRenderer>();

    void Awake()
    {
        ModuleId = ModuleCounter++;

        Module.OnActivate += Activate;
        Module.GetComponent<KMSelectable>().OnFocus += delegate() { ModuleSelected = true; };
        Module.GetComponent<KMSelectable>().OnDefocus += delegate() { ModuleSelected = false; };
        BtnNext.OnInteract += delegate()
        {
            Next();
            return false;
        };
        BtnPrev.OnInteract += delegate()
        {
            Prev();
            return false;
        };
        BtnSubmit.OnInteract += delegate()
        {
            ToogleConsole();
            return false;
        };
        ColourblindActive = colourblind.ColorblindModeActive;
        SetColourblindMode();
    }

    protected void Activate()
    {
    }

    void Start()
    {
        Init();
    }

    void SetColourblindMode()
    {
        colorblindText.gameObject.SetActive(ColourblindActive);
    }

    protected void Init()
    {
        IndicatorElements.Add(indicator.transform.Find("top").GetComponent<MeshRenderer>());
        IndicatorElements.Add(indicator.transform.Find("bottom").GetComponent<MeshRenderer>());
        IndicatorElements.Add(indicator.transform.Find("middle").GetComponent<MeshRenderer>());
        
        // Files generator 5-8 files
        int countFiles = Random.Range(5, 9);
        for (int i = 0; i < countFiles; i++)
        {
            Files file = new Files();
            AllFiles.Add(file);
            FilesAnswer.Add((Files)file.Clone());
        }

        consoleTM.GetComponent<Renderer>().enabled = false;
        FileIndex = 0;
        CurrentFile = AllFiles[FileIndex];
        SetScreen(CurrentFile.Name);
        SetIndicator(CurrentFile.Status);
        consoleTM.text = "";

        RepoName = Repositories[Random.Range(0, Repositories.Count)];
        repository.text = RepoName;

        ContributorName = Contributes[Random.Range(0, Contributes.Count)];
        contributor.text = ContributorName;

        DayOfWeekName = DaysOfWeek[Random.Range(0, DaysOfWeek.Count)];
        dayOfWeek.text = DayOfWeekName;

        IsSolved = false;
        ModuleSelected = false;
        IsConsoleOpen = false;
        IsAnimating = false;
        Typing = true;
        MaxLengthInput = 40;
        MaxEntersLength = 10;
        AnimationDuration = .2f;
        CountEnters = 0;
        UserInput = "";
        ConsoleText = "> ";
        Commands = "";

        Log(String.Format("Generated repo \"{0}\" with contributer \"{1}\" at \"{2}\" \nFile Count is {3}",
            repository.text, contributor.text, dayOfWeek.text, AllFiles.Count));

        string filesString = "";
        int index = 1;
        foreach (var file in AllFiles)
        {
            filesString += String.Format("{2} Name: {0}, status: {1}\n", file.Name, file.Status, index);
            index++;
        }

        Log(String.Format("Generated files:\n{0}", filesString));

        // Finding answer
        double startingValue = FindStartingValueForFile();
        DoWCoef = (int)FindDoWCoefficient();
        RepoCoef = (int)FindRepoCoefficient();
        
        Log(String.Format("Starting file value is {0}", startingValue));
        Log("Repository coefficient is " + RepoCoef);
        Log("Day of week coefficient is " + DoWCoef);
        
        foreach (Files file in FilesAnswer)
        {
            file.Status = FindStatusForFile(file, startingValue);
            file.Value = FindValueForFile(file, startingValue);
        }

        FilesAnswer = AllFiles.Count % 2 == 0 ?
            FilesAnswer.OrderBy(file => file.Value).ToList() :
            FilesAnswer.OrderByDescending(file => file.Value).ToList();

        filesString = "";
        index = 1;
        foreach (var file in FilesAnswer)
        {
            filesString += String.Format("{3} Name: {0}, required status: {1}, value is {2}\n", file.Name, file.Status,
                file.Value, index);
            index++;
        }

        Log(String.Format("Solition(in order committing):\n{0}", filesString));
    }

    protected void Next()
    {
        BtnNext.AddInteractionPunch(.5f);
        Audio.PlaySoundAtTransform(buttonPressedSound.name, Module.transform);
        FileIndex += 1;
        if (FileIndex > AllFiles.Count - 1)
            FileIndex = 0;

        CurrentFile = AllFiles[FileIndex];
        SetScreen(CurrentFile.Name);
        SetIndicator(CurrentFile.Status);
    }

    protected void Prev()
    {
        BtnPrev.AddInteractionPunch(.5f);
        Audio.PlaySoundAtTransform(buttonPressedSound.name, Module.transform);
        FileIndex -= 1;
        if (FileIndex < 0)
            FileIndex = AllFiles.Count - 1;

        CurrentFile = AllFiles[FileIndex];
        SetScreen(CurrentFile.Name);
        SetIndicator(CurrentFile.Status);
    }

    // Open/Close console
    protected void ToogleConsole()
    {
        if (IsAnimating) return;
        Audio.PlaySoundAtTransform(consoleOpensSound.name, Module.transform);
        BtnSubmit.AddInteractionPunch(.5f);
        IsAnimating = true;

        StartCoroutine(ToogleElementsVisible());
        Vector3 inFullSize = new Vector3(0.17f, 0.001f, 0.17f);
        Vector3 inFullPosition = new Vector3(0f, -0.02f, 0f);

        Vector3 inBaseSize = new Vector3(0.15f, 0.001f, 0.02f);
        Vector3 inBasePosition = new Vector3(-0.0035f, -0.02f, -0.065f);

        StartCoroutine(IsConsoleOpen
            ? PlayAnimation(inBaseSize, inBasePosition, AnimationDuration)
            : PlayAnimation(inFullSize, inFullPosition, AnimationDuration));

        IsConsoleOpen = !IsConsoleOpen;
        consoleTM.text = ConsoleText;
        Log("Console is " + (IsConsoleOpen ? "open" : "closed"));
    }

    protected IEnumerator PlayAnimation(Vector3 size, Vector3 position, float duration)
    {
        float delta = 0;
        while (delta < 1)
        {
            yield return null;
            delta += Time.deltaTime / duration;
            BtnSubmit.transform.localScale = delta * size;
            BtnSubmit.transform.localPosition = delta * position;
        }

        // These need be for fix extra scaling after animation
        BtnSubmit.transform.localScale = size;
        BtnSubmit.transform.localPosition = position;

        IsAnimating = false;
    }

    protected IEnumerator ToogleElementsVisible()
    {
        if (AllObjects == null)
        {
            MeshRenderer infoplate = Module.transform.Find("infoPlate").GetComponent<MeshRenderer>();
            AllObjects = new List<MeshRenderer>()
            {
                BtnPrev.GetComponent<MeshRenderer>(),
                BtnNext.GetComponent<MeshRenderer>(),
                repository.GetComponent<MeshRenderer>(),
                contributor.GetComponent<MeshRenderer>(),
                dayOfWeek.GetComponent<MeshRenderer>(),
                colorblindText.GetComponent<MeshRenderer>(),
                screen.GetComponent<MeshRenderer>(),
                infoplate,
                Module.transform.Find("submittion").transform.Find("sumbitBG").GetComponent<MeshRenderer>(),
                Module.transform.Find("submittion").transform.Find("submitionField").transform.Find("userText")
                    .GetComponent<MeshRenderer>(),
                Module.transform.Find("fileManager").transform.Find("indicator").transform.Find("top")
                    .GetComponent<MeshRenderer>(),
                Module.transform.Find("fileManager").transform.Find("indicator").transform.Find("bottom")
                    .GetComponent<MeshRenderer>(),
                Module.transform.Find("fileManager").transform.Find("screen").transform.Find("screen")
                    .GetComponent<MeshRenderer>(),
            };
            List<Transform> pins = new List<Transform>
            {
                infoplate.transform.Find("pin1"),
                infoplate.transform.Find("pin2"),
                infoplate.transform.Find("pin3"),
            };
            foreach (var pin in pins)
            {
                int children = pin.transform.childCount;
                for (int i = 0; i < children; ++i)
                    AllObjects.Add(pin.transform.GetChild(i).GetComponent<MeshRenderer>());
            }
        }

        bool shown = IsConsoleOpen;
        foreach (var obj in IndicatorElements)
        {
            obj.GetComponent<Renderer>().enabled = shown;
        }

        foreach (var obj in AllObjects)
        {
            obj.GetComponent<Renderer>().enabled = shown;
        }

        Transform btnPrevHl = BtnPrev.transform.Find("PH"),
            btnNextHl = BtnNext.transform.Find("NH"),
            btnSubmitHl = BtnSubmit.transform.Find("submitionFieldHighlight");
        if (shown)
        {
            btnSubmitHl.localScale = new Vector3(1.015f, 1f, 1.015f);
            btnNextHl.localScale = new Vector3(1.1f, 0.5f, 1.1f);
            btnPrevHl.localScale = new Vector3(1.1f, 0.5f, 1.1f);

            consoleTM.GetComponent<Renderer>().enabled = false;
        }
        else
        {
            btnSubmitHl.localScale = new Vector3(0f, 0f, 0f);
            btnNextHl.localScale = new Vector3(0f, 0f, 0f);
            btnPrevHl.localScale = new Vector3(0f, 0f, 0f);

            yield return new WaitForSeconds(AnimationDuration);
            consoleTM.GetComponent<Renderer>().enabled = true;
        }

        yield return null;
    }

    protected int GetAlphabeticPosition(char letter)
    {
        if (Char.IsDigit(letter))
        {
            return letter - '0';
        }

        return Alphabet.IndexOf(Char.ToLower(letter)) + 1;
    }

    protected int GetBase36Number(char letter)
    {
        if (Char.IsDigit(letter))
        {
            return letter - '0';
        }
        
        return Alphabet.IndexOf(Char.ToLower(letter)) + 1 + 9;
    }

    // Changing screen with file names
    protected void SetScreen(string text)
    {
        screen.text = text;
    }

    // Function that make indicator red \(0_o)/
    protected void SetIndicator(int status)
    {
        Color32 color = new Color32();
        string colorText = "";

        if (status == 0)
        {
            colorText = "Red";
            color = RedColor;
        }

        if (status == 1)
        {
            colorText = "Yellow";
            color = YellowColor;
        }

        if (status == 2)
        {
            colorText = "Green";
            color = GreenColor;
        }

        foreach (MeshRenderer obj in IndicatorElements)
            obj.material.color = color;
        if (ColourblindActive)
            colorblindText.text = colorText;
    }

    // Finding starting file coef 
    protected double FindStartingValueForFile()
    {
        int baseValue = 0;
        string letters = Bomb.GetSerialNumber();
        foreach (Files file in AllFiles)
            letters += file.Name[0];
        foreach (char letter in letters)
            if (Char.IsLetterOrDigit(letter))
                baseValue += GetBase36Number(letter);
            else
                baseValue += 13;
        return baseValue % 256;
    }

    // Finding value for file coef 
    protected double FindValueForFile(Files file, double value)
    {
        string fileName = file.Name;
        if (fileName.Any(char.IsDigit))
        {
            int multiplication = 1;
            foreach (var symbol in fileName)
            {
                if (Char.IsDigit(symbol))
                {
                    if (symbol == '0')
                        multiplication *= 10;
                    multiplication *= symbol - '0';
                }
            }

            value += multiplication;
        }

        if (!ContributorName.All(char.IsLetterOrDigit))
        {
            value += 50;
        }

        if (ContributorName.Any(char.IsDigit))
        {
            int count = 0;
            foreach (var symbol in ContributorName)
            {
                if (char.IsDigit(symbol))
                {
                    count++;
                }
            }

            value += (count + 1);
        }

        int countLowerCaseLetters = 0;
        foreach (char symbol in fileName)
        {
            if (!char.IsLetterOrDigit(symbol))
                value += 12;
            else if (char.IsLower(symbol))
                countLowerCaseLetters++;
            else if (char.IsUpper(symbol))
                value += 5;
        }

        if (fileName.Count(symbol => symbol == '.') > 1)
        {
            value *= 3;
        }

        if (countLowerCaseLetters > 0)
        {
            value -= countLowerCaseLetters * 3;
        }

        foreach (string ind in Bomb.GetIndicators())
            if (ind.Equals("CLR") ||
                ind.Equals("FRK") ||
                ind.Equals("FRQ") ||
                ind.Equals("NSA"))
                value += 15;

        if (ContributorName.Any(char.IsUpper))
        {
            value -= 20 * ContributorName.Count(char.IsUpper);
        }

        if (new[] { "Sunday", "Saturday", "Friday" }.Contains(DayOfWeekName))
        {
            value -= 30;
        }
        else if (new[] { "Monday", "Wednesday", "Thursday" }.Contains(DayOfWeekName))
        {
            value += 20;
        }
        
        foreach (string ind in Bomb.GetPorts())
            if (ind.Equals("RJ4") ||
                ind.Equals("PS2") ||
                ind.Equals("Serial"))
                value -= 10;

        bool firstRule = false, secondRule = false;
        foreach (var moduleName in Bomb.GetModuleNames())
        {
            if (new[] { "Scripting", "Brainf---", "Markscript" }.Contains(moduleName) && !firstRule)
            {
                firstRule = true;
            }

            if (new[]{ "Mortal Kombat", "Etterna", "Geometry Dash", "Sonic the Hedgehog" }.Contains(moduleName) &&
                !secondRule)
            {
                secondRule = true;
            }
        }

        if (firstRule)
        {
            value += 69;
        }

        if (secondRule)
        {
            value /= 2;
        }

        return value < 0 ? -value : value;
    }

    protected double FindStatusCoef(int color)
    {
        Dictionary<int, double> statuses = new Dictionary<int, double>()
        {
            { 0, 0.75 },
            { 1, 1.5 },
            { 2, 3 }
        };
        return statuses[color];
    }

    // Finding repo coef 
    protected double FindRepoCoefficient()
    {
        int sumALlLetters = 0;
        foreach (char letter in RepoName)
        {
            sumALlLetters += GetAlphabeticPosition(letter);
        }

        int coef = sumALlLetters % (2 * RepoName.Length) + 1;
        return coef;
    }

    // Finding day of week coef 
    protected double FindDoWCoefficient()
    {
        Dictionary<string, int> daysIndex = new Dictionary<string, int>()
        {
            { "Monday", 0 },
            { "Tuesday", 1 },
            { "Wednesday", 2 },
            { "Thursday", 3 },
            { "Friday", 4 },
            { "Saturday", 5 },
            { "Sunday", 6 },
        };

        List<List<double>> daysCoefficients = new List<List<double>>
        {
            new List<double> { 18, 14, 12, 9, 10, 9, 6 },
            new List<double> { 16, 12, 8, 11, 8, 6, 7 },
            new List<double> { 15, 13, 12, 4, 5, 8, 10 },
            new List<double> { 13, 10, 10, 2, 6, 7, 8 },
            new List<double> { 10, 11, 7, 6, 8, 6, 7 },
            new List<double> { 7, 12, 6, 6, 4, 4, 5 },
            new List<double> { 5, 4, 4, 7, 8, 3, 1 },
        };

        DateTime today = DateTime.Today;
        double coef = daysCoefficients[daysIndex[DayOfWeekName]][daysIndex[today.DayOfWeek.ToString()]];
        return coef;
    }

    // Finding final file status
    protected int FindStatusForFile(Files file, double baseValue)
    {
        // 0 - must be removed
        // 1 - must be commited
        int status = ((int)(FindValueForFile(file, baseValue) * FindStatusCoef(file.Status)) ^ (RepoCoef * DoWCoef)) % 2;
        return status;
    }

    // Handle command
    protected void HandleCommand(string command)
    {
        if (IsSolved) return;
        Log(String.Format("You entered command {0}", command));
        Commands = Commands + command + "/";
        command = command.ToLower();
        if (command == "back")
        {
            ToogleConsole();
            Commands += "You are getting back...";
        }
        else if (command == "clear")
        {
            Commands = "";
            CountEnters = -1;
            return;
        }
        else if (command == "git reset 3f496")
        {
            FilesInput = new List<Files>();
            CurrentCommittingFile = null;
            Commands += "Your repository has been reset to \\initial state...";
        }
        else if (command == "git reset head")
        {
            CurrentCommittingFile = null;
            Commands += "Your repository have been returned to current commit.";
        }
        else if (command == "git status")
        {
            if (CurrentCommittingFile != null)
            {
                Commands += String.Format("Added file {0} (status {1})", CurrentCommittingFile.Name,
                    CurrentCommittingFile.Status);
            }
            else
            {
                Commands += "You haven't been added file yet...";
            }
        }
        else if (command == "git commit")
        {
            if (CurrentCommittingFile == null)
            {
                Incorrect();
                Commands += "Count files in commit is wrong. \\Please try again...";
            }
            else
            {
                FilesInput.Add(CurrentCommittingFile);
                CurrentCommittingFile = null;
                Commands += "Committed successfully!";
            }
        }
        else if (command.StartsWith("git push"))
        {
                    
            // *If you have a lit BOB, day of week on module is sunday <u>AND</u> more than two battaries, so
            if (Bomb.Batta)
            {
                Commands += "Github isn't responding./";
                Solve();
            }
            if (FilesAnswer.Count != FilesInput.Count)
            {
                Incorrect();
                Commands += "Wrong number of files in commit./";
                return;
            }
            for (int index = 0; index < FilesAnswer.Count; index++)
            {
                if (FilesAnswer[index].Name != FilesInput[index].Name
                    || FilesAnswer[index].Status != FilesInput[index].Status)
                {
                    Incorrect();
                    Commands += "Strike... Wrong " + index + " file./";
                    return;
                }
                Commands += "Changes have been pushed./";
                Solve();
            }
        }
        else if (command.StartsWith("git add"))
        {
            if (FilesInput.Contains(CurrentCommittingFile))
            {
                Log("Trying to commit already commited file. Strike...");
                Incorrect();
                Commands += "This file is already committed./";
                return;
            }
            if (CurrentCommittingFile != null)
            {
                Log("Trying to add second file in commit. Strike...");
                Incorrect();
                Commands += "File already present in current commit./";
                return;
            }

            if (command.Length <= 8) {
                Commands += "No such file found./";
                return;
            }
            string fileNameOrId = command.Substring(8);

            if (fileNameOrId.All(Char.IsDigit))
            {
                int index = Int32.Parse(fileNameOrId) - 1;
                if (index >= 0 && index < AllFiles.Count)
                    CurrentCommittingFile = AllFiles[index];
            }
            else
            {
                CurrentCommittingFile = AllFiles.Find(file => file.Name.ToLower() == fileNameOrId.ToLower());
            }

            if (CurrentCommittingFile == null)
            {
                Commands += "No such file found./";
                return;
            }

            CurrentCommittingFile.Status = 1;
            Commands += "File added.";
        }
        else if (command.StartsWith("git rm"))
        {
            if (FilesInput.Contains(CurrentCommittingFile))
            {
                Log("Trying to commit already commited file. Strike...");
                Incorrect();
                Commands += "This file is already committed./";
                return;
            }
            if (CurrentCommittingFile != null)
            {
                Log("Trying to add second file in commit. Strike...");
                Incorrect();
                Commands += "File already present in current commit./";
                return;
            }

            string fileNameOrId = command.Substring(7);
            if (fileNameOrId.All(Char.IsDigit))
            {
                int index = Int32.Parse(fileNameOrId) - 1;
                if (index >= 0 && index < AllFiles.Count)
                    CurrentCommittingFile = AllFiles[index];
            }
            else
            {
                CurrentCommittingFile = AllFiles.Find(file => file.Name.ToLower() == fileNameOrId.ToLower());
            }

            if (CurrentCommittingFile == null)
            {
                Commands += "No such file found.";
                return;
            }

            CurrentCommittingFile.Status = 0;
            Commands += "File removed.";
        }
        else
        {
            Commands += "Unknown command...";
        }

        Commands += "/";
    }

    // Handle strike
    protected void Incorrect()
    {
        Log("Something wrong. Strike!");
        Module.HandleStrike();
    }

    // Solve module
    protected void Solve()
    {
        Log("Entered correct answer. Module solved!");
        Module.HandlePass();
        IsSolved = true;
    }

    // Logging function
    private void Log(string log)
    {
        Debug.LogFormat("[GitCommiting #{0}] {1}", ModuleId, log);
    }

    private IEnumerator WaitFor(float seconds)
    {
        yield return new WaitForSeconds(seconds);
    }

    // Handle button presses
    private void ButtonPressed(KeyCode key)
    {
        if (IsSolved) return;
        if (!Typing) return;
        if (!IsConsoleOpen) return;
        
        if (UserInput.Length != 0) 
            if (key == KeyCode.Backspace)
                UserInput = UserInput.Substring(0, UserInput.Length - 1);
        if (UserInput.Length >= MaxLengthInput && key != KeyCode.Return)
            return;
        
        if ((key >= KeyCode.A && key <= KeyCode.Z) 
            || key == KeyCode.Space 
            || (key >= KeyCode.Keypad0 && key <= KeyCode.Keypad9)
            || (key >= KeyCode.Alpha0 && key <= KeyCode.Alpha9) 
            || (key == KeyCode.Minus || key == KeyCode.KeypadMinus)
            || (key == KeyCode.KeypadPeriod || key == KeyCode.Period) 
            || (key == KeyCode.Return))
            Audio.PlaySoundAtTransform(keyboardSound.name, Module.transform);
        
        if (key >= KeyCode.A && key <= KeyCode.Z)
            UserInput += key.ToString().ToLower();
        if (key == KeyCode.Space)
            UserInput += " ";
        if (key >= KeyCode.Keypad0 && key <= KeyCode.Keypad9) 
            key = KeyCode.Alpha0 + (key - KeyCode.Keypad0);
        if (key >= KeyCode.Alpha0 && key <= KeyCode.Alpha9)
            UserInput += key - KeyCode.Alpha0;
        if (key == KeyCode.Minus || key == KeyCode.KeypadMinus)
            UserInput += '-';
        if (key == KeyCode.KeypadPeriod || key == KeyCode.Period)
            UserInput += '.';
        
        if (key == KeyCode.Return)
        {
            HandleCommand(UserInput);
            CountEnters++;
            UserInput = "";
            ConsoleText = "> ";

            if (CountEnters > MaxEntersLength)
            {
                for (int i = 0; i < 2; i++)
                {
                    Commands = Commands.Substring(Commands.IndexOf('/') + 1);
                }

                CountEnters--;
            }
            foreach (var str in Commands)
            {
                if (str == '/')
                {
                    ConsoleText += "\n> ";
                }
                else
                {
                    ConsoleText += str;
                }
            }
        }
        consoleTM.text = ConsoleText + UserInput;
    }
    void OnGUI()
    {
        if (!ModuleSelected)
            return;
        Event e = Event.current;
        if (e.type != EventType.KeyDown)
            return;
        ButtonPressed(e.keyCode);
        StartCoroutine(WaitFor(0.5f));
    }
}
