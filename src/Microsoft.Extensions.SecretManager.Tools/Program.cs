// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.SecretManager.Tools.Internal;
using Microsoft.Extensions.Tools.Internal;

namespace Microsoft.Extensions.SecretManager.Tools
{
    public class Program
    {
        private readonly IConsole _console;
        private readonly string _workingDirectory;

        public static int Main(string[] args)
        {
            DebugHelper.HandleDebugSwitch(ref args);

            int rc;
            new Program(PhysicalConsole.Singleton, Directory.GetCurrentDirectory()).TryRun(args, out rc);
            return rc;
        }

        public Program(IConsole console, string workingDirectory)
        {
            _console = console;
            _workingDirectory = workingDirectory;
        }

        public bool TryRun(string[] args, out int returnCode)
        {
            try
            {
                returnCode = RunInternal(args);
                return true;
            }
            catch (Exception exception)
            {
                var reporter = CreateReporter(verbose: true);
                reporter.Verbose(exception.ToString());
                reporter.Error(Resources.FormatError_Command_Failed(exception.Message));
                returnCode = 1;
                return false;
            }
        }

        internal int RunInternal(params string[] args)
        {
            CommandLineOptions options;
            try
            {
                options = CommandLineOptions.Parse(args, _console);
            }
            catch (CommandParsingException ex)
            {
                CreateReporter(verbose: false).Error(ex.Message);
                return 1;
            }

            if (options == null)
            {
                return 1;
            }

            if (options.IsHelp)
            {
                return 2;
            }

            var reporter = CreateReporter(options.IsVerbose);

            string userSecretsId;
            try
            {
                userSecretsId = ResolveId(options, reporter);
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is FileNotFoundException)
            {
                reporter.Error(ex.Message);
                return 1;
            }

            var store = new SecretsStore(userSecretsId, reporter);
            var context = new Internal.CommandContext(store, reporter, _console);
            options.Command.Execute(context);
            return 0;
        }

        private IReporter CreateReporter(bool verbose)
        {
            return new ReporterBuilder()
                .WithConsole(_console)
                .Verbose(f =>
                {
                    f.When(() => verbose);
                    if (!_console.IsOutputRedirected)
                    {
                        f.WithColor(ConsoleColor.DarkGray);
                    }
                })
                .Warn(f =>
                {
                    if (!_console.IsOutputRedirected)
                    {
                        f.WithColor(ConsoleColor.Yellow);
                    }
                })
                .Error(f =>
                {
                    if (!_console.IsErrorRedirected)
                    {
                        f.WithColor(ConsoleColor.Red);
                    }
                })
                .Build();
        }

        internal string ResolveId(CommandLineOptions options, IReporter reporter)
        {
            if (!string.IsNullOrEmpty(options.Id))
            {
                return options.Id;
            }

            var resolver = new ProjectIdResolver(reporter, _workingDirectory);
            return resolver.Resolve(options.Project, options.Configuration);
        }
    }
}