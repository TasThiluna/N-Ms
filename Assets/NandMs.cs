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
		private bool moduleSolved;
    public String[] allWords;

    public String[] row1;
    public String[] row2;
    public String[] row3;
    public String[] row4;
    public String[] row5;

    public String[] column1;
    public String[] column2;
    public String[] column3;
    public String[] column4;
    public String[] column5;

		void Awake()
		{
        moduleId = moduleIdCounter++;
		}

		void Start ()
		{

		}

}
