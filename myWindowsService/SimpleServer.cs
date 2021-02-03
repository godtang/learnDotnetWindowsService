using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;

namespace myWindowsService
{
    //外部数据会话需要实现的接口
    public interface ISession
    {
        void DoSend(string rawData);
        void DoClose();
        Action<string> OnRawMessage { get; set; }
        string Name { get; }
        uint Sid { get; }
    }

    //Server需要实现的接口
    public interface IServer
    {
        void OnSessionOpen(ISession session);
        void OnSessionClose(ISession session);
    }

    /// <summary>
    /// Server实现
    /// </summary>
    public class SimpleServer : IServer, IMessageFilter
    {
        const string CLASS_NAME = "SimpleServer";
        private System.Threading.SynchronizationContext _synchronizationContext;
        private List<Dispatcher> _dispatchers;
        private static SimpleServer _theServer;
        public static SimpleServer TheServer //外部ws没有好的方式拿到server对象，用TheServer访问
        {
            get
            {
                return _theServer;
            }
        }

        public SimpleServer()
        {
            _synchronizationContext = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();
            _dispatchers = new List<Dispatcher>();
            _theServer = this;

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }
        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Logger.Instance.F(CLASS_NAME, e.ExceptionObject.ToString());
        }
        public void StartScheduler()
        {
            Application.AddMessageFilter(this);
            Application.Run();
        }

        public bool PreFilterMessage(ref Message m)
        {
            return false;
        }

        /// <summary>
        /// 外部新建会话回调
        /// </summary>
        /// <param name="session"></param>
        public void OnSessionOpen(ISession session)
        {
            Logger.Instance.D(CLASS_NAME, $"OnSessionOpen sessionid={session.Sid}");
            Dictionary<string, Func<Dispatcher>> dispatcherFactory = new Dictionary<string, Func<Dispatcher>> {
                {"/Log",() =>new LogDispatcher(session,_synchronizationContext)  },
                {"/Gui",() =>new GuiDispatcher(session,_synchronizationContext)  }
            };
            Logger.Instance.D(CLASS_NAME, $"LogDispatcher,session.Name={session.Name}");

            try
            {
                _dispatchers.Add(dispatcherFactory[session.Name].Invoke());
            }
            catch (Exception)
            { }
        }
        public void OnSessionClose(ISession session)
        {
            Logger.Instance.D(CLASS_NAME, $"OnSessionClose sessionid={session.Sid}");
            try
            {
                var d = _dispatchers.Find(a => a.Session.Sid == session.Sid);
                d.OnClose();
                _dispatchers.Remove(d);
            }
            catch (Exception)
            { }
        }

        /// <summary>
        /// 用于引擎回调，直接找到对应session输出
        /// </summary>
        /// <param name="sid"></param>
        /// <param name="rawString"></param>
        public void SessionDirectOutput(uint sid, string rawString)
        {
            try
            {
                var d = _dispatchers.Find(a => a.Session.Sid == sid);
                d.Session.DoSend(rawString);
            }
            catch (Exception)
            { }
        }
        public void SessionShutdown(uint sid)
        {
            try
            {
                var d = _dispatchers.Find(a => a.Session.Sid == sid);
                d.Session.DoClose();
            }
            catch (Exception)
            { }
        }
    }
}
