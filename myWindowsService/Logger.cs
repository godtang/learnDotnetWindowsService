using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace myWindowsService
{
    //配置文件deputy-log.ini格式
    //level=INFO
    //默认DEBUG
    public class Logger
    {
        enum LoggerLevel {INFO,DEBUG,WARN,ERROR,FATAL }
        private static readonly object _lock = new object();
        private Logger()
        {
        }
        public static readonly Logger Instance = new Logger();

        private void writeFileLog(string filePath, string s)
        {
            try
            {
                lock (_lock)
                {
                    if (!File.Exists(filePath))
                    {
                        using (Stream outStream = new FileStream(filePath, FileMode.Create))
                        using (StreamWriter writer = new StreamWriter(outStream, Encoding.UTF8))
                        {
                            writer.WriteLine(s);
                        }
                    }
                    else
                    {
                        using (StreamWriter writer = new StreamWriter(filePath, true, Encoding.UTF8))
                        {
                            writer.WriteLine(s);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
        
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string lpAppName, string lpKeyName, string lpDefault, StringBuilder lpReturnedString, int nSize, string lpFileName);
        private void writeLog(LoggerLevel level,string m, string s, int lineNumber, string fileName)
        {
            var content=$"[{DateTime.Now.ToString("hh:mm:ss.fff")}]{m}[{level.ToString()}],{System.IO.Path.GetFileName(fileName)}({lineNumber}):{s}";
            if((int)level >= (int)LoggerLevel.FATAL)//fatal会被crashreport收集
            {
                try
                {
                    Console.WriteLine(content);
                    string path = string.Format(@".\log\{0}.log", "myWindowsService");
                    string filePath = Encoding.UTF8.GetString(Encoding.Default.GetBytes(path));
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                    writeFileLog(filePath, content);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
                return;
            }
            if(File.Exists(System.AppDomain.CurrentDomain.BaseDirectory+"log.ini"))
            {
                LoggerLevel userLevel = LoggerLevel.DEBUG;
                try
                {
                    StringBuilder sbConfig = new StringBuilder(1024);
                    GetPrivateProfileString("", "level", "INFO", sbConfig, 1024, @"log.ini");
                    if(sbConfig.ToString().Length>0)
                    {
                        Enum.TryParse(sbConfig.ToString(), out userLevel);
                    }
                }
                catch { }
                Console.WriteLine(content);
                if((int)level >= (int)userLevel)
                {
                    string path = string.Format(@"{2}log\{0}_{1}.log", "myWindowsService", DateTime.Now.ToString("yyyyMMdd"), System.AppDomain.CurrentDomain.BaseDirectory);
                    writeFileLog(path, content);
                }
            }
            else
            {
                try
                {
                    Console.WriteLine(content);
#if UDP_LOG
           
            UdpClient udpClient = new UdpClient();
            byte[] sendData = Encoding.Default.GetBytes(content);
            IPEndPoint targetPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8889);
            udpClient.Send(sendData, sendData.Length, targetPoint);
#endif
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }  
        }
        
       
        public void I(string m, string s, [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0, [System.Runtime.CompilerServices.CallerFilePath] string fileName = "")
        {
            writeLog(LoggerLevel.INFO, m, s, lineNumber, fileName);
        }
        public void D(string m, string s, [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0, [System.Runtime.CompilerServices.CallerFilePath] string fileName = "")
        {
            writeLog(LoggerLevel.DEBUG, m, s, lineNumber, fileName);
        }
        public void W(string m, string s, [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0, [System.Runtime.CompilerServices.CallerFilePath] string fileName = "")
        {
            writeLog(LoggerLevel.WARN, m, s, lineNumber, fileName);
        }
        public void E(string m, string s, [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0, [System.Runtime.CompilerServices.CallerFilePath] string fileName = "")
        {
            writeLog(LoggerLevel.ERROR, m, s, lineNumber, fileName);
        }
        public void F(string m, string s, [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0, [System.Runtime.CompilerServices.CallerFilePath] string fileName = "")
        {
            writeLog(LoggerLevel.FATAL, m, s, lineNumber, fileName);
        }
    }
}
