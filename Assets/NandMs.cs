using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using System.Text.RegularExpressions;

public class NandMs : MonoBehaviour
{
    public new KMAudio audio;
    public KMBombInfo bomb;
    public KMBombModule module;
    public KMRuleSeedable ruleSeedable;

    static int moduleIdCounter = 1;
    int moduleId;
    bool moduleSolved;
    bool recalcing = true;
    string[] allWords;
    public Color[] textColors;
    public KMSelectable[] buttons;
    public TextMesh[] buttonWords;

    string[][] sets;
    List<int> decidedButtons = new List<int>();
    List<int> decidedWords = new List<int>();

    int setIndex;
    int otherwordindex;
    string[] otherWords;

    void Awake()
    {
        moduleId = moduleIdCounter++;
        foreach (KMSelectable button in buttons)
            button.OnInteract += delegate () { ButtonPress(button); return false; };
        module.OnActivate += delegate () { Activate(); };

        var rnd = ruleSeedable.GetRNG();
        var allStrings = new List<string>();
        for (var strNum = 0; strNum < 32; strNum++)
        {
            var strNum2 = strNum;
            var str = "";
            for (var b = 0; b < 5; b++)
            {
                str += ((strNum2 & 1) == 1) ? "M" : "N";
                strNum2 >>= 1;
            }
            allStrings.Add(str);
        }
        rnd.ShuffleFisherYates(allStrings);

        sets = new string[10][];

        for (int r = 0; r < 5; r++)
            sets[r] = Enumerable.Range(0, 5).Select(c => allStrings[c + 5 * r]).ToArray();
        for (int c = 0; c < 5; c++)
            sets[5 + c] = Enumerable.Range(0, 5).Select(r => allStrings[c + 5 * r]).ToArray();

        allWords = allStrings.Take(25).ToArray();
    }

    void Activate()
    {
        recalcing = false;
        setIndex = UnityEngine.Random.Range(0, 10);
        otherwordindex = UnityEngine.Random.Range(0, 20);
        otherWords = allWords.Except(sets[setIndex]).ToArray();
        PickWords();
    }

    void ButtonPress(KMSelectable button)
    {
        audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, button.transform);
        button.AddInteractionPunch(.5f);
        if (moduleSolved || recalcing)
            return;
        var thisText = button.GetComponentInChildren<TextMesh>().text;
        if (otherWords[otherwordindex] != thisText)
        {
            module.HandleStrike();
            Debug.LogFormat("[N&Ms #{0}] You pressed {1}. That was incorrect. Strike! Resetting...", moduleId, thisText);
            Activate();
        }
        else
        {
            module.HandlePass();
            audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
            Debug.LogFormat("[N&Ms #{0}] You pressed {1}. That was correct. Module solved!", moduleId, thisText);
            moduleSolved = true;
            StartCoroutine(ShowWords());
        }
    }

    void PickWords()
    {
        decidedButtons = Enumerable.Range(0, 5).ToList().Shuffle().Take(4).ToList();
        decidedWords = Enumerable.Range(0, 5).ToList().Shuffle().Take(4).ToList();
        StartCoroutine(ShowWords());
    }

    IEnumerator ShowWords()
    {
        recalcing = true;
        for (int i = 0; i <= 4; i++)
        {
            buttonWords[i].text = "";
            yield return new WaitForSeconds(.3f);
        }
        if (!moduleSolved)
        {
            yield return new WaitForSeconds(.2f);
            for (int i = 0; i <= 4; i++)
            {
                int colorIndex = UnityEngine.Random.Range(0, 6);
                buttonWords[i].color = textColors[colorIndex];
                int ix = decidedButtons.IndexOf(i);
                if (ix == -1)
                    buttonWords[i].text = otherWords[otherwordindex];
                else
                    buttonWords[i].text = sets[setIndex][decidedWords[ix]];
                yield return new WaitForSeconds(.3f);
            }
        }
        recalcing = false;
        string TempLog = "";
        for (int i = 0; i < 4; i++)
          TempLog += buttonWords[i].text + " ";
        Debug.LogFormat("[N&Ms #{0}] The given texts are {1}.", moduleId, TempLog.Trim());
        Debug.LogFormat("[N&Ms #{0}] The correct word to press is {1}.", moduleId, otherWords[otherwordindex]);
    }

    // Twitch Plays
    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} press <1/2/3/4/5> [Presses the button in that position from top to bottom.]";
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
        buttons.First(x => x.GetComponentInChildren<TextMesh>().text == otherWords[otherwordindex]).OnInteract();
        while (recalcing)
        {
            yield return true;
            yield return new WaitForSeconds(.1f);
        }
    }
}
