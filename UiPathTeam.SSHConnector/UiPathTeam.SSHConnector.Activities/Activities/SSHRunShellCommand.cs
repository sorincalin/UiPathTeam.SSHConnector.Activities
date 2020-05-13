using System;
using System.Activities;
using System.Threading;
using System.Threading.Tasks;
using Renci.SshNet;
using UiPathTeam.SSHConnector.Activities.Properties;
using UiPath.Shared.Activities;
using UiPath.Shared.Activities.Localization;
using System.Text.RegularExpressions;

namespace UiPathTeam.SSHConnector.Activities
{
    [LocalizedDisplayName(nameof(Resources.SSHRunShellCommand_DisplayName))]
    [LocalizedDescription(nameof(Resources.SSHRunShellCommand_Description))]
    public class SSHRunShellCommand : ContinuableAsyncCodeActivity
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

        [LocalizedDisplayName(nameof(Resources.SSHRunShellCommand_Command_DisplayName))]
        [LocalizedDescription(nameof(Resources.SSHRunShellCommand_Command_Description))]
        [LocalizedCategory(nameof(Resources.Input_Category))]
        public InArgument<string> Command { get; set; }

        [LocalizedDisplayName(nameof(Resources.SSHTimeout_DisplayName))]
        [LocalizedDescription(nameof(Resources.SSHTimeout_Description))]
        [LocalizedCategory(nameof(Resources.Input_Category))]
        public InArgument<int> SSHTimeoutMS { get; set; } = 30000;

        [LocalizedDisplayName(nameof(Resources.SSHRunShellCommand_CheckExitCode_DisplayName))]
        [LocalizedDescription(nameof(Resources.SSHRunShellCommand_CheckExitCode_Description))]
        [LocalizedCategory(nameof(Resources.Options_Category))]
        public InArgument<bool> CheckExitCode { get; set; } = true;

        [LocalizedDisplayName(nameof(Resources.SSHRunShellCommand_ShellStream_DisplayName))]
        [LocalizedDescription(nameof(Resources.SSHRunShellCommand_ShellStream_Description))]
        [LocalizedCategory(nameof(Resources.Options_Category))]
        public InArgument<ShellStream> ShellStream { get; set; }

        [LocalizedDisplayName(nameof(Resources.SSHRunShellCommand_ShellExpectedPrompt_DisplayName))]
        [LocalizedDescription(nameof(Resources.SSHRunShellCommand_ShellExpectedPrompt_Description))]
        [LocalizedCategory(nameof(Resources.Options_Category))]
        public InArgument<string> ShellExpectedPrompt { get; set; }

        [LocalizedDisplayName(nameof(Resources.SSHRunShellCommand_Result_DisplayName))]
        [LocalizedDescription(nameof(Resources.SSHRunShellCommand_Result_Description))]
        [LocalizedCategory(nameof(Resources.Output_Category))]
        public OutArgument<string> Result { get; set; }

        #endregion


        #region Constructors

        public SSHRunShellCommand()
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
            var checkExitCode = CheckExitCode.Get(context);
            var shellStream = ShellStream.Get(context);
            var shellExpectedPrompt = ShellExpectedPrompt.Get(context);

            // Set a timeout on the execution
            var task = ExecuteWithTimeout(context, cancellationToken);
            if (await Task.WhenAny(task, Task.Delay(TimeoutMS.Get(context), cancellationToken)) == task)
            {
                await task;
            }
            else
            {
                throw new TimeoutException(Resources.Timeout_Error);
            }
            
            // Outputs
            return (ctx) => {
                Result.Set(ctx, task.Result);
            };
        }

        private async Task<string> ExecuteWithTimeout(AsyncCodeActivityContext context, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ShellStream shellStream;
            Regex expectedShellPromptRegex;
            IObjectContainer scopeObjectContainer = context.DataContext.GetProperties()[SSHConnectScope.ParentContainerPropertyTag]
                                                                                .GetValue(context.DataContext) as IObjectContainer;

            if (ShellStream.Expression == null)
            {
                if (scopeObjectContainer == null)
                {
                    throw new ArgumentException("ShellStream was not provided and cannot be retrieved from the container.");
                }

                shellStream = scopeObjectContainer.Get<ShellStream>();
            }
            else
            {
                shellStream = ShellStream.Get(context);
            }

            if (ShellExpectedPrompt.Expression == null)
            {
                if (scopeObjectContainer == null)
                {
                    throw new ArgumentException("Shell Expected Prompt was not provided and cannot be retrieved from the container.");
                }

                expectedShellPromptRegex = scopeObjectContainer.Get<Regex>();
            }
            else
            {
                expectedShellPromptRegex = new Regex(ShellExpectedPrompt.Get(context), RegexOptions.Compiled);
            }

            return SSHHelpers.Execute(  shellStream,
                                        expectedShellPromptRegex,
                                        Command.Get(context),
                                        TimeSpan.FromMilliseconds(SSHTimeoutMS.Get(context)),
                                        CheckExitCode.Get(context));
        }

        #endregion
    }
}

