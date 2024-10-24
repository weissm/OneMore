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
			public string Enclosure;
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
			public string indent = "";
			public string tags = "";
			public string bullets = "";

			public PrefixClass(string set_indent = "", string set_tags = "", string set_bullets = "")
			{
				indent = set_indent;
				tags = set_tags;
				bullets = set_bullets;
			}

		}

		public MarkdownWriter(Page page, bool saveAttachments)
		{
			this.page = page;
			ns = page.Namespace;

			quickStyles = page.GetQuickStyles();
			contexts = new Stack<Context>();

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
                        .Elements()
                        .ForEach(e => { PrefixClass prefix = new PrefixClass(); Write(e, ref prefix); });
				}
				else
				{
                    content.Elements()
                        .ForEach(e => { PrefixClass prefix = new PrefixClass(); Write(e, ref prefix); });
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
                    .ForEach(e => { PrefixClass prefix = new PrefixClass(); Write(e, ref prefix); });

				// page level Images outside of any Outline
                page.Root.Elements(ns + "Image")
                    .ForEach(e => {
                        PrefixClass prefix = new PrefixClass(); Write(e, ref prefix);
                        writer.WriteLine();
                    });

                writer.WriteLine();
            }
        }


		/// <summary>
		/// Save the page as markdown to a string
		/// </summary>
		private void Write(XElement container,
			ref PrefixClass prefix,
			bool startpara = false,
			bool contained = false)
		{
			bool pushed = false;
			bool dive = true;
			var keepindents = prefix.indent;

			// Lines start at the beginning of each paragraph/OE which contains a flat list of
			// Tag, List, and T, so startOfLine can be handled locally rather than recursively.
			var startOfLine = true;

			logger.Debug($"Write({container.Name.LocalName}, prefix:[{prefix}], depth:{dive}, contained:{contained})");

			foreach (var element in container.Elements())
			{
				var n = element.Name.LocalName;
				var m = $"- [prefix:[{prefix}] depth:{dive} start:{startOfLine} contained:{contained} element {n}";
				logger.Debug(n == "T" ? $"{m} [{element.Value}]" : m);


				switch (element.Name.LocalName)
				{
					case "OEChildren":
						pushed = DetectQuickStyle(element);
						if (contained)
						{
							writer.Write("<br>");
						}
						else
						{
							writer.WriteLine("");
						}
						prefix.indent = $"{Indent}{prefix.indent}";
						break;

					case "OE":
						pushed = DetectQuickStyle(element);
						startpara = true;
						break;

					case "Tag":
						prefix.tags += WriteTag(element, contained);
						break;

					case "T":
						if (element.GetCData().Value.Trim().IsNullOrEmpty())
						{
							break;
						}
						if (element.GetCData().Value.Trim().IsNullOrEmpty())
						{
							break;
						}
						pushed = DetectQuickStyle(element);
						Stylize(prefix);
						prefix.tags = "";
						prefix.bullets = "";
						WriteText(element.GetCData(), startpara, contained);
						break;

					case "Bullet":
						// in md dash needs to be first in line
						prefix.bullets = "- " + prefix.bullets;
						break;

					case "Number":
						// in md number needs to be first in line
						prefix.bullets = "1. " + prefix.bullets;

						break;

					case "Image":
						if (dive)
						{
							writer.Write(new String(Quote[0], 1));
						}
						WriteImage(element);
						dive = false;

						break;

					case "InkDrawing":
					case "InsertedFile":
					case "MediaFile":
						WriteFile(element);
						dive = false;
						break;

					case "Table":
						WriteTable(element, prefix.indent);
						dive = false;
						break;
				}

				if (dive && element.HasElements)
				{
					foreach (var child in element.Elements())
					{
						Write(child, ref prefix, startpara, contained);
						startpara = false;
					}
				}

				var context = pushed ? contexts.Pop() : null;
				if (element.Name.LocalName == "OE")
				{
					if (context != null && !string.IsNullOrEmpty(context.Enclosure))
					{
						writer.Write(context.Enclosure);
					}

					// if not in a table cell
					// or in a cell and this OE is followed by another OE
					if (!contained && (element.NextNode != null))
					{
						writer.WriteLine("");
					}
					else if (contained)
					{
						writer.Write("<br>");
					}
					prefix.indent = keepindents;
				}

				logger.Debug("out");
			}
		}


		private bool DetectQuickStyle(XElement element)
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
				return true;
			}

			return false;
		}


		private void Stylize(PrefixClass prefix)
		{
			var styleprefix = "";
			if (contexts.Count == 0) return;
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
			writer.Write(prefix.indent + prefix.bullets + styleprefix + prefix.tags);
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
				case 131: retValue = (":secret: "); break;			// password
				case 133: retValue = (":movie_camera: "); break;   // movie to see
				case 132: retValue = (":book: "); break;           // book to read
				case 140: retValue = (":zap: "); break;            // lightning bolt																	
				default: retValue = (":o: "); break;									   // retValue = (":o: "); break;
			}
			return retValue;
			return retValue;
		}


		private void WriteText(XCData cdata, bool startOfLine, bool contained)
		{
			cdata.Value = cdata.Value
				.Replace("<br>", "   ") // usually followed by NL so leave it there
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
					if (style.IsItalic) text = $"_{text.TrimEnd()}_{"".PadRight(text.Length - text.TrimEnd().Length, ' ')}";
					if (style.IsBold) text = $"**{text}**";
				}
				span.ReplaceWith(new XText(text));
			}

            foreach (var anchor in wrapper.Elements("a"))
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
                        anchor.ReplaceWith(new XText($"[{anchor.Value}]({href})"));
                    }
				}
			}

			// escape directives
			var raw = wrapper.GetInnerXml()
				.Replace("&lt;", "\\<")
				.Replace("|", "\\|");

			if (raw.Trim().IsNullOrEmpty())
			{
				return;
			}
			if (contained)
            {
				raw = raw.Replace("\n", "<br>");
            }
			if (raw.Trim().IsNullOrEmpty())
			{
				return;
			}
			if (contained)
            {
				raw = raw.Replace("\n", "<br>");
            }
			if (startOfLine && raw.Length > 0 && raw.StartsWith("#"))
			{
				writer.Write("\\");
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
				var imgPath = Path.Combine(attachmentFolder, name);
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


		private void WriteTable(XElement element, string indents)
		{
			var table = new Table(element);

			// table needs a blank line before it
			writer.WriteLine();
			bool first_row = true;

			// data
			foreach (var row in table.Rows)
			{
				writer.Write(indents + "| ");
				foreach (var cell in row.Cells)
				{
					cell.Root
						.Element(ns + "OEChildren")
						.Elements(ns + "OE")
						.ForEach(e => { PrefixClass prefix = new PrefixClass(set_indent:indents);  Write(e, ref prefix, contained: true); });
						
					writer.Write(" | ");
				}
				writer.WriteLine();
				if (first_row)
				{
					first_row = false;
                    // separator
                    writer.Write(indents + "|");
            for (int i = 0; i < table.ColumnCount; i++)
            {
                writer.Write(" :--- |");
            }
            writer.WriteLine();
                }

            }
        }
	}
}
