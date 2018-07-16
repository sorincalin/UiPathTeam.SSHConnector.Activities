using System;
using System.Activities;
using System.Activities.Statements;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UiPathTeam.SSHConnector.Activities.Test
{
    [TestClass]
    public class Test_SSHConnectScopeActivity
    {
        [TestMethod]
        public void SSHConnectScopeWithSSHRunCommandInside()
        {
            var connectScopeActivity = new SSHConnectScopeActivity
            {
                Host = "test.rebex.net",
                Username = "demo",
                Password = "password",
                Port = 22
            };

            var runCommandActivity = new SSHRunCommandActivity
            {
                Command = "help",
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
        public void SSHConnectScopeWithoutSSHRunCommandInside()
        {
            var connectScopeActivity = new SSHConnectScopeActivity
            {
                Host = "test.rebex.net",
                Username = "demo",
                Password = "password",
                Port = 22
            };

            var output = WorkflowInvoker.Invoke(connectScopeActivity);
        }
    }
}
