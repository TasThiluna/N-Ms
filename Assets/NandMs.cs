using System;
﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;

public class NandMs : MonoBehaviour
 {
	 	public KMAudio Audio;
		public KMBombInfo bomb;

		static int moduleIdCounter = 1;
		int moduleId;
		bool moduleSolved;
    bool recalcing;
    public String[] allWords;
    public Color[] textColors;
    public KMSelectable[] buttons;
    public TextMesh[] buttonWords;

    public String[][] sets;
    public string[] row1, row2, row3, row4, row5, column1, column2, column3, column4, column5;
    List <int> decidedButtons = new List <int>();
    List <int> decidedWords = new List <int>();
    public List <int> numbers;

    int setIndex;
    int otherwordindex;
    string [] otherWords;

		void Awake()
		{
        moduleId = moduleIdCounter++;
        foreach (KMSelectable button in buttons)
        {
          button.OnInteract += delegate () { buttonPress(button); return false; };
        }
		}

		void Start ()
		{
      sets = new[] { row1, row2, row3, row4, row5, column1, column2, column3, column4, column5 };
      setIndex = UnityEngine.Random.Range(0,10);
      otherwordindex = UnityEngine.Random.Range(0,20);
      otherWords = allWords.Except(sets[setIndex]).ToArray();
      pickWords();
		}

    void buttonPress(KMSelectable button)
    {
      Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, button.transform);
      button.AddInteractionPunch(.5f);
      if(moduleSolved || recalcing)
      {
        return;
      }
      if(otherWords[otherwordindex] != button.GetComponentInChildren<TextMesh>().text)
      {
        GetComponent<KMBombModule>().HandleStrike();
        Debug.LogFormat("[N&Ms #{0}] Strike! Resetting...", moduleId);
        Start();
      }
      else
      {
        GetComponent<KMBombModule>().HandlePass();
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
        Debug.LogFormat("[N&Ms #{0}] Module solved.", moduleId);
        moduleSolved = true;
        StartCoroutine(showWords());
      }
    }

    void pickWords()
    {
      decidedButtons = Enumerable.Range(0,5).ToList().Shuffle().Take(4).ToList();
      decidedWords = Enumerable.Range(0,5).ToList().Shuffle().Take(4).ToList();
      Debug.LogFormat("[N&Ms #{0}] The correct word to press is {1}.", moduleId, otherWords[otherwordindex]);
      StartCoroutine(showWords());
    }

    IEnumerator showWords()
    {
      recalcing = true;
      for(int i = 0; i <= 4; i++)
      {
        buttonWords[i].text = "";
        yield return new WaitForSeconds(.3f);
      }
      if(!moduleSolved)
      {
        yield return new WaitForSeconds(.2f);
        for(int i = 0; i <= 4; i++)
        {
          int colorIndex = UnityEngine.Random.Range(0,6);
          buttonWords[i].color = textColors[colorIndex];
          int ix = decidedButtons.IndexOf(i);
          if(ix == -1)
          {
            buttonWords[i].text = otherWords[otherwordindex];
          }
          else
          {
            buttonWords[i].text = sets[setIndex][decidedWords[ix]];
          }
          yield return new WaitForSeconds(.3f);
        }
      }
      recalcing = false;
    }

}
