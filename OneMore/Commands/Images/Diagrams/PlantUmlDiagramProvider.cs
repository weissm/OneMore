﻿//************************************************************************************************
// Copyright © 2023 Steven M Cohn. All rights reserved.
//************************************************************************************************

namespace River.OneMoreAddIn.Commands
{
	using River.OneMoreAddIn.Settings;
	using System;
	using System.IO;
	using System.IO.Compression;
	using System.Net;
	using System.Text;
	using System.Text.RegularExpressions;
	using System.Threading;
	using System.Threading.Tasks;
	using Resx = Properties.Resources;



	/// <summary>
	/// Invoke the remote PlantUml server to render the given script as an image.
	/// </summary>
	/// <remarks>
	/// Text encoding is performed according to https://plantuml.com/en-dark/text-encoding
	/// </remarks>
	internal class PlantUmlDiagramProvider : Loggable, IDiagramProvider
	{
		private const string DiagramErrorHeader = "X-PlantUML-Diagram-Error";

		public PlantUmlDiagramProvider()
		{
		}


		public string ErrorMessages { get; private set; }


		public string ReadTitle(string text)
		{
			var match = Regex.Match(text, @"[\n\r]+title[ ]+([^\n\r]+)[\n\r]+", RegexOptions.IgnoreCase);
			if (!match.Success)
			{
				match = Regex.Match(text, @"@startuml[ ]+([^\n\r]+)[\n\r]+", RegexOptions.IgnoreCase);
			}

			if (match.Success)
			{
				var title = match.Groups[1].Value.Trim();
				if (title.Length > 0)
				{
					return title;
				}
			}

			return "PlantUML";
		}


		public async Task<byte[]> RenderRemotely(string text, CancellationToken token)
		{
			var encoded = Encode64(Deflate(ToUtf8(text)));

			var settings = new SettingsProvider().GetCollection(nameof(ImagesSheet));

			var plantUri = settings == null
				? Resx.PlantUmlCommand_PlantUrl
				: settings.Get("plantUri", Resx.PlantUmlCommand_PlantUrl);

			if (!plantUri.EndsWith("/"))
			{
				plantUri = $"{plantUri}/";
			}

			var url = $"{plantUri}{encoded}";

			var client = HttpClientFactory.Create();
			client.DefaultRequestHeaders.Add("user-agent", "OneMore");
			client.DefaultRequestHeaders.Add("accept", "image/png");

			try
			{
				using var response = await client.GetAsync(url, token).ConfigureAwait(false);
				if (!response.IsSuccessStatusCode)
				{
					if (response.StatusCode == HttpStatusCode.BadRequest)
					{
						var messages = string.Join(Environment.NewLine,
							response.Headers.GetValues(DiagramErrorHeader));

						ErrorMessages = $"{response.ReasonPhrase}\n{messages}";
					}

					return new byte[0];
				}

				var bytes = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
				return bytes;
			}
			catch (Exception exc)
			{
				logger.WriteLine("error rendering PlantUml", exc);
				return new byte[0];
			}
		}


		#region Text Encoding
		private static byte[] ToUtf8(string text)
		{
			// convert Unicode string to a UTF8 byte array
			// do not store this in another String as it will corrupt the data!
			return Encoding.Convert(Encoding.Default, Encoding.UTF8, Encoding.Default.GetBytes(text));
		}

		private static byte[] Deflate(byte[] bytes)
		{
			using var buffer = new MemoryStream();
			using var stream = new DeflateStream(buffer, CompressionMode.Compress);
			using var input = new MemoryStream(bytes);
			input.CopyTo(stream);
			stream.Close();
			return buffer.ToArray();
		}

		/*
		 * Base64 encoding uses the 3-to-4 strategy where every three bytes in the byte array
		 * are converted to four bytes by segementing them into six-bit groupings...
		 * 
		 * Input data          X        Y        Z
		 * Input bits   01011000-01011001-01011010
		 * Bit groups   010110-000101-100101-011010
		 * Mapping           W      F      l      a
		*/

		private static string Encode64(byte[] data)
		{
			var builder = new StringBuilder();

			for (var i = 0; i < data.Length; i += 3)
			{
				if (i + 1 == data.Length)
					builder.Append(Append3Bytes(data[i], 0, 0));
				else if (i + 2 == data.Length)
					builder.Append(Append3Bytes(data[i], data[i + 1], 0));
				else
					builder.Append(Append3Bytes(data[i], data[i + 1], data[i + 2]));
			}

			return builder.ToString();
		}


		private static string Append3Bytes(byte b1, byte b2, byte b3)
		{
			var c1 = b1 >> 2;
			var c2 = ((b1 & 0x3) << 4) | (b2 >> 4);
			var c3 = ((b2 & 0xF) << 2) | (b3 >> 6);
			var c4 = b3 & 0x3F;
			var s = Encode6bit(c1 & 0x3F).ToString(); // 0x3F = 00111111
			s += Encode6bit(c2 & 0x3F);
			s += Encode6bit(c3 & 0x3F);
			s += Encode6bit(c4 & 0x3F);
			return s;
		}


		private static char Encode6bit(int b)
		{
			// PlantUml doesn't use the normal Base64 mapping, it uses the following map
			//
			// 48......57 65......................90 97.....................122
			// 0123456789 ABCDEFGHIJKLMNOPQRSTUVWXYZ abcdefghijklmnopqrstuvwxyz -_

			if (b < 10)
			{
				return Convert.ToChar(48 + b);
			}

			b -= 10;
			if (b < 26)
			{
				return Convert.ToChar(65 + b);
			}

			b -= 26;
			if (b < 26)
			{
				return Convert.ToChar(97 + b);
			}

			b -= 26;
			if (b == 0)
			{
				return '-';
			}

			if (b == 1)
			{
				return '_';
			}

			return '?';
		}
		#endregion Text Encoding
	}
}
