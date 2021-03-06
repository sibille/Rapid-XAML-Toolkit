﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RapidXamlToolkit.Analyzers;
using RapidXamlToolkit.Options;

namespace RapidXamlToolkit.Tests.Analysis
{
    public abstract class StarsRepresentCaretInDocsTests
    {
        public const string TestLibraryPath = "./Assets/TestLibrary.dll";

        public TestContext TestContext { get; set; }

        public Profile DefaultProfile => GetDefaultTestSettings().GetActiveProfile();

        public static Settings GetDefaultTestSettings()
        {
            return new Settings
            {
                ActiveProfileName = "UWP",
                Profiles = new List<Profile>
                {
                    new Profile
                    {
                        Name = "UWP",
                        ClassGrouping = "StackPanel",
                        FallbackOutput = "<TextBlock Text=\"FALLBACK_$name$\" />",
                        SubPropertyOutput = "<TextBlock Text=\"SUBPROP_$name$\" />",
                        Mappings = new ObservableCollection<Mapping>
                        {
                            new Mapping
                            {
                                Type = "String",
                                NameContains = "password|pwd",
                                Output = "<PasswordBox Password=\"{x:Bind $name$}\" />",
                                IfReadOnly = false,
                            },
                            new Mapping
                            {
                                Type = "String",
                                NameContains = string.Empty,
                                Output = "<TextBox Text=\"{x:Bind $name$, Mode=TwoWay}\" />",
                                IfReadOnly = false,
                            },
                            new Mapping
                            {
                                Type = "String",
                                NameContains = string.Empty,
                                Output = "<TextBlock Text=\"$name$\" />",
                                IfReadOnly = true,
                            },
                            new Mapping
                            {
                                Type = "int|Integer",
                                NameContains = string.Empty,
                                Output = "<Slider Minimum=\"0\" Maximum=\"100\" x:Name=\"$name$\" Value=\"{x:Bind $name$, Mode=TwoWay}\" />",
                                IfReadOnly = false,
                            },
                            new Mapping
                            {
                                Type = "List<string>",
                                NameContains = string.Empty,
                                Output = "<ItemsControl ItemsSource=\"{x:Bind $name$}\"></ItemsControl>",
                                IfReadOnly = false,
                            },
                        },
                    },

                    new Profile
                    {
                        Name = "UWP (with labels)",
                        ClassGrouping = "Grid-plus-RowDefs",
                        FallbackOutput = "<TextBlock Text=\"$name$\" Grid.Row=\"$incint$\"><TextBlock Text=\"FALLBACK_$name$\" Grid.Row=\"$incint$\" />",
                        SubPropertyOutput = "<TextBlock Text=\"$name$\" Grid.Row=\"$incint$\"><TextBlock Text=\"SUBPROP_$name$\" Grid.Row=\"$incint$\" />",
                        Mappings = new ObservableCollection<Mapping>
                        {
                            new Mapping
                            {
                                Type = "String",
                                NameContains = "password|pwd",
                                Output = "<TextBlock Text=\"$name$\" Grid.Row=\"$incint$\"><PasswordBox Password=\"{x:Bind $name$}\" Grid.Row=\"$incint$\" />",
                                IfReadOnly = false,
                            },
                            new Mapping
                            {
                                Type = "String",
                                NameContains = string.Empty,
                                Output = "<TextBlock Text=\"$name$\" Grid.Row=\"$incint$\"><TextBox Text=\"{x:Bind $name$, Mode=TwoWay}\" Grid.Row=\"$incint$\" />",
                                IfReadOnly = false,
                            },
                            new Mapping
                            {
                                Type = "String",
                                NameContains = string.Empty,
                                Output = "<TextBlock Text=\"$name$\" Grid.Row=\"$incint$\"><TextBlock Text=\"$name$\" Grid.Row=\"$incint$\" />",
                                IfReadOnly = true,
                            },
                            new Mapping
                            {
                                Type = "int|Integer",
                                NameContains = string.Empty,
                                Output = "<TextBlock Text=\"$name$\" Grid.Row=\"$incint$\"><Slider Minimum=\"0\" Maximum=\"100\" x:Name=\"$name$\" Value=\"{x:Bind $name$, Mode=TwoWay}\" />",
                                IfReadOnly = false,
                            },
                            new Mapping
                            {
                                Type = "List<string>",
                                NameContains = string.Empty,
                                Output = "<TextBlock Text=\"$name$\" Grid.Row=\"$incint$\"><ItemsControl ItemsSource=\"{x:Bind $name$}\"></ItemsControl>",
                                IfReadOnly = false,
                            },
                        },
                    },

                    new Profile
                    {
                        Name = "WPF",
                        ClassGrouping = "StackPanel",
                        FallbackOutput = "<TextBlock Text=\"$name$\" Grid.Row=\"$incint$\"><TextBlock Text=\"FALLBACK_$name$\" Grid.Row=\"$incint$\" />",
                        SubPropertyOutput = "<TextBlock Text=\"$name$\" Grid.Row=\"$incint$\"><TextBlock Text=\"SUBPROP_$name$\" Grid.Row=\"$incint$\" />",
                        Mappings = new ObservableCollection<Mapping>
                        {
                            new Mapping
                            {
                                Type = "String",
                                NameContains = string.Empty,
                                Output = "<TextBlock Text=\"$name$\" Grid.Row=\"$incint$\"><TextBox Text=\"{x:Bind $name$, Mode=TwoWay}\" Grid.Row=\"$incint$\" />",
                                IfReadOnly = false,
                            },
                            new Mapping
                            {
                                Type = "String",
                                NameContains = string.Empty,
                                Output = "<TextBlock Text=\"$name$\" Grid.Row=\"$incint$\"><TextBlock Text=\"$name$\" Grid.Row=\"$incint$\" />",
                                IfReadOnly = true,
                            },
                        },
                    },

                    new Profile
                    {
                        Name = "Xamarin.Forms",
                        ClassGrouping = "StackLayout",
                        FallbackOutput = "<TextBlock Text=\"$name$\" Grid.Row=\"$incint$\"><TextBlock Text=\"FALLBACK_$name$\" Grid.Row=\"$incint$\" />",
                        SubPropertyOutput = "<TextBlock Text=\"$name$\" Grid.Row=\"$incint$\"><TextBlock Text=\"SUBPROP_$name$\" Grid.Row=\"$incint$\" />",
                        Mappings = new ObservableCollection<Mapping>
                        {
                            new Mapping
                            {
                                Type = "String",
                                NameContains = "password|pwd",
                                Output = "<TextBlock Text=\"$name$\" Grid.Row=\"$incint$\"><PasswordBox Password=\"{x:Bind $name$}\" Grid.Row=\"$incint$\" />",
                                IfReadOnly = false,
                            },
                        },
                    },
                },
            };
        }

        protected void EachPositionBetweenStarsShouldProduceExpected(string code, AnalyzerOutput expected, bool isCSharp, Profile profileOverload)
        {
            this.EnsureTwoStars(code);

            var (startPos, endPos, actualCode) = this.GetCodeAndCursorRange(code);

            var syntaxTree = isCSharp ? CSharpSyntaxTree.ParseText(actualCode)
                                      : VisualBasicSyntaxTree.ParseText(actualCode);

            Assert.IsNotNull(syntaxTree);

            var semModel = isCSharp ? CSharpCompilation.Create(string.Empty).AddSyntaxTrees(syntaxTree).GetSemanticModel(syntaxTree, ignoreAccessibility: true)
                                    : VisualBasicCompilation.Create(string.Empty).AddSyntaxTrees(syntaxTree).GetSemanticModel(syntaxTree, ignoreAccessibility: true);

            var positionsTested = 0;

            for (var pos = startPos; pos < endPos; pos++)
            {
                var indent = new TestVisualStudioAbstraction().XamlIndent;
                var analyzer = isCSharp ? new CSharpAnalyzer(DefaultTestLogger.Create(), indent) as IDocumentAnalyzer
                                        : new VisualBasicAnalyzer(DefaultTestLogger.Create(), indent);

                var actual = analyzer.GetSingleItemOutput(syntaxTree.GetRoot(), semModel, pos, profileOverload);

                Assert.AreEqual(expected.OutputType, actual.OutputType, $"Failure at {pos} ({startPos}-{endPos})");
                Assert.AreEqual(expected.Name, actual.Name, $"Failure at {pos} ({startPos}-{endPos})");
                StringAssert.AreEqual(expected.Output, actual.Output, $"Failure at {pos} ({startPos}-{endPos})");

                positionsTested += 1;
            }

            this.TestContext.WriteLine($"{positionsTested} different positions tested.");
        }

        protected void PositionAtStarShouldProduceExpected(string code, AnalyzerOutput expected, bool isCSharp, Profile profileOverload)
        {
            this.EnsureOneStar(code);

            var (pos, actualCode) = this.GetCodeAndCursorPos(code);

            var syntaxTree = isCSharp ? CSharpSyntaxTree.ParseText(actualCode)
                                     : VisualBasicSyntaxTree.ParseText(actualCode);

            Assert.IsNotNull(syntaxTree);

            var semModel = isCSharp ? CSharpCompilation.Create(string.Empty).AddSyntaxTrees(syntaxTree).GetSemanticModel(syntaxTree, true)
                                    : VisualBasicCompilation.Create(string.Empty).AddSyntaxTrees(syntaxTree).GetSemanticModel(syntaxTree, true);

            var indent = new TestVisualStudioAbstraction().XamlIndent;
            var analyzer = isCSharp ? new CSharpAnalyzer(DefaultTestLogger.Create(), indent) as IDocumentAnalyzer
                                    : new VisualBasicAnalyzer(DefaultTestLogger.Create(), indent);

            var actual = analyzer.GetSingleItemOutput(syntaxTree.GetRoot(), semModel, pos, profileOverload);

            this.AssertOutput(expected, actual);
        }

        protected void PositionAtStarShouldProduceExpectedUsingAdditonalFiles(string code, AnalyzerOutput expected, bool isCSharp, Profile profileOverload, params string[] additionalCode)
        {
            this.EnsureOneStar(code);

            var (pos, actualCode) = this.GetCodeAndCursorPos(code);

            var projectId = ProjectId.CreateNewId();
            var documentId = DocumentId.CreateNewId(projectId);

            var language = isCSharp ? LanguageNames.CSharp : LanguageNames.VisualBasic;
            var fileExt = isCSharp ? "cs" : "vb";

            var solution = new AdhocWorkspace().CurrentSolution
                                               .AddProject(projectId, "MyProject", "MyProject", language)
                                               .AddDocument(documentId, $"MyFile.{fileExt}", actualCode);

            foreach (var addCode in additionalCode)
            {
                solution = solution.AddDocument(DocumentId.CreateNewId(projectId), $"{System.IO.Path.GetRandomFileName()}.{fileExt}", addCode);
            }

            var document = solution.GetDocument(documentId);

            var semModel = document.GetSemanticModelAsync().Result;
            var syntaxTree = document.GetSyntaxTreeAsync().Result;

            var indent = new TestVisualStudioAbstraction().XamlIndent;

            var analyzer = isCSharp ? new CSharpAnalyzer(DefaultTestLogger.Create(), indent) as IDocumentAnalyzer
                                    : new VisualBasicAnalyzer(DefaultTestLogger.Create(), indent);

            var actual = analyzer.GetSingleItemOutput(syntaxTree.GetRoot(), semModel, pos, profileOverload);

            this.AssertOutput(expected, actual);
        }

        protected void PositionAtStarShouldProduceExpectedUsingAdditonalReferences(string code, AnalyzerOutput expected, bool isCSharp, Profile profileOverload, params string[] additionalReferences)
        {
            this.EnsureOneStar(code);

            var (pos, actualCode) = this.GetCodeAndCursorPos(code);

            var projectId = ProjectId.CreateNewId();
            var documentId = DocumentId.CreateNewId(projectId);

            var language = isCSharp ? LanguageNames.CSharp : LanguageNames.VisualBasic;
            var fileExt = isCSharp ? "cs" : "vb";

            var solution = new AdhocWorkspace().CurrentSolution
                                               .AddProject(projectId, "MyProject", "MyProject", language)
                                               .AddDocument(documentId, $"MyFile.{fileExt}", actualCode);

            foreach (var addRef in additionalReferences)
            {
                var lib = MetadataReference.CreateFromFile(Type.GetType(addRef)?.Assembly.Location);

                solution = solution.AddMetadataReference(projectId, lib);
            }

            var document = solution.GetDocument(documentId);

            var semModel = document.GetSemanticModelAsync().Result;
            var syntaxTree = document.GetSyntaxTreeAsync().Result;

            var indent = new TestVisualStudioAbstraction().XamlIndent;

            var analyzer = isCSharp ? new CSharpAnalyzer(DefaultTestLogger.Create(), indent) as IDocumentAnalyzer
                                    : new VisualBasicAnalyzer(DefaultTestLogger.Create(), indent);

            var actual = analyzer.GetSingleItemOutput(syntaxTree.GetRoot(), semModel, pos, profileOverload);

            this.AssertOutput(expected, actual);
        }

        protected void PositionAtStarShouldProduceExpectedUsingAdditonalLibraries(string code, AnalyzerOutput expected, bool isCSharp, Profile profileOverload, params string[] additionalLibraryPaths)
        {
            this.EnsureOneStar(code);

            var (pos, actualCode) = this.GetCodeAndCursorPos(code);

            var projectId = ProjectId.CreateNewId();
            var documentId = DocumentId.CreateNewId(projectId);

            var language = isCSharp ? LanguageNames.CSharp : LanguageNames.VisualBasic;
            var fileExt = isCSharp ? "cs" : "vb";

            var solution = new AdhocWorkspace().CurrentSolution
                                               .AddProject(projectId, "MyProject", "MyProject", language)
                                               .AddDocument(documentId, $"MyFile.{fileExt}", actualCode);

            foreach (var libPath in additionalLibraryPaths)
            {
                var lib = MetadataReference.CreateFromFile(libPath);

                solution = solution.AddMetadataReference(projectId, lib);
            }

            var document = solution.GetDocument(documentId);

            var semModel = document.GetSemanticModelAsync().Result;
            var syntaxTree = document.GetSyntaxTreeAsync().Result;

            var indent = new TestVisualStudioAbstraction().XamlIndent;

            var analyzer = isCSharp ? new CSharpAnalyzer(DefaultTestLogger.Create(), indent) as IDocumentAnalyzer
                                    : new VisualBasicAnalyzer(DefaultTestLogger.Create(), indent);

            var actual = analyzer.GetSingleItemOutput(syntaxTree.GetRoot(), semModel, pos,  profileOverload);

            this.AssertOutput(expected, actual);
        }

        protected void SelectionBetweenStarsShouldProduceExpected(string code, AnalyzerOutput expected, bool isCSharp, Profile profileOverload)
        {
            this.EnsureTwoStars(code);

            var (startPos, endPos, actualCode) = this.GetCodeAndCursorRange(code);

            var syntaxTree = isCSharp ? CSharpSyntaxTree.ParseText(actualCode)
                                      : VisualBasicSyntaxTree.ParseText(actualCode);

            Assert.IsNotNull(syntaxTree);

            var semModel = isCSharp ? CSharpCompilation.Create(string.Empty).AddSyntaxTrees(syntaxTree).GetSemanticModel(syntaxTree, true)
                                    : VisualBasicCompilation.Create(string.Empty).AddSyntaxTrees(syntaxTree).GetSemanticModel(syntaxTree, true);

            var indent = new TestVisualStudioAbstraction().XamlIndent;

            var analyzer = isCSharp ? new CSharpAnalyzer(DefaultTestLogger.Create(), indent) as IDocumentAnalyzer
                                    : new VisualBasicAnalyzer(DefaultTestLogger.Create(), indent);

            var actual = analyzer.GetSelectionOutput(syntaxTree.GetRoot(), semModel, startPos, endPos,  profileOverload);

            this.AssertOutput(expected, actual);
        }

        private void AssertOutput(AnalyzerOutput expected, AnalyzerOutput actual)
        {
            Assert.AreEqual(expected.OutputType, actual.OutputType);
            Assert.AreEqual(expected.Name, actual.Name);
            StringAssert.AreEqual(expected.Output, actual.Output);
        }

        private (int startPos, int endPos, string actualCode) GetCodeAndCursorRange(string code)
        {
            var start = code.IndexOf("☆", StringComparison.Ordinal);
            var end = code.LastIndexOf("☆", StringComparison.Ordinal) - 1;

            var codeWithoutStar = code.Replace("☆", string.Empty);

            return (start, end, codeWithoutStar);
        }

        private (int cursorPos, string actualCode) GetCodeAndCursorPos(string code)
        {
            var pos = code.IndexOf("☆", StringComparison.Ordinal);

            var codeWithoutStar = code.Replace("☆", string.Empty);

            return (pos, codeWithoutStar);
        }

        private void EnsureOneStar(string code)
        {
            Assert.AreEqual(1, code.Count(c => c.ToString() == "☆"));
        }

        private void EnsureTwoStars(string code)
        {
            Assert.AreEqual(2, code.Count(c => c.ToString() == "☆"));
        }
    }
}
