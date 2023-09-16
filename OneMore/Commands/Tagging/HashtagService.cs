﻿//************************************************************************************************
// Copyright © 2023 Steven M Cohn.  All rights reserved.
//************************************************************************************************

namespace River.OneMoreAddIn.Commands
{
	using System;
	using System.Diagnostics;
	using System.Threading;
	using System.Threading.Tasks;


	/// <summary>
	/// Background service to collect ##hashtags within content.
	/// This is a polling mechanism with specified throttling limits.
	/// </summary>
	internal class HashtagService : Loggable
	{
		public const int DefaultPollingInterval = 60000 * 2; // 2 minutes

		private readonly int pollingInterval;


		public HashtagService()
		{
			pollingInterval = DefaultPollingInterval;
		}


		public void Startup()
		{
			logger.WriteLine("starting hashtag service");

			var thread = new Thread(async () =>
			{
				// 'errors' allows repeated consecutive exceptions but limits that to 5 so we
				// don't fall into an infinite loop. If it somehow miraculously recovers then
				// errors is reset back to zero and normal processing continues...

				var errors = 0;
				while (errors < 5)
				{
					try
					{
						await Scan();
						errors = 0;
					}
					catch (Exception exc)
					{
						logger.WriteLine($"hashtag service exception {errors}", exc);
						errors++;
					}

					await Task.Delay(pollingInterval);
				}

				logger.WriteLine("hashtag service has stopped; check for exceptions above");
			});

			thread.SetApartmentState(ApartmentState.STA);
			thread.IsBackground = true;
			thread.Priority = ThreadPriority.BelowNormal;
			thread.Start();
		}


		private async Task Scan()
		{
			var clock = new Stopwatch();
			clock.Start();

			using var scanner = new HashtagScanner();
			var totalPages = await scanner.Scan();

			clock.Stop();
			var time = clock.ElapsedMilliseconds;
			logger.WriteLine($"hashtag service scanned {totalPages} pages in {time}ms");
		}
	}
}
