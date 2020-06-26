using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using rnd = UnityEngine.Random;

public class MandMs : MonoBehaviour
{
    public new KMAudio audio;
    public KMBombInfo bomb;
    public KMBombModule module;

    public Color[] textColors;
    public KMSelectable[] buttons;
    public TextMesh[] buttonWords;

    private int[] solution = new int[5];
    private int[] buttonColors = new int[5];
    private int stage;
    private int gridIndex;
    private int rotationIndex;
    private Char whiteLetter;
    private Char blackLetter;

    private static readonly string[] ordinals = new string[9] { "first", "second", "third", "fourth", "fifth", "sixth", "seventh", "eighth", "ninth" };
    private static readonly string[] rotationNames = new string[4] { "not rotated", "rotated 90° clockwise", "rotated 180°", "rotated 90° counterclockwise" };
    private bool cantPress = true;
    private bool firstTime = true;
    private bool hasReset;

    private static int moduleIdCounter = 1;
    private int moduleId;
    private bool moduleSolved;

    void Awake()
    {
        moduleId = moduleIdCounter++;
        foreach (KMSelectable button in buttons)
            button.OnInteract += delegate () { ButtonPress(button); return false; };
        module.OnActivate += delegate () { StartCoroutine(ShowWords()); };
    }

    void Start()
    {
        if (firstTime)
        {
            whiteLetter = bomb.GetSerialNumberLetters().Any(x => "AEIOU".Contains(x)) ? 'M' : 'N';
            blackLetter = bomb.GetSerialNumberLetters().Any(x => "AEIOU".Contains(x)) ? 'N' : 'M';
            Debug.LogFormat("[M&Ms #{0}] {1} corresponds to white and {2} corresponds to black.", moduleId, whiteLetter, blackLetter);
        }
        solution = Enumerable.Range(0, 5).ToList().Shuffle().ToArray();
        gridIndex = rnd.Range(0, 9);
        rotationIndex = rnd.Range(0, 4);
        for (int i = 0; i < 5; i++)
            buttonColors[i] = rnd.Range(0, 6);
        Debug.LogFormat("[M&Ms #{0}] The grid present is the {1} one, {2}.", moduleId, ordinals[gridIndex], rotationNames[rotationIndex]);
        if (hasReset)
            StartCoroutine(ShowWords());
    }

    void ButtonPress(KMSelectable button)
    {
        audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, button.transform);
        button.AddInteractionPunch(.5f);
        if (moduleSolved || cantPress)
            return;
        var ix = Array.IndexOf(buttons, button);
        if (solution[stage] != ix)
        {
            module.HandleStrike();
            hasReset = true;
            Debug.LogFormat("[M&Ms #{0}] You pressed the {1} button {2} in the sequence. That was not correct. Strike! Resetting...", moduleId, ordinals[ix], ordinals[stage]);
            stage = 0;
            Start();
        }
        else
        {
            Debug.LogFormat("[M&Ms #{0}] You pressed the {1} button {2} in the sequence. That was correct.", moduleId, ordinals[ix], ordinals[stage]);
            stage++;
            if (stage == 5)
            {
                module.HandlePass();
                moduleSolved = true;
                audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
                Debug.LogFormat("[M&Ms #{0}] The entire sequence was entered correctly. Module solved!", moduleId);
                StartCoroutine(ShowWords());
            }
        }
    }

    IEnumerator ShowWords()
    {
        if (!firstTime)
        {
            cantPress = true;
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
                buttonWords[i].text = "MMMMM";
                buttonWords[i].color = textColors[buttonColors[i]];
                yield return new WaitForSeconds(.3f);
            }
        }
        cantPress = false;
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
