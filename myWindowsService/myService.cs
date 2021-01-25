using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace myWindowsService
{
    public partial class myService : ServiceBase
    {
        public myService()
        {
            InitializeComponent();
        }
        string filePath = @"D:\MyServiceLog.txt";
        protected override void OnStart(string[] args)
        {
            using (FileStream stream = new FileStream(filePath, FileMode.Append))
            using (StreamWriter writer = new StreamWriter(stream))
            {
                writer.WriteLine($"{DateTime.Now},服务启动！");
            }
        }

        protected override void OnStop()
        {
            using (FileStream stream = new FileStream(filePath, FileMode.Append))
            using (StreamWriter writer = new StreamWriter(stream))
            {
                writer.WriteLine($"{DateTime.Now},服务关闭！");
            }
        }
    }
}
