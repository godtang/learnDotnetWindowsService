﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace myWindowsService
{
    class ServiceInfo
    {
        string CLASS_NAME = "ServiceInfo";
        private static ServiceInfo instance = new ServiceInfo();
        public static ServiceInfo GetInstance()
        {
            return instance;
        }

        // 主桌面的explorer进程ID
        int mainProcessId = 0;
        private ServiceInfo()
        {
            Process[] ps = Process.GetProcessesByName("explorer");
            if (ps.Length != 1 && 0 == mainProcessId)
            {
                Logger.Instance.F(CLASS_NAME, $"explorer count exception, expect 1, got {ps.Length}, exit!!!!");
                System.Environment.Exit(-1);
            }
            mainProcessId = ps[0].Id;
            Logger.Instance.D(CLASS_NAME, $"explorer explorer pid={ps[0].Id}," +
                $" MachineName={ps[0].MachineName}" +
                $" MainWindowTitle={ps[0].MainWindowTitle}");
        }

        // 获得主工作桌面的explorer进程ID
        public int getMainProcessId()
        {
            return mainProcessId;
        }
    }
}
