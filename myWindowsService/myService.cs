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


    public partial class MyService : ServiceBase
    {
        public MyService()
        {
            InitializeComponent();
        }

        Thread mainTask;
        static WebSocketServer wssv;
        protected override void OnStart(string[] args)
        {
            Logger.Instance.D("MyService", "服务启动...");
            mainTask = new Thread(MainTask);
            mainTask.Start();

            Logger.Instance.D("MyService", "服务启动!");
        }

        protected override void OnStop()
        {
            Logger.Instance.D("MyService", "服务关闭...");
            wssv.Stop();
            mainTask.Abort();

            Logger.Instance.D("MyService", "服务关闭!");
        }

        public static void MainTask()
        {
            Logger.Instance.D("MyService", "MainTask running");
            int mainPort = 12345;
            var server = new myWindowsService.SimpleServer();
            wssv = new WebSocketServer(System.Net.IPAddress.Loopback, mainPort);
            wssv.AddWebSocketService<WSLog>("/Log");
            wssv.Start();
            Console.WriteLine($"Deputy, server start @{mainPort} ok.");
            server.StartScheduler();
        }
    }
}
