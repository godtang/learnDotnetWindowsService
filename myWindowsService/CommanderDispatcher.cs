using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
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
            Logger.Instance.I(CLASS_NAME, "#construct UBEngineDispatcher");
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
            string sResult = OnMessage(Session.Sid, rawData, (uint)utf8bytes.Length);
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


        string OnMessage(System.UInt32 nConnectionId, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(UTF8Marshaler))] string message, System.UInt32 length)
        {
            Logger.Instance.I(CLASS_NAME, "OnMessage");

            return "commander:" + message;

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
    }
}
