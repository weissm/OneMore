﻿//************************************************************************************************
// Copyright © 2016 Steven M Cohn.  All rights reserved.
//************************************************************************************************

namespace River.OneMoreAddIn
{
	using Microsoft.Office.Core;
	using OneMoreAddIn.Settings;
	using River.OneMoreAddIn.UI;
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading.Tasks;
	using System.Windows.Forms;
	using System.Xml.Linq;
	using Resx = River.OneMoreAddIn.Properties.Resources;


	/// <summary>
	/// Instantiates and runs OneMore commands
	/// </summary>
	internal class CommandFactory
	{
		public const string CollectionName = "mru";
		public const string SettingsName = "commands";
		public const string SettingName = "command";

		private readonly ILogger logger;
		private readonly IRibbonUI ribbon;
		private readonly IWin32Window owner;
		private readonly List<IDisposable> trash;


		/// <summary>
		/// Initialize a new factory with the given services
		/// </summary>
		/// <param name="logger">The logger</param>
		/// <param name="ribbon">The OneNote ribbon</param>
		/// <param name="trash">A colleciton of IDisposables for cleanup on shutdown</param>
		/// <param name="owner">The owner window</param>
		public CommandFactory(
			ILogger logger, IRibbonUI ribbon, List<IDisposable> trash, IWin32Window owner)
		{
			this.logger = logger;
			this.ribbon = ribbon;
			this.owner = owner;
			this.trash = trash;
		}


		/// <summary>
		/// Instantiates and executes the specified command with optional arguments.
		/// Provides catch-all exception handling - logging and a generic message to the user
		/// </summary>
		/// <typeparam name="T">The command type</typeparam>
		/// <param name="args">The argument list</param>
		/// <returns>Task</returns>
		public async Task<Command> Run<T>(params object[] args) where T : Command, new()
		{
			var command = new T();
			await Run("Running", command, args);

			if (!command.IsCancelled)
			{
				RecordLastAction(command, args);
			}

			return command;
		}


		private async Task Run(string note, Command command, params object[] args)
		{
			var type = command.GetType();
			logger.Start($"{note} command {type.Name}");

			command.SetFactory(this)
				.SetLogger(logger)
				.SetRibbon(ribbon)
				.SetOwner(owner)
				.SetTrash(trash);

			try
			{
				await command.Execute(args);

				logger.End();
			}
			catch (Exception exc)
			{
				// catch-all exception hander

				var msg = string.Format(Resx.Command_Error, type.Name);
				logger.End();
				logger.WriteLine(msg);
				logger.WriteLine(exc);
				logger.WriteLine();

				MoreMessageBox.ShowErrorWithLogLink(
					owner, string.Format(Resx.Command_ErrorMsg, msg));
			}
		}


		private void RecordLastAction(Command command, params object[] args)
		{
			// ignore commands that pass the ribbon as an argument
			if (args == null || args.Any(a => a != null && a.GetType().Name.Contains("ComObject")))
			{
				return;
			}

			try
			{
				var provider = new SettingsProvider();
				var settings = provider.GetCollection(CollectionName);
				var commands = settings.Get<XElement>(SettingsName);
				if (commands == null)
				{
					commands = new XElement(SettingsName);
					settings.Add(SettingsName, commands);
				}

				var arguments = new XElement("arguments");
				args.Where(a => a != null).ForEach(a =>
				{
					arguments.Add(new XElement("arg",
						new XAttribute("type", a.GetType().FullName),
						new XAttribute("value", a.ToString())
						));
				});

				var setting = new XElement(SettingName,
					new XAttribute("type", command.GetType().FullName),
					arguments
					);

				// "type" records the :Command inheritor class whereas
				// "cmd" records the AddInCommands xxxCmd method name
				var trace = new System.Diagnostics.StackTrace();
				var runner = trace.GetFrames()
					.Where(f => f.GetMethod().Name.EndsWith("Cmd"))
					.Select(f => f.GetMethod().Name)
					.FirstOrDefault();

				if (runner != null)
				{
					setting.Add(new XAttribute("cmd", runner));
				}

				var replay = command.GetReplayArguments();
				if (replay != null)
				{
					setting.Add(new XElement("context", replay));
				}

				provider.SetCollection(settings);

				commands.Add(setting);
				while (commands.Elements().Count() > 5)
				{
					commands.Elements().First().Remove();
				}

				provider.SetCollection(settings);
				provider.Save();
			}
			catch (Exception exc)
			{
				logger.WriteLine("error recording last action", exc);
			}
		}


		/// <summary>
		/// Instantiates and executes the most recently executed command.
		/// </summary>
		/// <returns>Task</returns>
		public async Task ReplayLastAction()
		{
			var provider = new SettingsProvider();
			var settings = provider.GetCollection(CollectionName);
			var action = settings.Get<XElement>(SettingsName)?.Elements(SettingName).LastOrDefault();
			if (action == null)
			{
				return;
			}

			try
			{
				var command = (Command)Activator.CreateInstance(
					Type.GetType(action.Attribute("type").Value)
					);

				var args = new List<object>();
				foreach (var arg in action.Element("arguments").Elements("arg"))
				{
					var type = Type.GetType(arg.Attribute("type").Value);
					if (type.IsEnum)
					{
						args.Add(Enum.Parse(type, arg.Attribute("value").Value));
					}
					else
					{
						args.Add(Convert.ChangeType(
							arg.Attribute("value").Value,
							Type.GetType(arg.Attribute("type").Value)
							));
					}
				}

				var context = action.Elements("context").FirstOrDefault();
				if (context != null && context.HasElements)
				{
					args.Add(context.Elements().First());
				}

				await Run("Replaying", command, args.ToArray());
			}
			catch (Exception exc)
			{
				provider.RemoveCollection(CollectionName);
				provider.Save();

				logger.WriteLine("error parsing last action; history cleared", exc);
			}
		}


		/// <summary>
		/// Invokes a command using its name and an array of strings as arguments. If the
		/// command's arguments are other types then this may need to defer to a proxy method
		/// that converts the string values to their proper types and then invokes the target
		/// command directly.
		/// </summary>
		/// <param name="action">The command name</param>
		/// <param name="arguments">The arguments to pass to the command</param>
		/// <returns></returns>
		public async Task Invoke(string action, string[] arguments)
		{
			var name = $"River.OneMoreAddIn.Commands.{action}";
			var type = Type.GetType(name, false);
			if (type == null)
			{
				logger.WriteLine($"factory failed to find command {name}");
				return;
			}

			if (!(Activator.CreateInstance(type) is Command command))
			{
				logger.WriteLine($"factory failed to create instance of '{name}'");
				return;
			}

			await Run("Invoking", command, arguments);
		}
	}
}
