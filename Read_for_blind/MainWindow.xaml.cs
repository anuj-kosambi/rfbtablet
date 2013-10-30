
#region System
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;
using System.Drawing;
using  System.Collections.Generic;
#endregion

#region usingOpenCVShape
using OpenCvSharp;
using OpenCvSharp.CPlusPlus;
#endregion

#region usingWindows8CameraUI
using Windows.Foundation;
using Windows.Media.Capture;

using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Streams;
using Windows.Media;
using Windows.Media.MediaProperties;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Enumeration;
using System.Drawing.Imaging;
#endregion

#region Speak
using System.Speech.Recognition;
#endregion


namespace Read_for_blind
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region varible intitalization
        public static State state;
        public enum State
        {
            Normal = -1,
            Replay = 0,
            Pause = 1,
            Resume = 2,
            Command=3,
            Invaild=4
        }
        int DIRECTION;

        private static Boolean PerspectiveCorrection=false;
        private Speak speakObj = null;
        private SpeechRecognitionEngine recognizer;
        public static double MARGINW = 5, MARGINH = 5;
        private Boolean CommandStatus = false;
        private static int[,] Status = new int[3, 3];
        private string[] DirectionText;
        private CvCapture cap;
        private Thread _cameraThread = null;
        private Thread _voiceThread = null;
        private Thread _mainThread = null;
        private Thread _restart = null;
        private MediaCapture capture = null;
        private String deviceId = "";
        private Tesseract tesseract = null;
        private Windows.Media.MediaProperties.VideoEncodingProperties resolutionMax = null;
        private MediaCaptureInitializationSettings settings = null;
        private List<DeviceInformation> cameraList = new List<DeviceInformation>();
        #endregion

        public MainWindow()
        {
            InitializeComponent();
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.WindowState = WindowState.Maximized;
            this.Title = "Read_For_Blind";
            this.preview.Width = SystemParameters.WorkArea.Width;
            this.preview.Height = SystemParameters.WorkArea.Height;
            this.preview.Stretch = Stretch.Fill;
            this.preview.StretchDirection = StretchDirection.Both;
            this.WindowStyle = WindowStyle.ToolWindow;
            
            this.preview.Source = (new ImageSourceConverter()).ConvertFromString("Main Page.png") as ImageSource;
            this.Loaded += MainWindow_Loaded;
           
            DirectionText = new string[10];
            DirectionText[0] = "Nice";
            DirectionText[1] = "Up";
            DirectionText[2] = "Down";
            DirectionText[3] = "Right";
            DirectionText[4] = "Bottom";
            DirectionText[5] = "Up";
            DirectionText[6] = "Left";

        
           
           
           


         

             tesseract = new Tesseract(".\\Tesseract-OCR");
            speakObj = new Speak("out.txt");

            _restart = new Thread(new ThreadStart(restartCallBack));
            _restart.Name = "RestratThread";
        }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {

            _mainThread = new Thread(new ThreadStart(StartUpMenu));
            _mainThread.Start();
            _mainThread.Name = "MainThread";
           // StartUpMenu();
        }

        private void StartUpMenu()
        {
            _voiceThread = new Thread(new ThreadStart(voiceCallBack));
            _voiceThread.Name = "VoiceThread";
            _voiceThread.Start();
           speakObj.speakTextAsync("If you want to Listen to Tutorial ... Say Read for Blind Tutorial ");
           speakObj.speakTextAsync("If you want to skip to the Reader ... Say Read for Blind Reader");
            Thread.Sleep(100);
            state = State.Command;
            while (state!=State.Normal)
                Thread.Sleep(100);

            _cameraThread = new Thread(new ThreadStart(CaptureCameraCallback));
            _cameraThread.Start();
            _cameraThread.Name = "CameraThread";

        }
        
        private void Waiting()
        {
            while (state != State.Normal)
            {
                Thread.Sleep(5000);
                    speakObj.speakText("I am waiting");
                
            }
        }
        
        private void restartCallBack()
        {
            if(_cameraThread.IsAlive)
               _cameraThread.Abort();
            if (_voiceThread.IsAlive)
                _voiceThread.Abort();
            StartUpMenu();
        }
        
        private void voiceCallBack()
        {
                state = State.Normal;
                recognizer = new SpeechRecognitionEngine(new System.Globalization.CultureInfo("en-US"));
                recognizer.LoadGrammar(RFBGrammar());
                recognizer.SpeechRecognized += new EventHandler<SpeechRecognizedEventArgs>(recognizer_SpeechRecognized);
                recognizer.SetInputToDefaultAudioDevice();
                recognizer.RecognizeAsync(RecognizeMode.Multiple);
              
           
        }

        void recognizer_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            
            if (speakObj != null)
            {
                if (e.Result.Text == "Read For Blind exit")
                {

                    exitFunction();

                }
                else
                {
                    if (state != State.Command)
                        if (e.Result.Text == "Read For Blind pause")
                        {

                            if (speakObj.speechSynt.State == System.Speech.Synthesis.SynthesizerState.Speaking)
                            {

                                speakObj.speechSynt.Pause();

                                state = State.Pause;
                            }


                        }

                    if (speakObj.speechSynt.State == System.Speech.Synthesis.SynthesizerState.Paused)
                    {

                        if (state != State.Command)
                            if (e.Result.Text == "Read For Blind resume")
                            {
                                speakObj.speechSynt.Resume();
                                state = State.Resume;
                            }
                    }
                    if (e.Result.Text == "Read For Blind auto on")
                    {
                        PerspectiveCorrection = true;
                        if (speakObj.speechSynt.State == System.Speech.Synthesis.SynthesizerState.Speaking)
                            speakObj.speechSynt.Pause();
                        Speak _s = new Speak("");
                        _s.speakText("Perspective correction " + PerspectiveCorrection);
                        if (speakObj.speechSynt.State == System.Speech.Synthesis.SynthesizerState.Paused)
                            speakObj.speechSynt.Resume();
                        _s.speechSynt.Dispose();
                        _s = null;
                      //  return;

                    }
                    else if (e.Result.Text == "Read For Blind auto off")
                    {
                        PerspectiveCorrection = false;
                        if (speakObj.speechSynt.State == System.Speech.Synthesis.SynthesizerState.Speaking)
                            speakObj.speechSynt.Pause();
                        Speak _s = new Speak("");
                        _s.speakText("Perspective correction " + PerspectiveCorrection);
                        if (speakObj.speechSynt.State == System.Speech.Synthesis.SynthesizerState.Paused)
                            speakObj.speechSynt.Resume();
                        _s.speechSynt.Dispose();
                        _s = null;
                       

                    }
        
       
                    if (speakObj.speechSynt.State == System.Speech.Synthesis.SynthesizerState.Ready && state == State.Command)
                    {
                        state = State.Invaild;
                        if (e.Result.Text == "Read For Blind yes")
                        {
                            speakObj.speakText("Yes Detected");
                            CommandStatus = true;

                            state = State.Normal;

                        }
                        else if (e.Result.Text == "Read For Blind no")
                        {
                            speakObj.speakText("No Detected ...Bye Bye");
                            CommandStatus = false;

                            state = State.Normal;
                        }
                        else if (e.Result.Text == "Read For Blind tutorial")
                        {
                            speakObj.speakText("Tutorial Detected");
                            state = State.Normal;
                        }
                        else if (e.Result.Text == "Read For Blind reader")
                        {
                            speakObj.speakText("Starting Reader");
                            state = State.Normal;
                        }
                        
                       
                    }
                }
               
             
            }
           
        }

        private Grammar RFBGrammar()
        {


            Choices commandChoice = new Choices(new string[] {  "auto off", "auto on", "tutorial", "pause", "resume", "yes", "no", "reader", "exit" });
            GrammarBuilder commandElement = new GrammarBuilder(commandChoice);

     
            GrammarBuilder commandPhrase = new GrammarBuilder("Read For Blind");
            commandPhrase.Append(commandElement);


            Choices newChoice = new Choices(new GrammarBuilder[] { commandPhrase });
            Grammar grammar = new Grammar((GrammarBuilder)newChoice);
            grammar.Name = "RFBCommand";
            return grammar;
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)

        {
        

            if (_cameraThread != null && _cameraThread.IsAlive )
                _cameraThread.Abort();
         
            if (_voiceThread != null && _voiceThread.IsAlive)
                _voiceThread.Abort();
         
            if (_restart != null && _restart.IsAlive)
                _restart.Abort();

            if (_mainThread != null && _mainThread.IsAlive)
                _mainThread.Abort();

            if (speakObj.speechSynt.State == System.Speech.Synthesis.SynthesizerState.Speaking)
            {
                speakObj.speechSynt.SpeakAsyncCancelAll();
             
            }
                base.OnClosing(e);
                
        }

        private void CaptureCameraCallback()
        {
            

            using (cap = CvCapture.FromCamera(CaptureDevice.Any, 0))
            {

                while (CvWindow.WaitKey(10) < 0)
                {
                    if (cap != null)
                    {

                        #region init
                        int[] HORI = new int[2];
                        int[] VERTI = new int[2];

                        for (int i = 0; i < 3; i++)
                            for (int j = 0; j < 3; j++)
                                Status[i, j] = 0;

                        //Cv.LoadImage("testing.png");
                        IplImage display = cap.QueryFrame();
                        IplImage gray = new IplImage(display.Size, BitDepth.U8, 1);
                        IplImage mainImage = display.Clone();


                        int WIDTH = (mainImage.Width);

                        int HEIGHT = (mainImage.Height);



                        HORI[0] = (int)(WIDTH / MARGINW);
                        HORI[1] = (int)((MARGINW - 1) * HORI[0]);
                        VERTI[0] = (int)(HEIGHT / MARGINH);
                        VERTI[1] = (int)((MARGINH - 1) * VERTI[0]);
                        #endregion

                        try
                        {
                            #region BLOCK_DETECTION

                            mainImage.PyrMeanShiftFiltering(mainImage, 10, 35, 3);
                          //  mainImage.Smooth(mainImage, SmoothType.Blur, 5,5);
                            mainImage.CvtColor(gray, ColorConversion.BgrToGray);


                           

                            
                            Cv.Smooth(gray, gray, SmoothType.Blur, 3, 3);
                 
                            Cv.Canny(gray, gray, 35, 35, ApertureSize.Size3);
                            gray.Smooth(gray, SmoothType.Blur, 3, 3);
                           /*
                            CvSeq<CvPoint> contours;
                            
                            CvMemStorage _storage = new CvMemStorage();
                            Cv.FindContours(gray, _storage, out contours, CvContour.SizeOf, ContourRetrieval.Tree, ContourChain.ApproxSimple);
                            Cv.DrawContours(display, contours, CvColor.Blue, CvColor.Green, 2,2, LineType.AntiAlias);
                            */
                            CvMemStorage storage = new CvMemStorage();
                            storage.Clear();

#if DEBUG
                            setupDebug(mainImage, HORI, VERTI);
#endif
                            int minL = 0;
                            int minT = 0;

                            // gray.HoughLines2(storage, HoughLinesMethod.Standard, Cv.PI / 180, 0, 0);
                            CvSeq lines = gray.HoughLines2(storage, HoughLinesMethod.Probabilistic, 1, Math.PI / 180, 50, 50, 10);
                            for (int i = 0; i < lines.Total; i++)
                            {

                             

                                CvLineSegmentPoint elem = lines.GetSeqElem<CvLineSegmentPoint>(i).Value;
#if VERBOSE
                                display.Line(elem.P1, elem.P2, CvColor.Navy, 2);
#endif
                                if (elem.P1.X < HORI[0])
                                { minL = Math.Max(elem.P1.X, minL); }
                                if (elem.P2.X < HORI[0])
                                { minL = Math.Max(elem.P2.X, minL); }

                                if (elem.P1.Y < VERTI[0])
                                { minT = Math.Max(elem.P1.Y, minT); }
                                if (elem.P2.Y < VERTI[0])
                                { minT = Math.Max(elem.P2.Y, minT); }





                                try
                                {
                                    if (elem.P1.Y <= HEIGHT - 10 && elem.P2.Y <= HEIGHT - 10)
                                    {
                                        int i1 = (elem.P1.X < HORI[0]) ? 0 : (elem.P1.X / HORI[1]) + 1;
                                        int j1 = (elem.P1.Y < VERTI[0]) ? 0 : (elem.P1.Y / VERTI[1]) + 1;
                                        Status[i1, j1]++;
                                        int i2 = (elem.P2.X < HORI[0]) ? 0 : (elem.P2.X / HORI[1]) + 1;
                                        int j2 = (elem.P2.Y < VERTI[0]) ? 0 : (elem.P2.Y / VERTI[1]) + 1;
                                        Status[i2, j2]++;
                                        double slope = 0, c = 0;
                                        List<CvPoint> points = new List<CvPoint>(Math.Abs(i1 - i2) + Math.Abs(j1 - j2) + 2);
                                        if (elem.P1.X != elem.P2.X)
                                        {
                                            slope = 1.0d * (elem.P2.Y - elem.P1.Y) / (elem.P2.X - elem.P1.X);
                                            c = elem.P1.Y - slope * elem.P1.X;
                                            for (int p = Math.Min(i1, i2); p != Math.Max(i1, i2); p++)
                                                points.Add(new CvPoint(HORI[p], (int)(slope * HORI[p] + c)));
                                            for (int p = Math.Min(j1, j2); p != Math.Max(j1, j2); p++)
                                                points.Add(new CvPoint((int)((VERTI[p] - c) / slope), VERTI[p]));
                                        }
                                        else
                                        {

                                            for (int p = Math.Min(j1, j2); p != Math.Max(j1, j2); p++)
                                            {
                                                points.Add(new CvPoint(elem.P1.X, VERTI[p]));

                                            }
                                        }
#if VERBOSE
                                        foreach (var point in points)
                                            mainImage.Line(point, point, CvColor.Yellow, 10);
#endif


                                        points.Add(elem.P1);
                                        points.Add(elem.P2);

                                        points.Sort((a, b) =>
                                        {
                                            int result = a.X.CompareTo(b.X);
                                            if (result == 0) result = a.Y.CompareTo(b.Y);
                                            return result;
                                        });

                                        for (int p = 0; p < points.Capacity - 1; p++)
                                        {
                                            CvPoint mid = new CvPoint((points[p].X + points[p + 1].X) / 2,
                                                                     (points[p].Y + points[p + 1].Y) / 2);

                                            int x = (mid.X < HORI[0]) ? 0 : (mid.X / HORI[1]) + 1;
                                            int y = (mid.Y < VERTI[0]) ? 0 : (mid.Y / VERTI[1]) + 1;
                                            Status[x, y]++;
#if VERBOSE
                                            mainImage.Line(mid, mid, CvColor.Green, 10);
#endif




                                        }





                                    }
                                }
                                catch (IndexOutOfRangeException )
                                {
                                    
                                }

                            }
                            #endregion

                            #region Display Direction
                            DIRECTION = getClipDirection(display, HORI, VERTI, minL, minT);
                            //display.PutText(getDirection(DIRECTION), new CvPoint(1 * WIDTH / 3 + (WIDTH / 6), 1 * HEIGHT / 3 + HEIGHT / 6), new CvFont(FontFace.HersheyTriplex, 1, 1), CvColor.Navy);
                          
                            // speakObj.speakText("" + getDirection(DIRECTION));
                             if (speakObj.speechSynt.State != System.Speech.Synthesis.SynthesizerState.Speaking|| DIRECTION==0)
                             {

                                 Thread speechThread = new Thread(new ThreadStart(() =>
                                     {
                                         speakObj.speakText("" + getDirection(DIRECTION));
                                     }));
                                 speechThread.Start();

                             }
                             if (DIRECTION == 0)
                                 break;
                            #endregion
                            
                        }
                        catch (OpenCvSharp.OpenCvSharpException){}
                        catch (OpenCVException ) { }

                        #region SetPreview

                            Bitmap bm = BitmapConverter.ToBitmap(display);
                            BitmapImage bitmapImage;
                            this.Dispatcher.Invoke((Action)(() =>
                            {
                                using (MemoryStream memory = new MemoryStream())
                                {
                                    bm.Save(memory, ImageFormat.Jpeg);
                                    memory.Position = 0;
                                    bitmapImage = new BitmapImage();
                                    bitmapImage.BeginInit();
                                    bitmapImage.StreamSource = memory;
                                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                                    bitmapImage.EndInit();
                                    preview.Source = bitmapImage;
                                }
                            }));
                       
                        #endregion

                    }//if
                }//while

            }//using

        
        }
          
        private void setMargin(double i, double j)
        {
            MARGINW = i;
            MARGINH = j;
        }

        private int getClipDirection(IplImage mainImage, int[] HORI, int[] VERTI, int minL, int minT)
        {

            int[] _HORI = new int[4];
            int[] _VERTI = new int[4];
            int WIDTH = mainImage.Width;
            int HEIGHT = mainImage.Height;
            _HORI[0] = 0;
            _VERTI[0] = 0;
            _HORI[1] = HORI[0]; _HORI[2] = HORI[1];
            _VERTI[1] = VERTI[0]; _VERTI[2] = VERTI[1];
            _HORI[3] = mainImage.Width;
            _VERTI[3] = mainImage.Height;

            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                {
                    if (Status[i, j] > 0)
                        Cv.Rectangle(mainImage, new CvPoint(_HORI[i], _VERTI[j]), new CvPoint(_HORI[i + 1], _VERTI[j + 1]), new CvScalar(255, 255, 0, 0), 10);


                }

            int leftD = 0, topD = 0, bottomD = 0, rightD = 0;
            if (Status[0, 1] > 0) leftD = 3; if (Status[1, 0] > 0) topD = 4; if (Status[1, 2] > 1) bottomD = 5; if (Status[2, 1] > 0) rightD = 6;
            int sum = leftD + topD + bottomD + rightD;
            int DIRECTION = 1;
            if (sum == 18)
            {
           
                DIRECTION = 0;
                speakObj.speakText("Please Hold in this position while image is Captured");
                ClickPhoto();
            }
            else if (sum > 11)
            {
                DIRECTION = sum - 9;
            }
            else if (sum == 9)
            {
                if (Status[1, 1] == 0)
                {
                    if (leftD == 3)
                    {

                        setMargin(WIDTH / (minL + 20), MARGINH);
                    }
                    if (topD == 4)
                    {

                        setMargin(MARGINW, HEIGHT / (minT + 20));
                    }
                    DIRECTION = 1;
                }
                else
                {
                    DIRECTION = 3 + (int)((leftD > 0) ? 0 : 1);

                    for (int i = 0; i < 3; i += 2)
                        for (int j = 0; j < 3; j += 2)
                            if (Status[i, j] > 0)
                                if (leftD > 0)
                                    DIRECTION = 5- (int)(j * 0.5);
                                else if (topD > 0)
                                    DIRECTION =6 - (int)(i * 1.5);


                }

            }
            else if (sum > 6)
            {
                if (sum > 9)
                {
                    if (Status[1, 1] == 0)
                        DIRECTION = 6;
                    else
                    {
                        DIRECTION = 3;
                    }

                }
                else
                {
                    if (Status[1, 1] == 0)
                        DIRECTION = 3;
                    else
                    {
                        DIRECTION = 6;
                    }

                }
            }
            else if (sum > 0)
            {
                DIRECTION = sum;
            }
            else if (Status[1, 1] > 0)
                DIRECTION = 2;
            return DIRECTION;
        }

        private CvPoint getIntersect(CvLineSegmentPoint line1,CvLineSegmentPoint line2)
        {
            CvPoint point=new CvPoint();
            int x1 =line1.P1.X, y1 = line1.P1.Y, x2 = line1.P2.X, y2 = line1.P2.Y;  
            int x3 =line2.P1.X, y3 = line2.P1.Y, x4 = line2.P2.X, y4 = line2.P2.Y;   
            float d=0;

            if (Math.Sqrt(Math.Abs((line1.P1.X - line1.P2.X) * (line1.P1.X - line1.P2.X) + (line1.P1.Y - line1.P2.Y) * (line1.P1.Y - line1.P2.Y))) < 100)
                return new CvPoint(-1, -1);

            if (Math.Sqrt(Math.Abs((line2.P1.X - line2.P2.X) * (line2.P1.X - line2.P2.X) + (line2.P1.Y - line2.P2.Y) * (line2.P1.Y - line2.P2.Y))) < 100)
                return new CvPoint(-1, -1);

            d = ((float)(x1 - x2) * (y3 - y4)) - ((y1 - y2) * (x3 - x4));
            point.X =(int) (((x1*y2 - y1*x2) * (x3-x4) - (x1-x2) * (x3*y4 - y3*x4)) / d);  
            point.Y = (int) (((x1*y2 - y1*x2) * (y3-y4) - (y1-y2) * (x3*y4 - y3*x4)) / d);

            if (point.X < Math.Min(x1, x2) - 10 || point.X > Math.Max(x1, x2) + 10 || point.Y < Math.Min(y1, y2) - 10 || point.Y > Math.Max(y1, y2) + 10)
            {  
                return new CvPoint(-1,-1);  
           }
            if (point.X < Math.Min(x3, x4) - 10 || point.X > Math.Max(x3, x4) + 10 || point.Y < Math.Min(y3, y4) - 10 || point.Y > Math.Max(y3, y4) + 10)
            {  
                return new CvPoint(-1,-1);  
           } 
            
         return point;  
   }  

        private void setupDebug(IplImage mainImage, int[] HORI, int[] VERTI)
        {
#if DEBUG
            CvPoint p01 = new CvPoint(HORI[0], 0);
            CvPoint p31 = new CvPoint(HORI[0], mainImage.Height);
            CvPoint p02 = new CvPoint(HORI[1], 0);
            CvPoint p32 = new CvPoint(HORI[1], mainImage.Height);
            //Horizatial
            CvPoint p10 = new CvPoint(0, VERTI[0]);
            CvPoint p13 = new CvPoint(mainImage.Width, VERTI[0]);
            CvPoint p20 = new CvPoint(0, VERTI[1]);
            CvPoint p23 = new CvPoint(mainImage.Width, VERTI[1]);

            mainImage.Line(p01, p31, CvColor.LightGreen, 1, LineType.AntiAlias, 0);
            mainImage.Line(p02, p32, CvColor.LightGreen, 1, LineType.AntiAlias, 0);
            mainImage.Line(p10, p13, CvColor.LightGreen, 1, LineType.AntiAlias, 0);
            mainImage.Line(p20, p23, CvColor.LightGreen, 1, LineType.AntiAlias, 0);
            int WIDTH = mainImage.Width;
            int HEIGHT = mainImage.Height;

#endif
        }

        private String getDirection(int i)
        {

            return DirectionText[i];
        }

        private IplImage setAutoRotation(String filePath)
        {
         
            CvMemStorage cornerStorage = new CvMemStorage();
            IplImage orig = new IplImage(filePath);
            IplImage original = orig.Clone();
            IplImage gray = new IplImage(orig.Size, BitDepth.U8, 1);
            IplImage ipl = null;
           // Cv.Smooth(orig, orig, SmoothType.Blur, 5, 5);
          
            Cv.PyrMeanShiftFiltering(orig, orig, 10, 35, 3);
           
            orig.CvtColor(gray, ColorConversion.BgrToGray);
            Cv.Canny(gray, gray, 35, 35, ApertureSize.Size3);
            Cv.Smooth(gray, gray, SmoothType.Blur, 3, 3);
            double threshold=orig.Width+orig.Height/2;

            CvSeq cornerLines = gray.HoughLines2(cornerStorage, HoughLinesMethod.Probabilistic, 1, Math.PI / 180, 100,50,50);
        
            int[] poly = new int[cornerLines.Total];
            int curPoly = 0;
            for (int k = 0; k < cornerLines.Total; k++)
                poly[k] = -1;
            List<List<CvPoint>> corners = new List<List<CvPoint>>();
           
            #region Dividing into Groups
            for (int i = 0; i < cornerLines.Total; i++)
            {
                CvLineSegmentPoint line1 = cornerLines.GetSeqElem<CvLineSegmentPoint>(i).Value;
                orig.Line(line1.P1, line1.P2, CvColor.Blue, 1);
               
                for (int j = i + 1; j < cornerLines.Total; j++)
                {
                    

                    CvLineSegmentPoint line2 = cornerLines.GetSeqElem<CvLineSegmentPoint>(j).Value;


                    CvPoint pt = getIntersect(line1, line2);


                    if (pt.X >= 0 && pt.Y >= 0 && pt.X < gray.Width && pt.Y < gray.Height)
                    {

                        if (poly[i] == -1 && poly[j] == -1)
                        {
                            List<CvPoint> v = new List<CvPoint>();
                            v.Add(pt);
                            corners.Insert(curPoly, v);
                            poly[i] = curPoly;
                            poly[j] = curPoly;
                            curPoly++;
                            continue;
                        }
                        if (poly[i] == -1 && poly[j] >= 0)
                        {
                            corners[poly[j]].Add(pt);
                            poly[i] = poly[j];
                            continue;
                        }
                        if (poly[i] >= 0 && poly[j] == -1)
                        {
                            corners[poly[i]].Add(pt);
                            poly[j] = poly[i];
                            continue;
                        }
                        if (poly[i] >= 0 && poly[j] >= 0)
                        {
                            if (poly[i] == poly[j])
                            {
                                corners[poly[i]].Add(pt);
                                continue;
                            }

                            for (int k = 0; k < corners[poly[j]].Count; k++)
                            {
                                corners[poly[i]].Add(corners[poly[j]][k]);
                            }

                            corners[poly[j]].Clear();
                            poly[j] = poly[i];
                            continue;
                        }
                    }

                }
            }

            #endregion
         

#if DEBUG

            for (int i = 0; i < corners.Count; i++)
            {
                for (int j = 0; j < corners[i].Count; j++)
                {
                    CvPoint pt = corners[i].ElementAt(j);
                    orig.Line(pt, pt, new CvScalar(i*55,i*55,i*0,i*50),10);
                }
            }
             
#endif
           

            for (int i = 0; i < corners.Count; i++)
            {
                CvPoint center = new CvPoint(0, 0);
                if (corners[i].Count < 4) continue;
                for (int j = 0; j < corners[i].Count; j++)
                {
                    center += corners[i][j];
                }
                center *= (1.0 / corners[i].Count);
                sortCorners(corners[i], center,orig);
                 
#if DEBUG
                for (int j = 0; j < corners[i].Count; j++)
                {
                    orig.Line(corners[i].ElementAt(j), corners[i].ElementAt(j), CvColor.Red, 10);
                }
                orig.Line(center, center, CvColor.White, 10);
#endif

            }
           
           int area = 0;
           int I = 0;
           for (int i = 0; i < corners.Count; i++)
           {

               if (corners[i].Count < 4) continue;
               CvRect r = Cv.BoundingRect(corners[i]);
               if (r.Width * r.Height < area)
                   continue;
               area = r.Width * r.Height;
               I = i;
           }

           CvRect rect=new CvRect();
           try
           {
               rect = Cv.BoundingRect(corners[I]);
           }
           catch
           {
                
           }
           if(rect.Width!=0 && rect.Height!=0){

               CvMat quad = new CvMat(rect.Height, rect.Width, MatrixType.U8C3);
                   //  quad.Zero();
                   CvPoint2D32f[] src_pf = new CvPoint2D32f[4];
                   CvPoint2D32f[] dst_pf = new CvPoint2D32f[4];
                   for (int j = 0; j < 4; j++)
                   {
                       src_pf[j] = new CvPoint2D32f(corners[I].ElementAt(j).X, corners[I].ElementAt(j).Y);
                   }
                   dst_pf[0] = new CvPoint2D32f(0, 0);
                   dst_pf[1] = new CvPoint2D32f(quad.Cols, 0);
                   dst_pf[2] = new CvPoint2D32f(0, quad.Rows);
                   dst_pf[3] = new CvPoint2D32f(quad.Cols, quad.Rows);




                   CvMat transmtx = Cv.GetPerspectiveTransform(src_pf, dst_pf);
                    ipl = new IplImage(rect.Width, rect.Height, BitDepth.U8, 3);

                   Cv.WarpPerspective(original, ipl, transmtx);
                   
                 // Cv.ShowImage(filePath + I, ipl);

       }
            
        //    Cv.ShowImage("Gray",gray);
        //    Cv.ShowImage(filePath, orig);

            return ipl;
        
        }

        private  void sortCorners(List<CvPoint> corners, CvPoint center,IplImage orig)
        {
            List<CvPoint> top = new List<CvPoint>();
            List<CvPoint> bot = new List<CvPoint>();
           
        
            for (int i = 0; i < corners.Count; i++)  
            {  
                if (corners[i].Y < center.Y)  
                    top.Add(corners[i]);  
                else  
                    bot.Add(corners[i]);  
            }
            /*
            CvPoint centerTop = new CvPoint();
            for (int j = 0; j < top.Count; j++)
            {
                centerTop += top[j];
            }
            centerTop *= (1.0 / top.Count);
            orig.Line(centerTop, centerTop, CvColor.Green, 10);
            CvPoint centerBot = new CvPoint();
            for (int j = 0; j < bot.Count; j++)
            {
                centerBot += bot[j];
            }
            centerBot *= (1.0 / bot.Count);
            orig.Line(centerBot, centerBot, CvColor.Green, 10);
            */

            top.Sort((a, b) =>
            {
                int result = a.X.CompareTo(b.X);
                if (result == 0) result = a.Y.CompareTo(b.Y);
                return result;
            });

            bot.Sort((a, b) =>
            {
                int result = a.X.CompareTo(b.X);
                if (result == 0) result = a.Y.CompareTo(b.Y);
                return result;
            });
           
            List<CvPoint> topLeft = new List<CvPoint>();
            List<CvPoint> topRight = new List<CvPoint>();
            List<CvPoint> botLeft = new List<CvPoint>();
            List<CvPoint> botRight = new List<CvPoint>();
            for (int i = 0; i < top.Count; i++)
            {
                if (top[i].X < center.X)
                    topLeft.Add(top[i]);
                else
                    topRight.Add(top[i]);
            }

            for (int i = 0; i < bot.Count; i++)
            {
                if (bot[i].X < center.X)
                    botLeft.Add(bot[i]);
                else
                   botRight.Add(bot[i]);
            }  
            topLeft.Sort((a, b) =>
            {
                int result = a.Y.CompareTo(b.Y);
                if (result == 0) result = a.X.CompareTo(b.X);
                return result;
            });
          
           botLeft.Sort((a, b) =>
           {
               int result = b.Y.CompareTo(a.Y);
               if (result == 0) result = b.X.CompareTo(a.X);
               return result;
           }); 
           topRight.Sort((a, b) =>
           {
               int result = a.Y.CompareTo(b.Y);
               if (result == 0) result =a.X.CompareTo(b.X);
               return result;
           });
           
          botRight.Sort((a, b) =>
          {
              int result = b.Y.CompareTo(a.Y);
              if (result == 0) result = b.X.CompareTo(a.X);
              return result;
          });
          CvPoint tl, tr, bl, br;

          if (top.Count < 1 || bot.Count < 1 || botLeft.Count < 1 || botRight.Count < 1 || topLeft.Count < 1 || topRight.Count < 1)
              return;
           tl = top[0];
           tr = topRight[0];
          
          
            bl =botLeft[0];
           
            br = bot[bot.Count-1];
           double slope = Double.MaxValue;
           if(top[0].X!=topLeft[0].X)
           slope=(top[0].Y-topLeft[0].Y)/(top[0].X-topLeft[0].X)*1.0;

         
          if (slope < 0)
          {

              tl = topLeft[0];
              tr = top[top.Count - 1];
              bl = bot[0];
              br = botRight[0];
          }
          corners.Clear();
          corners.Add(tl);
          corners.Add(tr);
          corners.Add(bl);
          corners.Add(br);



        }
   
        private  void ClickPhoto()

        {
            cap.Dispose();
            
            Thread.Sleep(500);
            String filename = "";
            this.Dispatcher.BeginInvoke((Action)(async () =>
            {
              
                capture = new MediaCapture();

                var devices = await Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(Windows.Devices.Enumeration.DeviceClass.VideoCapture);
                for (int i = 0; i < devices.Count; i++)
                {
                 
                    cameraList.Add(devices[i]);
                    deviceId = devices[0].Id;
                }
                settings = new MediaCaptureInitializationSettings();
                settings.AudioDeviceId = "";
                settings.VideoDeviceId = deviceId;
                
                settings.PhotoCaptureSource = Windows.Media.Capture.PhotoCaptureSource.VideoPreview;
                settings.StreamingCaptureMode = Windows.Media.Capture.StreamingCaptureMode.AudioAndVideo;
             
                await capture.InitializeAsync(settings);
                

                int max = 0;
                var resolutions = capture.VideoDeviceController.GetAvailableMediaStreamProperties(MediaStreamType.Photo);
            
                for (var i = 0; i < resolutions.Count; i++)
                {
                    Windows.Media.MediaProperties.VideoEncodingProperties res = (Windows.Media.MediaProperties.VideoEncodingProperties)resolutions[i];
                    
                    if (res.Width * res.Height > max)
                    {
                        max = (int)(res.Width * res.Height);
                        resolutionMax = res;
                        
                    }
                }

                System.Diagnostics.Debug.WriteLine(resolutionMax.Width);
                await capture.VideoDeviceController.SetMediaStreamPropertiesAsync(MediaStreamType.Photo, resolutionMax);
                capture.VideoDeviceController.Focus.TrySetAuto(true);
                capture.VideoDeviceController.WhiteBalance.TrySetAuto(true);
                capture.VideoDeviceController.Contrast.TrySetAuto(true);
                capture.VideoDeviceController.Exposure.TrySetAuto(true);
                capture.VideoDeviceController.BacklightCompensation.TrySetAuto(true);
                capture.VideoDeviceController.Brightness.TrySetAuto(true);
                
                ImageEncodingProperties imageProperties = Windows.Media.MediaProperties.ImageEncodingProperties.CreateJpeg();
                var fPhotoStream = new InMemoryRandomAccessStream();

                await capture.CapturePhotoToStreamAsync(imageProperties, fPhotoStream);
                await fPhotoStream.FlushAsync();
                fPhotoStream.Seek(0);

                byte[] bytes = new byte[fPhotoStream.Size];
                await fPhotoStream.ReadAsync(bytes.AsBuffer(), (uint)fPhotoStream.Size, InputStreamOptions.None);
                
                BitmapImage bitmapImage = new BitmapImage();
                
                using (MemoryStream byteStream = new MemoryStream(bytes))
                {
                    //  Image image = new System.Windows.Controls.Image();
                    bitmapImage.BeginInit();
                    bitmapImage.StreamSource = byteStream;
                    bitmapImage.EndInit();
                    preview.Source = bitmapImage;
                 
                    JpegBitmapEncoder encoder = new JpegBitmapEncoder();
                    String photolocation = "temp-orig.jpg";
                    filename = photolocation;
                    encoder.Frames.Add(BitmapFrame.Create(bitmapImage));

                    using (FileStream filestream = new FileStream(photolocation, FileMode.Create))
                        encoder.Save(filestream);

                }
                try
                {

                    if (PerspectiveCorrection)
                    {
                        Bitmap bitmap = BitmapConverter.ToBitmap(setAutoRotation("temp-orig.jpg"));

                        bitmap.Save("temp.jpg");
                        filename = "temp.jpg";
                    }
                   
                }
                catch { }
                  
            }));
            processTesseract(filename);
            
          
           
        
              
          

        }
       
        private void processTesseract(String filename)
        {

            
            speakObj.speakText("Please Wait...While Image is processed...");
            tesseract.getTextFile(filename);
            speakObj.speakText("Process Done");
            speakObj.speakText("Reading Text Now");
            speakObj.speakFile();
            speakObj.speakText("Reading Done");
            speakObj.speakText("Would You Like To Read Another Text...");
            speakObj.speakText("Say Read For Blind Yes ... Or ... Read For Blind No for Exit");
            state = State.Command;

            while (state != State.Normal)
                Thread.Sleep(100);

            if (CommandStatus ==true)
            {

                if (_restart.IsAlive)
                    _restart.Abort();
                _restart = new Thread(new ThreadStart(restartCallBack));
                _restart.Start();
                Thread.Sleep(1000);
                       
              
              
            }
            else if (CommandStatus == false)
            {


                exitFunction();
               
           
            }

          
          

        }

        private void exitFunction()
        {
            Dispatcher.BeginInvoke((Action)(() =>
            {
                OnClosed(null);
                Application.Current.Shutdown();
            }));
             
           
        }

    }
}
