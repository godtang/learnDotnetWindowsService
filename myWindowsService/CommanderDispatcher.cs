using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;


namespace myWindowsService
{

    /// <summary>
    /// 转发botscript
    /// </summary>
    class CommanderDispatcher : Dispatcher
    {
        const string CLASS_NAME = "CommanderDispatcher";
        public CommanderDispatcher(ISession session, System.Threading.SynchronizationContext synchronizationContext)
            : base(session, synchronizationContext)
        {
            Logger.Instance.I(CLASS_NAME, $"construct {CLASS_NAME}");
            try
            {
                //Console.WriteLine($"call OnConnect({session.Sid})");
                OnConnect(session.Sid);
            }
            catch (Exception ex)
            {
                Logger.Instance.E(CLASS_NAME, ex.ToString());
                System.Windows.Forms.MessageBox.Show("Botscript.dll 加载失败！");
                Environment.Exit(-1);
            }
        }
        protected override bool PreHandler(string rawData)
        {
            Logger.Instance.D(CLASS_NAME, $"PreHandler={rawData}");
            byte[] utf8bytes = Encoding.UTF8.GetBytes(rawData);
            JObject msg;
            try
            {
                msg = JObject.Parse(rawData);
            }
            catch (Exception ex)
            {
                Logger.Instance.E(CLASS_NAME, ex.Message);
                SendUTF8(Newtonsoft.Json.JsonConvert.SerializeObject(new JObject
                {
                    ["Method"] = "unknow",
                    ["ProcessId"] = -1,
                    ["Action"] = "error",
                }));
                return false;
            }
            string sResult = OnMessage(msg);
            //Console.WriteLine(sResult);
            Session.DoSend(sResult);
            return true;
        }
        public override void OnClose()
        {
            OnDisconnect(Session.Sid);
        }

        void OnConnect(System.UInt32 nConnectionId)
        {
            Logger.Instance.I(CLASS_NAME, "OnConnect");
        }


        string OnMessage(JObject msg)
        {
            //bool ret = ClientProcessHelper.ProcessAsUser.Launch(message);
            if ("restart" == msg.Value<string>("Method"))
            {
                DownladNewVersion(msg.Value<string>("Url"));
                ShellExecute(IntPtr.Zero,
                    new StringBuilder("Open"),
                    new StringBuilder(System.AppDomain.CurrentDomain.BaseDirectory + "update.cmd"),
                    new StringBuilder(""),
                    new StringBuilder(""), 1);
                return "restart";
            }
            else
            {
                bool ret = ClientProcessHelper.ProcessAsUser.Launch(msg.Value<string>("Method"), msg.Value<int>("ProcessId"));
                return Newtonsoft.Json.JsonConvert.SerializeObject(new JObject
                {
                    ["Method"] = msg.Value<string>("Method"),
                    ["ProcessId"] = msg.Value<int>("ProcessId"),
                    ["Result"] = ret ? 0 : 1,
                });
            }

        }


        /// <summary>
        /// 这是原来释放内存的函数
        /// </summary>
        /// <param name="pString"></param>
        /// <returns></returns>
        void OnMessageFinished(IntPtr pString)
        {
            Logger.Instance.I(CLASS_NAME, "OnMessageFinished");
        }


        void OnDisconnect(System.UInt32 nConnectionId)
        {
            Logger.Instance.I(CLASS_NAME, "OnDisconnect");
        }

        [DllImport("shell32.dll")]
        public static extern int ShellExecute(IntPtr hwnd, StringBuilder lpszOp, StringBuilder lpszFile, StringBuilder lpszParams, StringBuilder lpszDir, int FsShowCmd);


        void DownladNewVersion(string url)
        {
            using (var webClient = new WebClient())
            {
                byte[] fileBytes = webClient.DownloadData(url);
                System.IO.File.WriteAllBytes(System.AppDomain.CurrentDomain.BaseDirectory + "myWindowsService_new.exe", fileBytes);
            }
        }
    }
}
