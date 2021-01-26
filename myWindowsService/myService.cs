﻿using System;
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
        static string filePath = @"D:\MyServiceLog.txt";
        Thread mainTask;
        static WebSocketServer wssv;
        protected override void OnStart(string[] args)
        {
            using (FileStream stream = new FileStream(filePath, FileMode.Append))
            using (StreamWriter writer = new StreamWriter(stream))
            {
                writer.WriteLine($"[{DateTime.Now}]服务启动...");
            }

            mainTask = new Thread(MainTask);
            mainTask.Start();

            using (FileStream stream = new FileStream(filePath, FileMode.Append))
            using (StreamWriter writer = new StreamWriter(stream))
            {
                writer.WriteLine($"[{DateTime.Now}]服务启动！");
            }
        }

        protected override void OnStop()
        {
            using (FileStream stream = new FileStream(filePath, FileMode.Append))
            using (StreamWriter writer = new StreamWriter(stream))
            {
                writer.WriteLine($"[{DateTime.Now}]服务关闭...");
            }
            wssv.Stop();
            mainTask.Abort();
            using (FileStream stream = new FileStream(filePath, FileMode.Append))
            using (StreamWriter writer = new StreamWriter(stream))
            {
                writer.WriteLine($"[{DateTime.Now}]服务关闭！");
            }
        }

        public static void MainTask()
        {
            using (FileStream stream = new FileStream(filePath, FileMode.Append))
            using (StreamWriter writer = new StreamWriter(stream))
            {
                writer.WriteLine($"[{DateTime.Now}]MainTask running");
            }
            int mainPort = 12345;
            var server = new myWindowsService.SimpleServer();
            wssv = new WebSocketServer(System.Net.IPAddress.Loopback, mainPort);
            wssv.AddWebSocketService<WSLog>("/Log");
            wssv.KeepClean = false;
            wssv.Start();
            Console.WriteLine($"Deputy, server start @{mainPort} ok.");
            server.StartScheduler();
        }
    }
}