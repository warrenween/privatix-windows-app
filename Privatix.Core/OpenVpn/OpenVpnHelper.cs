using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using Privatix.Core.Delegates;


namespace Privatix.Core.OpenVpn
{
    public class OpenVpnHelper : MarshalByRefObject
    {
        private string exePath;
        private Process process;
        private object processLock = new object();


        public event TextEventHandler OnOutputDataReceived;
        public event TextEventHandler OnErrorDataReceived;
        public event EventHandler OnProcessExited;


        public OpenVpnHelper()
        {
            exePath = Path.Combine(Config.OpenVpnDir, "openvpn.exe");
        }

        private static OpenVpnHelper _instance = null;
        public static OpenVpnHelper Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = (OpenVpnHelper)Activator.GetObject(typeof(OpenVpnHelper), "ipc://PrivatixIpcChannel/OpenVpnHelper.rem");
                }

                return _instance;
            }
        }


        public bool Install()
        {
            DriverInstaller.Uninstall(Config.OpenVpnDriversDir);
            return DriverInstaller.Install(Config.OpenVpnDriversDir);
        }

        public bool Connect(string configFilename, string authFilename)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = string.Format("--dev-node \"{0}\" --config \"{1}\" --auth-user-pass \"{2}\" --pull", Config.OpenVpnTapName, configFilename, authFilename),
                    WorkingDirectory = Config.OpenVpnDir,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.GetEncoding("windows-1251"),
                    StandardErrorEncoding = Encoding.GetEncoding(866),
                };

                lock (processLock)
                {
                    try
                    {
                        if (process != null && !process.HasExited)
                        {
                            Logger.Log.Warn("Old openvpn process running on connect");

                            process.Exited -= process_Exited;
                            process.Kill();
                            if (!process.WaitForExit(5000))
                            {
                                Logger.Log.Error("Can not close old process");
                            }
                            else
                            {
                                process.Close();
                            }

                            process = null;

                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Log.Error("Close old process error", ex);
                        Thread.Sleep(500);
                    }


                    process = new Process();
                    process.StartInfo = psi;
                    process.OutputDataReceived += process_OutputDataReceived;
                    process.ErrorDataReceived += process_ErrorDataReceived;
                    process.Exited += process_Exited;
                    process.EnableRaisingEvents = true;
                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    Logger.Log.Info("Started OpenVPN process id = " + process.Id);
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.Log.Error("Could not start openvpn process", ex);
                return false;
            }
        }

        void process_Exited(object sender, EventArgs e)
        {
            lock (processLock)
            {
                Logger.Log.Info("OpenVPN process exited");

                if (OnProcessExited == null)
                {
                    Logger.Log.Info("OnProcessExited == null");
                    return;
                }

                EventHandler handler = null;
                Delegate[] delegates = OnProcessExited.GetInvocationList();
                foreach (Delegate d in delegates)
                {
                    try
                    {
                        handler = (EventHandler)d;
                        handler.BeginInvoke(this, e, null, null);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log.Info(ex.Message, ex);
                        OnProcessExited -= handler;
                    }
                }

                try
                {
                
                    process.OutputDataReceived -= process_OutputDataReceived;
                    process.ErrorDataReceived -= process_ErrorDataReceived;
                    process.Exited -= process_Exited;
                    process.Close();
                    process = null;
                    
                }
                catch (Exception) { }
            }
        }

        void process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (OnErrorDataReceived == null)
            {
                return;
            }

            if (e.Data == null)
            {
                return;
            }

            TextEventHandler handler = null;
            Delegate[] delegates = OnErrorDataReceived.GetInvocationList();
            foreach (Delegate d in delegates)
            {
                try
                {
                    handler = (TextEventHandler)d;
                    handler.BeginInvoke(this, new TextEventArgs(e.Data), null, null);
                }
                catch (Exception ex)
                {
                    Logger.Log.Info(ex.Message, ex);
                    OnErrorDataReceived -= handler;
                }
            }
        }

        void process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (OnOutputDataReceived == null)
            {
                return;
            }

            if (e.Data == null)
            {
                return;
            }

            TextEventHandler handler = null;
            Delegate[] delegates = OnOutputDataReceived.GetInvocationList();
            foreach (Delegate d in delegates)
            {
                try
                {
                    handler = (TextEventHandler)d;
                    handler.BeginInvoke(this, new TextEventArgs(e.Data), null, null);
                }
                catch (Exception ex)
                {
                    Logger.Log.Info(ex.Message, ex);
                    OnOutputDataReceived -= handler;
                }
            }
        }

        public bool Disconnect(int managmentPort)
        {
            try
            {
                if (TrySendTerminate(managmentPort))
                {
                    return true;
                }

                Logger.Log.Warn("Could not send SIGTERM");

                lock (processLock)
                {
                    if (process == null)
                    {
                        return false;
                    }

                    if (!process.HasExited)
                    {
                        process.Kill();
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.Log.Warn(ex.Message, ex);
                return false;
            }
        }

        private bool TrySendTerminate(int managmentPort)
        {
            bool result = false;

            try
            {
                TcpClient tcpClient = new TcpClient("127.0.0.1", managmentPort);
                NetworkStream stream = tcpClient.GetStream();
                if (stream.CanTimeout)
                {
                    stream.ReadTimeout = 15 * 1000;
                }
                byte[] readBuffer = new byte[2048];
                int readed = stream.Read(readBuffer, 0, readBuffer.Length);
                byte[] writeBuffer = Encoding.ASCII.GetBytes("signal SIGTERM\r\n");
                stream.Write(writeBuffer, 0, writeBuffer.Length);
                readed = stream.Read(readBuffer, 0, readBuffer.Length);
                if (readed > 0)
                {
                    string answer = Encoding.ASCII.GetString(readBuffer, 0, readed);
                    if (answer.Contains("SUCCESS"))
                    {
                        result = true;
                    }
                    else
                    {
                        Logger.Log.Warn("Answer: " + answer);
                    }
                }
                tcpClient.Close();
            }
            catch (Exception ex)
            {
                Logger.Log.Warn(ex.Message, ex);
                return false;
            }

            return result;
        }

        public bool CloseAllProcesses()
        {
            bool result = false;
            Process[] allProcesses = Process.GetProcesses();
            foreach (Process p in allProcesses)
            {
                try
                {
                    if (p.ProcessName.Equals("openvpn"))
                    {
                        p.Kill();
                        result = true;
                    }                    
                }
                catch (Exception) { }
            }

            return result;
        }

        public override object InitializeLifetimeService()
        {
            return (null);
        }
    }
}
