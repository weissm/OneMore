﻿//************************************************************************************************
// Copyright © 2021 Steven M Cohn.  All rights reserved.
//************************************************************************************************

namespace River.OneMoreAddIn.Commands
{
	using River.OneMoreAddIn.Models;
	using System.Linq;
	using System.Threading.Tasks;
	using System.Windows.Forms;
	using System.Xml.Linq;
	using Resx = Properties.Resources;


	/// <summary>
	/// Deletes the reminder associated with the current paragraph and optionally removes the tag.
	/// </summary>
	internal class DeleteReminderCommand : Command
	{

		public DeleteReminderCommand()
		{
			// prevent replay
			IsCancelled = true;
		}


		public override async Task Execute(params object[] args)
		{
			using var one = new OneNote(out var page, out var ns);
			var paragraph = page.Root.Descendants(ns + "T")
				.Where(e => e.Attribute("selected")?.Value == "all")
				.Select(e => e.Parent)
				.FirstOrDefault();

			if (paragraph == null)
			{
				UIHelper.ShowInfo(one.Window, Resx.RemindCommand_noContext);
				return;
			}

			var serializer = new ReminderSerializer();
			var reminders = serializer.LoadReminders(page);
			if (!reminders.Any())
			{
				UIHelper.ShowError(one.Window, Resx.RemindCommand_noReminder);
				return;
			}

			var objectID = paragraph.Attribute("objectID").Value;
			var reminder = reminders.Find(r => r.ObjectId == objectID);
			if (reminder == null)
			{
				// second-chance for multi-client users
				var uri = one.GetHyperlink(page.PageId, objectID);
				reminder = reminders.Find(r => r.ObjectUri == uri);
			}

			if (reminder == null)
			{
				UIHelper.ShowError(one.Window, Resx.RemindCommand_noReminder);
				return;
			}

			XElement tag = null;
			if (!string.IsNullOrEmpty(reminder.Symbol) && reminder.Symbol != "0")
			{
				reminder.TagIndex = page.GetTagDefIndex(reminder.Symbol);
				if (reminder.TagIndex != null)
				{
					// confirm tag still exists
					tag = paragraph.Elements(ns + "Tag")
						.FirstOrDefault(e => e.Attribute("index").Value == reminder.TagIndex);
				}
			}

			if (tag == null)
			{
				reminders.Remove(reminder);
				page.SetMeta(MetaNames.Reminder, serializer.EncodeContent(reminders));
				await one.Update(page);

				UIHelper.ShowError(one.Window, Resx.RemindCommand_noReminder);
				return;
			}

			var result = UI.MoreMessageBox.ShowQuestion(owner,
				Resx.DeleteReminderCommand_deleteTag, cancel: true);

			if (result == DialogResult.Cancel)
			{
				return;
			}

			if (result == DialogResult.Yes)
			{
				tag.Remove();
			}

			reminders.Remove(reminder);
			page.SetMeta(MetaNames.Reminder, serializer.EncodeContent(reminders));

			await one.Update(page);
		}
	}
}
