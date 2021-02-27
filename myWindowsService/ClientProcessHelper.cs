using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace myWindowsService
{

    class ClientProcessHelper
    {
        const string CLASS_NAME = "ClientProcessHelper";
        [StructLayout(LayoutKind.Sequential)]
        internal struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public uint dwProcessId;
            public uint dwThreadId;
        }
        [StructLayout(LayoutKind.Sequential)]
        internal struct SECURITY_ATTRIBUTES
        {
            public uint nLength;
            public IntPtr lpSecurityDescriptor;
            public bool bInheritHandle;
        }


        [StructLayout(LayoutKind.Sequential)]
        public struct STARTUPINFO
        {
            public uint cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public uint dwX;
            public uint dwY;
            public uint dwXSize;
            public uint dwYSize;
            public uint dwXCountChars;
            public uint dwYCountChars;
            public uint dwFillAttribute;
            public uint dwFlags;
            public short wShowWindow;
            public short cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;

        }

        internal enum SECURITY_IMPERSONATION_LEVEL
        {
            SecurityAnonymous,
            SecurityIdentification,
            SecurityImpersonation,
            SecurityDelegation
        }

        internal enum TOKEN_TYPE
        {
            TokenPrimary = 1,
            TokenImpersonation
        }

        #region Win32 Constants

        private const int CREATE_UNICODE_ENVIRONMENT = 0x00000400;
        private const int CREATE_NO_WINDOW = 0x08000000;

        private const int CREATE_NEW_CONSOLE = 0x00000010;

        private const uint INVALID_SESSION_ID = 0xFFFFFFFF;
        private static readonly IntPtr WTS_CURRENT_SERVER_HANDLE = IntPtr.Zero;

        #endregion

        #region DllImports

        [DllImport("advapi32.dll", EntryPoint = "CreateProcessAsUser", SetLastError = true, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern bool CreateProcessAsUser(
            IntPtr hToken,
            String lpApplicationName,
            String lpCommandLine,
            IntPtr lpProcessAttributes,
            IntPtr lpThreadAttributes,
            bool bInheritHandle,
            uint dwCreationFlags,
            IntPtr lpEnvironment,
            String lpCurrentDirectory,
            ref STARTUPINFO lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation);


        [DllImport("userenv.dll", SetLastError = true)]
        private static extern bool CreateEnvironmentBlock(ref IntPtr lpEnvironment, IntPtr hToken, bool bInherit);

        [DllImport("userenv.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DestroyEnvironmentBlock(IntPtr lpEnvironment);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hSnapshot);

        [DllImport("kernel32.dll")]
        private static extern uint WTSGetActiveConsoleSessionId();

        [DllImport("Wtsapi32.dll")]
        private static extern uint WTSQueryUserToken(uint SessionId, ref IntPtr phToken);

        [DllImport("wtsapi32.dll", SetLastError = true)]
        private static extern int WTSEnumerateSessions(
            IntPtr hServer,
            int Reserved,
            int Version,
            ref IntPtr ppSessionInfo,
            ref int pCount);

        #endregion

        #region Win32 Structs

        private enum SW
        {
            SW_HIDE = 0,
            SW_SHOWNORMAL = 1,
            SW_NORMAL = 1,
            SW_SHOWMINIMIZED = 2,
            SW_SHOWMAXIMIZED = 3,
            SW_MAXIMIZE = 3,
            SW_SHOWNOACTIVATE = 4,
            SW_SHOW = 5,
            SW_MINIMIZE = 6,
            SW_SHOWMINNOACTIVE = 7,
            SW_SHOWNA = 8,
            SW_RESTORE = 9,
            SW_SHOWDEFAULT = 10,
            SW_MAX = 10
        }

        private enum WTS_CONNECTSTATE_CLASS
        {
            WTSActive,
            WTSConnected,
            WTSConnectQuery,
            WTSShadow,
            WTSDisconnected,
            WTSIdle,
            WTSListen,
            WTSReset,
            WTSDown,
            WTSInit
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WTS_SESSION_INFO
        {
            public readonly UInt32 SessionID;

            [MarshalAs(UnmanagedType.LPStr)]
            public readonly String pWinStationName;

            public readonly WTS_CONNECTSTATE_CLASS State;
        }

        #endregion

        public class ProcessAsUser
        {

            [DllImport("advapi32.dll", SetLastError = true)]
            private static extern bool CreateProcessAsUser(
                IntPtr hToken,
                string lpApplicationName,
                string lpCommandLine,
                ref SECURITY_ATTRIBUTES lpProcessAttributes,
                ref SECURITY_ATTRIBUTES lpThreadAttributes,
                bool bInheritHandles,
                uint dwCreationFlags,
                IntPtr lpEnvironment,
                string lpCurrentDirectory,
                ref STARTUPINFO lpStartupInfo,
                out PROCESS_INFORMATION lpProcessInformation);


            [DllImport("advapi32.dll", EntryPoint = "DuplicateTokenEx", SetLastError = true)]
            private static extern bool DuplicateTokenEx(
                IntPtr hExistingToken,
                uint dwDesiredAccess,
                ref SECURITY_ATTRIBUTES lpThreadAttributes,
                Int32 ImpersonationLevel,
                Int32 dwTokenType,
                ref IntPtr phNewToken);


            [DllImport("advapi32.dll", SetLastError = true)]
            private static extern bool OpenProcessToken(
                IntPtr ProcessHandle,
                UInt32 DesiredAccess,
                ref IntPtr TokenHandle);

            [DllImport("userenv.dll", SetLastError = true)]
            private static extern bool CreateEnvironmentBlock(
                    ref IntPtr lpEnvironment,
                    IntPtr hToken,
                    bool bInherit);


            [DllImport("userenv.dll", SetLastError = true)]
            private static extern bool DestroyEnvironmentBlock(
                    IntPtr lpEnvironment);

            [DllImport("kernel32.dll", SetLastError = true)]
            private static extern bool CloseHandle(
                IntPtr hObject);

            private const short SW_SHOW = 5;
            private const uint TOKEN_QUERY = 0x0008;
            private const uint TOKEN_DUPLICATE = 0x0002;
            private const uint TOKEN_ASSIGN_PRIMARY = 0x0001;
            private const int GENERIC_ALL_ACCESS = 0x10000000;
            private const int STARTF_USESHOWWINDOW = 0x00000001;
            private const int STARTF_FORCEONFEEDBACK = 0x00000040;
            private const uint CREATE_UNICODE_ENVIRONMENT = 0x00000400;


            private static bool LaunchProcessAsUser(string cmdLine, IntPtr token, IntPtr envBlock)
            {
                bool result = false;


                PROCESS_INFORMATION pi = new PROCESS_INFORMATION();
                SECURITY_ATTRIBUTES saProcess = new SECURITY_ATTRIBUTES();
                SECURITY_ATTRIBUTES saThread = new SECURITY_ATTRIBUTES();
                saProcess.nLength = (uint)Marshal.SizeOf(saProcess);
                saThread.nLength = (uint)Marshal.SizeOf(saThread);

                STARTUPINFO si = new STARTUPINFO();
                si.cb = (uint)Marshal.SizeOf(si);

                si.lpDesktop = @"WinSta0\Default"; //Modify as needed 
                si.dwFlags = STARTF_USESHOWWINDOW | STARTF_FORCEONFEEDBACK;
                si.wShowWindow = SW_SHOW;
                //Set other si properties as required. 

                result = CreateProcessAsUser(
                    token,
                    null,
                    cmdLine,
                    ref saProcess,
                    ref saThread,
                    false,
                    CREATE_UNICODE_ENVIRONMENT,
                    envBlock,
                    null,
                    ref si,
                    out pi);


                if (result == false)
                {
                    int error = Marshal.GetLastWin32Error();
                    string message = String.Format("CreateProcessAsUser Error: {0}", error);
                    Debug.WriteLine(message);
                    Logger.Instance.E(CLASS_NAME, message);

                }

                return result;
            }


            private static IntPtr GetPrimaryToken(int processId)
            {
                IntPtr token = IntPtr.Zero;
                IntPtr primaryToken = IntPtr.Zero;
                bool retVal = false;
                Process p = null;

                try
                {
                    p = Process.GetProcessById(processId);
                }

                catch (ArgumentException)
                {

                    string details = String.Format("ProcessID {0} Not Available", processId);
                    Debug.WriteLine(details);
                    Logger.Instance.E(CLASS_NAME, details);
                    throw;
                }


                //Gets impersonation token 
                retVal = OpenProcessToken(p.Handle, TOKEN_DUPLICATE, ref token);
                if (retVal == true)
                {

                    SECURITY_ATTRIBUTES sa = new SECURITY_ATTRIBUTES();
                    sa.nLength = (uint)Marshal.SizeOf(sa);

                    //Convert the impersonation token into Primary token 
                    retVal = DuplicateTokenEx(
                        token,
                        TOKEN_ASSIGN_PRIMARY | TOKEN_DUPLICATE | TOKEN_QUERY,
                        ref sa,
                        (int)SECURITY_IMPERSONATION_LEVEL.SecurityIdentification,
                        (int)TOKEN_TYPE.TokenPrimary,
                        ref primaryToken);

                    //Close the Token that was previously opened. 
                    CloseHandle(token);
                    if (retVal == false)
                    {
                        string message = String.Format("DuplicateTokenEx Error: {0}", Marshal.GetLastWin32Error());
                        Debug.WriteLine(message);
                        Logger.Instance.E(CLASS_NAME, message);
                    }

                }

                else
                {

                    string message = String.Format("OpenProcessToken Error: {0}", Marshal.GetLastWin32Error());
                    Debug.WriteLine(message);
                    Logger.Instance.E(CLASS_NAME, message);

                }

                //We'll Close this token after it is used. 
                return primaryToken;

            }

            private static IntPtr GetEnvironmentBlock(IntPtr token)
            {

                IntPtr envBlock = IntPtr.Zero;
                bool retVal = CreateEnvironmentBlock(ref envBlock, token, false);
                if (retVal == false)
                {

                    //Environment Block, things like common paths to My Documents etc. 
                    //Will not be created if "false" 
                    //It should not adversley affect CreateProcessAsUser. 

                    string message = String.Format("CreateEnvironmentBlock Error: {0}", Marshal.GetLastWin32Error());
                    Debug.WriteLine(message);
                    Logger.Instance.E(CLASS_NAME, message);

                }
                return envBlock;
            }

            public static bool Launch(string appCmdLine /*,int processId*/)
            {

                bool ret = false;

                //Either specify the processID explicitly 
                //Or try to get it from a process owned by the user. 
                //In this case assuming there is only one explorer.exe 

                Process[] ps = Process.GetProcessesByName("explorer");
                Logger.Instance.I(CLASS_NAME, $"Process.GetProcessesByName(\"explorer\").length={ps.Length}");
                int processId = -1;//=processId 
                if (ps.Length > 0)
                {
                    foreach (Process p in ps)
                    {
                        Logger.Instance.I(CLASS_NAME, $"p.id={p.Id}");
                        if (p.Id != ServiceInfo.GetInstance().getMainProcessId())
                        {
                            processId = p.Id;
                            break;
                        }
                    }
                }

                if (processId > 1)
                {
                    IntPtr token = GetPrimaryToken(processId);

                    if (token != IntPtr.Zero)
                    {

                        IntPtr envBlock = GetEnvironmentBlock(token);
                        ret = LaunchProcessAsUser(appCmdLine, token, envBlock);
                        if (envBlock != IntPtr.Zero)
                            DestroyEnvironmentBlock(envBlock);

                        CloseHandle(token);
                    }

                }
                return ret;
            }

            public static bool Launch(string appCmdLine, int processId)
            {

                bool ret = false;

                //Either specify the processID explicitly 
                //Or try to get it from a process owned by the user. 
                //In this case assuming there is only one explorer.exe 

                Process[] ps = Process.GetProcessesByName("explorer");
                Logger.Instance.I(CLASS_NAME, $"Process.GetProcessesByName(\"explorer\").length={ps.Length}");

                bool bFind = false;
                if (ps.Length > 0)
                {
                    foreach (Process p in ps)
                    {
                        if (p.Id == processId)
                        {
                            Logger.Instance.I(CLASS_NAME, $"p.id={p.Id}");
                            bFind = true;
                            break;
                        }
                    }
                }
                if (false == bFind)
                {
                    return ret;
                }

                if (processId > 1)
                {
                    IntPtr token = GetPrimaryToken(processId);

                    if (token != IntPtr.Zero)
                    {

                        IntPtr envBlock = GetEnvironmentBlock(token);
                        ret = LaunchProcessAsUser(appCmdLine, token, envBlock);
                        if (envBlock != IntPtr.Zero)
                            DestroyEnvironmentBlock(envBlock);

                        CloseHandle(token);
                    }

                }
                return ret;
            }
            private static bool GetSessionUserToken(ref IntPtr phUserToken)
            {
                var bResult = false;
                var hImpersonationToken = IntPtr.Zero;
                var activeSessionId = INVALID_SESSION_ID;
                var pSessionInfo = IntPtr.Zero;
                var sessionCount = 0;

                // Get a handle to the user access token for the current active session.
                if (WTSEnumerateSessions(WTS_CURRENT_SERVER_HANDLE, 0, 1, ref pSessionInfo, ref sessionCount) != 0)
                {
                    var arrayElementSize = Marshal.SizeOf(typeof(WTS_SESSION_INFO));
                    var current = pSessionInfo;

                    for (var i = 0; i < sessionCount; i++)
                    {
                        var si = (WTS_SESSION_INFO)Marshal.PtrToStructure((IntPtr)current, typeof(WTS_SESSION_INFO));
                        current += arrayElementSize;

                        if (si.State == WTS_CONNECTSTATE_CLASS.WTSActive)
                        {
                            activeSessionId = si.SessionID;
                        }
                    }
                }

                // If enumerating did not work, fall back to the old method
                if (activeSessionId == INVALID_SESSION_ID)
                {
                    activeSessionId = WTSGetActiveConsoleSessionId();
                }

                if (WTSQueryUserToken(activeSessionId, ref hImpersonationToken) != 0)
                {
                    SECURITY_ATTRIBUTES sa = new SECURITY_ATTRIBUTES();
                    sa.nLength = (uint)Marshal.SizeOf(sa);
                    // Convert the impersonation token to a primary token
                    bResult = DuplicateTokenEx(hImpersonationToken, 0, ref sa,
                        (int)SECURITY_IMPERSONATION_LEVEL.SecurityImpersonation, (int)TOKEN_TYPE.TokenPrimary,
                        ref phUserToken);

                    CloseHandle(hImpersonationToken);
                }

                return bResult;
            }

            public static JToken GetSessions()
            {
                var hImpersonationToken = IntPtr.Zero;
                var pSessionInfo = IntPtr.Zero;
                var sessionCount = 0;

                // Get a handle to the user access token for the current active session.
                if (WTSEnumerateSessions(WTS_CURRENT_SERVER_HANDLE, 0, 1, ref pSessionInfo, ref sessionCount) != 0)
                {
                    var arrayElementSize = Marshal.SizeOf(typeof(WTS_SESSION_INFO));
                    var current = pSessionInfo;
                    var ret = new JObject();
                    for (var i = 0; i < sessionCount; i++)
                    {
                        var si = (WTS_SESSION_INFO)Marshal.PtrToStructure((IntPtr)current, typeof(WTS_SESSION_INFO));
                        current += arrayElementSize;

                        JToken temp = new JObject();
                        temp["SessionID"] = si.SessionID;
                        temp["State"] = (int)si.State;
                        //if (si.State == WTS_CONNECTSTATE_CLASS.WTSActive)
                        {
                            ret[i.ToString()] = temp;
                        }
                    }
                    return ret;
                }

                return null;
            }

            public static bool LaunchOnSessionId(string appCmdLine, uint activeSessionId)
            {
                IntPtr hImpersonationToken = IntPtr.Zero;
                IntPtr phUserToken = IntPtr.Zero;
                bool bResult = false;
                if (WTSQueryUserToken(activeSessionId, ref hImpersonationToken) != 0)
                {
                    SECURITY_ATTRIBUTES sa = new SECURITY_ATTRIBUTES();
                    sa.nLength = (uint)Marshal.SizeOf(sa);
                    // Convert the impersonation token to a primary token
                    bResult = DuplicateTokenEx(hImpersonationToken, 0, ref sa,
                        (int)SECURITY_IMPERSONATION_LEVEL.SecurityImpersonation, (int)TOKEN_TYPE.TokenPrimary,
                        ref phUserToken);

                    CloseHandle(hImpersonationToken);
                    if (phUserToken != IntPtr.Zero)
                    {

                        IntPtr envBlock = GetEnvironmentBlock(phUserToken);
                        bResult = LaunchProcessAsUser(appCmdLine, phUserToken, envBlock);
                        if (envBlock != IntPtr.Zero)
                            DestroyEnvironmentBlock(envBlock);

                        CloseHandle(phUserToken);
                    }
                }


                return bResult;
            }
        }

    }

}
