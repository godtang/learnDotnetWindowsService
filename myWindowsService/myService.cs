using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace myWindowsService
{
    abstract class WSBase : WebSocketBehavior, myWindowsService.ISession
    {
        protected override void OnOpen()
        {
            myWindowsService.SimpleServer.TheServer.OnSessionOpen(this);
        }
        protected override void OnMessage(WebSocketSharp.MessageEventArgs e)
        {
            OnRawMessage?.Invoke(e.Data);
        }
        protected override void OnClose(WebSocketSharp.CloseEventArgs e)
        {
            myWindowsService.SimpleServer.TheServer.OnSessionClose(this);
        }
        protected override void OnError(WebSocketSharp.ErrorEventArgs e)
        {
            Console.WriteLine(e.Message);
        }
        public void DoSend(string rawData)
        {
            Send(rawData);
        }
        public void DoClose()
        {
            Close();
        }
        public Action<string> OnRawMessage { get; set; }
        public virtual string Name { get; }
        public uint Sid
        {
            get
            {
                return (uint)this.GetHashCode();
            }
        }
    }
    class WSLog : WSBase
    {
        public override string Name
        {
            get
            {
                return "/Log";
            }
        }
    }
    class WSGui : WSBase
    {
        public override string Name
        {
            get
            {
                return "/Gui";
            }
        }
    }
    class WSCommander : WSBase
    {
        public override string Name
        {
            get
            {
                return "/Commander";
            }
        }
    }


    public partial class MyService : ServiceBase
    {
        static public string CLASS_NAME = "MyService";
        public MyService()
        {
            InitializeComponent();
        }

        Thread mainTask;
        static WebSocketServer wssv;
        protected override void OnStart(string[] args)
        {
            Logger.Instance.D(CLASS_NAME, "服务启动...");
            mainTask = new Thread(MainTask);
            mainTask.Start();

            // 服务启动的时候必须初始化
            Logger.Instance.D(CLASS_NAME, $"main explorer pid={ServiceInfo.GetInstance().getMainProcessId()}");

            Logger.Instance.D(CLASS_NAME, "服务启动!");
        }

        protected override void OnStop()
        {
            Logger.Instance.D(CLASS_NAME, "服务关闭...");
            wssv.Stop();
            mainTask.Abort();

            Logger.Instance.D(CLASS_NAME, "服务关闭!");
        }

        public static void MainTask()
        {
            Logger.Instance.D(CLASS_NAME, "MainTask running ...");
            int mainPort = 12345;
            var server = new myWindowsService.SimpleServer();
            wssv = new WebSocketServer(System.Net.IPAddress.Any, mainPort);
            wssv.AddWebSocketService<WSLog>("/Log");
            wssv.AddWebSocketService<WSGui>("/Gui");
            wssv.AddWebSocketService<WSCommander>("/Commander");
            wssv.Start();
            Logger.Instance.D(CLASS_NAME, $"Deputy, server start @{mainPort} ok.");
            //server.StartScheduler();
            Logger.Instance.D(CLASS_NAME, "MainTask running !");
        }

        // 这个函数无效！！！！
        protected override void OnSessionChange(SessionChangeDescription changeDescription)
        {
            Logger.Instance.I(CLASS_NAME, $"session id = {changeDescription.SessionId}, reason = {changeDescription.Reason}");
            switch (changeDescription.Reason)
            {
                case SessionChangeReason.SessionLogon:
                case SessionChangeReason.RemoteConnect:
                case SessionChangeReason.SessionLogoff:
                case SessionChangeReason.RemoteDisconnect:
                case SessionChangeReason.SessionLock:
                case SessionChangeReason.SessionUnlock:
                default:
                    break;
            }
        }
    }
}
