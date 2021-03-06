using System;
using System.Activities;
using System.Threading;
using System.Threading.Tasks;
using System.Security;
using System.Activities.Statements;
using System.ComponentModel;
using UiPathTeam.SSHConnector.Activities.Properties;
using UiPath.Shared.Activities;
using UiPath.Shared.Activities.Localization;
using System.Net;
using Renci.SshNet;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Renci.SshNet.Common;

namespace UiPathTeam.SSHConnector.Activities
{
    [LocalizedDisplayName(nameof(Resources.SSHConnectScope_DisplayName))]
    [LocalizedDescription(nameof(Resources.SSHConnectScope_Description))]
    public class SSHConnectScope : ContinuableAsyncNativeActivity
    {
        #region Properties

        [Browsable(false)]
        public ActivityAction<IObjectContainer​> Body { get; set; }

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

        [LocalizedDisplayName(nameof(Resources.SSHConnectScope_EnableKeyboardInteractiveAuthenticationMethod_DisplayName))]
        [LocalizedDescription(nameof(Resources.SSHConnectScope_EnableKeyboardInteractiveAuthenticationMethod_Description))]
        [LocalizedCategory(nameof(Resources.SSHKeyboardInteractiveAuthenticationSettings_Category))]
        public InArgument<bool> EnableKeyboardInteractiveAuthenticationMethod { get; set; }

        [LocalizedDisplayName(nameof(Resources.SSHConnectScope_ExpectedPasswordPrompt_DisplayName))]
        [LocalizedDescription(nameof(Resources.SSHConnectScope_ExpectedPasswordPrompt_Description))]
        [LocalizedCategory(nameof(Resources.SSHKeyboardInteractiveAuthenticationSettings_Category))]
        public InArgument<string> ExpectedPasswordPrompt { get; set; } = "Password:";

        [LocalizedDisplayName(nameof(Resources.SSHConnectScope_Host_DisplayName))]
        [LocalizedDescription(nameof(Resources.SSHConnectScope_Host_Description))]
        [LocalizedCategory(nameof(Resources.SSHSettings_Category))]
        public InArgument<string> Host { get; set; }

        [LocalizedDisplayName(nameof(Resources.SSHConnectScope_Port_DisplayName))]
        [LocalizedDescription(nameof(Resources.SSHConnectScope_Port_Description))]
        [LocalizedCategory(nameof(Resources.SSHSettings_Category))]
        public InArgument<int> Port { get; set; }

        [LocalizedDisplayName(nameof(Resources.SSHConnectScope_Username_DisplayName))]
        [LocalizedDescription(nameof(Resources.SSHConnectScope_Username_Description))]
        [LocalizedCategory(nameof(Resources.SSHSettings_Category))]
        public InArgument<string> Username { get; set; }

        [LocalizedDisplayName(nameof(Resources.SSHConnectScope_Password_DisplayName))]
        [LocalizedDescription(nameof(Resources.SSHConnectScope_Password_Description))]
        [LocalizedCategory(nameof(Resources.SSHSettings_Category))]
        public InArgument<SecureString> Password { get; set; }

        [LocalizedDisplayName(nameof(Resources.SSHConnectScope_PrivateKey_DisplayName))]
        [LocalizedDescription(nameof(Resources.SSHConnectScope_PrivateKey_Description))]
        [LocalizedCategory(nameof(Resources.SSHSettings_Category))]
        public InArgument<string> PrivateKey { get; set; }

        [LocalizedDisplayName(nameof(Resources.SSHTimeout_DisplayName))]
        [LocalizedDescription(nameof(Resources.SSHTimeout_Description))]
        [LocalizedCategory(nameof(Resources.SSHSettings_Category))]
        public InArgument<int> SSHTimeoutMS { get; set; } = 30000;

        [LocalizedDisplayName(nameof(Resources.SSHConnectScope_ShellExpectedPrompt_DisplayName))]
        [LocalizedDescription(nameof(Resources.SSHConnectScope_ShellExpectedPrompt_Description))]
        [LocalizedCategory(nameof(Resources.Options_Category))]
        public InArgument<string> ShellExpectedPrompt { get; set; }

        [LocalizedDisplayName(nameof(Resources.SSHConnectScope_ProxyHost_DisplayName))]
        [LocalizedDescription(nameof(Resources.SSHConnectScope_ProxyHost_Description))]
        [LocalizedCategory(nameof(Resources.ProxySettings_Category))]
        public InArgument<string> ProxyHost { get; set; }

        [LocalizedDisplayName(nameof(Resources.SSHConnectScope_ProxyPort_DisplayName))]
        [LocalizedDescription(nameof(Resources.SSHConnectScope_ProxyPort_Description))]
        [LocalizedCategory(nameof(Resources.ProxySettings_Category))]
        public InArgument<int> ProxyPort { get; set; }

        [LocalizedDisplayName(nameof(Resources.SSHConnectScope_ProxyUsername_DisplayName))]
        [LocalizedDescription(nameof(Resources.SSHConnectScope_ProxyUsername_Description))]
        [LocalizedCategory(nameof(Resources.ProxySettings_Category))]
        public InArgument<string> ProxyUsername { get; set; }

        [LocalizedDisplayName(nameof(Resources.SSHConnectScope_ProxyPassword_DisplayName))]
        [LocalizedDescription(nameof(Resources.SSHConnectScope_ProxyPassword_Description))]
        [LocalizedCategory(nameof(Resources.ProxySettings_Category))]
        public InArgument<SecureString> ProxyPassword { get; set; }

        [LocalizedDisplayName(nameof(Resources.SSHConnectScope_ShellWelcomeMessage_DisplayName))]
        [LocalizedDescription(nameof(Resources.SSHConnectScope_ShellWelcomeMessage_Description))]
        [LocalizedCategory(nameof(Resources.Output_Category))]
        public OutArgument<string> ShellWelcomeMessage { get; set; }

        // A tag used to identify the scope in the activity context
        internal static string ParentContainerPropertyTag => "ScopeActivity";

        // Object Container: Add strongly-typed objects here and they will be available in the scope's child activities.
        private readonly IObjectContainer _objectContainer;

        private SshClient sshClient;
        private string sshPassword;
        private ShellStream currentShellStream;
        private Regex expectedPromptRegex;
        private string expectedPasswordPrompt;
        #endregion


        #region Constructors

        public SSHConnectScope(IObjectContainer objectContainer)
        {
            _objectContainer = objectContainer;
            Body = new ActivityAction<IObjectContainer>
            {
                Argument = new DelegateInArgument<IObjectContainer> (ParentContainerPropertyTag),
                Handler = new Sequence { DisplayName = Resources.Do }
            };
        }

        public SSHConnectScope() : this(new ObjectContainer())
        {

        }

        #endregion


        #region Protected Methods

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            if (Host == null) metadata.AddValidationError(string.Format(Resources.ValidationValue_Error, nameof(Host)));
            if (Port == null) metadata.AddValidationError(string.Format(Resources.ValidationValue_Error, nameof(Port)));
            if (Username == null) metadata.AddValidationError(string.Format(Resources.ValidationValue_Error, nameof(Username)));
            
            if ((Password == null && PrivateKey == null) || (Password !=null && PrivateKey != null)) 
                metadata.AddValidationError(string.Format(Resources.ValidationExclusiveProperties_Error, nameof(Password), nameof(PrivateKey)));

            if (ProxyHost != null && ProxyPort == null)
                metadata.AddValidationError(string.Format(Resources.ValidationValue_Error, nameof(ProxyPort)));
            if (ProxyHost != null && ProxyUsername != null && ProxyPassword == null)
                metadata.AddValidationError(string.Format(Resources.ValidationValue_Error, nameof(ProxyHost)));

            base.CacheMetadata(metadata);
        }

        protected override async Task<Action<NativeActivityContext>> ExecuteAsync(NativeActivityContext  context, CancellationToken cancellationToken)
        {
            // Inputs
            var activityTimeout = TimeoutMS.Get(context);
            
            // Set a timeout on the execution
            var task = ExecuteWithTimeout(context, cancellationToken);
            if (await Task.WhenAny(task, Task.Delay(activityTimeout, cancellationToken)) == task)
            {
                await task;
            }
            else
            {
                throw new TimeoutException(Resources.Timeout_Error);
            }

            _objectContainer.Add(sshClient);
            _objectContainer.Add(currentShellStream);
            _objectContainer.Add(expectedPromptRegex);

            return (ctx) => {
                // Schedule child activities
                if (Body != null)
				    ctx.ScheduleAction<IObjectContainer>(Body, _objectContainer, OnCompleted, OnFaulted);
            };
        }

        private async Task ExecuteWithTimeout(NativeActivityContext context, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            sshPassword = new NetworkCredential("", Password.Get(context)).Password;
            expectedPasswordPrompt = ExpectedPasswordPrompt.Get(context);

            var proxyPassword = new NetworkCredential("", ProxyPassword.Get(context)).Password;
            var sshTimeout = TimeSpan.FromMilliseconds(SSHTimeoutMS.Get(context));
            var keybAuth = new KeyboardInteractiveAuthenticationMethod(Username.Get(context));
            keybAuth.AuthenticationPrompt += new EventHandler<AuthenticationPromptEventArgs>(HandleKeyEvent);

            ConnectionInfo connectionInfo;
            if (!string.IsNullOrEmpty(ProxyHost.Get(context))) // Proxy defined
            {
                if (!string.IsNullOrEmpty(ProxyUsername.Get(context))) // Proxy authentication
                {
                    if (!string.IsNullOrEmpty(PrivateKey.Get(context)))
                        connectionInfo = new PrivateKeyConnectionInfo(Host.Get(context), Port.Get(context), Username.Get(context), ProxyTypes.Http, ProxyHost.Get(context), ProxyPort.Get(context), ProxyUsername.Get(context), proxyPassword, new[] { new PrivateKeyFile(PrivateKey.Get(context)) });
                    else if (EnableKeyboardInteractiveAuthenticationMethod.Get(context))
                        connectionInfo = new ConnectionInfo(Host.Get(context), Port.Get(context), Username.Get(context), ProxyTypes.Http, ProxyHost.Get(context), ProxyPort.Get(context), ProxyUsername.Get(context), proxyPassword, keybAuth);
                    else
                        connectionInfo = new PasswordConnectionInfo(Host.Get(context), Port.Get(context), Username.Get(context), Encoding.UTF8.GetBytes(sshPassword), ProxyTypes.Http, ProxyHost.Get(context), ProxyPort.Get(context), ProxyUsername.Get(context), proxyPassword);
                }
                else // No proxy authentication
                {
                    if (!string.IsNullOrEmpty(PrivateKey.Get(context)))
                        connectionInfo = new PrivateKeyConnectionInfo(Host.Get(context), Port.Get(context), Username.Get(context), ProxyTypes.Http, ProxyHost.Get(context), ProxyPort.Get(context), new[] { new PrivateKeyFile(PrivateKey.Get(context)) });
                    else
                        connectionInfo = new PasswordConnectionInfo(Host.Get(context), Port.Get(context), Username.Get(context), sshPassword, ProxyTypes.Http, ProxyHost.Get(context), ProxyPort.Get(context));
                }
            }
            else // No Proxy defined
            {
                if (!string.IsNullOrEmpty(PrivateKey.Get(context)))
                    connectionInfo = new PrivateKeyConnectionInfo(Host.Get(context), Port.Get(context), Username.Get(context), new[] { new PrivateKeyFile(PrivateKey.Get(context)) });
                else if (EnableKeyboardInteractiveAuthenticationMethod.Get(context))
                    connectionInfo = new ConnectionInfo(Host.Get(context), Port.Get(context), Username.Get(context), keybAuth);
                else
                    connectionInfo = new PasswordConnectionInfo(Host.Get(context), Port.Get(context), Username.Get(context), sshPassword);
            }

            connectionInfo.Timeout = sshTimeout;

            sshClient = new SshClient(connectionInfo);
            sshClient.Connect();

            if (ShellExpectedPrompt.Expression != null)
            {
                var terminalMode = new Dictionary<TerminalModes, uint>
                {
                    { TerminalModes.ECHO, 53 }
                };

                expectedPromptRegex = new Regex(ShellExpectedPrompt.Get(context), RegexOptions.Compiled);
                currentShellStream = sshClient.CreateShellStream("UiPathTeam.SSHConnector.Shell", 0, 0, 0, 0, 4096, terminalMode);
                var welcomeMessage = SSHHelpers.Expect(currentShellStream, expectedPromptRegex, string.Empty, sshTimeout);
                ShellWelcomeMessage.Set(context, welcomeMessage);
            }
        }

        #endregion


        #region Events

        private void HandleKeyEvent(object sender, AuthenticationPromptEventArgs e)
        {
            foreach (AuthenticationPrompt prompt in e.Prompts)
            {
                if (prompt.Request.IndexOf(expectedPasswordPrompt, StringComparison.InvariantCultureIgnoreCase) != -1)
                {
                    prompt.Response = sshPassword;
                }
            }
        }

        private void OnFaulted(NativeActivityFaultContext faultContext, Exception propagatedException, ActivityInstance propagatedFrom)
        {
            faultContext.CancelChildren();
            Cleanup();
        }

        private void OnCompleted(NativeActivityContext context, ActivityInstance completedInstance)
        {
            Cleanup();
        }

        #endregion


        #region Helpers
        
        private void Cleanup()
        {
            if (sshClient != null)
            {
                if (sshClient.IsConnected)
                    sshClient.Disconnect();
                sshClient.Dispose();
            }

            if (currentShellStream != null)
            {
                currentShellStream.Close();
                currentShellStream.Dispose();
            }

            var disposableObjects = _objectContainer.Where(o => o is IDisposable);
            foreach (var obj in disposableObjects)
            {
                if (obj is IDisposable dispObject)
                    dispObject.Dispose();
            }
            _objectContainer.Clear();
        }

        #endregion
    }
}

