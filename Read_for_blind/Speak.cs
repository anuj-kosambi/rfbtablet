﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Speech.Synthesis;
using System.IO;
namespace Read_for_blind
{
    class Speak
    {
        public SpeechSynthesizer speechSynt;
        String Path = "";
        public Speak(String filePath)
        {
            this.Path = filePath;
           // string fileText = File.ReadAllText(filePath);
           // System.Diagnostics.Debug.WriteLine(fileText);
            speechSynt = new SpeechSynthesizer();
           

        }
        public void speakText(String text)
        {
            speechSynt.Speak(text);
        }
        public void speakFile()
        {
            StreamReader streamRead= new StreamReader(this.Path);
            String line="";
                while ((line = streamRead.ReadLine()) != null) 
                {
                    speechSynt.Speak(line);

                }

        }

    }
}
