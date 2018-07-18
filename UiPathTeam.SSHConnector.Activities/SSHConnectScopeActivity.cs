using System;
using System.Activities;
using System.Activities.Statements;
using System.ComponentModel;
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
        public InArgument<string> Host { get; set; }
        [RequiredArgument]
        [Category("SSH Settings")]
        public InArgument<int> Port { get; set; }
        [RequiredArgument]
        [Category("SSH Settings")]
        public InArgument<string> Username { get; set; }
        [RequiredArgument]
        [Category("SSH Settings")]
        public InArgument<string> Password { get; set; }
        [RequiredArgument]
        [Category("SSH Settings")]
        public InArgument<TimeSpan>Timeout { get; set; }
        [Category("Proxy Settings")]
        [DisplayName("Proxy Host")]
        public InArgument<string> ProxyHost { get; set; }
        [Category("Proxy Settings")]
        [DisplayName("Proxy Port")]
        public InArgument<int> ProxyPort { get; set; }
        [Category("Proxy Settings")]
        [DisplayName("Proxy Username")]
        public InArgument<string> ProxyUsername { get; set; }
        [Category("Proxy Settings")]
        [DisplayName("Proxy Password")]
        public InArgument<string> ProxyPassword { get; set; }

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
            if (!string.IsNullOrEmpty(ProxyHost.Get(context))) // Proxy defined
            {
                if (!string.IsNullOrEmpty(ProxyUsername.Get(context))) // Proxy authentication
                {
                    connectionInfo = new PasswordConnectionInfo(Host.Get(context), Port.Get(context), Username.Get(context), Encoding.UTF8.GetBytes(Password.Get(context)), ProxyTypes.Http, ProxyHost.Get(context), ProxyPort.Get(context), ProxyUsername.Get(context), ProxyPassword.Get(context));
                }
                else // No proxy authentication
                {
                    connectionInfo = new PasswordConnectionInfo(Host.Get(context), Port.Get(context), Username.Get(context), Password.Get(context), ProxyTypes.Http, ProxyHost.Get(context), ProxyPort.Get(context));
                }
            }
            else // No Proxy defined
            {
                connectionInfo = new PasswordConnectionInfo(Host.Get(context), Port.Get(context), Username.Get(context), Password.Get(context));
            }

            connectionInfo.Timeout = Timeout.Get(context);

            sshClient = new SshClient(connectionInfo);
            sshClient.Connect();

            if (Body != null)
            {
                //scheduling the execution of the child activities
                // and passing the value of the delegate argument
                context.ScheduleAction(Body, sshClient, OnCompleted, OnFaulted);
            }
        }


        private void OnFaulted(NativeActivityFaultContext faultContext, Exception propagatedException, ActivityInstance propagatedFrom)
        {
            if (sshClient.IsConnected)
                sshClient.Disconnect();
            sshClient.Dispose();
        }

        private void OnCompleted(NativeActivityContext context, ActivityInstance completedInstance)
        {
            if (sshClient.IsConnected)
                sshClient.Disconnect();
            sshClient.Dispose();
        }
    }
}
