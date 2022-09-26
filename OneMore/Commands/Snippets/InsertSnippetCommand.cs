﻿//************************************************************************************************
// Copyright © 2021 Steven M Cohn.  All rights reserved.
//************************************************************************************************

namespace River.OneMoreAddIn.Commands
{
	using System.Threading.Tasks;
	using System.Xml.Linq;
	using Resx = River.OneMoreAddIn.Properties.Resources;


	internal class InsertSnippetCommand : Command
	{

		public InsertSnippetCommand()
		{
		}


		public override async Task Execute(params object[] args)
		{
			var path = args[0] as string;
			string snippet = null;

			var provider = new SnippetsProvider();

			using (var one = new OneNote(out var page, out _))
			{
				if (!page.ConfirmBodyContext())
				{
					UIHelper.ShowError(Resx.Error_BodyContext);
					return;
				}

				if (string.IsNullOrWhiteSpace(path))
				{
					// assume Expand command and infer name from current word...

					path = page.GetSelectedText();
					if (!string.IsNullOrWhiteSpace(path))
					{
						snippet = await provider.LoadByName(path);
						if (!string.IsNullOrEmpty(snippet))
						{
							// remove placeholder
							var updated = page.EditSelected((s) =>
							{
								if (s is XText text)
								{
									text.Value = string.Empty;
									return text;
								}

								var element = (XElement)s;
								element.Value = string.Empty;
								return element;
							});

							if (updated)
							{
								await one.Update(page);
							}
						}
					}
				}
				else
				{
					snippet = await provider.Load(path);
				}
			}

			if (string.IsNullOrWhiteSpace(snippet))
			{
				UIHelper.ShowMessage(string.Format(Resx.InsertSnippets_CouldNotLoad, path));
				return;
			}

			var clippy = new ClipboardProvider();
			await clippy.StashState();
			await clippy.SetHtml(snippet);
			await clippy.Paste(true);
			await clippy.RestoreState();
		}
	}
}
