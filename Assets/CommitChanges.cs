using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;
using Random = UnityEngine.Random;

public class CommitChanges : MonoBehaviour
{
    public KMAudio sound;
    public KMBombModule module;
    public KMBombInfo bomb;
    public KMColorblindMode colourblind;
    public KMSelectable btnPrev, btnNext, btnSubmit;
    public TextMesh screen, consoleTM, repository, contributor, dayOfWeek, colorblindText;
    public GameObject indicator;

    private readonly Dictionary<string, string> _sounds = new Dictionary<string, string>()
    {
        {"keyPressed", "key-pressed"},
        {"consoleOpens", "console-opening"},
        {"buttonPressed", "SwitchSound"},
        {"SolvingSound", "SolvingSound"},
    };

    private int _maxLengthInput;
    private int _maxLinesLength;
    private bool _isSolved;
    private bool _isTyping;

    private int _repositoryCoefficient;
    private string _repositoryName;

    private readonly List<string> _possiblesRepositories = new List<string>()
    {
        "KtaneContent", "GoldenApple", "StrangeProject", "HelloWorld", 
        "TFCThirdUltra", "UnityEngineCode", "HexOS", "ValvesSteam",
        "UbuntuSource", "WindowsNine", "KeepTalking", "ThirdTry",
        "GitStudying", "UnityProject", "Pentagon", "FifthCommit",
        "YoutubeSrc", "SnapChatRepo", "qwertyuiop"
    };

    private string _contributorName;

    private readonly List<string> _possiblesContributes = new List<string>()
    {
        "_Play_", "poisongreen", "Megum",
        "Timwi", "samfundev", "Kuro",
        "Emik", "Blananas2", "eXish", "SpeakingEvil",
        "Royal_Flu$h", "rand06", "Hexicube",
        "SL7205", "BigCrunch22", "Deaf", "Hawker",
        "SteelCrateGames", "QuinnWuest", "GhostSalt",
        "BigCrunch22", "Awesome7285", "TylerY2992",
        "ObjectsCountries", "GoodHood", "lingomaniac88",
        "Obvious", "Eltrick", "Crazycaleb",
    };

    private int _dayOfWeekCoefficient;
    private string _dayOfWeekName;

    private readonly List<string> _possiblesDaysOfWeek = new List<string>()
    { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };

    private bool _isColourblindActive;
    private readonly List<string> _possiblesCommands = new List<string>()
    {
        "git",
        "back",
        "clear"
    }; 
    private readonly List<Files> _allFiles = new List<Files>();
    private Files _currentFile;
    private int _currentFileIndex;
    private int _amountLines;

    private int _moduleID;
    private static int _moduleCounter = 1;

    private float _animationDuration;
    private string _userInput;
    private string _consoleText;
    private bool _isModuleSelected;
    private bool _isAnimating;
    private bool _isConsoleOpen;
    private string _commands;
    private List<Files> _answerFiles = new List<Files>();
    private List<Files> _inputtedFiles = new List<Files>();
    private Files _currentCommittingFile;

    private List<MeshRenderer> _allObjects;
    private Color32 _colorRed = new Color32(255, 0, 0, 255);
    private Color32 _colorGreen = new Color32(0, 200, 0, 255);
    private Color32 _colorWhite = new Color32(255, 255, 255, 255);

    // 0 - red, 1 - green
    private readonly List<MeshRenderer> _indicatorElements = new List<MeshRenderer>();

    void Awake()
    {
        _moduleID = _moduleCounter++;
        module.OnActivate += Activate;
        module.GetComponent<KMSelectable>().OnFocus += delegate() { _isModuleSelected = true; };
        module.GetComponent<KMSelectable>().OnDefocus += delegate() { _isModuleSelected = false; };
        btnNext.OnInteract += delegate() { Next(); return false; };
        btnPrev.OnInteract += delegate() { Prev(); return false; };
        btnSubmit.OnInteract += delegate() { ToggleConsole(); return false; };
        _isColourblindActive = colourblind.ColorblindModeActive;
        SetColourblindMode();
    }

    private void Activate()
    {
    }

    void Start()
    {
        Init();
    }

    private void Init()
    {
        _indicatorElements.Add(indicator.transform.Find("top").GetComponent<MeshRenderer>());
        _indicatorElements.Add(indicator.transform.Find("bottom").GetComponent<MeshRenderer>());
        _indicatorElements.Add(indicator.transform.Find("middle").GetComponent<MeshRenderer>());

        // Files generator 3-5 files
        int countFiles = Random.Range(3, 6);
        for (int i = 0; i < countFiles; i++)
        {
            Files file = new Files();
            _allFiles.Add(file);
            _answerFiles.Add((Files)file.Clone());
        }

        consoleTM.GetComponent<Renderer>().enabled = false;
        _currentFileIndex = 0;
        screen.color = _currentFileIndex == 0 ? Color.green : Color.white;
        _currentFile = _allFiles[_currentFileIndex];
        SetScreen(_currentFile.Name);
        SetIndicator(_currentFile.Status);
        consoleTM.text = "";

        _repositoryName = _possiblesRepositories[Random.Range(0, _possiblesRepositories.Count)];
        repository.text = _repositoryName;

        _contributorName = _possiblesContributes[Random.Range(0, _possiblesContributes.Count)];
        contributor.text = _contributorName;

        _dayOfWeekName = _possiblesDaysOfWeek[Random.Range(0, _possiblesDaysOfWeek.Count)];
        dayOfWeek.text = _dayOfWeekName;

        _isSolved = false;
        _isModuleSelected = false;
        _isConsoleOpen = false;
        _isAnimating = false;
        _isTyping = true;
        _maxLengthInput = 35;
        _maxLinesLength = 10;
        _animationDuration = .2f;
        _amountLines = 0;
        _userInput = "";
        _consoleText = "> ";
        _commands = "";

        Log(String.Format("Generated repo \"{0}\" with contributor \"{1}\" at \"{2}\" \nFile Count is {3}",
            repository.text, contributor.text, dayOfWeek.text, _allFiles.Count));

        string filesString = "";
        int index = 1;
        foreach (var file in _allFiles)
        {
            filesString += String.Format("{2} Name: {0}, status: {1}\n", file.Name, file.Status, index);
            index++;
        }

        Log(String.Format("Generated files:\n{0}", filesString));

        // Finding answer
        double startingValue = FindStartingValueForFile();
        _dayOfWeekCoefficient = (int)FindDayOfWeekCoefficient();
        _repositoryCoefficient = (int)FindRepositoryCoefficient();
        
        Log(String.Format("Starting file value is {0}", startingValue));
        Log("Repository coefficient is " + _repositoryCoefficient);
        Log("Day of week coefficient is " + _dayOfWeekCoefficient);
        
        foreach (Files file in _answerFiles)
        {
            file.Status = FindStatusForFile(file, startingValue);
            file.Value = FindValueForFile(file, startingValue);
        }


        _answerFiles = _allFiles.Count % 2 == 0 ?
            _answerFiles.OrderBy(file => file.Value).ToList() :
            _answerFiles.OrderByDescending(file => file.Value).ToList();

        filesString = "";
        index = 1;
        foreach (var file in _answerFiles)
        {
            filesString += String.Format("{3} Name: {0}, required status: {1}, value is {2}\n", file.Name, file.Status,
                file.Value, index);
            index++;
        }

        Log(String.Format("Solution(in order committing):\n{0}", filesString));
    }

    void SetColourblindMode()
    {
        colorblindText.gameObject.SetActive(_isColourblindActive);
    }

    private void Next()
    {
        btnNext.AddInteractionPunch(.5f);
        sound.PlaySoundAtTransform(_sounds["buttonPressed"], btnNext.transform);
        _currentFileIndex += 1;
        if (_currentFileIndex > _allFiles.Count - 1)
            _currentFileIndex = 0;
        
        screen.color = _currentFileIndex == 0 ? Color.green : Color.white;

        _currentFile = _allFiles[_currentFileIndex];
        SetScreen(_currentFile.Name);
        SetIndicator(_currentFile.Status);
    }

    private void Prev()
    {
        btnPrev.AddInteractionPunch(.5f);
        sound.PlaySoundAtTransform(_sounds["buttonPressed"], btnPrev.transform);
        _currentFileIndex -= 1;
        if (_currentFileIndex < 0)
            _currentFileIndex = _allFiles.Count - 1;
        
        screen.color = _currentFileIndex == 0 ? Color.green : Color.white;

        _currentFile = _allFiles[_currentFileIndex];
        SetScreen(_currentFile.Name);
        SetIndicator(_currentFile.Status);
    }

    // Open/Close console
    private void ToggleConsole()
    {
        btnSubmit.AddInteractionPunch(.5f);
        if (_isAnimating) return;
        sound.PlaySoundAtTransform(_sounds["consoleOpens"], btnSubmit.transform);
        _isAnimating = true;

        StartCoroutine(ToggleElementsVisible());
        Vector3 inFullSize = new Vector3(0.17f, 0.001f, 0.17f);
        Vector3 inFullPosition = new Vector3(0f, -0.02f, 0f);

        Vector3 inBaseSize = new Vector3(0.15f, 0.001f, 0.02f);
        Vector3 inBasePosition = new Vector3(-0.0035f, -0.02f, -0.065f);

        StartCoroutine(_isConsoleOpen
            ? PlayAnimation(inBaseSize, inBasePosition, _animationDuration)
            : PlayAnimation(inFullSize, inFullPosition, _animationDuration));

        _isConsoleOpen = !_isConsoleOpen;
        consoleTM.text = _consoleText;
        Log("Console is " + (_isConsoleOpen ? "open" : "closed"));
    }

    private IEnumerator PlayAnimation(Vector3 size, Vector3 position, float duration)
    {
        float delta = 0;
        while (delta < 1)
        {
            yield return null;
            delta += Time.deltaTime / duration;
            btnSubmit.transform.localScale = delta * size;
            btnSubmit.transform.localPosition = delta * position;
        }

        // These need be for fix extra scaling after animation
        btnSubmit.transform.localScale = size;
        btnSubmit.transform.localPosition = position;

        _isAnimating = false;
    }

    private IEnumerator ToggleElementsVisible()
    {
        if (_allObjects == null)
        {
            GatherAllElement();
        }

        bool shown = _isConsoleOpen;
        foreach (var obj in _indicatorElements)
            obj.GetComponent<Renderer>().enabled = shown;

        foreach (var obj in _allObjects)
            obj.GetComponent<Renderer>().enabled = shown;

        Transform btnPrevHl = btnPrev.transform.Find("PH"),
            btnNextHl = btnNext.transform.Find("NH"),
            btnSubmitHl = btnSubmit.transform.Find("submittingFieldHighlight");
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

            yield return new WaitForSeconds(_animationDuration);
            consoleTM.GetComponent<Renderer>().enabled = true;
        }

        yield return null;
    }

    private int GetAlphabeticPosition(char letter)
    {
        if (Char.IsDigit(letter))
            return letter - '0';

        return GetBase36Number(letter) - 9;
    }

    private int GetBase36Number(char letter)
    {
        return "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ".IndexOf(Char.ToUpper(letter));
    }

    private void GatherAllElement()
    {
        MeshRenderer infoPlate = module.transform.Find("infoPlate").GetComponent<MeshRenderer>();
            
        _allObjects = new List<MeshRenderer>()
        {
            btnPrev.GetComponent<MeshRenderer>(),
            btnNext.GetComponent<MeshRenderer>(),
            repository.GetComponent<MeshRenderer>(),
            contributor.GetComponent<MeshRenderer>(),
            dayOfWeek.GetComponent<MeshRenderer>(),
            colorblindText.GetComponent<MeshRenderer>(),
            screen.GetComponent<MeshRenderer>(),
            infoPlate,
            module.transform.Find("submitting").transform.Find("submittingBG").GetComponent<MeshRenderer>(),
            module.transform.Find("submitting").transform.Find("submittingField").transform.Find("consoleStartingText")
                .GetComponent<MeshRenderer>(),
            module.transform.Find("fileManager").transform.Find("Indicator").transform.Find("top")
                .GetComponent<MeshRenderer>(),
            module.transform.Find("fileManager").transform.Find("Indicator").transform.Find("bottom")
                .GetComponent<MeshRenderer>(),
            module.transform.Find("fileManager").transform.Find("screen").transform.Find("screen")
                .GetComponent<MeshRenderer>(),
        };
            
        List<Transform> pins = new List<Transform>
        {
            infoPlate.transform.Find("pin1"),
            infoPlate.transform.Find("pin2"),
            infoPlate.transform.Find("pin3"),
        };
            
        foreach (var pin in pins)
        {
            int children = pin.transform.childCount;
            for (int i = 0; i < children; ++i)
                _allObjects.Add(pin.transform.GetChild(i).GetComponent<MeshRenderer>());
        }
    }

    // Changing screen with file names
    private void SetScreen(string text)
    {
        screen.text = text;
    }

    // Function that make indicator red \(0_o)/
    private void SetIndicator(int status)
    {
        Color32 color;
        string colorText = "";
        
        if (status == 0)
        {
            colorText = "Red";
            color = _colorRed;
        }
        else if (status == 1)
        {  
            colorText = "Green";
            color = _colorGreen;
        }
        else
            color = _colorWhite;

        foreach (MeshRenderer obj in _indicatorElements)
            obj.material.color = color;
        if (_isColourblindActive)
            colorblindText.text = colorText;
    }

    // Finding starting file coefficient 
    private double FindStartingValueForFile()
    {
        int baseValue = 0;
        string letters = bomb.GetSerialNumber();
        foreach (Files file in _allFiles)
            letters += file.Name[0];
        foreach (char letter in letters)
            if (Char.IsLetterOrDigit(letter))
                baseValue += GetBase36Number(Char.ToUpper(letter));
            else
                baseValue += 13;
        return baseValue % 64;
    }

    // Finding value for file coefficient 
    private double FindValueForFile(Files file, double value)
    {
        string fileName = file.Name;
        if (fileName.Any(char.IsDigit))
        {
            int additon = 0;
            foreach (var symbol in fileName)
            {
                if (Char.IsDigit(symbol))
                {
                    if (symbol == '0')
                    {
                        additon += 5;
                        continue;
                    }
                    additon += symbol - '0';
                }
            }

            value += additon;
        }

        if (!_contributorName.All(char.IsLetterOrDigit))
            value += 50;
        
        value += 3*_contributorName.Count(char.IsLetter);

        int countLowerCaseLetters = 0;
        foreach (char symbol in fileName)
        {
            if (!char.IsLetterOrDigit(symbol))
                value += 9;
            else if (char.IsLower(symbol))
                countLowerCaseLetters++;
            else if (char.IsUpper(symbol))
                value += 5;
        }

        if (countLowerCaseLetters > 0)
            value -= countLowerCaseLetters * 2;
        
        if (new[] { "Sunday", "Saturday", "Friday" }.Contains(_dayOfWeekName))
            value -= 30;
        
        bool firstRule = false, secondRule = false;
        foreach (var moduleName in bomb.GetModuleNames())
        {
            if (new[] { "Scripting", "Brainf---", "Markscript" }.Contains(moduleName) && !firstRule)
                firstRule = true;

            if (new[]{ "Mortal Kombat", "Etterna", "Geometry Dash", "Sonic the Hedgehog" }.Contains(moduleName)
                && !secondRule)
                secondRule = true;
        }

        if (firstRule)
            value += 33;

        if (secondRule)
            value /= 2;

        return value < 0 ? -value : value;
    }

    private double FindStatusCoefficient(int color)
    {
        Dictionary<int, double> statuses = new Dictionary<int, double>()
        {
            { 0, bomb.GetSerialNumberNumbers().Min() / (double)bomb.GetSerialNumberNumbers().Max() },
            { 1, bomb.GetSerialNumberNumbers().Sum() % 3 + 1.5 }
        };
        return statuses[color];
    }

    // Finding repo coefficient 
    private double FindRepositoryCoefficient()
    {
        int sumALlLetters = 0;
        foreach (char letter in _repositoryName)
        {
            sumALlLetters += GetAlphabeticPosition(letter);
        }
        
        return sumALlLetters % (2 * _repositoryName.Length) + 1;
    }

    // Finding day of week coefficient 
    private double FindDayOfWeekCoefficient()
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
            new List<double> { 18, 14, 12,  9, 10,  9,  6 },
            new List<double> { 16, 12,  8, 11,  8,  6,  7 },
            new List<double> { 15, 13, 12,  4,  5,  8, 10 },
            new List<double> { 13, 10, 10,  2,  6,  7,  8 },
            new List<double> { 10, 11, 7,   6,  8,  6,  7 },
            new List<double> { 7, 12,   6,  6,  4,  4,  5 },
            new List<double> { 5,  4,   4,  7,  8,  3,  1 },
        };
        return daysCoefficients[daysIndex[_dayOfWeekName]][daysIndex[DateTime.Today.DayOfWeek.ToString()]];
    }

    // Finding final file status
    private int FindStatusForFile(Files file, double baseValue)
    {
        // 0 - must be removed
        // 1 - must be commited
        return ((int)(FindValueForFile(file, baseValue) * FindStatusCoefficient(file.Status)) 
                ^ (_repositoryCoefficient * _dayOfWeekCoefficient)) % 2;
    }

    // Handle command
    private void HandleCommand(string command)
    {
        if (_isSolved) return;
        Log(String.Format("You entered command \"{0}\"", command));
        _commands = _commands + command + "/";
        command = command.ToLower();
        if (command == "back")
        {
            ToggleConsole();
            _commands += "You are getting back...";
        }
        else if (command == "clear")
        {
            _commands = "";
            _amountLines = -1;
            return;
        }
        else if (command.StartsWith("git reset 3f496"))
        {
            _inputtedFiles = new List<Files>();
            _currentCommittingFile = null;
            _commands += "Your repository has been reset to /initial state...";
        }
        else if (command.StartsWith("git reset head"))
        {
            _currentCommittingFile = null;
            _commands += "Your repository have been returned to /current commit.";
        }
        else if (command.StartsWith("git status"))
        {  
            if (_currentCommittingFile != null)
            {
                _commands += String.Format("New file {0} (status {1})", _currentCommittingFile.Name,
                    _currentCommittingFile.Status);
            }
            else
            {
                _commands += "Commit is clear...";
            }
        }
        else if (command.StartsWith("git commit"))
        {
            if (_currentCommittingFile == null)
            {
                Incorrect();
                _commands += "Count files in commit is wrong. /Please try again...";
            }
            else
            {
                _inputtedFiles.Add(_currentCommittingFile);
                _currentCommittingFile = null;
                _commands += "Committed successfully!";
            }
        }
        else if (command.StartsWith("git push"))
        {
                    
            // a lit BOB + day of week on module is sunday + more than two batteries, so
            if (bomb.GetBatteryCount() > 2 && bomb.IsIndicatorOn("BOB") && _dayOfWeekName == "Sunday")
            {
                _commands += "Github isn't responding./";
                Solve();
                return;
            }
            if (_answerFiles.Count != _inputtedFiles.Count)
            {
                Incorrect();
                _commands += "Wrong number of files in commit./";
                return;
            }
            for (int index = 0; index < _answerFiles.Count; index++)
            {
                if (_answerFiles[index].Name != _inputtedFiles[index].Name
                    || _answerFiles[index].Status != _inputtedFiles[index].Status)
                {
                    Incorrect();
                    _commands += "Strike... Wrong file is #" + (index + 1) + " file./";
                    return;
                }
            }
            _commands += "Changes have been pushed./";
            Solve();
            return;
        }
        else if (command.StartsWith("git add"))
        {
            if (_inputtedFiles.Contains(_currentCommittingFile))
            {
                Log("Trying to commit already commited file. Strike...");
                Incorrect();
                _commands += "This file is already committed./";
                return;
            }
            if (_currentCommittingFile != null)
            {
                Log("Trying to add second file in commit. Strike...");
                Incorrect();
                _commands += "File already present in current commit./";
                return;
            }

            if (command.Length <= 8) {
                _commands += "No such file found./";
                return;
            }
            string fileNameOrId = command.Substring(8);

            if (fileNameOrId.All(Char.IsDigit))
            {
                int index = Int32.Parse(fileNameOrId) - 1;
                if (index >= 0 && index < _allFiles.Count)
                    _currentCommittingFile = _allFiles[index];
            }
            else
            {
                _currentCommittingFile = _allFiles.Find(file => file.Name.ToLower() == fileNameOrId.ToLower());
            }

            if (_currentCommittingFile == null)
            {
                _commands += "No such file found./";
                return;
            }
            _commands += "File added.";
        }
        else if (command.StartsWith("git rm"))
        {
            if (_inputtedFiles.Contains(_currentCommittingFile))
            {
                Log("Trying to commit already commited file. Strike...");
                Incorrect();
                _commands += "This file is already committed./";
                return;
            }
            if (_currentCommittingFile != null)
            {
                Log("Trying to add second file in commit. Strike...");
                Incorrect();
                _commands += "File already present in current commit./";
                return;
            }

            string fileNameOrId = command.Substring(7);
            if (fileNameOrId.All(Char.IsDigit))
            {
                int index = Int32.Parse(fileNameOrId) - 1;
                if (index >= 0 && index < _allFiles.Count)
                    _currentCommittingFile = _allFiles[index];
            }
            else
            {
                _currentCommittingFile = _allFiles.Find(file => file.Name.ToLower() == fileNameOrId.ToLower());
            }

            if (_currentCommittingFile == null)
            {
                _commands += "No such file found.";
                return;
            }
            _commands += "File removed.";
        }
        else if (command.StartsWith("git log"))
        {
            string files = "";
            foreach (var file in _answerFiles)
            {
                files += String.Format("{0}, {1};/", file.Name, file.Status);
            }
            _commands += files;
        }
        else
        {
            _commands += "Unknown command...";
        }

        _commands += "/";
    }

    // Handle strike
    private void Incorrect()
    {
        Log("Something went wrong. Strike...");
        module.HandleStrike();
    }

    // Solve module
    private void Solve()
    {
        Log("Entered correct answer. Module solved!");
        sound.PlaySoundAtTransform(_sounds["SolvingSound"], module.transform);
        module.HandlePass();
        _isSolved = true;
        _isTyping = false;
    }

    // Logging function
    private void Log(string log)
    {
        Debug.LogFormat("[CommitChanges #{0}] {1}", _moduleID, log);
    }

    private IEnumerator WaitFor(float seconds)
    {
        yield return new WaitForSeconds(seconds);
    }

    // Handle button presses
    private void ButtonPressed(KeyCode key)
    {
        if (_isSolved) return;
        if (!_isTyping) return;
        if (!_isConsoleOpen) return;

        if (_userInput.Length != 0)
            if (key == KeyCode.Backspace)
                _userInput = _userInput.Substring(0, _userInput.Length - 1);
    
        if (_userInput.Length >= _maxLengthInput && key != KeyCode.Return) return;
        
        if ((key >= KeyCode.A && key <= KeyCode.Z) 
            || key == KeyCode.Space 
            || (key >= KeyCode.Keypad0 && key <= KeyCode.Keypad9)
            || (key >= KeyCode.Alpha0 && key <= KeyCode.Alpha9) 
            || (key == KeyCode.Minus || key == KeyCode.KeypadMinus)
            || (key == KeyCode.KeypadPeriod || key == KeyCode.Period) 
            || (key == KeyCode.Return) 
            || (key == KeyCode.Backspace && _userInput.Length != 0))
            sound.PlaySoundAtTransform(_sounds["keyPressed"], module.transform);
        
        if (key >= KeyCode.A && key <= KeyCode.Z) _userInput += key.ToString().ToLower();
        if (key == KeyCode.Space) _userInput += " ";
        if (key >= KeyCode.Keypad0 && key <= KeyCode.Keypad9) key = KeyCode.Alpha0 + (key - KeyCode.Keypad0);
        if (key >= KeyCode.Alpha0 && key <= KeyCode.Alpha9) _userInput += key - KeyCode.Alpha0;
        if (key == KeyCode.Minus || key == KeyCode.KeypadMinus) _userInput += '-';
        if (key == KeyCode.KeypadPeriod || key == KeyCode.Period) _userInput += '.';
        
        if (key == KeyCode.Return)
        {
            HandleCommand(_userInput);
            _amountLines++;
            _userInput = "";
            _consoleText = "> ";

            if (_amountLines > _maxLinesLength)
            {
                for (int i = 0; i < 2; i++) _commands = _commands.Substring(_commands.IndexOf('/') + 1);
                _amountLines--;
            }
            foreach (var str in _commands)
            {
                if (str == '/')
                    _consoleText += "\n> ";
                else
                    _consoleText += str;
            }
        }
        
        consoleTM.text = _consoleText + _userInput;
    }
    void OnGUI()
    {
        if (!_isModuleSelected)
            return;
        Event e = Event.current;
        if (e.type != EventType.KeyDown)
            return;
        ButtonPressed(e.keyCode);
        StartCoroutine(WaitFor(0.5f));
    }
    
#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Use !{0} right/left/cycle to watch files. !{0} console to open console. !{0} [command] to enter [command] in console. After entering command automatically inputting a enter.";
#pragma warning restore 414

    public IEnumerator ProcessTwitchCommand (string command) {
        if (!Regex.IsMatch(command, @"^[a-zA-Z0-9. ]{2,40}$"))
        {
            yield return "sendtochat {0}, {1}: Unknown characters or wrong length of command";
            yield break;
        }
        if (command.StartsWith("cycle"))
        {
            for (int i = 0; i < _answerFiles.Count; i++)
            {
                yield return new WaitForSeconds(5f);
                btnNext.OnInteract();
            }
            yield break;
        }
        if (command.StartsWith("left"))
        {
            btnPrev.OnInteract();
            yield break;
        }
        if (command.StartsWith("right"))
        {
            btnNext.OnInteract();
            yield break;
        }
        if (command.StartsWith("console"))
        {
            btnSubmit.OnInteract();
            yield break;
        }
        foreach (var startOfCommand in _possiblesCommands)
        {
            if (command.StartsWith(startOfCommand))
            {
                if (_isConsoleOpen)
                    HandleCommand(command);
                else
                    yield return "sendtochat {0}, use `!{1} console` to open console";
                yield break;
            }
        }
        yield return "sendtochat {0}, {1}: Unknown command. To watch help message use !{1} help";
    }

    public void TwitchHandleForcedSolve () {
        if (_isSolved) return;
        Log("Module force-solved");
        Solve();
    }
}
