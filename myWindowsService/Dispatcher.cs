using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;


namespace myWindowsService
{

    /// <summary>
    /// 基类
    /// </summary>
    class Dispatcher
    {
        const string CLASS_NAME = "UBDispatcher";
        const int DEFAULT_QUIT_ID = 65535;
        private ISession _session;

        public ISession Session
        {
            get
            {
                return _session;
            }
        }

        public Dispatcher(ISession session, System.Threading.SynchronizationContext synchronizationContext)
        {
            _session = session;
            _session.OnRawMessage = (string rawData) =>
            {
                //Logger.Instance.D(CLASS_NAME,"OnRawMessage");
                try
                {
                    if (PreHandler(rawData))
                        return;
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
                            ["ReqId"] = DEFAULT_QUIT_ID,
                            ["Method"] = "unknow",
                            ["Status"] = -1,
                            ["Action"] = "error",
                            ["ErrorString"] = ex.Message
                        }));
                        return;
                    }
                    var reqId = msg.ContainsKey("ReqId") ? msg["ReqId"] : new JValue(0);
                    var method = msg.Value<string>("Method");
                    var methodInfo = GetType().GetMethod(method);
                    if (methodInfo == null)
                    {
                        throw new Exception($"method {method} not exist");
                    }
                    List<object> paramers = new List<object>();
                    if (methodInfo.IsDefined(typeof(ParamertersAttribute), false))
                    {
                        msg["ReqId"] = reqId;
                        var paramersAttr = methodInfo.GetCustomAttribute(typeof(ParamertersAttribute)) as ParamertersAttribute;
                        var paramersInfo = methodInfo.GetParameters();
                        for (int i = 0; i < paramersInfo.Length; i++)
                        {
                            if ((i < paramersAttr.Names.Length) && msg.ContainsKey(paramersAttr.Names[i]))
                                paramers.Add(msg[paramersAttr.Names[i]].ToObject(paramersInfo[i].ParameterType));
                            else
                                paramers.Add(null);
                        }
                    }
                    if (methodInfo.ReturnType == typeof(Task<JToken>))
                    {
                        synchronizationContext.Post((object state) =>
                        {
                            var task = OnSessionMessageAsync(reqId, method, paramers);
                        }, null);
                    }
                    else
                    {
                        synchronizationContext.Send((object state) =>
                        {
                            OnSessionMessage(reqId, method, paramers);
                        }, null);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Instance.E(CLASS_NAME, ex.Message);
                    SendUTF8(Newtonsoft.Json.JsonConvert.SerializeObject(new JObject
                    {
                        ["ReqId"] = DEFAULT_QUIT_ID,
                        ["Method"] = "unknow",
                        ["Status"] = -1,
                        ["Action"] = "error",
                        ["ErrorString"] = ex.Message
                    }));
                }
            };
        }
        public virtual void OnClose()
        {
        }
        protected virtual bool PreHandler(string rawData)
        {
            return false;
        }
        void OnSessionMessage(JToken reqId, string method, List<object> paramers)
        {
            try
            {
                var response = new JObject();
                response["ReqId"] = reqId;
                response["Method"] = method;
                Logger.Instance.D(CLASS_NAME, $"to call {method}");
                var result = GetType().GetMethod(method).Invoke(this, paramers.ToArray());
                response.Merge(result);
                SendUTF8(Newtonsoft.Json.JsonConvert.SerializeObject(response));
            }
            catch (Exception ex)
            {
                Logger.Instance.E(CLASS_NAME, ex.Message);
                JObject errMsg = new JObject();
                errMsg["ReqId"] = reqId;
                errMsg["Method"] = method;
                SendUTF8(Newtonsoft.Json.JsonConvert.SerializeObject(errMsg));
            }
        }
        async Task OnSessionMessageAsync(JToken reqId,string method, List<object> paramers)
        {
            try
            {
                var response = new JObject();
                response["ReqId"] = reqId;
                response["Method"] = method;
                Logger.Instance.D(CLASS_NAME, $"to call {method}");
                var result = await (GetType().GetMethod(method).Invoke(this, paramers.ToArray()) as Task<JToken>);
                response.Merge(result);
                SendUTF8(Newtonsoft.Json.JsonConvert.SerializeObject(response));
            }
            catch (Exception ex)
            {
                Logger.Instance.E(CLASS_NAME, ex.Message);
                JObject errMsg = new JObject();
                errMsg["ReqId"] = reqId;
                errMsg["Method"] = method;
                SendUTF8(Newtonsoft.Json.JsonConvert.SerializeObject(errMsg));
            }
        }

        protected void SendUTF8(string s)
        {
            byte[] encodeBytes = System.Text.Encoding.UTF8.GetBytes(s);
            string outString = System.Text.Encoding.UTF8.GetString(encodeBytes);
            try
            {
                _session.DoSend(outString);
            }
            catch (Exception ex)
            {
                Logger.Instance.E(CLASS_NAME,ex.Message);
            }
        }
    }

    class ParamertersAttribute : Attribute
    {
        private string[] _names;
        public ParamertersAttribute(params string[] names)
        {
            _names = names;
        }
        public string[] Names
        {
            get
            {
                return _names;
            }
        }
    }
}

