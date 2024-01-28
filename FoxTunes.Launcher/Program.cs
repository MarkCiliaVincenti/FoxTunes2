﻿using FoxTunes.Interfaces;
using System;

namespace FoxTunes.Launcher
{
    public static class Program
    {
        public static readonly TimeSpan SHUTDOWN_INTERVAL = TimeSpan.FromSeconds(1);

        public static readonly TimeSpan SHUTDOWN_TIMEOUT = TimeSpan.FromSeconds(10);

        static Program()
        {
            AssemblyResolver.Instance.EnableExecution();
            AssemblyResolver.Instance.EnableReflectionOnly();
        }

        [STAThread]
        public static void Main(string[] args)
        {
            using (var server = new Server())
            {
                if (server.IsDisposed)
                {
                    var client = new Client();
                    //TODO: Bad .Wait()
                    client.Send(Environment.CommandLine).Wait();
                    return;
                }
                using (var core = new Core(CoreSetup.Default))
                {
                    AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
                    {
                        if (core.Components.UserInterface != null)
                        {
                            core.Components.UserInterface.Fatal(e.ExceptionObject as Exception);
                        }
                    };
                    try
                    {
                        core.Load();
                        if (!CoreValidator.Instance.Validate(core))
                        {
                            throw new InvalidOperationException("One or more required components were not loaded.");
                        }
                        var test = core.Factories.Database.Test();
                        if (test != DatabaseTestResult.OK)
                        {
                            if (test == DatabaseTestResult.Missing)
                            {
                                if (core.Factories.Database.Flags.HasFlag(DatabaseFactoryFlags.ConfirmCreate))
                                {
                                    if (!core.Components.UserInterface.Confirm("The database was not found, initialize it?."))
                                    {
                                        throw new OperationCanceledException("Database initialization was cancelled.");
                                    }
                                }
                            }
                            else if (test == DatabaseTestResult.Mismatch)
                            {
                                if (!core.Components.UserInterface.Confirm("The database is incompatible, delete it?."))
                                {
                                    throw new OperationCanceledException("Database initialization was cancelled.");
                                }
                            }
                            core.Factories.Database.Initialize();
                            if (core.Factories.Database.Test() != DatabaseTestResult.OK)
                            {
                                throw new InvalidOperationException("Failed to initialize the database.");
                            }
                            using (var database = core.Factories.Database.Create())
                            {
                                core.InitializeDatabase(database, DatabaseInitializeType.All);
                            }
                        }
                        core.Initialize();
                        server.Message += (sender, e) =>
                        {
                            core.Components.UserInterface.Run(e.Message);
                        };
                        core.Components.UserInterface.Run(Environment.CommandLine);
                    }
                    catch (Exception e)
                    {
                        if (core.Components.UserInterface != null)
                        {
                            core.Components.UserInterface.Fatal(e);
                            return;
                        }
                    }
                    try
                    {
                        if (core.Components.UserInterface != null)
                        {
                            core.Components.UserInterface.Show();
                        }
                        Core.IsShuttingDown = true;
                        if (core.Components.Output != null)
                        {
                            if (core.Components.Output.IsStarted)
                            {
                                //TODO: Bad .Wait().
                                if (!core.Components.Output.Shutdown().Wait(SHUTDOWN_TIMEOUT))
                                {
                                    //TODO: Warn.
                                }
                            }
                        }
                        //TODO: Bad .Result
                        if (!BackgroundTask.Shutdown(SHUTDOWN_INTERVAL, SHUTDOWN_TIMEOUT).Result)
                        {
                            //TODO: Warn.
                        }
                    }
                    catch (Exception e)
                    {
                        if (core.Components.UserInterface != null)
                        {
                            core.Components.UserInterface.Fatal(e);
                        }
                    }
                }
            }
        }
    }
}
