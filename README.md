# UiPathTeam.SSHConnector.Activities
Custom UiPath Activities that allow for creation of an SSH connection and running commands inside it.

<b>Summary</b>

Allows establishing an SSH connection and sending commands.

<b>Benefits</b>

Useful for dealing with UNIX systems that expose an SSH endpoint. 

<b>Package specifications</b>	

Contains two custom activities that facilitate establishing an SSH connection and sending commands through it. Please note that the Unit tests are based on free, online public SSH and Proxy servers so they might go offline at some point. 

<b>SSHConnectionScopeActivity:</b>

<string> Host (required)
<int> Port (required)
<string> Username (required)
<string> Password (required)
<TimeSpan> Timeout (required)
<string> ProxyHost
<int> ProxyPort
<string> ProxyUsername
<string> ProxyPassword

<b>SSHRunCommandActivity:</b>

InArguments
<string> Command (required)
<TimeSpan> Timeout (required)

<SshClient> SSHClient - provide this if you would prefer to create your own SshClient rather than use the activity inside a SSHConnectionScopeActivity.

OutArguments
<string> Result 
<string> Error
<int> ExitStatus

<b>Dependencies</b>

SSH.NET >= 2016.1.0 

<b>Installation guide</b>

Install the UiPathTeam.SSHConnector.Activities.1.0.1.nupkg using the Package Manager
