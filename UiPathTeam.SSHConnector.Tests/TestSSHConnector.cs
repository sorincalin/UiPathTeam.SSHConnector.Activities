using System;
using System.Activities;
using System.Activities.Statements;
using System.Net;
using System.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Renci.SshNet;
using UiPathTeam.SSHConnector.Activities;

namespace UiPathTeam.SSHConnector.Tests
{
    [TestClass]
    public class Test_SSHConnectorActivities
    {
        // ****************** WARNING *******************
        // Please note that I'm using a local SSH server and a free, online Proxy server.
        public const string Test_SSHHost = "192.168.173.132";
        public const string Test_SSHUsername = "sorin";
        public const string Test_SSHPassword = "1qazXSW@";
        public const string Test_ExpectedPrompt = @"[a-zA-Z0-9_.-]*@[a-zA-Z0-9_.-]*:\~.*\\$ $";

        public const string Test_SSHCommand = "help";

        public const string Test_ProxyHost = "80.211.41.5";
        public const int Test_ProxyPort = 3128;
        // ***********************************************

        [TestMethod]
        public void SSHConnectScopeWithSSHRunCommandInside()
        {

            var connectScopeActivity = new SSHConnectScope
            {
                Host = Test_SSHHost,
                Username = Test_SSHUsername,
                Password = new InArgument<SecureString>(value => new NetworkCredential("", Test_SSHPassword).SecurePassword),
                Port = 22,
                TimeoutMS = 5000
            };

            var runCommandActivity = new SSHRunCommand
            {
                Command = Test_SSHCommand,
                TimeoutMS = 3000
            };

            connectScopeActivity.Body.Handler = new Sequence()
            {
                Activities =
                {
                   runCommandActivity
                }
            };

            var output = WorkflowInvoker.Invoke(connectScopeActivity);
        }

        [TestMethod]
        public void SSHConnectScopeWithSSHRunShellCommandInside()
        {

            var connectScopeActivity = new SSHConnectScope
            {
                Host = Test_SSHHost,
                Username = Test_SSHUsername,
                Password = new InArgument<SecureString>(value => new NetworkCredential("", Test_SSHPassword).SecurePassword),
                Port = 22,
                ShellExpectedPrompt = Test_ExpectedPrompt,
                TimeoutMS = 3000
            };

            connectScopeActivity.Body.Handler = new Sequence()
            {
                Activities =
                {
                    new SSHRunShellCommand { Command = $"ssh {Test_SSHUsername}@localhost", TimeoutMS = 3000, ShellExpectedPrompt = "password\\:", CheckExitCode = false},
                    new SSHRunShellCommand { Command = Test_SSHPassword, TimeoutMS = 3000, CheckExitCode = true},
                    new SSHRunShellCommand { Command = "ls", TimeoutMS = 3000, CheckExitCode = true },
                    new SSHRunShellCommand { Command = "exit", TimeoutMS = 3000, CheckExitCode = true }
                }
            };

            var output = WorkflowInvoker.Invoke(connectScopeActivity);
        }

        [TestMethod]
        public void SSHConnectScopeWithoutSSHRunCommandInside()
        {
            var connectScopeActivity = new SSHConnectScope
            {
                Host = Test_SSHHost,
                Username = Test_SSHUsername,
                Password = new InArgument<SecureString>(value => new NetworkCredential("", Test_SSHPassword).SecurePassword),
                Port = 22,
                TimeoutMS = 3000
            };

            var output = WorkflowInvoker.Invoke(connectScopeActivity);
        }

        [TestMethod]
        public void SSHRunCommandWithBuiltClient()
        {
            var sshClient = new SshClient(Test_SSHHost, Test_SSHUsername, Test_SSHPassword);
            sshClient.Connect();

            var output = WorkflowInvoker.Invoke(new SSHRunCommand
            {
                Command = Test_SSHCommand,
                TimeoutMS = 3000,
                SSHClient = new InArgument<SshClient>((ctx) => sshClient)
            });

            sshClient.Disconnect();
            sshClient.Dispose();

            Assert.IsTrue(string.IsNullOrEmpty(Convert.ToString(output["Error"])));
            Assert.IsFalse(string.IsNullOrEmpty(Convert.ToString(output["Result"])));
            Assert.IsTrue(Convert.ToInt32(output["ExitStatus"]) == 0);
        }


        [TestMethod]
        [Ignore]
        public void SSHConnectScopeWithProxy()
        {
            var connectScopeActivity = new SSHConnectScope
            {
                Host = Test_SSHHost,
                Username = Test_SSHUsername,
                Password = new InArgument<SecureString>(value => new NetworkCredential("", Test_SSHPassword).SecurePassword),
                Port = 22,
                TimeoutMS = 3000,
                ProxyHost = Test_ProxyHost,
                ProxyPort = Test_ProxyPort
            };

            var runCommandActivity = new SSHRunCommand
            {
                Command = Test_SSHCommand,
                TimeoutMS = 3000
            };

            connectScopeActivity.Body.Handler = new Sequence()
            {
                Activities =
                {
                   runCommandActivity
                }
            };

            var output = WorkflowInvoker.Invoke(connectScopeActivity);
        }
    }
}
