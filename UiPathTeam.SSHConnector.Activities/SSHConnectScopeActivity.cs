using System;
using System.Activities;
using System.Activities.Statements;
using System.ComponentModel;
using System.Net;
using System.Security;
using System.Text;
using Renci.SshNet;

namespace UiPathTeam.SSHConnector.Activities
{
    [Designer(typeof(SSHConnectScopeActivityDesigner))]
    [DisplayName("SSH Connect Scope")]
    [Description("Creates a SSH connection. Drop SSH Run Command activities inside this scope.")]
    public class SSHConnectScopeActivity : NativeActivity
    {
        [Browsable(false)]
        public ActivityAction<SshClient> Body { get; set; }

        [Category("SSH Settings")]
        [RequiredArgument]
        [Description("Hostname or IP of the server to connect to.")]
        public InArgument<string> Host { get; set; }
        [RequiredArgument]
        [Category("SSH Settings")]
        [Description("Port on which the SSH connection will run.")]
        public InArgument<int> Port { get; set; }
        [RequiredArgument]
        [Category("SSH Settings")]
        [Description("Username used to authenticate in the SSH connection.")]
        public InArgument<string> Username { get; set; }
        [RequiredArgument]
        [Category("SSH Settings")]
        [Description("Password used to authenticate in the SSH connection.")]
        public InArgument<SecureString> Password { get; set; }
        [RequiredArgument]
        [Category("SSH Settings")]
        [Description("SSH Connection timeout. ")]
        public InArgument<TimeSpan>Timeout { get; set; }
        [Category("Proxy Settings")]
        [DisplayName("Proxy Host")]
        [Description("Proxy hostname or IP to be used for the SSH connection.")]
        public InArgument<string> ProxyHost { get; set; }
        [Category("Proxy Settings")]
        [DisplayName("Proxy Port")]
        [Description("Proxy port to be used for the SSH connection.")]
        public InArgument<int> ProxyPort { get; set; }
        [Category("Proxy Settings")]
        [DisplayName("Proxy Username")]
        [Description("Proxy username to be used when authenticating to the proxy server.")]
        public InArgument<string> ProxyUsername { get; set; }
        [Category("Proxy Settings")]
        [DisplayName("Proxy Password")]
        [Description("Proxy password to be used when authenticating to the proxy server.")]
        public InArgument<SecureString> ProxyPassword { get; set; }

        private SshClient sshClient;
        private ConnectionInfo connectionInfo;

        public SSHConnectScopeActivity()
        {
            Body = new ActivityAction<SshClient>
            {

                Argument = new DelegateInArgument<SshClient>("SSHClient"),
                Handler = new Sequence { DisplayName = "Run SSH Commands" }
            };
        }

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            base.CacheMetadata(metadata);

            if (ProxyHost != null && ProxyPort == null)
            {
                metadata.AddValidationError("If Proxy Host is set, Proxy Port is also required");
            }

            if (ProxyHost!= null && ProxyUsername != null && ProxyPassword == null)
            {
                metadata.AddValidationError("If Proxy Host and Proxy Username are set, Proxy Password is also required");
            }
        }

        protected override void Execute(NativeActivityContext context)
        {
            var sshPassword = new NetworkCredential("", Password.Get(context)).Password;
            var proxyPassword = new NetworkCredential("", ProxyPassword.Get(context)).Password;

            if (!string.IsNullOrEmpty(ProxyHost.Get(context))) // Proxy defined
            {
                if (!string.IsNullOrEmpty(ProxyUsername.Get(context))) // Proxy authentication
                {
                    connectionInfo = new PasswordConnectionInfo(Host.Get(context), Port.Get(context), Username.Get(context), Encoding.UTF8.GetBytes(sshPassword), ProxyTypes.Http, ProxyHost.Get(context), ProxyPort.Get(context), ProxyUsername.Get(context), proxyPassword);
                }
                else // No proxy authentication
                {
                    connectionInfo = new PasswordConnectionInfo(Host.Get(context), Port.Get(context), Username.Get(context), sshPassword, ProxyTypes.Http, ProxyHost.Get(context), ProxyPort.Get(context));
                }
            }
            else // No Proxy defined
            {
                connectionInfo = new PasswordConnectionInfo(Host.Get(context), Port.Get(context), Username.Get(context), sshPassword);
            }

            connectionInfo.Timeout = Timeout.Get(context);

            try
            {
                sshClient = new SshClient(connectionInfo);
                sshClient.Connect();
                if (Body != null)
                {
                    //scheduling the execution of the child activities
                    // and passing the value of the delegate argument
                    context.ScheduleAction(Body, sshClient, OnCompleted, OnFaulted);
                }
            }
            catch (Exception)
            {
                CleanupSSHClient();
                throw;
            }
        }

        private void CleanupSSHClient()
        {
            if (sshClient != null)
            {
                if (sshClient.IsConnected)
                    sshClient.Disconnect();
                sshClient.Dispose();
            }
        }
        private void OnFaulted(NativeActivityFaultContext faultContext, Exception propagatedException, ActivityInstance propagatedFrom)
        {
            faultContext.CancelChildren();
            CleanupSSHClient();
        }

        private void OnCompleted(NativeActivityContext context, ActivityInstance completedInstance)
        {
            CleanupSSHClient();
        }
    }
}
