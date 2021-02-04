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
    class GuiDispatcher : Dispatcher
    {
        const string CLASS_NAME = "GuiDispatcherGuiDispatcher";
        static SendMessage_Func _callBack = SendMessageProxy;
        public GuiDispatcher(ISession session, System.Threading.SynchronizationContext synchronizationContext)
            : base(session, synchronizationContext)
        {
            Logger.Instance.I(CLASS_NAME, "#construct UBEngineDispatcher");
            try
            {
                //Console.WriteLine($"call OnConnect({session.Sid})");
                OnConnect(session.Sid, _callBack);
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
        public static int SendMessageProxy(uint nConnectionId, IntPtr message, int length)
        {
            Logger.Instance.I(CLASS_NAME, $"enter SendMessageProxy {nConnectionId}");
            try
            {
                if (message != IntPtr.Zero)
                {
                    List<byte> bytes = new List<byte>();
                    for (int offset = 0; offset < length; offset++)
                    {
                        byte b = Marshal.ReadByte(message, offset);
                        bytes.Add(b);
                    }
                    string sOut = Encoding.UTF8.GetString(bytes.ToArray(), 0, bytes.Count);
                    bool bReset = false;
                    JObject request = JObject.Parse(sOut);
                    if ((string)request["Method"] == "ReadMessage" && (int)request["type"] == 0 && (int)request["state"] == 1)
                    {
                        //执行器调用
                        //尝试申请内存，失败则主动退出，设置重启进程标记
                        JObject response = new JObject
                        {
                            ["Executor"] = request["Executor"],
                            ["Method"] = "ReadMessage",
                            ["type"] = 999,
                            ["state"] = 0,
                            ["reason"] = 1
                        };

                        IntPtr pmem = IntPtr.Zero;
                        try
                        {
                            //throw new Exception("mem not enough ");
                            pmem = Marshal.AllocHGlobal(100 * 1024 * 1024);
                            if (pmem == IntPtr.Zero)
                            {
                                SimpleServer.TheServer.SessionDirectOutput(nConnectionId, Newtonsoft.Json.JsonConvert.SerializeObject(response));
                                bReset = true;
                            }
                            Console.WriteLine("mem enough gt 100m,@" + pmem);//使用一下避免优化
                        }
                        catch (Exception ex)
                        {
                            Logger.Instance.E(CLASS_NAME, ex.ToString());
                            SimpleServer.TheServer.SessionDirectOutput(nConnectionId, Newtonsoft.Json.JsonConvert.SerializeObject(response));
                            bReset = true;

                        }
                        finally
                        {
                            Marshal.FreeCoTaskMem(pmem);
                        }
                    }
                    SimpleServer.TheServer.SessionDirectOutput(nConnectionId, sOut);
                    if (bReset)
                    {
                        SimpleServer.TheServer.SessionShutdown(nConnectionId);
                        Environment.Exit(unchecked((int)0xFee1Dead));
                    }
                    return 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return 1;
        }

        [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl)]
        public delegate int SendMessage_Func(System.UInt32 nConnectionId, IntPtr message, int length);



        void OnConnect(System.UInt32 nConnectionId, SendMessage_Func pSendMsgFunc)
        {
            Logger.Instance.I(CLASS_NAME, "OnConnect");
        }


        string OnMessage(System.UInt32 nConnectionId, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(UTF8Marshaler))] string message, System.UInt32 length)
        {
            Logger.Instance.I(CLASS_NAME, "OnMessage");
            SendUTF8("gui:" + message);
            SendUTF8($"Environment.UserName={Environment.UserName},Environment.UserDomainName={Environment.UserDomainName}");
            if ("start" == message)
            {
                SendUTF8("start notepad");
                string appPath = $"C:\\Windows\\System32\\notepad.exe";
                bool ret = ClientProcessHelper.ProcessAsUser.Launch(appPath);
                return ret ? "start succ" : "start fail";
            }
            else if ("logout" == message)
            {
                SendUTF8("logout");
                string appPath = $"{System.AppDomain.CurrentDomain.BaseDirectory}logout.cmd";
                bool ret = ClientProcessHelper.ProcessAsUser.Launch(appPath);
                return ret ? "logout succ" : "logout fail";
            }
            else if ("sessions" == message)
            {
                SendUTF8("sessions");
                int[] pidList = ServiceInfo.GetInstance().getExplorerIds();
                int mainPid = ServiceInfo.GetInstance().getMainProcessId();
                JObject msg = new JObject();
                for (int i = 0; i < pidList.Length; i++)
                {
                    int pid = pidList[i];
                    if (pid == mainPid)
                    {
                        msg.Add("main", pid);
                    }
                    else
                    {
                        msg.Add($"remote{i}", pid);
                    }
                }
                return msg.ToString();
            }
            else
            {
                return "unknown";
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
    }
}
