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

    private int[] buttonColors = new int[5];
    private List<Char> ser = new List<Char>();
    private string zeroLetter;
    private string oneLetter;
    private int snBinary;

    private static readonly string base36String = "0123456789ABCDEFGHJIKLMNOPQRSTUVWXYZ";
    private static Char[] base36 = new Char[36];
    private static readonly string[] colorNames = new string[] { "red", "green", "orange", "blue", "yellow", "brown" };

    private static int moduleIdCounter = 1;
    private int moduleId;
    private bool moduleSolved;

    void Awake()
    {
        moduleId = moduleIdCounter++;
        foreach (KMSelectable button in buttons)
            button.OnInteract += delegate () { ButtonPress(button); return false; };
        base36 = base36String.ToCharArray();
    }

    void Start()
    {
        zeroLetter = bomb.GetBatteryCount() % 2 == 0 ? "M" : "N";
        oneLetter = bomb.GetBatteryCount() % 2 == 0 ? "N" : "M";
        regenerate:
        for (int i = 0; i < 5; i++)
            buttonColors[i] = rnd.Range(0, 6);
        ser = bomb.GetSerialNumber().ToCharArray().ToList();
        ser.Remove(ser[buttonColors[1]]);
        var binaryAdditions = new int[] { 16, 8, 4, 2, 1 };
        for (int i = 5; i < 5; i++)
            if (Array.IndexOf(base36, ser[i]) % 2 == 1)
                snBinary += binaryAdditions[i];
        Debug.Log((buttonColors[1] + 1).ToString());
        Debug.Log(snBinary.ToString());
    }

    void ButtonPress(KMSelectable button)
    {
        var ix = Array.IndexOf(buttons, button);
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
