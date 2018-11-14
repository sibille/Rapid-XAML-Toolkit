﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.IO;
using System.Windows;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.DragDrop;
using Microsoft.VisualStudio.Text.Operations;
using RapidXamlToolkit.Analyzers;
using RapidXamlToolkit.Commands;
using RapidXamlToolkit.Logging;

namespace RapidXamlToolkit.DragDrop
{
    internal class RapidXamlDropHandler : IDropHandler
    {
        private readonly ILogger logger;
        private readonly IWpfTextView view;
        private readonly ITextBufferUndoManager undoManager;
        private readonly IFileSystemAbstraction fileSystem;
        private readonly IVisualStudioAbstraction vs;
        private string draggedFilename;

        public RapidXamlDropHandler(ILogger logger, IWpfTextView view, ITextBufferUndoManager undoManager, IVisualStudioAbstraction vs, IFileSystemAbstraction fileSystem = null)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.view = view;
            this.undoManager = undoManager;
            this.vs = vs ?? throw new ArgumentNullException(nameof(vs));
            this.fileSystem = fileSystem ?? new WindowsFileSystem();
        }

        public DragDropPointerEffects HandleDataDropped(DragDropInfo dragDropInfo)
        {
            var position = dragDropInfo.VirtualBufferPosition.Position;

            var fileContents = this.fileSystem.GetAllFileText(this.draggedFilename);
            var fileExt = this.fileSystem.GetFileExtension(this.draggedFilename);

            // Note. Much of this is also in CreateViewCommandLogic.ExecuteAsync
            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                (var syntaxTree, var semModel) = await this.vs.GetDocumentModelsAsync(this.draggedFilename);

                var indent = await this.vs.GetXamlIndentAsync();

                var analyzer = fileExt == ".cs" ? new CSharpAnalyzer(this.logger, indent)
                                                : (IDocumentAnalyzer)new VisualBasicAnalyzer(this.logger, indent);

                var profile = AnalyzerBase.GetSettings().GetActiveProfile();

                var treeRoot = await syntaxTree.GetRootAsync();

                // IndexOf is allowing for "class " in C# and "Class " in VB
                var analyzerOutput = analyzer.GetSingleItemOutput(treeRoot, semModel, fileContents.IndexOf("lass "), profile);

                string textOutput = analyzerOutput.Output;

                var insertline = this.view.GetTextViewLineContainingBufferPosition(position);

                if (insertline.Length > 0)
                {
                    textOutput = textOutput.Replace(Environment.NewLine, Environment.NewLine + new string(' ', insertline.Length));
                }

                this.view.TextBuffer.Insert(position.Position, textOutput);
            });

            return DragDropPointerEffects.Copy;
        }

        public void HandleDragCanceled()
        {
        }

        public DragDropPointerEffects HandleDragStarted(DragDropInfo dragDropInfo)
        {
            return DragDropPointerEffects.All;
        }

        public DragDropPointerEffects HandleDraggingOver(DragDropInfo dragDropInfo)
        {
            return DragDropPointerEffects.All;
        }

        public bool IsDropEnabled(DragDropInfo dragDropInfo)
        {
            var fileName = GetFilename(dragDropInfo);

            this.draggedFilename = fileName;

            return File.Exists(fileName) && (fileName.EndsWith(".cs") || fileName.EndsWith(".vb"));
        }

        private static string GetFilename(DragDropInfo info)
        {
            DataObject data = new DataObject(info.Data);

            // The drag and drop operation came from the VS solution explorer
            if (info.Data.GetDataPresent("CF_VSSTGPROJECTITEMS"))
            {
                return data.GetText(); // This is the file path
            }

            return null;
        }
    }
}