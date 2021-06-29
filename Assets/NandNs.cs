using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using rnd = UnityEngine.Random;
using System.Text.RegularExpressions;

public class NandNs : MonoBehaviour
{
    public new KMAudio audio;
    public KMBombInfo bomb;
    public KMBombModule module;

    public Color[] textColors;
    public KMSelectable[] buttons;
    public TextMesh[] buttonWords;
    public Color brown;
    public Color cyan;

    private List<int>[] solution = new List<int>[5];
    private List<int> pressedButtons = new List<int>();

    private int[] buttonColors = new int[5];
    private string[] labels = new string[5];
    private int stage;
    private bool cantPress = true;
    private bool firstTime = true;

    private static readonly string[] ordinals = new string[6] { "first", "second", "third", "fourth", "fifth", "sixth" };
    private static readonly string[] colorNames = new string[6] { "red", "green", "orange", "blue", "yellow", "brown" };

    private static int moduleIdCounter = 1;
    private int moduleId;
    private bool moduleSolved;

    void Awake()
    {
        moduleId = moduleIdCounter++;
        foreach (KMSelectable button in buttons)
            button.OnInteract += delegate () { ButtonPress(button); return false; };
        module.OnActivate += delegate () { GenerateStage(); };
        for (int i = 0; i < 5; i++)
            solution[i] = new List<int>();
    }

    void GenerateStage()
    {
        if (stage != 5)
        {
            Debug.LogFormat("[N&Ns #{0}] Stage {1}:", moduleId, stage + 1);
            for (int i = 0; i < 5; i++)
                buttons[i].GetComponent<Renderer>().material.color = stage == i ? cyan : brown;
        }
        switch (stage)
        {
            case 0:
                tryAgain1:
                for (int i = 0; i < 5; i++)
                    labels[i] = new string(Enumerable.Repeat("MN", 5).Select(s => s.PickRandom()).ToArray());
                var counts = new int[5];
                for (int i = 0; i < 5; i++)
                {
                    buttonColors[i] = rnd.Range(0, 6);
                    for (int j = 0; j < 5; j++)
                        if (labels[j][i] == 'N')
                            counts[i]++;
                }
                var unique = new bool[5];
                for (int i = 0; i < 5; i++)
                    if (counts.Count(x => x == counts[i]) == 1)
                        unique[i] = true;
                if (unique.Count(b => b) != 1)
                    goto tryAgain1;
                solution[0].Add(Array.IndexOf(unique, true));
                Debug.LogFormat("[N&Ns #{0}] Labels: {1}", moduleId, labels.Join(", "));
                Debug.LogFormat("[N&Ns #{0}] Position {1} has a unique number of N's. Press that button.", moduleId, Array.IndexOf(unique, true) + 1);
                break;
            case 1:
                for (int i = 0; i < 5; i++)
                    buttonColors[i] = rnd.Range(0, 6);
                var alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
                var word = new string[] { "ATOM", "BIKE", "CELL", "DASH", "EGAD", "FONT", "GYRO", "HIKE", "ICED", "JACK", "KIND", "LONG", "MOON", "NEWT", "OXEN", "PACK", "QUIZ", "RUST", "STAN", "THAW", "USER", "VAPE", "WEST", "XYST", "YULE", "ZINC" }.PickRandom();
                Debug.LogFormat("[N&Ns #{0}] The word from the word bank is {1}.", moduleId, word);
                var extra = alphabet.Where(c => !word.Contains(c)).PickRandom();
                word += extra;
                word = new string(word.ToList().Shuffle().ToArray());
                solution[1].Add(word.IndexOf(extra));
                Debug.LogFormat("[N&Ns #{0}] The button to press is button {1}.", moduleId, solution[1][0] + 1);
                for (int i = 0; i < 5; i++)
                    labels[i] = Convert.ToString(alphabet.IndexOf(word[i]), 2).Replace('0', 'N').Replace('1', 'M').PadLeft(5, 'N');
                break;
            case 2:
                var base36 = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
                var directionTable = new string[8] { "TUMJY", "6SHA", "O751G", "2NPD", "9LKZE", "0WRX", "IQC3V", "B48F" };
                var directionNames = new string[8] { "north", "northeast", "east", "southeast", "south", "southwest", "west", "northwest" };
                buttonColors = Enumerable.Range(0, 6).ToList().Shuffle().Take(5).ToArray();
                var missingColor = Enumerable.Range(0, 6).First(x => !buttonColors.Contains(x));
                var targetSequence = "";
                for (int i = 0; i < 6; i++)
                    if (i != missingColor)
                        targetSequence += base36.IndexOf(bomb.GetSerialNumber().ElementAt(i)) % 2 == 1 ? "N" : "M";
                if (targetSequence == "NNNNN")
                    targetSequence = "NNMNN";
                if (targetSequence == "MMMMM")
                    targetSequence = "MMNMM";
                Debug.LogFormat("[N&Ns #{0}] The missing color is {1}, so ignore the {2} serial number character.", moduleId, colorNames[missingColor], ordinals[missingColor]);
                Debug.LogFormat("[N&Ns #{0}] The target sequence is {1}.", moduleId, targetSequence);
                var direction = Array.IndexOf(directionTable, directionTable.First(x => x.Contains(bomb.GetSerialNumber().ElementAt(missingColor)))); // Starts from north, goes clockwise
                Debug.LogFormat("[N&Ns #{0}] The ignored character is {1}, so go {2}.", moduleId, bomb.GetSerialNumber().ElementAt(missingColor), directionNames[direction]);
                tryAgain3:
                for (int i = 0; i < 5; i++)
                    labels[i] = new string(Enumerable.Repeat("MN", 5).Select(s => s.PickRandom()).ToArray());
                var torus = labels.Reverse().Join("");
                var startingPos = rnd.Range(0, 25);
                var curPos = startingPos;
                for (int i = 0; i < 5; i++)
                {
                    var a = torus.ToCharArray();
                    a[curPos] = targetSequence[i];
                    torus = new string(a);
                    curPos = Process(curPos, direction);
                }
                for (int i = 0; i < 25; i++)
                {
                    var str = "";
                    var tempPos = i;
                    for (int k = 0; k < 5; k++)
                    {
                        str += torus[tempPos];
                        tempPos = Process(tempPos, direction);
                    }
                    if (str == targetSequence && i / 5 != startingPos / 5)
                        goto tryAgain3;
                }
                Debug.LogFormat("[N&Ns #{0}] The torus is {1}.", moduleId, torus);
                for (int i = 0; i < 5; i++)
                    labels[4 - i] = new string(torus.Skip(5 * i).Take(5).ToArray());
                var answer = 4 - (startingPos / 5);
                solution[2].Add(answer);
                Debug.LogFormat("[N&Ns #{0}] The target sequence can be found starting at character {1} of the {2} button and going {3}. Press that button.", moduleId, (startingPos % 5) + 1, ordinals[answer], directionNames[direction]);
                break;
            case 3:
                var mButton = rnd.Range(0, 5);
                Debug.LogFormat("[N&Ns #{0}] The {1} button is the only one that begins with an M.", moduleId, ordinals[mButton]);
                tryAgain4:
                for (int i = 0; i < 5; i++)
                {
                    buttonColors[i] = rnd.Range(0, 6);
                    labels[i] = new string(Enumerable.Repeat("MN", 5).Select(s => s[rnd.Range(0, 2)]).ToArray());
                    if (i != mButton && labels[i][0] == 'M')
                    {
                        var a = labels[i].ToCharArray();
                        a[0] = 'N';
                        labels[i] = new string(a);
                    }
                    if (i == mButton && labels[i][0] == 'N')
                    {
                        var a = labels[i].ToCharArray();
                        a[0] = 'M';
                        labels[i] = new string(a);
                    }
                }
                var highestAmount = labels.Select(s => s.Count(c => c == 'N')).Max();
                if (labels.Select(s => s.Count(c => c == 'N')).Count(x => x == highestAmount) != 1)
                    goto tryAgain4;
                var c1 = buttonColors[mButton] == 0 || buttonColors[mButton] == 1 || buttonColors[mButton] == 3;
                var c2 = pressedButtons.Contains(mButton);
                var c3 = labels[mButton].Count(c => c == 'N') % 2 == 0;
                var c4 = mButton % 2 == 0;
                if (c1 && c2 && c3 && c4)
                    solution[3] = new List<int> { 0, 1, 2, 3, 4 };
                else if (c1 && c2 && c3)
                    solution[3].Add(pressedButtons[0]);
                else if (c1 && c2 && c4)
                {
                    if (!buttonColors.Any(x => x == 1))
                        buttonColors[rnd.Range(0, 5)] = 1;
                    solution[3] = buttonColors.Select((x, i) => new { value = x, index = i }).Where(x => x.value == 1).Select(x => x.index).ToList();
                }
                else if (c1 && c3 && c4)
                    solution[3].Add(2);
                else if (c2 && c3 && c4)
                {
                    if (!buttonColors.Any(x => x == 2))
                        buttonColors[rnd.Range(0, 5)] = 2;
                    solution[3] = buttonColors.Select((x, i) => new { value = x, index = i }).Where(x => x.value == 2).Select(x => x.index).ToList();
                }
                else if (c1 && c2)
                    solution[3].Add(pressedButtons[1]);
                else if (c1 && c3)
                {
                    if (!buttonColors.Any(x => x == 3))
                        buttonColors[rnd.Range(0, 5)] = 3;
                    solution[3] = buttonColors.Select((x, i) => new { value = x, index = i }).Where(x => x.value == 3).Select(x => x.index).ToList();
                }
                else if (c1 && c4)
                {
                    if (!buttonColors.Any(x => x == 5))
                        buttonColors[rnd.Range(0, 5)] = 5;
                    solution[3] = buttonColors.Select((x, i) => new { value = x, index = i }).Where(x => x.value == 5).Select(x => x.index).ToList();
                }
                else if (c2 && c3)
                    solution[3].Add(4);
                else if (c2 && c4)
                    solution[3].Add(3);
                else if (c3 && c4)
                {
                    if (!buttonColors.Any(x => x == 4))
                        buttonColors[rnd.Range(0, 5)] = 4;
                    solution[3] = buttonColors.Select((x, i) => new { value = x, index = i }).Where(x => x.value == 4).Select(x => x.index).ToList();
                }
                else if (c1)
                {
                    if (!buttonColors.Any(x => x == 0))
                        buttonColors[rnd.Range(0, 5)] = 0;
                    solution[3] = buttonColors.Select((x, i) => new { value = x, index = i }).Where(x => x.value == 0).Select(x => x.index).ToList();
                }
                else if (c2)
                    solution[3].Add(1);
                else if (c3)
                    solution[3].Add(pressedButtons[2]);
                else if (c4)
                    solution[3].Add(0);
                else
                    solution[3].Add(Array.IndexOf(labels.Select(s => s.Count(c => c == 'N')).ToArray(), highestAmount));
                Debug.LogFormat("[N&Ns #{0}] Valid buttons: {1}", moduleId, solution[3].Select(x => ordinals[x]).Join(", "));
                break;
            case 4:
                if (pressedButtons.Distinct().Count() == 4)
                {
                    var a = Enumerable.Range(0, 5).First(x => !pressedButtons.Contains(x));
                    Debug.LogFormat("[N&Ns #{0}] All buttons have been pressed except for one. Press the {1} button.", moduleId, ordinals[a]);
                    solution[4].Add(a);
                }
                else
                {
                    var base5 = Enumerable.Range(0, 5).Select(x => pressedButtons.Count(xx => xx == x)).Join("");
                    Debug.LogFormat("[N&Ns #{0}] The base-5 number from the pressed buttons is {1}.", moduleId, base5);
                    var binary = Convert.ToString(base5.Select(x => (int) x - 48).Aggregate(0, (x, y) => x * 5 + y), 2).Replace("0", "M").Replace("1", "N");
                    if (binary.Length < 5)
                        binary = binary.PadLeft(5, 'M');
                    else
                        binary = binary.Substring(0, 5);
                    var a = rnd.Range(0, 5);
                    var decoys = Enumerable.Range(0, 32).Select(x => Convert.ToString(x, 2).PadLeft(5, '0').Replace("0", "M").Replace("1", "N")).Where(x => x != binary);
                    for (int i = 0; i < 5; i++)
                        labels[i] = a == i ? binary : decoys.PickRandom();
                    Debug.LogFormat("[N&Ns #{0}] Converted to binary and taking the first 5 bits, this is {1}. Press the {2} button.", moduleId, binary, ordinals[a]);
                    solution[4].Add(a);
                }
                break;
            case 5:
                buttons[4].GetComponent<Renderer>().material.color = brown;
                moduleSolved = true;
                module.HandlePass();
                Debug.LogFormat("[N&Ns #{0}] Module solved!", moduleId);
                audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
                StartCoroutine(ShowWords(true));
                break;
        }
        StartCoroutine(ShowWords(true));
    }

    void ButtonPress(KMSelectable button)
    {
        audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, button.transform);
        button.AddInteractionPunch(.5f);
        if (moduleSolved || cantPress)
            return;
        var ix = Array.IndexOf(buttons, button);
        Debug.LogFormat("[N&Ns #{0}] You pressed the {1} button.", moduleId, ordinals[ix]);
        if (solution[stage].Contains(ix))
        {
            Debug.LogFormat("[N&Ns #{0}] That was correct.{1}", moduleId, stage == 4 ? " Progressing to the next stage..." : "");
            stage++;
            pressedButtons.Add(ix);
        }
        else
        {
            module.HandleStrike();
            Debug.LogFormat("[N&Ns #{0}] That was incorrect. Strike!", moduleId);
            Debug.LogFormat("[N&Ns #{0}] Resetting...", moduleId);
            stage = 0;
            pressedButtons.Clear();
            for (int i = 0; i < 5; i++)
                solution[i].Clear();
        }
        GenerateStage();
    }

    IEnumerator ShowWords(bool hiding)
    {
        cantPress = true;
        if (hiding && !firstTime)
        {
            for (int i = 0; i < 5; i++)
            {
                buttonWords[i].text = "";
                yield return new WaitForSeconds(.3f);
            }
        }
        firstTime = false;
        if (!moduleSolved)
        {
            yield return new WaitForSeconds(.2f);
            for (int i = 0; i < 5; i++)
            {
                buttonWords[i].text = labels[i];
                buttonWords[i].color = textColors[buttonColors[i]];
                yield return new WaitForSeconds(.3f);
            }
        }
        cantPress = false;
    }

    static string Shift(string str, int i)
    {
        return str.Substring(str.Length - i) + str.Substring(0, str.Length - i);
    }

    static int Process(int i, int direction)
    {
        var x = i % 5;
        var y = i / 5;
        switch (direction)
        {
            case 0:
                y--;
                break;
            case 1:
                y--;
                x++;
                break;
            case 2:
                x++;
                break;
            case 3:
                y++;
                x++;
                break;
            case 4:
                y++;
                break;
            case 5:
                y++;
                x--;
                break;
            case 6:
                x--;
                break;
            case 7:
                y--;
                x--;
                break;
        }
        return ((x + 5) % 5) + 5 * ((y + 5) % 5);
    }

    // Twitch Plays
    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} press <1/2/3/4/5> [Presses the button in that position from left to right.]";
    #pragma warning restore 414

    KMSelectable[] ProcessTwitchCommand(string command)
    {
        Match m;
        if ((m = Regex.Match(command, @"^\s*(?:press\s+)?([1-5])$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)).Success)
            return new[] { buttons[int.Parse(m.Groups[1].Value) - 1] };
        return null;
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        while (!moduleSolved)
        {
            while (cantPress)
                yield return true;
            buttons[solution[stage][0]].OnInteract();
        }
    }
}
