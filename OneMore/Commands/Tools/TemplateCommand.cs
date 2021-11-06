//************************************************************************************************
// Copyright © 2016 Steven M Cohn.  All rights reserved.
//************************************************************************************************

namespace River.OneMoreAddIn.Commands
{
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using System.Xml.Linq;
    using OneNote = Microsoft.Office.Interop.OneNote;

    internal class TemplateCommand : Command
	{
		public TemplateCommand()
		{
			// prevent replay
			IsCancelled = true;
		}


		public override async Task Execute(params object[] args)
		{
            /*
			using (var dialog = new AboutDialog(factory))
			{
				dialog.ShowDialog(owner);
			}
			*/
            System.Diagnostics.Debugger.Launch();

            var _onenoteApp = new OneNote.Application();
            string mysectionId;
            string executableLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string inputFile = Path.Combine(executableLocation, @"Resources\MoM_Template.one");
            _onenoteApp.OpenHierarchy(inputFile, null, out mysectionId, OneNote.CreateFileType.cftNone);
            _onenoteApp.SyncHierarchy(mysectionId);
            await Task.Yield();
        }
    }
}
