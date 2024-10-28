//************************************************************************************************
// Copyright © 2021 Steven M Cohn. All rights reserved.
//************************************************************************************************

// mask this definition to debug raw markdown processing to ILogger instead of a file/folder
#define WriteToDisk

namespace River.OneMoreAddIn.Commands
{
	using River.OneMoreAddIn.Models;
	using River.OneMoreAddIn.Styles;
	using River.OneMoreAddIn.UI;
	using System;
	using System.Collections.Generic;
	using System.Drawing;
	using System.Drawing.Imaging;
	using System.IO;
	using System.Linq;
	using System.Threading.Tasks;
	using System.Xml.Linq;
	using Resx = Properties.Resources;


	internal class MarkdownWriter : Loggable
	{
		private sealed class Context
		{
			//// the container element
			//public string Container;
			//// true if at start of line
			//public bool StartOfLine;
			// index of quick style from container
			public int QuickStyleIndex;
			// accent enclosure char, asterisk* or backquote`
			public string Accent;
		}

		// Note that if pasting md text directly into OneNote, there's no good way to indent text
		// and prevent OneNote from auto-formatting. Closest alt is to use a string of nbsp's
		// but that conflicts with other directives like headings and list numbering. One way is
		// to substitute indentations (e.g., OEChildren) with the blockquote directive instead.
		private const string Indent = "  "; //">"; //&nbsp;&nbsp;&nbsp;&nbsp;";
		private const string Quote = ">";

		private readonly Page page;
		private readonly XNamespace ns;
		private readonly List<Style> quickStyles;
		private readonly Stack<Context> contexts;
		private readonly List<XElement> tables;
		private int nestedReadtables;
		private int nestedWritetables;
		private bool saveAttachments;
		private int imageCounter;
#if WriteToDisk
		private StreamWriter writer;
		private string attachmentPath;
		private string attachmentFolder;
#else
		private readonly ILogger writer = Logger.Current;
#endif
		// helper class to pass parameter
		public class PrefixClass
		{
			public string indents = "";
			public string tags = "";
			public string bullets = "";
			public enum Listtype { None, Bullet, Numbered };
			public Listtype liststarted = Listtype.None;
			public bool containedstart;

			public PrefixClass(string set_indents = "", string set_tags = "", string set_bullets = "", Listtype liststarted = Listtype.None, bool containedstart = true)
			{
				this.indents = set_indents;
				this.tags = set_tags;
				this.bullets = set_bullets;
				this.liststarted = liststarted;
				this.containedstart = containedstart;
			}

		}

		public MarkdownWriter(Page page, bool saveAttachments)
		{
			this.page = page;
			ns = page.Namespace;

			quickStyles = page.GetQuickStyles();
			contexts = new Stack<Context>();
			tables = new List<XElement>();
			nestedReadtables = 0;
			nestedWritetables = 0;

			this.saveAttachments = saveAttachments;
		}


		/// <summary>
		/// Copy the given content as markdown to the clipboard using the current
		/// page as a template for tag and style references.
		/// </summary>
		/// <param name="content"></param>
		public async Task Copy(XElement content)
		{
			using var stream = new MemoryStream();
			using (writer = new StreamWriter(stream))
			{
				await writer.WriteLineAsync($"# {page.Title}");

				if (content.Name.LocalName == "Page")
				{
					content.Elements(ns + "Outline")
						.Elements(ns + "OEChildren")
						.ForEach(e => Write(e));
				}
				else
				{
					Write(content);
				}

				await writer.WriteLineAsync();
				await writer.FlushAsync();

				stream.Position = 0;
				using var reader = new StreamReader(stream);
				var text = await reader.ReadToEndAsync();

				logger.Debug("markdown - - - - - - - -");
				logger.Debug(text);
				logger.Debug("end markdown - - - - - -");

				var clippy = new ClipboardProvider();
				var success = await clippy.SetText(text, true);
				if (!success)
				{
					MoreMessageBox.ShowError(null, Resx.Clipboard_locked);
				}

				logger.Debug("copied");
			}
		}


		/// <summary>
		/// Save the page as markdown to the specified file.
		/// </summary>
		/// <param name="filename"></param>
		public void Save(string filename)
		{
#if WriteToDisk
			var path = Path.GetDirectoryName(filename);
			attachmentFolder = Path.GetFileNameWithoutExtension(filename);
			attachmentPath = Path.Combine(path, attachmentFolder);

			using (writer = File.CreateText(filename))
#endif
			{
				saveAttachments = true;

				writer.WriteLine($"# {page.Title}");

				page.Root.Elements(ns + "Outline")
					.Elements(ns + "OEChildren")
					.Elements()
					.ForEach(e => Write(e));

				// page level Images outside of any Outline
				page.Root.Elements(ns + "Image")
					.ForEach(e =>
					{
						Write(e);
						writer.WriteLine();
					});

				writer.WriteLine();
			}
		}

		/// <summary>
		/// prepare for recursive processing
		/// </summary>
		/// <param name="container">typically an OEChildren with elements and OEChildren</param>
		private void Write(XElement container)
		{
			var prefix = new PrefixClass(containedstart:true);
			Write(container, ref prefix);
		}


		/// <summary>
		/// Save the page as markdown to a string
		/// </summary>
		/// <param name="container">typically an OEChildren with elements and OEChildren</param>
		/// <param name="prefix">prefix used to indent markdown lines</param>
		/// <param name="contained"></param>
		private void Write(XElement container,
			ref PrefixClass prefix,
			int depth = 0,
			bool contained = false)
		{
			// Lines start at the beginning of each paragraph/OE which contains a flat list of
			// Tag, List, and T, so startOfLine can be handled locally rather than recursively.
			var startOfLine = true;

			logger.Debug($"Write({container.Name.LocalName}, prefix:[{prefix.indents}], depth:{depth}, contained:{contained})");

			foreach (var element in container.Elements())
			{
				var n = element.Name.LocalName;
				var m = $"- [prefix:[{prefix.indents}] depth:{depth} start:{startOfLine} contained:{contained} element {n}";
				logger.Debug(n == "T" ? $"{m} [{element.Value}]" : m);

				switch (element.Name.LocalName)
				{
					case "OEChildren":
						{
							var currentindents = prefix.indents;
							prefix.indents = $"{Indent}{prefix.indents}";
							Write(element, ref prefix, depth + 1, contained);
							prefix.indents = currentindents;

							if (contained && prefix.liststarted == PrefixClass.Listtype.Bullet)
							{
								writer.Write("</ol>");
							}
							break;
						}

					case "OE":
						{
							var context = DetectQuickStyle(element);
							Write(element, ref prefix, depth, contained);

							if (context is not null)
							{
								if (!string.IsNullOrEmpty(context.Accent))
								{
									// close the accent
									writer.Write(context.Accent);
								}

								contexts.Pop();
							}
						}
						break;

					case "Tag":
						prefix.tags += WriteTag(element, contained);
						break;

					case "T":
						{
							if (!contained) // not in table cell
							{
								writer.WriteLine();
							} else
							{
								if (prefix.bullets.Contains("1."))
								{
									if (prefix.liststarted == PrefixClass.Listtype.None)
									{
										prefix.bullets = "<ol><li>";
										prefix.liststarted = PrefixClass.Listtype.Numbered;
									}
									else
									{
										prefix.bullets = "<li>";
									}
								}
								if (!prefix.containedstart)
								{
									writer.Write("<br>");
								}
								else
								{
									prefix.containedstart = false; // avoid <br> for last element in table
								}
							}

							Stylize(prefix, contained);
							WriteText(element.GetCData(), startOfLine, contained);

							if (contained && prefix.bullets.Contains("<li>"))
							{
								writer.Write("</li>");
							}

							prefix.bullets = "";
							prefix.tags = "";
							startOfLine = false;
						}
						break;

					case "List":
						Write(element, ref prefix, depth, contained);
						startOfLine = false;
						break;

					case "Bullet":
						// in md dash needs to be first in line
						prefix.bullets = "- ";
						break;

					case "Number":
						// in md number needs to be first in line
						prefix.bullets = "1. ";
						break;

					case "Image":
						WriteImage(element);
						break;

					case "InkDrawing":
					case "InsertedFile":
					case "MediaFile":
						WriteFile(element);
						break;

					case "Table":
						if (contained)
						{
							tables.Add(element);
							writer.Write(prefix.indents + "[nested-table" + nestedReadtables + "](#nested-table" + nestedReadtables++ + ")");
						}
						else
						{
							WriteTable(element, prefix);
							while (tables.Count() != 0)
							{
								var table = tables.First();
								writer.WriteLine(prefix.indents + "<details id=\"nested-table" + nestedWritetables++ +"\" open>");
								writer.WriteLine(prefix.indents + "<summary>");
								WriteTable(table, prefix);
								writer.WriteLine(prefix.indents + "</summary>");
								tables.RemoveAt(0);
							}
						}
						break;
				}
			}

			logger.Debug("out");
		}


		private Context DetectQuickStyle(XElement element)
		{
			if (element.GetAttributeValue("quickStyleIndex", out int index))
			{
				var context = new Context
				{
					QuickStyleIndex = index
				};

				var quick = quickStyles.First(q => q.Index == index);
				if (quick != null)
				{
					var name = quick.Name.ToLower();

					// cite becomes italic
					if (name.In("cite", "citation")) context.Accent = "*";
					else if (name.Contains("code")) context.Accent = "`";

				}

				contexts.Push(context);
				return context;
			}

			return null;
		}


		private void Stylize(PrefixClass prefix, bool contained = false)
		{
			var styleprefix = "";
			if (contexts.Count > 0)
			{
				var context = contexts.Peek();
				var quick = quickStyles.First(q => q.Index == context.QuickStyleIndex);
				switch (quick.Name)
				{
					case "PageTitle":
					case "h1": styleprefix = ("# "); break;
					case "h2": styleprefix = ("## "); break;
					case "h3": styleprefix = ("### "); break;
					case "h4": styleprefix = ("#### "); break;
					case "h5": styleprefix = ("##### "); break;
					case "h6": styleprefix = ("###### "); break;
					case "blockquote": styleprefix = ("> "); break;
					// cite and code are both block-scope style, on the OE
					case "cite": styleprefix = ("*"); break;
					case "code": styleprefix = ("`"); break;
						//case "p": logger.Write(Environment.NewLine); break;
				}
			}
			if (contained)
			{
				writer.Write(prefix.bullets + prefix.tags);
			}
			else
			{
				writer.Write(prefix.indents + prefix.bullets + styleprefix + prefix.tags);
			}
		}


		private string WriteTag(XElement element, bool contained)
		{
			var symbol = page.Root.Elements(ns + "TagDef")
				.Where(e => e.Attribute("index").Value == element.Attribute("index").Value)
				.Select(e => int.Parse(e.Attribute("symbol").Value))
				.FirstOrDefault();
			var retValue = "";

			switch (symbol)
			{
				case 3:     // to do
				case 8:     // client request
				case 12:    // schedule/callback
				case 28:    // todo prio 1
				case 71:    // todo prio 2
				case 94:    // discuss person a/b
				case 95:    // discuss manager
					var check = element.Attribute("completed").Value == "true" ? "x" : " ";
					retValue = contained
					  ? @"<input type=""checkbox"" disabled " + (check == "x" ? "checked" : "unchecked") + @" />"
					  : ($"[{check}] ");

					break;

				case 6: retValue = (":question: "); break;         // question
				case 13: retValue = (":star: "); break;            // important
				case 17: retValue = (":exclamation: "); break;     // critical
				case 18: retValue = (":phone: "); break;           // phone
				case 21: retValue = (":bulb: "); break;            // idea
				case 23: retValue = (":house: "); break;           // address
				case 33: retValue = (":three: "); break;           // three
				case 39: retValue = (":zero: "); break;            // zero
				case 51: retValue = (":two: "); break;              // two
				case 59: retValue = (":arrow_right: "); break;                // agenda
				case 64: retValue = (":star: "); break;             // custom 1
				case 70: retValue = (":one: "); break;              // one
				case 116: retValue = (":busts_in_silhouette: "); break;            // busts_in_silhouette																	
				case 117: retValue = (":notebook: "); break;            // notebook																	
				case 118: retValue = (":mailbox: "); break;        // contact
				case 121: retValue = (":musical_note: "); break;   // music to listen to
				case 131: retValue = (":secret: "); break;          // password
				case 133: retValue = (":movie_camera: "); break;   // movie to see
				case 132: retValue = (":book: "); break;           // book to read
				case 140: retValue = (":zap: "); break;            // lightning bolt																	
				default: retValue = (":o: "); break;                                       // retValue = (":o: "); break;
			}
			return retValue;
		}


		private void WriteText(XCData cdata, bool startOfLine, bool contained)
		{
			cdata.Value = cdata.Value
				.Replace("<br>", "  ") // usually followed by NL so leave it there
				.Replace("[", "\\[")   // escape to prevent confusion with md links
				.TrimEnd();

			var wrapper = cdata.GetWrapper();
			foreach (var span in wrapper.Descendants("span").ToList())
			{
				var text = span.Value;
				var att = span.Attribute("style");
				// span might only have a lang attribute
				if (att != null)
				{
					var style = new Style(span.Attribute("style").Value);
					if (style.IsStrikethrough) text = $"~~{text}~~";
					if (style.IsItalic) text = $"*{text}*";
					if (style.IsBold) text = $"**{text}**";
				}
				span.ReplaceWith(new XText(text));
			}

			// escape directives
			var raw = wrapper.GetInnerXml()
				.Replace("&lt;", "\\<")
				.Replace("|", "\\|")
				.Replace("à", "&rarr; ")                    // right arrow
				.Replace("\n", contained ? "<br>" : "\n");  // newlines in tables

			// replace links with <> to allow special characters and hence place if after escape directives
			foreach (var anchor in wrapper.Elements("a").ToList())
			{
				var href = anchor.Attribute("href")?.Value;
				if (!string.IsNullOrEmpty(href))
				{
					if (href.StartsWith("onenote:") || href.StartsWith("onemore:"))
					{
						// removes the hyperlink but preserves the text
						anchor.ReplaceWith(anchor.Value);
					}
					else
					{
						anchor.ReplaceWith(new XText($"[{anchor.Value}](<{href}>)"));
					}
				}
			}

			if (startOfLine && raw.Length > 0 && raw.StartsWith("#"))
			{
				writer.Write("\\");
			}

			if (startOfLine && raw.Length > 0)
			{
				raw += "  "; // add extra space to end of line
			}

			logger.Debug($"text [{raw}]");
			writer.Write(raw);
		}


		private void WriteImage(XElement element)
		{
			if (saveAttachments)
			{
				var data = element.Element(ns + "Data");
				var binhex = Convert.FromBase64String(data.Value);

				using var stream = new MemoryStream(binhex, 0, binhex.Length);
				using var image = Image.FromStream(stream);

				var name = $"{attachmentFolder}_{++imageCounter}.png";
				var filename = Path.Combine(attachmentPath, name);
#if WriteToDisk
				if (!Directory.Exists(attachmentPath))
				{
					Directory.CreateDirectory(attachmentPath);
				}

				image.Save(filename, ImageFormat.Png);
#endif
				var imgPath = Path.Combine(attachmentFolder, name).Replace("\\", "/").Replace(" ", "%20");
				writer.Write($"![Image-{imageCounter}]({imgPath})");
			}
			else
			{
				writer.Write($"(*Image:{++imageCounter}.png*)");
			}
		}


		private void WriteFile(XElement element)
		{
			// get and validate source
			var source = element.Attribute("pathSource")?.Value;
			if (string.IsNullOrEmpty(source) || !File.Exists(source))
			{
				source = element.Attribute("pathCache")?.Value;
				if (string.IsNullOrEmpty(source) || !File.Exists(source))
				{
					// broken link, remove marker
					return;
				}
			}

			// get preferredName; this will become the output file name
			var name = element.Attribute("preferredName")?.Value;
			if (string.IsNullOrEmpty(name))
			{
				// broken link, remove marker
				return;
			}

			if (saveAttachments)
			{
				var target = Path.Combine(attachmentPath, name);

				try
				{
#if WriteToDisk
					if (!Directory.Exists(attachmentPath))
					{
						Directory.CreateDirectory(attachmentPath);
					}

					// copy cached/source file to md output directory
					File.Copy(source, target, true);
#endif
				}
				catch
				{
					// error copying, drop marker
					return;
				}

				// this is a relative path that allows us to move the folder around
				var uri = new Uri(target).AbsoluteUri;
				writer.WriteLine($"[{name}]({uri})");
			}
			else
			{
				writer.WriteLine($"(*File:{name}*)");
			}
		}


		private void WriteTable(XElement element, PrefixClass prefix)
		{
			#region WriteRow(TableRow row)
			void WriteRow(TableRow row)
			{
				writer.Write(prefix.indents + "| ");
				foreach (var cell in row.Cells)
				{
					PrefixClass nestedprefix = new PrefixClass();
					Write(cell.Root, ref nestedprefix, contained: true);
					writer.Write(" | ");
				}
				writer.WriteLine();
			}
			#endregion WriteRow

			var table = new Table(element);

			// table needs a blank line before it, even 2nd one sometimes needed
			writer.WriteLine();
			writer.WriteLine();

			var rows = table.Rows;

			// header - - - - - - - - - - - - - - - - - - -

			if ((table.HasHeaderRow && rows.Any()) || rows.Count() == 1)
			{
				// use first row data as header
				WriteRow(rows.First());
				// skip the header row, leaving data rows
				rows = rows.Skip(1);
			}
			else
			{
				// write generic column headers: A, B, C, ...
				writer.Write("| ");
				for (var i = 0; i < table.ColumnCount; i++)
				{
					writer.Write($" {TableCell.IndexToLetters(i + 1)} |");
				}
				writer.WriteLine();
			}

			// separator - - - - - - - - - - - - - - - - -

			writer.Write(prefix.indents + "| ");
			for (int i = 0; i < table.ColumnCount; i++)
			{
				writer.Write(" :--- |");
			}
			writer.WriteLine();

			// data - - - - - - - - - - - - - - - - - - - -

			foreach (var row in rows)
			{
				WriteRow(row);
			}
		}
	}
}
