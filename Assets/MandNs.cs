using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using rnd = UnityEngine.Random;
using System.Text.RegularExpressions;

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

    private static readonly string base36 = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private static readonly string[] colorNames = new string[] { "red", "green", "orange", "blue", "yellow", "brown" };
    private static readonly string[] operatorNames = new string[] { "AND", "OR", "XOR", "NAND", "NOR", "XNOR" };
    private static readonly string[] ordinals = new string[] { "first", "second", "third", "fourth", "fifth" };
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
        zeroLetter = bomb.GetBatteryCount() % 2 == 0 ? "M" : "N";
        oneLetter = bomb.GetBatteryCount() % 2 == 0 ? "N" : "M";
        if (!hasReset)
            Debug.LogFormat("[M&Ns #{0}] {1} corresponds to 0 and {2} corresponds to 1.", moduleId, zeroLetter, oneLetter);
        regenerate:
        for (int i = 0; i < 5; i++)
        {
            buttonColors[i] = rnd.Range(0, 6);
            buttonValues[i] = rnd.Range(0, 32);
            convertedValues[i] = Convert.ToString(buttonValues[i], 2).Replace("0", zeroLetter).Replace("1", oneLetter).PadLeft(5, zeroLetter[0]);
        }
        ser = bomb.GetSerialNumber().ToList();
        ser.RemoveAt(buttonColors[1]);
        var binaryAdditions = new int[] { 16, 8, 4, 2, 1 };
        for (int i = 0; i < 5; i++)
        {
            if (base36.IndexOf(ser[i]) % 2 == 1)
                snBinary += binaryAdditions[i];
        }
        Debug.LogFormat("[M&Ns #{0}] The considered serial number is {1}.", moduleId, new string(ser.ToArray()));
        string snBinaryString = Convert.ToString(snBinary, 2).PadLeft(5, '0');
        Debug.LogFormat("[M&Ns #{0}] The binary from the serial number is {1}.", moduleId, snBinaryString);
        for (int i = 0; i < 5; i++)
        {
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
            results[i] &= 0x1f;
        }
        if (results.Count(x => bomb.GetSerialNumber().Contains(base36[x])) != 1)
            goto regenerate;
        solution = Array.IndexOf(results, results.First(x => bomb.GetSerialNumber().Contains(base36[x])));
        string solutionBinary;
        for (int i = 0; i < 5; i++)
        {
            Debug.LogFormat("[M&Ns #{0}] The {1} button has {2} text and says {3}.", moduleId, ordinals[i], colorNames[buttonColors[i]], convertedValues[i]);
            var binaryString = Convert.ToString(buttonValues[i], 2).PadLeft(5, '0');
            Debug.LogFormat("[M&Ns #{0}] This button has a binary value of {1}.", moduleId, binaryString);
            binaryString = Convert.ToString(results[i], 2).PadLeft(5, '0');
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
            Debug.LogFormat("[M&Ns #{0}] You pressed {1}. That was correct. Module solved!", moduleId, thisText);
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
    private readonly string TwitchHelpMessage = @"!{0} press <1/2/3/4/5> [presses the button in that position from left to right]";
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
        buttons[solution].OnInteract();
        while (cantPress)
        {
            yield return true;
            yield return new WaitForSeconds(.1f);
        }
    }
}
