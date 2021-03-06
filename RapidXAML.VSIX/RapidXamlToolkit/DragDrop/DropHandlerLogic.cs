﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Threading.Tasks;
using RapidXamlToolkit.Analyzers;
using RapidXamlToolkit.Commands;
using RapidXamlToolkit.Logging;
using RapidXamlToolkit.Options;
using RapidXamlToolkit.Resources;

namespace RapidXamlToolkit.DragDrop
{
    public class DropHandlerLogic
    {
        private readonly Profile profile;
        private readonly ILogger logger;
        private readonly IFileSystemAbstraction fileSystem;
        private readonly IVisualStudioAbstraction vs;

        public DropHandlerLogic(Profile profile, ILogger logger, IVisualStudioAbstraction vs, IFileSystemAbstraction fileSystem = null)
        {
            this.profile = profile ?? throw new ArgumentNullException(nameof(profile));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.vs = vs ?? throw new ArgumentNullException(nameof(vs));
            this.fileSystem = fileSystem ?? new WindowsFileSystem();
        }

        public async Task<string> ExecuteAsync(string draggedFilename, int insertLineLength)
        {
            var fileContents = this.fileSystem.GetAllFileText(draggedFilename);
            var fileExt = this.fileSystem.GetFileExtension(draggedFilename);

            var indent = await this.vs.GetXamlIndentAsync();

            var analyzer = fileExt == ".cs" ? new CSharpAnalyzer(this.logger, indent)
                                            : (IDocumentAnalyzer)new VisualBasicAnalyzer(this.logger, indent);

            // IndexOf is allowing for "class " in C# and "Class " in VB
            var cursorPos = fileContents.IndexOf("lass ");

            if (cursorPos == -1 && fileExt == ".vb")
            {
                // If not a class, there may be a module
                cursorPos = fileContents.IndexOf("odule ");
            }

            if (cursorPos < 0)
            {
                this.logger.RecordInfo(StringRes.Info_CouldNotFindClassInFile.WithParams(draggedFilename));
                return null;
            }

            (var syntaxTree, var semModel) = await this.vs.GetDocumentModelsAsync(draggedFilename);

            var treeRoot = await syntaxTree.GetRootAsync();

            var analyzerOutput = analyzer.GetSingleItemOutput(treeRoot, semModel, cursorPos, this.profile);

            string textOutput = analyzerOutput.Output;

            if (insertLineLength > 0)
            {
                textOutput = textOutput.Replace(Environment.NewLine, Environment.NewLine + new string(' ', insertLineLength));
            }

            return textOutput;
        }
    }
}
