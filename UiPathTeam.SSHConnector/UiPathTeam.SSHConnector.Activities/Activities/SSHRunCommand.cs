using System;
using System.Activities;
using System.Threading;
using System.Threading.Tasks;
using Renci.SshNet;
using UiPathTeam.SSHConnector.Activities.Properties;
using UiPath.Shared.Activities;
using UiPath.Shared.Activities.Localization;

namespace UiPathTeam.SSHConnector.Activities
{
    [LocalizedDisplayName(nameof(Resources.SSHRunCommand_DisplayName))]
    [LocalizedDescription(nameof(Resources.SSHRunCommand_Description))]
    public class SSHRunCommand : ContinuableAsyncCodeActivity
    {
        #region Properties

        /// <summary>
        /// If set, continue executing the remaining activities even if the current activity has failed.
        /// </summary>
        [LocalizedCategory(nameof(Resources.Common_Category))]
        [LocalizedDisplayName(nameof(Resources.ContinueOnError_DisplayName))]
        [LocalizedDescription(nameof(Resources.ContinueOnError_Description))]
        public override InArgument<bool> ContinueOnError { get; set; }

        [LocalizedCategory(nameof(Resources.Common_Category))]
        [LocalizedDisplayName(nameof(Resources.Timeout_DisplayName))]
        [LocalizedDescription(nameof(Resources.Timeout_Description))]
        public InArgument<int> TimeoutMS { get; set; } = 60000;

        [LocalizedDisplayName(nameof(Resources.SSHRunCommand_Command_DisplayName))]
        [LocalizedDescription(nameof(Resources.SSHRunCommand_Command_Description))]
        [LocalizedCategory(nameof(Resources.Input_Category))]
        public InArgument<string> Command { get; set; }

        [LocalizedDisplayName(nameof(Resources.SSHTimeout_DisplayName))]
        [LocalizedDescription(nameof(Resources.SSHTimeout_Description))]
        [LocalizedCategory(nameof(Resources.Input_Category))]
        public InArgument<int> SSHTimeoutMS { get; set; } = 30000;

        [LocalizedDisplayName(nameof(Resources.SSHRunCommand_SSHClient_DisplayName))]
        [LocalizedDescription(nameof(Resources.SSHRunCommand_SSHClient_Description))]
        [LocalizedCategory(nameof(Resources.Options_Category))]
        public InArgument<SshClient> SSHClient { get; set; }

        [LocalizedDisplayName(nameof(Resources.SSHRunCommand_ExitCode_DisplayName))]
        [LocalizedDescription(nameof(Resources.SSHRunCommand_ExitCode_Description))]
        [LocalizedCategory(nameof(Resources.Output_Category))]
        public OutArgument<int> ExitCode { get; set; }

        [LocalizedDisplayName(nameof(Resources.SSHRunCommand_Result_DisplayName))]
        [LocalizedDescription(nameof(Resources.SSHRunCommand_Result_Description))]
        [LocalizedCategory(nameof(Resources.Output_Category))]
        public OutArgument<string> Result { get; set; }

        [LocalizedDisplayName(nameof(Resources.SSHRunCommand_Error_DisplayName))]
        [LocalizedDescription(nameof(Resources.SSHRunCommand_Error_Description))]
        [LocalizedCategory(nameof(Resources.Output_Category))]
        public OutArgument<string> Error { get; set; }

        #endregion


        #region Constructors

        public SSHRunCommand()
        {
        }

        #endregion


        #region Protected Methods

        protected override void CacheMetadata(CodeActivityMetadata metadata)
        {
            if (Command == null) metadata.AddValidationError(string.Format(Resources.ValidationValue_Error, nameof(Command)));

            base.CacheMetadata(metadata);
        }

        protected override async Task<Action<AsyncCodeActivityContext>> ExecuteAsync(AsyncCodeActivityContext context, CancellationToken cancellationToken)
        {
            // Inputs
            var command = Command.Get(context);
            var sshClient = SSHClient.Get(context);

            // Set a timeout on the execution
            var task = ExecuteWithTimeout(context, cancellationToken);
            if (await Task.WhenAny(task, Task.Delay(TimeoutMS.Get(context), cancellationToken)) != task) throw new TimeoutException(Resources.Timeout_Error);

            if (task.Exception != null) { throw task.Exception; }

            // Outputs
            return (ctx) => {
                ExitCode.Set(ctx, task.Result.ExitStatus);
                Result.Set(ctx, task.Result.Result);
                Error.Set(ctx, task.Result.Error);
            };
        }

        private async Task<SshCommand> ExecuteWithTimeout(AsyncCodeActivityContext context, CancellationToken cancellationToken = default)
        {
            SshClient sshClient;
            IObjectContainer scopeObjectContainer = context.DataContext.GetProperties()[SSHConnectScope.ParentContainerPropertyTag]?.GetValue(context.DataContext) as IObjectContainer;

            if (SSHClient.Expression == null)
            {
                if (scopeObjectContainer == null)
                {
                    throw new ArgumentException("SSHClient was not provided and cannot be retrieved from the container.");
                }

                sshClient = scopeObjectContainer.Get<SshClient>();
            }
            else
            {
                sshClient = SSHClient.Get(context);
            }

            var sshCommand = sshClient.CreateCommand(Command.Get(context));
            sshCommand.CommandTimeout = TimeSpan.FromMilliseconds(SSHTimeoutMS.Get(context));
            sshCommand.Execute();

            return sshCommand;
        }

        #endregion
    }
}

