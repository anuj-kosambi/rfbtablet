using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Speech.Synthesis;
using System.Speech.Synthesis.TtsEngine;
using System.IO;
using System.Speech.Recognition;
namespace Read_for_blind
{
    class Recognition
    {
        public enum State
        {
            Normal=-1,
            Replay=0,
            Pause=1,
            Resume=2
            
        }
        public static State state;
        public Recognition()
        {
            using (SpeechRecognitionEngine recognizer = new SpeechRecognitionEngine(new System.Globalization.CultureInfo("en-US")))
            {
                
                // Create and load a dictation grammar.
                recognizer.LoadGrammar(RFBGrammar());

                // Add a handler for the speech recognized event.
                recognizer.SpeechRecognized += new EventHandler<SpeechRecognizedEventArgs>(recognizer_SpeechRecognized);
               
                // Configure input to the speech recognizer.
                recognizer.SetInputToDefaultAudioDevice();

                // Start asynchronous, continuous speech recognition.
                recognizer.RecognizeAsync(RecognizeMode.Multiple);
                state= State.Normal;
            }

        }
        static void recognizer_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
          

            if (e.Result.Text == "Read For Blind pause")
            {
                state = State.Pause;
            }
            else if (e.Result.Text =="Read For Blind resume")
            {
                state = State.Resume;
            }

          
        }
        static private Grammar RFBGrammar()
        {

            // Create a set of color choices.
            Choices commandChoice = new Choices(new string[] { "replay", "pause", "resume"});
            GrammarBuilder commandElement = new GrammarBuilder(commandChoice);

            // Create grammar builders for the two versions of the phrase.
            GrammarBuilder commandPhrase = new GrammarBuilder("Read For Blind");
            commandPhrase.Append(commandElement);


            // Create a Choices for the two alternative phrases, convert the Choices
            // to a GrammarBuilder, and construct the grammar from the result.
            Choices newChoice = new Choices(new GrammarBuilder[] { commandPhrase });
            Grammar grammar = new Grammar((GrammarBuilder)newChoice);
            grammar.Name = "RFBCommand";
            return grammar;
        }
    }
}
