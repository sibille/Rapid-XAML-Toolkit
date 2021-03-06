﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.VisualStudio.Shell;
using RapidXamlToolkit.Logging;
using RapidXamlToolkit.Resources;

namespace RapidXamlToolkit.Options
{
    public partial class SettingsControl
    {
        private ConfiguredSettings settings;
        private bool disabled = false;

        public SettingsControl()
        {
            this.InitializeComponent();
        }

        public ConfiguredSettings SettingsProvider
        {
            get
            {
                return this.settings;
            }

            set
            {
                this.settings = value;
                this.DataContext = value.ActualSettings;
            }
        }

        public void DisableButtonsForEmulator()
        {
            this.disabled = true;
        }

        private void SetActiveClicked(object sender, RoutedEventArgs e)
        {
            if (this.disabled)
            {
                return;
            }

            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                var selectedIndex = this.DisplayedProfiles.SelectedIndex;

                if (selectedIndex >= 0)
                {
                    var selectedProfile = this.SettingsProvider.ActualSettings.ProfilesList[this.DisplayedProfiles.SelectedIndex];

                    if (!selectedProfile.IsActive)
                    {
                        this.SettingsProvider.ActualSettings.ActiveProfileName = selectedProfile.Name;
                        this.SettingsProvider.Save();
                        this.SettingsProvider.ActualSettings.RefreshProfilesList();

                        this.DisplayedProfiles.SelectedIndex = selectedIndex;
                    }
                }
            }
            catch (Exception exc)
            {
                RapidXamlPackage.Logger?.RecordException(exc);
                throw;  // Remove for launch. see issue #90
            }
        }

        private void AddClicked(object sender, RoutedEventArgs e)
        {
            if (this.disabled)
            {
                return;
            }

            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                var selectedIndex = this.DisplayedProfiles.SelectedIndex;

                this.SettingsProvider.ActualSettings.Profiles.Add(Profile.CreateNew());
                this.SettingsProvider.Save();
                this.SettingsProvider.ActualSettings.RefreshProfilesList();

                this.DisplayedProfiles.SelectedIndex = selectedIndex;
            }
            catch (Exception exc)
            {
                RapidXamlPackage.Logger?.RecordException(exc);
                throw;  // Remove for launch. see issue #90
            }
        }

        private void EditClicked(object sender, RoutedEventArgs e)
        {
            if (this.disabled)
            {
                return;
            }

            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                var selectedIndex = this.DisplayedProfiles.SelectedIndex;

                if (selectedIndex >= 0)
                {
                    var dialog = new ProfileConfigPage();
                    dialog.SetDataContext(this.SettingsProvider.ActualSettings.Profiles[this.DisplayedProfiles.SelectedIndex]);

                    dialog.ShowModal();

                    this.SettingsProvider.Save();
                    this.SettingsProvider.ActualSettings.RefreshProfilesList();

                    this.DisplayedProfiles.SelectedIndex = selectedIndex;
                }
            }
            catch (Exception exc)
            {
                RapidXamlPackage.Logger?.RecordException(exc);
                throw;  // Remove for launch. see issue #90
            }
        }

        private void CopyClicked(object sender, RoutedEventArgs e)
        {
            if (this.disabled)
            {
                return;
            }

            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                var selectedIndex = this.DisplayedProfiles.SelectedIndex;

                if (selectedIndex >= 0)
                {
                    var copy = (Profile)this.SettingsProvider.ActualSettings.Profiles[this.DisplayedProfiles.SelectedIndex].Clone();

                    this.SettingsProvider.ActualSettings.Profiles.Add(copy);
                    this.SettingsProvider.Save();
                    this.SettingsProvider.ActualSettings.RefreshProfilesList();

                    this.DisplayedProfiles.SelectedIndex = selectedIndex;
                }
            }
            catch (Exception exc)
            {
                RapidXamlPackage.Logger?.RecordException(exc);
                throw;  // Remove for launch. see issue #90
            }
        }

        private void DeleteClicked(object sender, RoutedEventArgs e)
        {
            if (this.disabled)
            {
                return;
            }

            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                if (this.DisplayedProfiles.SelectedIndex >= 0)
                {
                    var selectedProfile = this.SettingsProvider.ActualSettings.ProfilesList[this.DisplayedProfiles.SelectedIndex];
                    var msgResult = MessageBox.Show(
                                                    StringRes.Prompt_ConfirmDeleteProfileMessage.WithParams(selectedProfile.Name),
                                                    StringRes.Prompt_ConfirmDeleteProfileTitle,
                                                    MessageBoxButton.YesNo,
                                                    MessageBoxImage.Warning);

                    if (msgResult == MessageBoxResult.Yes)
                    {
                        this.SettingsProvider.ActualSettings.Profiles.RemoveAt(this.DisplayedProfiles.SelectedIndex);

                        if (selectedProfile.Name == this.SettingsProvider.ActualSettings.ActiveProfileName)
                        {
                            var firstProfile = this.SettingsProvider.ActualSettings.Profiles.FirstOrDefault();

                            this.SettingsProvider.ActualSettings.ActiveProfileName = firstProfile?.Name ?? string.Empty;
                        }

                        this.SettingsProvider.Save();
                        this.SettingsProvider.ActualSettings.RefreshProfilesList();
                    }
                }
            }
            catch (Exception exc)
            {
                RapidXamlPackage.Logger?.RecordException(exc);
                throw;  // Remove for launch. see issue #90
            }
        }

        private async void ImportClicked(object sender, RoutedEventArgs e)
        {
            if (this.disabled)
            {
                return;
            }

            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                var openFileDialog = new System.Windows.Forms.OpenFileDialog
                {
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    Filter = $"{StringRes.UI_ProfileFilterDescription} (*.rxprofile)|*.rxprofile",
                    Multiselect = false,
                };

                if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    var fileContents = File.ReadAllText(openFileDialog.FileName);

                    var analyzer = new ApiAnalysis.SimpleJsonAnalyzer();

                    var analyzerResults = await analyzer.AnalyzeJsonAsync(fileContents, typeof(Profile));

                    if (analyzerResults.Count == 1 && analyzerResults.First() == analyzer.MessageBuilder.AllGoodMessage)
                    {
                        var profile = Newtonsoft.Json.JsonConvert.DeserializeObject<Profile>(fileContents);

                        this.SettingsProvider.ActualSettings.Profiles.Add(profile);
                        this.SettingsProvider.Save();
                        this.SettingsProvider.ActualSettings.RefreshProfilesList();
                    }
                    else
                    {
                        MessageBox.Show(
                                        StringRes.Prompt_ImportFailedMessage.WithParams(string.Join(Environment.NewLine + "- ", analyzerResults)),
                                        StringRes.Prompt_ImportFailedTitle,
                                        MessageBoxButton.OK,
                                        MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception exc)
            {
                RapidXamlPackage.Logger?.RecordException(exc);
                throw;  // Remove for launch. see issue #90
            }
        }

        private void ExportClicked(object sender, RoutedEventArgs e)
        {
            if (this.disabled)
            {
                return;
            }

            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                if (this.DisplayedProfiles.SelectedIndex >= 0)
                {
                    var selectedProfile = this.SettingsProvider.ActualSettings.Profiles[this.DisplayedProfiles.SelectedIndex];
                    var profileJson = selectedProfile.AsJson();

                    var saveFileDialog = new System.Windows.Forms.SaveFileDialog
                    {
                        InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                        Filter = $"{StringRes.UI_ProfileFilterDescription} (*.rxprofile)|*.rxprofile",
                        FileName = $"{selectedProfile.Name}.rxprofile",
                    };

                    if (saveFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        File.WriteAllText(saveFileDialog.FileName, profileJson);
                    }
                }
            }
            catch (Exception exc)
            {
                RapidXamlPackage.Logger?.RecordException(exc);
                throw;  // Remove for launch. see issue #90
            }
        }

        private void ResetClicked(object sender, RoutedEventArgs e)
        {
            if (this.disabled)
            {
                return;
            }

            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                var msgResult = MessageBox.Show(
                    StringRes.Prompt_ConfirmResetProfilesMessage,
                    StringRes.Prompt_ConfirmResetProfilesTitle,
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (msgResult == MessageBoxResult.Yes)
                {
                    this.SettingsProvider.Reset();
                    this.SettingsProvider.Save();
                    this.SettingsProvider.ActualSettings.RefreshProfilesList();
                }
            }
            catch (Exception exc)
            {
                RapidXamlPackage.Logger?.RecordException(exc);
                throw;  // Remove for launch. see issue #90
            }
        }
    }
}
