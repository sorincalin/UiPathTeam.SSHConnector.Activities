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
        [RequiredArgument]
        [Description("Command to be sent over the SSH connection. Multiple commands can be linked together according to the target's machine specific OS.")]
        public InArgument<string> Command { get; set; }
        [Category("Input")]
        [RequiredArgument]
        [Description("Timeout for the SSH command.")]
        public InArgument<TimeSpan> Timeout { get; set; }

        [Category("Input Optional")]
        [Description("SshClient object to be used when not in a SSH Connect Scope.")]
        public InArgument<SshClient> SSHClient { get; set; }

        [Category("Output")]
        [Description("Status code of the command.")]
        public OutArgument<int> ExitStatus { get; set; }
        [Category("Output")]
        [Description("Text result of the command.")]
        public OutArgument<string> Result { get; set; }
        [Category("Output")]
        [Description("Execution error of the command.")]
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
