﻿//************************************************************************************************
// Copyright © 2021 Steven M Cohn.  All rights reserved.
//************************************************************************************************

namespace River.OneMoreAddIn.Commands
{
	using Microsoft.Win32;
	using River.OneMoreAddIn.Models;
	using River.OneMoreAddIn.UI;
	using System;
	using System.Drawing;
	using System.IO;
	using System.Linq;
	using System.Text.RegularExpressions;
	using System.Threading;
	using System.Threading.Tasks;
	using System.Windows.Forms;
	using System.Xml.Linq;
	using Hap = HtmlAgilityPack;
	using System.Text;
	using System.Web.Script.Serialization;
	using Newtonsoft.Json.Linq;
	using static River.OneMoreAddIn.Models.Page;
	using Resx = Properties.Resources;
    using Windows.Storage;
    using Windows.Storage.Streams;


    /// <summary>
    /// Imports the content of a Web page given its URL. The content can be added as a new page 
    /// in the current section, as a new child page of the current page, or appended to the 
    /// content of the current page. Can run in one of two modes. By default, the page is imported
    /// as HTML and "optimized" by OneNote, meaning that styles are generally not preserved due
    /// to the inherent limitations of OneNote.
    /// 
    /// The second mode is to import the Web page as a series of static images.This will preserve
    /// most styling and layout of the page.It does this by internally printing the page to a PDF
    /// and then importing each page of the PDF as an image. This can be a time consuming process,
    /// taking up to 30 seconds, so give it time. The first time this mode is used, OneMore
    /// downloads a local copy of the chromium browser so this will take some extra time.
    /// Subsequent uses should be faster however.
    /// </summary>
    public class ImportWebCommand : Command
	{
		private const string ClientKey = @"SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients";
		private const string RuntimeId = "{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}";

		private sealed class WebPageInfo
		{
			public string Content;
			public string Title;
		}

		private string address = null;
        private string markdown = null;
        private string title = null;

		private bool importImages = false;
		private ImportWebTarget target;
		private ProgressDialog progress;


		public ImportWebCommand()
		{
		}

        public async static void ImportAsMarkdown(string address,string markdown, string title)
        {
            ImportWebCommand ImportWeb = new ImportWebCommand();
            ImportWeb.address = address;
            ImportWeb.target = ImportWebTarget.Append;
            ImportWeb.importImages = true;
            ImportWeb.title = title;
            ImportWeb.markdown = markdown;
            ProgressDialog progress = new ProgressDialog();
			CancellationToken token = new CancellationToken();

            await ImportWeb.ImportMarkdown( progress,  token);
        }

		public override async Task Execute(params object[] args)
		{
			if (!HttpClientFactory.IsNetworkAvailable())
			{
				ShowInfo(Resx.NetwordConnectionUnavailable);
				return;
			}

			var key = Registry.LocalMachine.OpenSubKey($"{ClientKey}\\{RuntimeId}");
			if (key == null)
			{
				ShowError("Unable to use this command; Edge WebView2 is not installed");
				return;
			}

            System.Diagnostics.Debugger.Launch();
            using (var dialog = new ImportWebDialog())
			{
				if (dialog.ShowDialog(owner) != DialogResult.OK)
				{
					return;
				}

				address = dialog.Address;
				target = dialog.Target;
				importImages = dialog.ImportImages;
			}


            var name = "ReadGitLab";
            var path = "C:\\Users\\MatthasWeiss\\AppData\\Roaming\\OneMore\\Plugins\\" + name + ".js";
            if ((address.Contains("gitlab")  || address.Contains("-/issues")) && File.Exists(path))
            {
                var target = Path.Combine(Path.GetTempPath(), $"{name}");

                // add html link to argument list
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
                using (var reader = new StreamReader(stream, System.Text.Encoding.UTF8))
                {
                    var json = await reader.ReadToEndAsync();

                    var serializer = new JavaScriptSerializer();
                    var plugin = serializer.Deserialize<Plugin>(json);

                    plugin.Name = target;
                    plugin.Arguments += $" -i \"{address}\"";

                    var provider = new PluginsProvider();
                    await provider.Save(plugin);
                }

                await factory.Run<RunPluginCommand>(target + ".js");

                ImportAsMarkdown(address,markdown, title);
            }
            else
            if (importImages)
			{
				ImportAsImages();
			}

			await Task.Yield();
		}


		// = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = =

		#region ImportAsImages

		private void ImportAsImages()
		{
			progress = new ProgressDialog(ImportImages);
			progress.RunModeless();
		}


		// https://github.com/LanderVe/WPF_PDFDocument/blob/master/WPF_PDFDocument/WPF_PDFDocument.csproj
		// https://blogs.u2u.be/lander/post/2018/01/23/Creating-a-PDF-Viewer-in-WPF-using-Windows-10-APIs
		// https://docs.microsoft.com/en-us/uwp/api/windows.data.pdf.pdfdocument.getpage?view=winrt-20348

		private async Task ImportImages(ProgressDialog progress, CancellationToken token)
		{
			logger.Start();
			logger.StartClock();

			progress.SetMaximum(4);
			progress.SetMessage($"Importing {address}...");

			var pdfFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

			// WebView2 needs to run in an STA thread
			await SingleThreaded.Invoke(() =>
			{
				// WebView2 needs a message pump so host in its own invisible worker dialog
				using var form = new WebViewDialog(
					new WebViewWorker(async (webview) =>
					{
						webview.Source = new Uri(address);
						progress.Increment();
						await Task.Yield();
						return true;
					}),
					new WebViewWorker(async (webview) =>
					{
						progress.Increment();
						await Task.Delay(2000);
						await webview.CoreWebView2.PrintToPdfAsync(pdfFile);
						progress.Increment();
						return true;
					}));

				form.ShowDialog(progress);
			});

			if (token.IsCancellationRequested)
			{
				return;
			}

			if (!File.Exists(pdfFile))
			{
				logger.WriteLine($"PDF file not found, {pdfFile}");
				return;
			}

			// convert PDF pages to images...
			logger.WriteLine("rendering images");

			try
			{
				Page page = null;
				await using (var one = new OneNote())
				{
					page = target == ImportWebTarget.Append
						? await one.GetPage()
						: await CreatePage(one,
							target == ImportWebTarget.ChildPage ? await one.GetPage() : null, address);
				}

				var ns = page.Namespace;
				var container = page.EnsureContentContainer();

				var file = await StorageFile.GetFileFromPathAsync(pdfFile);
                var doc = await Windows.Data.Pdf.PdfDocument.LoadFromFileAsync(file);

				await file.DeleteAsync();

				progress.SetMaximum((int)doc.PageCount);

				for (int i = 0; i < doc.PageCount; i++)
				{
					progress.SetMessage($"Rasterizing image {i} of {doc.PageCount}");
					progress.Increment();

					//logger.WriteLine($"rasterizing page {i}");
					var pdfpage = doc.GetPage((uint)i);

					using var stream = new InMemoryRandomAccessStream();
					await pdfpage.RenderToStreamAsync(stream);

					using var image = new Bitmap(stream.AsStream());

					var data = Convert.ToBase64String(
						(byte[])new ImageConverter().ConvertTo(image, typeof(byte[]))
						);

					container.Add(new XElement(ns + "OE",
						new XElement(ns + "Image",
							new XAttribute("format", "png"),
							new XElement(ns + "Size",
								new XAttribute("width", $"{image.Width}.0"),
								new XAttribute("height", $"{image.Height}.0")),
							new XElement(ns + "Data", data)
						)),
						new Paragraph(ns, " ")
					);
				}

				progress.SetMessage($"Updating page");

				await using (var one = new OneNote())
				{
					await one.Update(page);
				}
			}
			catch (Exception exc)
			{
				logger.WriteLine(exc.Message, exc);
			}

			logger.WriteTime("import complete");
			logger.End();
		}

		private async Task<Page> CreatePage(OneNote one, Page parent, string title)
		{
			var section = await one.GetSection();
			var sectionId = section.Attribute("ID").Value;

			one.CreatePage(sectionId, out var pageId);
			var page = await one.GetPage(pageId);

			if (parent != null)
			{
				// get current section again after new page is created
				section = await one.GetSection();

				var parentElement = section.Elements(parent.Namespace + "Page")
					.First(e => e.Attribute("ID").Value == parent.PageId);

				var childElement = section.Elements(parent.Namespace + "Page")
					.First(e => e.Attribute("ID").Value == pageId);

				if (childElement != parentElement.NextNode)
				{
					// move new page immediately after its original in the section
					childElement.Remove();
					parentElement.AddAfterSelf(childElement);
				}

				parentElement.GetAttributeValue("pageLevel", out var level, 1);
				var pageLevel = (level + 1).ToString();

				// must set level on the hierarchy entry and on the page itself
				childElement.SetAttributeValue("pageLevel", pageLevel);
				page.Root.SetAttributeValue("pageLevel", pageLevel);

				one.UpdateHierarchy(section);
			}

			await one.NavigateTo(pageId);

			page.Title = title;
			return page;
		}

		#endregion ImportAsImages


		// new function to implement markdown import
		public async Task<bool> ImportMarkdown(ProgressDialog progress, CancellationToken token)
		{
			//,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,
			await using (var one = new River.OneMoreAddIn.OneNote())
			{
				var page = await one.GetPage();
				var ns = page.Namespace;

				if (markdown.IsNullOrEmpty())
				{
                    // read all selected items into markdown
                    page.Root
                        .Elements(ns + "Outline")
                        .Elements(ns + "OEChildren")
                        .Descendants(ns + "T")
                        .Where(e =>
                        {
                            var node = e;
                            if (node != null)
                            {
                                var attr = node.Attribute("selected");
                                return (attr != null && attr?.Value == "all");
                            }
                            else
                            {
                                return false;
                            }
                        })
                        .ForEach(e => { markdown += e.Value + "\n"; });

                    // remove all selected items
                    var elements = page.Root
                        .Elements(ns + "Outline")
                        .Elements(ns + "OEChildren")
                        .Descendants(ns + "OE")
                        .Where(e =>
                        {
                            var node = e.Descendants(ns + "T").FirstOrDefault();
                            if (node != null)
                            {
                                var attr = node.Attribute("selected");
                                return (attr != null && attr?.Value == "all");
                            }
                            else
                            {
                                return false;
                            }
                        });
                    elements.Remove();
                }
				var paragraphIDs = page.Root
					.Elements(ns + "Outline")
					.Elements(ns + "OEChildren")
					.Descendants(ns + "OE")
					.Select(e => e.Attribute("objectID").Value).ToList();


				var container = page.EnsureContentContainer();
				//^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
				if (target == ImportWebTarget.NewPage)
				{
					if (token.IsCancellationRequested)
					{
						progress.DialogResult = DialogResult.Cancel;
						progress.Close();
						return false;
					}

					await one.Update(page);
					page = await CreatePage(one,
						target == ImportWebTarget.ChildPage ? await one.GetPage() : null,
						title: title == null? "New Page" : title
						);
				} else if (page.Title.IsNullOrEmpty())
				{
					page.SetTitle(title);
                }

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

                var text = markdown;
				var body = OneMoreDig.ConvertMarkdownToHtml(filepath, text);
                body = Regex.Replace(body, @"<br>([\n\r]+)", "$1");
                body = Regex.Replace(body, @"\<*input\s+type*=*\""checkbox\""\s+unchecked\s+[a-zA-Z *]*\/\>", "[ ]");
                body = Regex.Replace(body, @"\<*input\s+type*=*\""checkbox\""\s+checked\s+[a-zA-Z *]*\/\>", "[x]");


                //,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,
                // convert to markdown
                var html1 = Markdig.Markdown.ToHtml(markdown);
				// parse MD into html as interim format

				var builder = new StringBuilder();
				builder.AppendLine("<html>");
				builder.AppendLine("<body>");
				builder.AppendLine(html1);
				builder.AppendLine("</body>");
				builder.AppendLine("</html>");
				var html = builder.ToString();


				var baseUri = new Uri(address);
				var targetProject = baseUri.AbsolutePath.Split(new string[] { @"/-/issues" }, StringSplitOptions.None)[0].Substring(1);
				var targetID = int.Parse(baseUri.Segments[baseUri.Segments.Length - 1]);
				var targetProjectURL = baseUri.AbsoluteUri.Split(new string[] { baseUri.LocalPath }, StringSplitOptions.None)[0];
                var replaceString = (@"<img src=""/uploads");
                // http://localhost:84/-/project/1/uploads/3b5f3ad0403aac83d7b75c31cf5d6382/20220310-15.4assessment_3.png
                targetProject = baseUri.AbsoluteUri.Replace(baseUri.AbsolutePath, "") + "/-/project/1";

                var newString = string.Format(@"<img src=""{0}/uploads", targetProject);
				html = html.Replace(replaceString, newString);
                using (StreamWriter writer = new StreamWriter(@"c:\tmp\writegitlab\outtmp1.html"))
                {
                    writer.Write(html);
                }
                var doc = ReplaceImagesWithAnchors(html, new Uri(targetProjectURL), out var hasImages);
				//^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
				if ((progress != null) && doc == null)
				{
					Giveup(Resx.ImportWebCommand_BadUrl);
					progress.DialogResult = DialogResult.Abort;
					progress.Close();
					return false;
				}

				if ((progress != null) && token.IsCancellationRequested)
				{
					progress.DialogResult = DialogResult.Cancel;
					progress.Close();
					return false;
				}
				//,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,
				var hasAnchors = EncodeLocalAnchors(doc, baseUri);
				//^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
				//,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,
				if ((progress != null) && token.IsCancellationRequested)
				{
					progress.DialogResult = DialogResult.Cancel;
					progress.Close();
					return false;
				}
				//^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
				var outerHtmlorg = doc.DocumentNode.OuterHtml;
                //Write to a file
                using (StreamWriter writer = new StreamWriter(@"c:\tmp\writegitlab\outtmp.html"))
                {
                    writer.WriteLine(outerHtmlorg);
                }
                page.AddHtmlContent(outerHtmlorg);

				// update will remove unmodified omHash outlines from the in-memory Page
				await one.Update(page);
                using (StreamWriter writer = new StreamWriter(@"c:\tmp\writegitlab\outtmp2.xml"))
                {
                    var xml = page.Root.ToString();
                    writer.Write(xml);
                }
                hasAnchors = false;
                if (hasImages || hasAnchors)
                {
                    await PatchPage(page, one, hasImages, hasAnchors);
                }

                // Pass 2, cleanup...

                // find and convert headers based on styles
                page = await one.GetPage(page.PageId, OneNote.PageDetail.Basic);
                using (StreamWriter writer = new StreamWriter(@"c:\tmp\writegitlab\outtmp3.xml"))
                {
                    var xml = page.Root.ToString();
                    writer.Write(xml);
                }

                var converter = new MarkdownConverter(page);
                converter.RewriteHeadings();
                converter.RewriteTodo();
                using (StreamWriter writer = new StreamWriter(@"c:\tmp\writegitlab\outtmp4.xml"))
                {
                    var xml = page.Root.ToString();
                    writer.Write(xml);
                }

                logger.WriteLine($"updating...");

                await one.Update(page);

                logger.WriteLine("pass 1 updated page with injected HTML");

			}
//			await ImportMarkdownPostprocessing(escapeID);
			logger.WriteTime("import markdown completed");
			return true;
		}


		private void Giveup(string msg)
		{
			ShowInfo($"Cannot load web page.\n\n{msg}");
		}


		// - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

		/// <summary>
		/// Specialized entry point for CrawlWebPageCommand. Similar to ImportHtml but optimized
		/// to create subpages in quiet mode.
		/// </summary>
		/// <param name="one"></param>
		/// <param name="parent"></param>
		/// <param name="uri"></param>
		/// <returns></returns>
		public async Task<Page> ImportSubpage(
			OneNote one, Page parent, Uri uri, string linkText, CancellationToken token)
		{
			logger.WriteLine($"importing subpage {uri.AbsoluteUri}");

			WebPageInfo info;
			try
			{
				info = await DownloadWebContent(uri);
			}
			catch (Exception exc)
			{
				logger.WriteLine("error downloading web content", exc);
				return null;
			}

			if (string.IsNullOrEmpty(info.Content) || token.IsCancellationRequested)
			{
				logger.WriteLine("web page returned empty content");
				return null;
			}

			var doc = ReplaceImagesWithAnchors(info.Content, uri, out var hasImages);
			if (doc == null || token.IsCancellationRequested)
			{
				return null;
			}

			var hasAnchors = EncodeLocalAnchors(doc, uri);
			if (token.IsCancellationRequested)
			{
				return null;
			}

			string title;
			if (linkText != null)
			{
				title = linkText;
			}
			else
			{
				if (string.IsNullOrEmpty(info.Title))
				{
					info.Title = doc.DocumentNode.SelectSingleNode("//title")?.InnerText;
				}

				title = string.IsNullOrEmpty(info.Title)
					? $"<a href=\"{uri.AbsoluteUri}\">{uri.AbsoluteUri}</a>"
					: $"<a href=\"{uri.AbsoluteUri}\">{info.Title}</a>";
			}

			if (token.IsCancellationRequested)
			{
				return null;
			}

			var page = await CreatePage(one, parent, title);

			// add html to page and let OneNote rehydrate as it sees fit
			page.AddHtmlContent(doc.DocumentNode.OuterHtml);

			await one.Update(page);
			logger.WriteLine("pass 1 updated subpage with injected HTML");

			if (hasImages || hasAnchors)
			{
				await PatchPage(page, one, hasImages, hasAnchors);
			}

			logger.WriteTime("import subpage completed");
			return page;
		}


		// = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = =

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Major Bug",
			"S2583:Conditionally executed code should be reachable", Justification = "<Pending>")]
		private async Task<WebPageInfo> DownloadWebContent(Uri uri)
		{
			const string GetTitleJS = "document.getElementsByTagName('title')[0].innerText;";
			const string GetContentJS = "document.documentElement.outerHTML;";

			string content = null;
			string title = null;

			// WebView2 needs to run in an STA thread
			await SingleThreaded.Invoke(() =>
			{
				// WebView2 needs a message pump so host in its own invisible worker dialog
				using var form = new WebViewDialog(
					startup:
					new WebViewWorker(async (webview) =>
					{
						//logger.WriteLine($"starting up webview with {uri}");
						webview.Source = uri;
						await Task.Yield();
						return true;
					}),
					work:
					new WebViewWorker(async (webview) =>
					{
						title = await webview.ExecuteScriptAsync(GetTitleJS);
						//logger.WriteLine($"title=[{title}]");

						content = await webview.ExecuteScriptAsync(GetContentJS);
						//logger.WriteLine($"content=[{content}]");

						await Task.Yield();
						return true;
					}));

				form.ShowDialog(/* leave empty */);
			});

			if (!string.IsNullOrWhiteSpace(title) &&
				title[0] == '"' && title[title.Length - 1] == '"')
			{
				// remove double quotes
				title = title.Substring(1, title.Length - 2);
			}

			if (!string.IsNullOrWhiteSpace(content))
			{
				// unescape and remove double quotes
				content = Regex.Unescape(content);
				content = content.Substring(1, content.Length - 2);
			}

			var bycount = content == null ? 0 : content.Length;
			logger.WriteLine($"retrieved {bycount} bytes from {title} ({uri.AbsoluteUri})");

			return new WebPageInfo
			{
				Content = content,
				Title = title
			};
		}


		private Hap.HtmlDocument ReplaceImagesWithAnchors(
			string content, Uri baseUri, out bool replaced)
		{
			// use HtmlAgilityPack to normalize and clean up the HTML...

			var doc = new Hap.HtmlDocument();
			doc.LoadHtml(content);

			// convert img tags to anchor tags
			// OneNote will remove img tags but will keep anchors

			var body = doc.DocumentNode.SelectSingleNode("//body");
			if (body == null)
			{
				logger.WriteLine("no <body> found in content");
				replaced = false;
				return null;
			}

			var images = body.Descendants("img")
				.Where(e => !string.IsNullOrEmpty(e.GetAttributeValue("src", string.Empty)))
				.ToList();

			if (images.Count == 0)
			{
				replaced = false;
				return doc;
			}

			var oneUri = (new UriBuilder(baseUri) { Host = $"onemore.{baseUri.Host}" }).Uri;

			foreach (var image in images)
			{
				var src = image.GetAttributeValue("src", string.Empty);
				if (!string.IsNullOrEmpty(src))
				{
					var uri = new Uri(oneUri, src);
					if (!uri.Host.StartsWith("onemore."))
					{
						uri = new UriBuilder(uri) { Host = $"onemore.{uri.Host}" }.Uri;
					}
					src = uri.AbsoluteUri;

					var anchor = Hap.HtmlNode.CreateNode($"<a href=\"{src}\">{src}</a>");
					image.ParentNode.ReplaceChild(anchor, image);
				}
			}

			replaced = true;
			return doc;
		}


		private bool EncodeLocalAnchors(Hap.HtmlDocument doc, Uri baseUri)
		{
			var body = doc.DocumentNode.SelectSingleNode("//body");
			if (body == null)
			{
				return false;
			}

			// find all hyperlink that references anchors on this page
			var links = body.Descendants("a")
				.Where(e => !string.IsNullOrEmpty(e.GetAttributeValue("href", string.Empty)))
				.Select(e => new
				{
					Link = e,
					Uri = new Uri(baseUri, e.GetAttributeValue("href", string.Empty))
				})
				.Where(a => !string.IsNullOrEmpty(a.Uri.Fragment) && a.Uri.SamePage(baseUri));

			var count = 0;
			foreach (var link in links)
			{
				// find the referenced anchor
				var match = Regex.Match(link.Uri.Fragment, @"#(\w+)=?");
				if (match.Success)
				{
					var name = match.Groups[1].Value;
					var anchor = body.Descendants("a")
						.FirstOrDefault(e => e.GetAttributeValue("name", string.Empty) == name);

					if (anchor != null)
					{
						// replace the link with a temporary holding element
						link.Link.SetAttributeValue("href",
							(new UriBuilder(link.Uri)
							{
								Host = $"onemore-link{count}.{baseUri.Host}"
							}).Uri.AbsoluteUri);

						// set a temporary href so anchor won't be removed by OneNote
						anchor.Attributes.Remove("name");
						anchor.SetAttributeValue("href",
							(new UriBuilder(baseUri)
							{
								Host = $"onemore-anchor{count}.{baseUri.Host}"
							}).Uri.AbsoluteUri);

						if (string.IsNullOrEmpty(anchor.InnerText))
						{
							// something so OneNote doesn't trash it
							anchor.InnerHtml = "-";
						}
					}

					count++;
				}
			}

			return count > 0;
		}


		private async Task PatchPage(Page page, OneNote one, bool hasImages, bool hasAnchors)
		{
			try
			{
				logger.WriteLine("pass 2 patching images and anchors");

				// fetch page again with temp links
				page = await one.GetPage(page.PageId, OneNote.PageDetail.All);
                using (StreamWriter writer = new StreamWriter(@"c:\tmp\writegitlab\outtmp3a.xml"))
                {
                    var xml = page.Root.ToString();
                    writer.Write(xml);
                }

                var updated = false;
				if (hasImages)
				{
					updated |= await PatchImages(page);
				}

				if (hasAnchors)
				{
					updated |= PatchAnchors(page, one);
				}

				if (updated)
				{
					// second update to page
					await one.Update(page);
				}
			}
			catch (Exception exc)
			{
				logger.WriteLine("error patching page", exc);
			}
		}


		private async Task<bool> PatchImages(Page page)
		{
			try
			{
				// transform anchors to downloaded images...
				logger.WriteLine("patching images");

				var regex = new Regex(
					@"<a\s+href=""[^:]+://(onemore\.)[^:]+://(onemore\.)",
					RegexOptions.Compiled);

				// download and embed images
				var cmd = new GetImagesCommand(regex);
				if (await cmd.GetImages(page))
				{
					return true;
				}
			}
			catch (Exception exc)
			{
				logger.WriteLine("error patching images", exc);
			}

			logger.WriteLine("no images found");
			return false;
		}


		private bool PatchAnchors(Page page, OneNote one)
		{
			try
			{
				// rewrite anchors to on-page self-references
				logger.WriteLine("patching anchors");

				var updated = false;
				var regex = new Regex(@"<a\s+href=""[^:]+://onemore-link([\d]+)\.");

				var list = page.Root
					.Elements(page.Namespace + "Outline")
					.DescendantNodes().OfType<XCData>()
					.Select(e => new
					{
						Data = e,
						Match = regex.Match(e.Value)
					})
					.Where(r => r.Match.Success)
					.ToList();

				foreach (var item in list)
				{
					var key = item.Match.Groups[1].Value;
					var anchor = page.Root
						.Elements(page.Namespace + "Outline")
						.DescendantNodes().OfType<XCData>()
						.Where(c => Regex.IsMatch(c.Value, $@"<a\s+href=""[^:]+://onemore-anchor({key})\."))
						.Select(e => e.Parent)
						.FirstOrDefault();

					if (anchor != null)
					{
						var objectId = anchor.Parent.NodesAfterSelf().OfType<XElement>()
							.Where(e => e.Name.LocalName == "OE")
							.Select(e => e.Attribute("objectID").Value)
							.FirstOrDefault();

						if (!string.IsNullOrEmpty(objectId))
						{
							var hyperlink = one.GetHyperlink(page.PageId, objectId);
							var wrapper = item.Data.GetWrapper();

							// a cdata may contain more than one <a>
							wrapper.Descendants("a")
								.FirstOrDefault(a => a.Attribute("href").Value.Contains($"://onemore-link{key}."))?
								.SetAttributeValue("href", hyperlink);

							item.Data.Value = wrapper.GetInnerXml();

							anchor.Parent.Remove();
							updated = true;
						}
					}
				}

				return updated;
			}
			catch (Exception exc)
			{
				logger.WriteLine("error patching anchors", exc);
			}

			logger.WriteLine("no anchors found");
			return false;
		}
	}
}
