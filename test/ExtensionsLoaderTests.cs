﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.IO;
using AutoRest.Core.Model;
using AutoRest.Core.Extensibility;
using AutoRest.Core.Logging;
using AutoRest.Core.Tests.Resource;
using AutoRest.Core.Utilities;
using Xunit;
using static AutoRest.Core.Utilities.DependencyInjection;

namespace AutoRest.Core.Tests
{
    [Collection("AutoRest Tests")]
    public class ExtensionsLoaderTests
    {
        private readonly IFileSystem _fileSystem = new MemoryFileSystem();

        public ExtensionsLoaderTests()
        {
            SetupMock();
        }

        private void SetupMock()
        {
            _fileSystem.WriteFile("RedisResource.json", File.ReadAllText(Path.Combine("Resource", "RedisResource.json")));
        }

        [Fact]
        public void LanguageWithSettingsLoadsFromJsonFile()
        {
            using (NewContext)
            {
                var settings = new Settings
                {
                    CodeGenerator = "NodeJS",
                    FileSystem = _fileSystem,
                    Input = "X:\\RedisResource.json",
                    OutputDirectory = "X:\\Output"
                };

                var language = ExtensionsLoader.GetPlugin();
                settings.Validate();

                Assert.Equal("NodeJS", language.Settings.Name);
            }
        }

        [Fact]
        public void LanguageLoadsFromJsonFile()
        {
            using (NewContext)
            {
                var settings = new Settings {CodeGenerator = "CSharp", FileSystem = _fileSystem};
                var language = ExtensionsLoader.GetPlugin();

                Assert.Equal("CSharp", language.Settings.Name);
            }
        }

        [Fact]
        public void LanguageWithoutSettingsLoadsFromJsonFile()
        {
            using (NewContext)
            {
                var settings = new Settings {CodeGenerator = "CSharp", FileSystem = _fileSystem};

                var language = ExtensionsLoader.GetPlugin();
                Assert.Equal("CSharp", language.Settings.Name);
            }
        }

        [Fact]
        public void ModelerLoadsFromJsonFile()
        {
            using (NewContext)
            {
                var settings = new Settings
                {
                    Modeler = "Swagger",
                    Input = "RedisResource.json",
                    FileSystem = _fileSystem
                };
                Modeler modeler = ExtensionsLoader.GetModeler();

                Assert.Equal("Swagger", modeler.Name);
            }
        }

        [Fact]
        public void InvalidModelerNameThrowsException()
        {
            using (NewContext)
            {
                string modeler = "Foo.Bar";
                var settings = new Settings {Modeler = modeler, FileSystem = _fileSystem};
                AssertThrows<CodeGenerationException>(
                    () => ExtensionsLoader.GetModeler(),
                    string.Format("Plugin {0} not found", modeler));
            }
        }

        [Fact]
        public void NullOrEmptyAutoRestSettings()
        {
            using (NewContext)
            {
                var settings = new Settings {CodeGenerator = string.Empty, FileSystem = _fileSystem};
                Assert.Throws<ArgumentException>(() => ExtensionsLoader.GetPlugin());
            }
            using (NewContext)
            {
                var settings = new Settings {Modeler = string.Empty, FileSystem = _fileSystem};
                Assert.Throws<ArgumentException>(() => ExtensionsLoader.GetModeler());
            }
        }

        [Fact]
        public void InvalidLanguageNameThrowsException()
        {
            using (NewContext)
            {
                string codeGenerator = "Foo.Bar";
                var settings = new Settings
                {
                    CodeGenerator = codeGenerator,
                    FileSystem = _fileSystem
                };
            

            AssertThrows<CodeGenerationException>(
                () => ExtensionsLoader.GetPlugin(),
                $"Plugin {codeGenerator} not found");
            }
        }

        private void AssertThrows<T>(Action action, string message) where T : Exception
        {
            try
            {
                action();
                Assert.True(false);
            }
            catch (T ex)
            {
                Assert.Contains(message, ex.Message);
            }
        }
    }
}
