using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace Read_for_blind
{
    class Tesseract
    {
        public String Path = "";
        public Process process;
        public Tesseract(String path)
        {
            this.Path = path;
        }

        public void getTextFile(String imageName)
        {
             process = new Process();
            
            process.StartInfo.FileName = "tesseract.exe";
            process.StartInfo.WorkingDirectory = this.Path;
            if (File.Exists("../out.text"))
            {
                File.Delete("../out.text");
            }
            process.StartInfo.Arguments = "../"+imageName+"  " + "../out";
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            
            process.Start();
            while (!process.HasExited)
                System.Threading.Thread.Sleep(100);
           
         

            
        }
    }
}
