using System;
using System.Configuration.Install;
using System.ServiceProcess;

namespace IOTLink
{
    partial class ProjectInstaller
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.serviceProcessInstaller1 = new System.ServiceProcess.ServiceProcessInstaller();
            this.serviceInstaller1 = new System.ServiceProcess.ServiceInstaller();
            // 
            // serviceProcessInstaller1
            // 
            this.serviceProcessInstaller1.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            this.serviceProcessInstaller1.Password = null;
            this.serviceProcessInstaller1.Username = null;
            // 
            // serviceInstaller1
            // 
            this.serviceInstaller1.DisplayName = "IOT Link";
            this.serviceInstaller1.ServiceName = "IOTLink";
            this.serviceInstaller1.Description = "Service to provide Internet Of Things (IOT) integration using MQTT.";
            this.serviceInstaller1.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
            this.serviceInstaller1.AfterInstall += AfterInstallHandler;
            this.serviceInstaller1.BeforeUninstall += BeforeUninstallHandler;
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.serviceProcessInstaller1,
            this.serviceInstaller1});

        }

        private void AfterInstallHandler(object sender, InstallEventArgs e)
        {
            ChangeServiceStatus(ServiceControllerStatus.Running);
        }

        private void BeforeUninstallHandler(object sender, InstallEventArgs e)
        {
            ChangeServiceStatus(ServiceControllerStatus.Stopped);
        }

        private void ChangeServiceStatus(ServiceControllerStatus desiredStatus)
        {
            try
            {
                using (ServiceController sc = new ServiceController(serviceInstaller1.ServiceName))
                {
                    ServiceControllerStatus currentStatus = sc.Status;
                    switch (currentStatus)
                    {
                        case ServiceControllerStatus.ContinuePending:
                        case ServiceControllerStatus.StartPending:
                        case ServiceControllerStatus.Running:
                            if (desiredStatus == ServiceControllerStatus.Stopped)
                                sc.Stop();
                            if (desiredStatus == ServiceControllerStatus.Paused)
                                sc.Pause();
                            break;

                        case ServiceControllerStatus.StopPending:
                        case ServiceControllerStatus.Stopped:
                            if (desiredStatus == ServiceControllerStatus.Running)
                                sc.Start();
                            break;

                        case ServiceControllerStatus.PausePending:
                        case ServiceControllerStatus.Paused:
                            if (desiredStatus == ServiceControllerStatus.Stopped)
                                sc.Stop();
                            if (desiredStatus == ServiceControllerStatus.Running)
                                sc.Start();
                            break;

                        default: break;
                    }
                }
            }
            catch (Exception)
            {

            }
        }

        private System.ServiceProcess.ServiceProcessInstaller serviceProcessInstaller1;
        private System.ServiceProcess.ServiceInstaller serviceInstaller1;
    }
}