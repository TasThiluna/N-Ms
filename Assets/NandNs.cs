using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using rnd = UnityEngine.Random;

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
    private int[] stage2Colors = new int[5];

    private int[] buttonColors = new int[5];
    private string[] labels = new string[5];
    private int stage;
    private bool cantPress = true;

    private static readonly string[] ordinals = new string[5] { "first", "second", "third", "fourth", "fifth" };
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
            for (int i = 0; i < 5; i++)
                buttons[i].GetComponent<Renderer>().material.color = stage == i ? cyan : brown;
        for (int i = 0; i < 5; i++) // TEMPORARY
        {
            buttonColors[i] = rnd.Range(0, 6);
            //labels[i] = "MMMMM";
            solution[i].Add(0);
        }
        var sRnd = new System.Random();
        switch (stage)
        {
            case 0:
                tryAgain1:
                for (int i = 0; i < 5; i++)
                    labels[i] = new string(Enumerable.Repeat("MN", 5).Select(s => s[sRnd.Next(s.Length)]).ToArray());
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
                Debug.Log(solution[0].Join(", "));
                break;
            case 1:
                var morseWords = new string[6] { "MNMMNMM", "NNMMNMMMNM", "NNNMNMMNNMNNMM", "NMMMMNMMMMNM", "NMNNMMNMMMNMMNNNMNN", "NMMMMNMNNNMNNNM" };
                buttonColors = Enumerable.Range(0, 6).ToList().Shuffle().Take(5).ToArray();
                stage2Colors = buttonColors.ToArray();
                var color = rnd.Range(0, 6);
                while (!stage2Colors.Contains(color))
                    color = rnd.Range(0, 6);
                solution[1].Add(Array.IndexOf(stage2Colors, color));
                Debug.LogFormat("[N&Ns #{0}] \"{1}\" appears in the concatenation of all labels. Press the {2} button.", moduleId, colorNames[color].ToUpperInvariant(), ordinals[Array.IndexOf(stage2Colors, color)]);
                var concat = morseWords[color];
                tryAgain2:
                while (concat.Length != 25)
                    concat += rnd.Range(0, 2) == 0 ? "M" : "N";
                if (morseWords.Any(x => concat.Contains(x) && morseWords[color] != x))
                    goto tryAgain2;
                concat = Shift(concat, rnd.Range(0, morseWords[color].Length));
                for (int i = 0; i < 5; i++)
                    labels[i] = new string(concat.Skip(5 * i).Take(5).ToArray());
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
            GenerateStage();
        }
        else
        {
            Debug.LogFormat("[N&Ns #{0}] That was incorrect. Strike!", moduleId);
            Debug.LogFormat("[N&Ns #{0}] Resetting...", moduleId);
            stage = 0;
            pressedButtons.Clear();
            GenerateStage();
        }
    }

    IEnumerator ShowWords(bool hiding)
    {
        cantPress = true;
        if (hiding)
        {
            for (int i = 0; i < 5; i++)
            {
                buttonWords[i].text = "";
                yield return new WaitForSeconds(.3f);
            }
        }
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

    // Twitch Plays
    #pragma warning disable 414
    private readonly string TwitchHelpMessage = "!{0} ";
    #pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string input)
    {
        yield return null;
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        yield return null;
    }
}
