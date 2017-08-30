using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Runtime.Serialization.Formatters;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Privatix.Core;
using Privatix.Core.OpenVpn;
using Bugsnag.Clients;


namespace Privatix.Service
{
    public partial class Service : ServiceBase
    {
        public Service()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                Logger.InitLogger();
                Config.Load();
                Logger.Log.Info("Service started");

                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

                WPFClient.Config.AppVersion = Config.Version;
#if _DEV_API_
                WPFClient.Config.ReleaseStage = "development";
#elif _STAGE_API_
                WPFClient.Config.ReleaseStage = "staging";
#else
                WPFClient.Config.ReleaseStage = "production";
#endif
                WPFClient.Start();

                Hashtable channelConfig = new Hashtable();
                channelConfig["portName"] = "PrivatixIpcChannel";
                channelConfig["authorizedGroup"] = GetNameForSid(WellKnownSidType.WorldSid);

                BinaryServerFormatterSinkProvider serverProvider = new BinaryServerFormatterSinkProvider();
                serverProvider.TypeFilterLevel = TypeFilterLevel.Full;
                IpcServerChannel serverChannel = new IpcServerChannel(channelConfig, serverProvider);
                ChannelServices.RegisterChannel(serverChannel, false);

                RemotingServices.Marshal(Guard.Instance, "Guard.rem");
                RemotingServices.Marshal(DnsChanger.Instance, "DnsChanger.rem");
                RemotingServices.Marshal(TimeZoneChanger.Instance, "TimeZoneChanger.rem");
                RemotingServices.Marshal(ServiceWorker.Instance, "ServiceWorker.rem");
                RemotingConfiguration.RegisterWellKnownServiceType(typeof(OpenVpnHelper), "OpenVpnHelper.rem", WellKnownObjectMode.Singleton);

                ImportCert();
                ImportHttpsCerts();
                DeleteSslCert();
                AddSslCert();

                ThreadPool.QueueUserWorkItem(o =>
                {
                    HttpService httpService = new HttpService();
                    httpService.Start();
                });
            }
            catch (Exception ex)
            {
                Logger.Log.Info(ex.Message, ex);
            }            
        }

        void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = (Exception)e.ExceptionObject;
            Logger.Log.Error(ex.Message, ex);
            WPFClient.Notify(ex);
        }

        protected override void OnStop()
        {
            bool tryCreateNewApp;
            EventWaitHandle closeEvent = new EventWaitHandle(false, EventResetMode.AutoReset, Config.CloseEventName, out tryCreateNewApp);
            if (!tryCreateNewApp)
            {
                closeEvent.Set();
            }

            Logger.Log.Info("Service stopped");
        }

        private static string GetNameForSid(WellKnownSidType wellKnownSidType)
        {
            SecurityIdentifier id = new SecurityIdentifier(wellKnownSidType, null);
            return id.Translate(typeof(NTAccount)).Value;
        }

        private bool ImportCert()
        {
            string certFilename = "";

            try
            {
                Process process = Process.GetCurrentProcess();

                certFilename = Path.Combine(Path.GetDirectoryName(process.MainModule.FileName), "ca_privatix.crt");

                X509Certificate2 certificate = new X509Certificate2(certFilename);

                X509Store store = new X509Store(StoreName.Root, StoreLocation.LocalMachine);
                store.Open(OpenFlags.ReadWrite);
                store.Add(certificate);
                store.Close();

                return true;
            }
            catch (Exception ex)
            {
                Logger.Log.Info("Cert file: " + certFilename + "\n" + ex.Message, ex);

                return false;
            }
        }

        private bool ImportHttpsCerts()
        {
            string certFilename = "";

            try
            {
                Process process = Process.GetCurrentProcess();
                string appDir = Path.GetDirectoryName(process.MainModule.FileName);

                certFilename = Path.Combine(appDir, "privatix.pfx");
                X509Certificate2 certificate = new X509Certificate2(certFilename, "privatix123");

                X509Store store = new X509Store(StoreName.Root, StoreLocation.LocalMachine);
                store.Open(OpenFlags.ReadWrite);
                store.Add(certificate);
                store.Close();

                store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
                store.Open(OpenFlags.ReadWrite);
                store.Add(certificate);
                store.Close();

                return true;
            }
            catch (Exception ex)
            {
                Logger.Log.Info("Cert file: " + certFilename + "\n" + ex.Message, ex);

                return false;
            }
        }

        private bool AddSslCert()
        {
            var dirSys = Environment.GetFolderPath(Environment.SpecialFolder.SystemX86);
            var dirWin = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            var args = string.Format("http add sslcert ipport=0.0.0.0:{0} certhash=e2e9000b5e3b3aa1fb82c31e71332b7be3ac6636 appid={1}", Config.WebCheckPortHttps, "{ad935451-f6b3-4eac-bb6c-cc71062f1a58}");

            var psi = new ProcessStartInfo
            {
                FileName = Path.Combine(dirSys, "netsh.exe"),
                Arguments = args,
                WorkingDirectory = dirWin,
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            var process = new Process { StartInfo = psi };
            process.Start();
            process.WaitForExit();

            return process.ExitCode == 0;
        }

        private bool DeleteSslCert()
        {
            try
            {
                var dirSys = Environment.GetFolderPath(Environment.SpecialFolder.SystemX86);
                var dirWin = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
                var args = string.Format("http delete sslcert ipport=0.0.0.0:{0}", Config.WebCheckPortHttps);

                var psi = new ProcessStartInfo
                {
                    FileName = Path.Combine(dirSys, "netsh.exe"),
                    Arguments = args,
                    WorkingDirectory = dirWin,
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                var process = new Process { StartInfo = psi };
                process.Start();
                process.WaitForExit();

                return process.ExitCode == 0;
            }
            catch (Exception ex)
            {
                Logger.Log.Error(ex.Message, ex);
                return false;
            }
        }
    }
}
