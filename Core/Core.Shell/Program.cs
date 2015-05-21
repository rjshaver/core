﻿using System;
using System.IO;
using Core.Common;
using Core.IO;
using Core.Platform;
using Core.Shell.Common;
using Mono.Options;
using System.Collections.Generic;

namespace Core.Shell
{
	public class MainClass
	{
		public static void Main (string[] args)
		{
			Logging.Enable ();

			new MainClass ().Run (args);

			Logging.Finish ();
		}

		public void Run (string[] args)
		{
			fixFileAssociations ();

			Logging.Targets.StandardOutput = false;

			// option values
			bool help = false;
			Mode mode = Mode.Interactive;
			string commandString = null;
			string scriptFile = null;
			List<string> parameters = new List<string> ();

			// option parser
			OptionSet optionSet = new OptionSet ();
			optionSet.Add ("h|help|?", "Prints out the options.", option => help = (option != null));
			optionSet.Add ("log|debug", "Show log messages.", option => Logging.Targets.StandardOutput = option != null);
			optionSet.Add ("c=", "Execute a command string.", option => {
				mode = Mode.CommandString;
				commandString = option;
			});
			optionSet.Add ("<>", option => {
				if (mode != Mode.CommandString && scriptFile == null) {
					mode = Mode.ScriptFile;
					scriptFile = option;
					Log.Warning ("Script file: ", option);
				} else {
					parameters.Add (option);
					Log.Warning ("Parameter: ", option);
				}
			});
			try {
				optionSet.Parse (args);
			} catch (OptionException) {
				help = true;
			}

			// need help ?
			if (help) {
				printOptions (optionSet);
				return;
			}

			UnixShell shell = new UnixShell ();
			shell.Output = output;

			// run code line
			if (mode == Mode.CommandString) {
				try {
					shell.RunScript (code: commandString);
				} catch (Exception ex) {
					Log.Error (ex);
				}
			}
			// run script
			if (mode == Mode.ScriptFile) {
				try {
					shell.RunScript (code: File.ReadAllText (scriptFile));
				} catch (Exception ex) {
					Log.Error (ex);
				}
			}
			// run interactively
			if (mode == Mode.Interactive) {
				
				// run test code if there is no interactive console
				if (!SystemInfo.IsInteractive) {
					Logging.Targets.StandardOutput = true;
					test (shell);
				}

				// run interactively
				else {
					string line;
					while (NonBlockingConsole.IsInputOpen) {
						while (NonBlockingConsole.TryReadLine (result: out line)) {
							if (!string.IsNullOrWhiteSpace (line)) {
								shell.Interactive (line: line);
							}
						}
					}
				}
			}
		}

		private void printOptions (OptionSet optionSet)
		{
			optionSet.WriteOptionDescriptions (Console.Out);
		}

		public enum Mode
		{
			Interactive,
			CommandString,
			ScriptFile,
		}

		void test (UnixShell shell)
		{
			shell.Interactive (@"
				echo test;echo test2
				echo test7 a  ""b c d"" """" ''      'c' abc;
				if true;
				then
					echo test4;
				elif false ; then
					echo test5
				else ;;;
					echo test6; echo test7
				fi
			");
		}

		void fixFileAssociations ()
		{
			FileAssociation.SetAssociation (
				extensions: new[]{ ".sh", ".coresh", ".bash" },
				id: "ShellScript",
				description: "Shell Script",
				exePath: SystemInfo.ApplicationPath,
				iconPath: Path.Combine (Path.GetDirectoryName (SystemInfo.ApplicationPath), "icon.ico")
			);
		}

		void output (string text)
		{
			Console.Write (text);
		}
	}
}
