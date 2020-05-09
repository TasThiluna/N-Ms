using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using rnd = UnityEngine.Random;

public class MandNs : MonoBehaviour
{
    public new KMAudio audio;
    public KMBombInfo bomb;
    public KMBombModule module;

    public Color[] textColors;
    public KMSelectable[] buttons;
    public TextMesh[] buttonWords;

    private int solution;
    private int[] buttonColors = new int[5];
    private int[] buttonValues = new int[5];
    private string[] convertedValues = new string[5];
    private int[] results = new int[5];

    private List<Char> ser = new List<Char>();
    private string zeroLetter;
    private string oneLetter;
    private int snBinary;

    private static readonly string base36String = "0123456789ABCDEFGHJIKLMNOPQRSTUVWXYZ";
    private static Char[] base36 = new Char[36];
    private static readonly string[] colorNames = new string[] { "red", "green", "orange", "blue", "yellow", "brown" };
    private static readonly string[] operatorNames = new string[] { "AND", "OR", "XOR", "NAND", "NOR", "XNOR" };
    private static readonly string[] ordinals = new string[] { "first", "second", "third", "fourth", "fifth" };
    private bool cantPress = true;
    private bool firstTime = true;
    private bool hasReset;

    private int attempts = 1;
    private static int moduleIdCounter = 1;
    private int moduleId;
    private bool moduleSolved;

    void Awake()
    {
        moduleId = moduleIdCounter++;
        foreach (KMSelectable button in buttons)
            button.OnInteract += delegate () { ButtonPress(button); return false; };
        module.OnActivate += delegate () { StartCoroutine(ShowWords()); };
        base36 = base36String.ToCharArray();
    }

    void Start()
    {
        zeroLetter = bomb.GetBatteryCount() % 2 == 0 ? "M" : "N";
        oneLetter = bomb.GetBatteryCount() % 2 == 0 ? "N" : "M";
        if (!hasReset)
            Debug.LogFormat("[M&Ns #{0}] {1} corresponds to 0 and {2} corresponds to 1.", moduleId, zeroLetter, oneLetter);
        solution = rnd.Range(0, 5);
        for (int i = 0; i < 5; i++)
        {
            buttonColors[i] = rnd.Range(0, 6);
            buttonValues[i] = rnd.Range(0, 32);
            convertedValues[i] = Convert.ToString(buttonValues[i], 2).Replace("0", zeroLetter).Replace("1", oneLetter);
            while (convertedValues[i].Length != 5)
                convertedValues[i] = zeroLetter + convertedValues[i];
        }
        ser = bomb.GetSerialNumber().ToCharArray().ToList();
        ser.Remove(ser[buttonColors[1]]);
        var binaryAdditions = new int[] { 16, 8, 4, 2, 1 };
        for (int i = 0; i < 5; i++)
        {
            if (Array.IndexOf(base36, ser[i]) % 2 == 1)
                snBinary += binaryAdditions[i];
        }
        Debug.LogFormat("[M&Ns #{0}] The considered serial number is {1}.", moduleId, new string(ser.ToArray()));
        string snBinaryString = Convert.ToString(snBinary, 2);
        while (snBinaryString.Length != 5)
            snBinaryString = "0" + snBinaryString;
        Debug.LogFormat("[M&Ns #{0}] The binary from the serial number is {1}.", moduleId, snBinaryString);
        for (int i = 0; i < 5; i++)
        {
            regenerate:
            switch (buttonColors[i])
            {
                case 0:
                    results[i] = buttonValues[i] & snBinary;
                    break;
                case 1:
                    results[i] = buttonValues[i] | snBinary;
                    break;
                case 2:
                    results[i] = buttonValues[i] ^ snBinary;
                    break;
                case 3:
                    results[i] = ~(buttonValues[i] & snBinary);
                    break;
                case 4:
                    results[i] = ~(buttonValues[i] | snBinary);
                    break;
                default:
                    results[i] = ~(buttonValues[i] ^ snBinary);
                    break;
            }
            if (i == solution)
            {
                if (results[i] < 0 || !bomb.GetSerialNumber().Contains(base36[results[i]]))
                {
                    if (i != 1)
                        buttonColors[i] = rnd.Range(0, 6);
                    buttonValues[i] = rnd.Range(0, 32);
                    goto regenerate;
                }
            }
            else
            {
                if (results[i] > -1 && bomb.GetSerialNumber().Contains(base36[results[i]]))
                {
                    if (i != 1)
                        buttonColors[i] = rnd.Range(0, 6);
                    buttonValues[i] = rnd.Range(0, 32);
                    goto regenerate;
                }
            }
        }
        string solutionBinary;
        for (int i = 0; i < 5; i++)
        {
            Debug.LogFormat("[M&Ns #{0}] The {1} button has {2} text and says {3}.", moduleId, ordinals[i], colorNames[buttonColors[i]], convertedValues[i]);
            var binaryString = Convert.ToString(buttonValues[i], 2);
            while (binaryString.Length != 5)
                binaryString = "0" + binaryString;
            Debug.LogFormat("[M&Ns #{0}] This button has a binary value of {1}.", moduleId, binaryString);
            binaryString = Convert.ToString(results[i], 2);
            while (binaryString.Length != 5)
                binaryString = "0" + binaryString;
            Debug.LogFormat("[M&Ns #{0}] The value of this button {1} the binary from the serial number yields {2}.", moduleId, operatorNames[buttonColors[i]], binaryString);
            if (i == solution)
                solutionBinary = binaryString;
        }
        Debug.LogFormat("[M&Ns #{0}] The {1} button yields {2} when converted to base-36. It is the correct button to press.", moduleId, ordinals[solution], base36[results[solution]]);
        if (hasReset)
            StartCoroutine(ShowWords());
    }

    void ButtonPress(KMSelectable button)
    {
        if (moduleSolved || cantPress)
            return;
        var ix = Array.IndexOf(buttons, button);
        var thisText = buttonWords[ix].text;
        if (solution != ix)
        {
            module.HandleStrike();
            hasReset = true;
            Debug.LogFormat("[M&Ns #{0}] You pressed {1}. That was incorrect. Strike! Resetting...", moduleId, thisText);
            Start();
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
                buttonWords[i].color = textColors[buttonColors[i]];
                buttonWords[i].text = convertedValues[i];
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
