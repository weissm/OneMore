//************************************************************************************************
// Copyright © 2021 Steven M Cohn.  All rights reserved.
//************************************************************************************************

namespace River.OneMoreAddIn
{
	public abstract class Loggable
	{
		protected ILogger logger;


		protected Loggable()
		{
			logger = Logger.Current;
		}
	}
}
