﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using Microsoft.DotNet.Watcher.Core.Internal;

namespace Microsoft.DotNet.Watcher.FunctionalTests
{
    public class DotNetWatchScenario : IDisposable
    {
        private static readonly TimeSpan _processKillTimeout = TimeSpan.FromSeconds(30);

        protected const string DotnetWatch = "dotnet-watch";

        protected static readonly string _repositoryRoot = FindRepoRoot();
        protected static readonly string _artifactsFolder = Path.Combine(_repositoryRoot, "artifacts", "build");

        protected ProjectToolScenario _scenario;

        public DotNetWatchScenario()
        {
            _scenario = new ProjectToolScenario();
            _scenario.AddNugetFeed(DotnetWatch, _artifactsFolder);
        }

        public Process WatcherProcess { get; private set; }

        protected void RunDotNetWatch(string arguments, string workingFolder)
        {
            WatcherProcess = _scenario.ExecuteDotnet("watch " + arguments, workingFolder);
        }

        public virtual void Dispose()
        {
            if (WatcherProcess != null)
            {
                if (!WatcherProcess.HasExited)
                {
                    WatcherProcess.KillTree(_processKillTimeout);
                }
                WatcherProcess.Dispose();
            }
            _scenario.Dispose();
        }

        private static string FindRepoRoot()
        {
            var di = new DirectoryInfo(Directory.GetCurrentDirectory());

            while (di.Parent != null)
            {
                var globalJsonFile = Path.Combine(di.FullName, "global.json");

                if (File.Exists(globalJsonFile))
                {
                    return di.FullName;
                }

                di = di.Parent;
            }

            return null;
        }
    }
}