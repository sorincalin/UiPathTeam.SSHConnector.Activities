using System;
using System.Activities;
using System.Activities.Statements;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Renci.SshNet;

namespace UiPathTeam.SSHConnector.Activities.Test
{
    [TestClass]
    public class Test_SSHConnectorActivities
    {
        // ****************** WARNING *******************
        // Please note that I'm using free, online SSH and Proxy servers.
        // In case they go offline you will have to configure different ones to perform the tests.
        public const string Test_SSHHost = "test.rebex.net";
        public const string Test_SSHUsername = "demo";
        public const string Test_SSHPassword = "password";
        public const string Test_SSHCommand = "help";

        public const string Test_ProxyHost = "80.211.41.5";
        public const int Test_ProxyPort = 3128;
        // ***********************************************

        public const int TimeoutInSeconds = 30;

        [TestMethod]
        public void SSHConnectScopeWithSSHRunCommandInside()
        {
            
            var connectScopeActivity = new SSHConnectScopeActivity
            {
                Host = Test_SSHHost,
                Username = Test_SSHUsername,
                Password = Test_SSHPassword,
                Port = 22,
                Timeout = TimeSpan.FromSeconds(TimeoutInSeconds)
            };

            var runCommandActivity = new SSHRunCommandActivity
            {
                Command = Test_SSHCommand,
                Timeout = TimeSpan.FromSeconds(3)
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
        public void SSHConnectScopeWithProxy()
        {
            var connectScopeActivity = new SSHConnectScopeActivity
            {
                Host = Test_SSHHost,
                Username = Test_SSHUsername,
                Password = Test_SSHPassword,
                Port = 22,
                Timeout = TimeSpan.FromSeconds(TimeoutInSeconds),
                ProxyHost = Test_ProxyHost,
                ProxyPort = Test_ProxyPort
            };

            var runCommandActivity = new SSHRunCommandActivity
            {
                Command = Test_SSHCommand,
                Timeout = TimeSpan.FromSeconds(TimeoutInSeconds)
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
        public void SSHConnectScopeWithoutSSHRunCommandInside()
        {
            var connectScopeActivity = new SSHConnectScopeActivity
            {
                Host = Test_SSHHost,
                Username = Test_SSHUsername,
                Password = Test_SSHPassword,
                Port = 22,
                Timeout = TimeSpan.FromSeconds(TimeoutInSeconds)
            };

            var output = WorkflowInvoker.Invoke(connectScopeActivity);
        }

        [TestMethod]
        public void SSHRunCommandWithBuiltClient()
        {
            var sshClient = new SshClient(Test_SSHHost, Test_SSHUsername, Test_SSHPassword);
            sshClient.Connect();

            var runCommandActivity = new SSHRunCommandActivity
            {
                Command = Test_SSHCommand,
                Timeout = TimeSpan.FromSeconds(TimeoutInSeconds),
                SSHClient = new InArgument<SshClient>((ctx) => sshClient)
            };

            var output = WorkflowInvoker.Invoke(runCommandActivity);

            sshClient.Disconnect();
            sshClient.Dispose();

            Assert.IsTrue(string.IsNullOrEmpty(Convert.ToString(output["Error"])));
            Assert.IsFalse(string.IsNullOrEmpty(Convert.ToString(output["Result"])));
            Assert.IsTrue(Convert.ToInt32(output["ExitStatus"]) == 0);

        }
    }
}
