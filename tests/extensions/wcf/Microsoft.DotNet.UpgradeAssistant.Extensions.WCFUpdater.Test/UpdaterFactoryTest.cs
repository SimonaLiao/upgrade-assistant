﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.WCFUpdater.Tests
{
    public class UpdaterFactoryTest
    {
        private readonly NullLogger _logger = NullLogger.Instance;

        [Theory]
        [InlineData("TestInputFiles\\UpdateFactoryConfig.txt", "TestExpectedFiles\\ExpectedTemplate.txt")]
        [InlineData("TestInputFiles\\UpdateFactoryConfigWithHttps.txt", "TestExpectedFiles\\ExpectedTemplateWithHttps.txt")]
        public void UpdateFactoryTemplateTest(string input, string expected)
        {
            var context = UpdateRunner.GetContext(new ConfigUpdater(XDocument.Load(input), _logger));
            var actual = UpdaterFactory.UpdateTemplateCode(context, _logger);
            Assert.Equal(File.ReadAllText(expected), actual);
        }
    }
}
