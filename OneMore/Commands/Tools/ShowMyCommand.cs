//************************************************************************************************
// Copyright © 2016 Steven M Cohn.  All rights reserved.
//************************************************************************************************

namespace River.OneMoreAddIn.Commands
{
using System.Threading.Tasks;
using OneNote2X.Forms;
    using System.Xml.Linq;
    using OneNote = Microsoft.Office.Interop.OneNote;
    using System.IO;
    using System.Reflection;
    using System.Linq;

    internal class ShowMyCommand : Command
	{
		public ShowMyCommand ()
		{
        }


        public override async Task Execute(params object[] args)
		{
            System.Diagnostics.Debugger.Launch();
            // This is a work around for a blocking behaviour in the genmoms
            var _onenoteApp = new OneNote.Application();
            string mysectionId;
            string executableLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string inputFile = Path.Combine(executableLocation, @"Resources\MoM_Template.one");
            _onenoteApp.OpenHierarchy(inputFile, null, out mysectionId, OneNote.CreateFileType.cftNone);
            _onenoteApp.SyncHierarchy(mysectionId);
            await Task.Yield();
            using (var dialog = new GenMoMs())
			{
				dialog.ShowDialog(owner);
			}

			await Task.Yield();
		}
	}
}
