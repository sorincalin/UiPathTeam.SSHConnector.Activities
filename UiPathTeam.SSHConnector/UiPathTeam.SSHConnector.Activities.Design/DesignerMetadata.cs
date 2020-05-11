using System.Activities.Presentation.Metadata;
using System.ComponentModel;
using System.ComponentModel.Design;
using UiPathTeam.SSHConnector.Activities.Design.Designers;
using UiPathTeam.SSHConnector.Activities.Design.Properties;

namespace UiPathTeam.SSHConnector.Activities.Design
{
    public class DesignerMetadata : IRegisterMetadata
    {
        public void Register()
        {
            var builder = new AttributeTableBuilder();
            builder.ValidateTable();

            var categoryAttribute = new CategoryAttribute($"{Resources.Category}");

            builder.AddCustomAttributes(typeof(SSHConnectScope), categoryAttribute);
            builder.AddCustomAttributes(typeof(SSHConnectScope), new DesignerAttribute(typeof(SSHConnectScopeDesigner)));
            builder.AddCustomAttributes(typeof(SSHConnectScope), new HelpKeywordAttribute(""));

            builder.AddCustomAttributes(typeof(SSHRunShellCommand), categoryAttribute);
            builder.AddCustomAttributes(typeof(SSHRunShellCommand), new DesignerAttribute(typeof(SSHRunShellCommandDesigner)));
            builder.AddCustomAttributes(typeof(SSHRunShellCommand), new HelpKeywordAttribute(""));

            builder.AddCustomAttributes(typeof(SSHRunCommand), categoryAttribute);
            builder.AddCustomAttributes(typeof(SSHRunCommand), new DesignerAttribute(typeof(SSHRunCommandDesigner)));
            builder.AddCustomAttributes(typeof(SSHRunCommand), new HelpKeywordAttribute(""));


            MetadataStore.AddAttributeTable(builder.CreateTable());
        }
    }
}
