﻿//************************************************************************************************
// Copyright © 2024 Steven M Cohn. All rights reserved.
//************************************************************************************************

namespace River.OneMoreAddIn.Commands
{
	using River.OneMoreAddIn.Models;
	using System.IO;
	using System.Linq;
	using System.Text.RegularExpressions;
	using System.Threading.Tasks;
	using System.Xml.Linq;


	/// <summary>
	/// Convert the selected markdown text to OneNote native content.
	/// </summary>
	internal class ConvertMarkdownCommand : Command
	{
		public ConvertMarkdownCommand()
		{
		}


		public override async Task Execute(params object[] args)
		{
			using var one = new OneNote(out var page, out var ns);
			page.GetTextCursor();

			if (page.SelectionScope != SelectionScope.Region)
			{
				ShowError("Select markdown text to convert to OneNote format");
				return;
			}
			var editor = new PageEditor(page)
			{
				AllContent = (page.SelectionScope != SelectionScope.Region)
			};

			IEnumerable<XElement> outlines;
			if (page.SelectionScope != SelectionScope.Region)
			{
				// process all outlines
				outlines = page.Root.Elements(ns + "Outline");
			}
			else
			{
				// process only the selected outline
				outlines = new List<XElement>
				{
					page.Root.Elements(ns + "Outline")
						.Descendants(ns + "T")
						.First(e => e.Attributes().Any(a => a.Name == "selected" && a.Value == "all"))
						.FirstAncestor(ns + "Outline")
				};
			}

			// process each outline in sequence. By scoping to an outline, PageReader/Editor
			// can maintain positional context and scope updates to the outline

			foreach (var outline in outlines.ToList())
			{
				var content = await editor.ExtractSelectedContent(outline);
				var paragraphs = content.Elements(ns + "OE").ToList();

				var reader = new PageReader(page)
				{
					// configure to read for markdown
					IndentationPrefix = "\n",
					Indentation = ">",
					ColumnDivider = "|",
					ParagraphDivider = "<br>",
					TableSides = "|"
				};

				var filepath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

				var text = reader.ReadTextFrom(paragraphs, allContent);
				text = Regex.Replace(text, @"<br>([\n\r]+)", "$1");

				var body = OneMoreDig.ConvertMarkdownToHtml(filepath, text);

			editor.InsertAtAnchor(new XElement(ns + "HTMLBlock",
				new XElement(ns + "Data",
					new XCData($"<html><body>{body}</body></html>")
					)
				));

			MarkdownConverter.RewriteHeadings(page);

			await one.Update(page);

			// Pass 2, cleanup...

			// find and convert headers based on styles
			page = await one.GetPage(page.PageId, OneNote.PageDetail.Basic);

			// re-reference paragraphs by ID from newly loaded Page instance
			var touched = page.Root.Descendants(ns + "OE")
				.Where(e => !paragraphIDs.Contains(e.Attribute("objectID").Value))
				.ToList();

			if (touched.Any())
			{
				var converter = new MarkdownConverter(page);
				converter.RewriteHeadings(touched);
				converter.SpaceOutParagraphs(touched, 12);

				await one.Update(page);
			}
		}
	}
}
