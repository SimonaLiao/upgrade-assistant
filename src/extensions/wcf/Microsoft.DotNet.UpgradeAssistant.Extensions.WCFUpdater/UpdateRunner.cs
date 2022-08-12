﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.WCFUpdater
{
    public class UpdateRunner
    {
        public static List<XDocument>? ConfigUpdate(ConfigUpdater updater, ILogger logger)
        {
            try
            {
                logger.LogDebug("Start updating configuration files...");
                XDocument wcfConfig = updater.GenerateNewConfig();
                XDocument appConfig = updater.UpdateOldConfig();
                var result = new List<XDocument>
                            {
                                appConfig,
                                wcfConfig
                            };
                logger.LogInformation("Finish updating configuration files.");
                return result;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error updating config files");
                return null;
            }
        }

        public static void WriteConfigUpdate(XDocument appConfig, XDocument wcfConfig, string path, ILogger logger)
        {
            try
            {
                logger.LogDebug("Start writing changes to configuration files...");
                wcfConfig.Save(Path.Combine(Path.GetDirectoryName(path), "wcf.config"));
                appConfig.Save(path);
                logger.LogInformation("Finish writing changes to configuration files.");
            }
            catch (Exception e)
            {
                logger.LogError(e, $"Error writing changes to {path}");
            }
        }

        public static XDocument? PackageUpdate(PackageUpdater packageUpdater, ILogger logger)
        {
            try
            {
                logger.LogDebug("Start updating project file...");
                packageUpdater.UpdatePackages();
                var projFile = packageUpdater.UpdateSDK();
                logger.LogInformation("Finish updating project file.");
                return projFile;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error updating project file");
                return null;
            }
        }

        public static void WritePackageUpdate(XDocument projFile, string path, ILogger logger)
        {
            try
            {
                logger.LogDebug("Start writing changes to project file...");
                projFile.Save(path);
                logger.LogInformation("Finish writing changes to project file.");
            }
            catch (Exception e)
            {
                logger.LogError(e, $"Error writing changes to {path}");
            }
        }

        public static SyntaxNode? SourceCodeUpdate(SourceCodeUpdater sourceCodeUpdater, ILogger logger)
        {
            try
            {
                logger.LogDebug("Start updating source code...");
                var root = sourceCodeUpdater.UpdateDirectives();
                root = sourceCodeUpdater.RemoveOldCode(sourceCodeUpdater.AddTemplateCode(root));
                logger.LogInformation("Finish updating source code.");
                return root;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error updating source code");
                return null;
            }
        }

        public static void WriteSourceCodeUpdate(SyntaxNode root, string path, ILogger logger)
        {
            try
            {
                logger.LogDebug("Start writing changes to source code to update Main()...");
                File.WriteAllText(path, root.ToFullString());
                logger.LogInformation("Finish writing changes to source code/Main().");
            }
            catch (Exception e)
            {
                logger.LogError(e, $"Error writing changes to {path}");
            }
        }

        public static List<SyntaxNode>? DirectiveUpdate(List<SourceCodeUpdater> list, ILogger logger)
        {
            try
            {
                logger.LogDebug("Start updating directives...");
                var result = new List<SyntaxNode>();
                foreach (var updater in list)
                {
                    result.Add(updater.UpdateDirectives());
                }

                logger.LogInformation("Finish updating directives.");
                return result;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error updating using directives");
                return null;
            }
        }

        public static void WriteDirectiveUpdate(List<SyntaxNode> root, IEnumerable<string> path, ILogger logger)
        {
            try
            {
                logger.LogDebug("Start writing changes to source code to update directives...");
                if (root.Count != path.Count())
                {
                    logger.LogError("The number of files does not match the number of path. Cannot write directive updates.");
                    return;
                }

                for (int i = 0; i < root.Count; i++)
                {
                    File.WriteAllText(path.ElementAtOrDefault(i), root[i].ToFullString());
                }

                logger.LogInformation("Finish writing changes to source code/directives.");
            }
            catch (Exception e)
            {
                logger.LogError(e, $"Error writing changes to {path.ToList()}");
            }
        }

        public static Dictionary<string, Dictionary<string, object>> GetContexts(ConfigUpdater configUpdater)
        {
            var contexts = new Dictionary<string, Dictionary<string, object>>();
            //var behaviorNames = configUpdater.GetAllBehaviorNames();
            var pair = configUpdater.GetServiceBehaviorPair();
            foreach (var serviceName in pair.Keys)
            {
                contexts.Add(serviceName, GetContext(configUpdater, pair[serviceName]));
            }

            return contexts;
        }

        public static Dictionary<string, object> GetContext(ConfigUpdater configUpdater, string name)
        {
            Dictionary<string, object> context = new Dictionary<string, object>
            {
                { "uri", configUpdater.GetUri(name) },
                { "metadata", configUpdater.SupportsMetadataBehavior(name) },
                { "debug", configUpdater.SupportsServiceDebug(name) },
                { "bindings", configUpdater.GetBindings(name) }
            };
            return context;
        }
    }
}
