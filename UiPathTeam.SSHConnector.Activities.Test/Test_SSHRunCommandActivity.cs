using System;
using System.Activities;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Renci.SshNet;

namespace UiPathTeam.SSHConnector.Activities.Test
{
    [TestClass]
    public class Test_SSHRunCommandActivity
    {
        [TestMethod]
        public void SSHRunCommandWithBuiltClient()
        {
            var sshClient = new SshClient("test.rebex.net", "demo", "password");
            sshClient.Connect();

            var runCommandActivity = new SSHRunCommandActivity
            {
                Command = "help",
                Timeout = TimeSpan.FromSeconds(3),
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
