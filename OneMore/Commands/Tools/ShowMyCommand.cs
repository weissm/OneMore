//************************************************************************************************
// Copyright © 2016 Steven M Cohn.  All rights reserved.
//************************************************************************************************

namespace River.OneMoreAddIn.Commands
{
	using System.Threading.Tasks;
	using OneNote2X.Forms;

	internal class ShowMyCommand : Command
	{
		public ShowMyCommand ()
		{
		}


		public override async Task Execute(params object[] args)
		{
			using (var dialog = new GenMoMs())
			{
				dialog.ShowDialog(owner);
			}

			await Task.Yield();
		}
	}
}
