using Renci.SshNet;
using System;
using System.Activities;
using System.ComponentModel;

namespace UiPathTeam.SSHConnector.Activities
{
    [Designer(typeof(SSHRunCommandActivityDesigner))]
    [DisplayName("SSH Run Command")]
    public class SSHRunCommandActivity : CodeActivity
    {
        [Category("Input")]
        public InArgument<SshClient> SSHClient { get; set; }

        [Category("Input")]
        [RequiredArgument]
        public InArgument<string> Command { get; set; }
        [Category("Input")]
        [RequiredArgument]
        public InArgument<TimeSpan> Timeout { get; set; }

        [Category("Output")]
        public OutArgument<int> ExitStatus { get; set; }
        [Category("Output")]
        public OutArgument<string> Result { get; set; }
        [Category("Output")]
        public OutArgument<string> Error { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            SshClient sshClient = null;

            if (SSHClient.Expression== null)
            {
                var sshClientArgument = context.DataContext.GetProperties()["SSHClient"];
                sshClient = sshClientArgument.GetValue(context.DataContext) as SshClient;
                if (sshClient == null)
                {
                    throw (new ArgumentException("SSHClient was not provided and cannot be retrieved from the container."));
                }
            }
            else
            {
                sshClient = SSHClient.Get(context);
            }
            
            var sshCommand = sshClient.CreateCommand(Command.Get(context));
            sshCommand.CommandTimeout = Timeout.Get(context);
            sshCommand.Execute();

            ExitStatus.Set(context, sshCommand.ExitStatus);
            Result.Set(context, sshCommand.Result);
            Error.Set(context, sshCommand.Error);
        }
    }
}
