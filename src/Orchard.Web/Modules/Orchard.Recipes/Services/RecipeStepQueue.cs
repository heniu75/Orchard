﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Orchard.FileSystems.AppData;
using Orchard.Localization;
using Orchard.Logging;
using Orchard.Recipes.Models;

namespace Orchard.Recipes.Services {
    public class RecipeStepQueue : IRecipeStepQueue {
        private readonly IAppDataFolder _appDataFolder;
        private readonly string _recipeQueueFolder = "RecipeQueue" + Path.DirectorySeparatorChar;

        public RecipeStepQueue(IAppDataFolder appDataFolder) {
            _appDataFolder = appDataFolder;
            Logger = NullLogger.Instance;
            T = NullLocalizer.Instance;
        }

        public Localizer T { get; set; }
        public ILogger Logger { get; set; }

        public void Enqueue(string executionId, RecipeStep step) {
            Logger.Information("Enqueuing step '{0}' for recipe {1}.", step.Name, executionId);
            var recipeStepElement = new XElement("RecipeStep");
            recipeStepElement.Add(new XElement("Name", step.Name));
            recipeStepElement.Add(new XElement("FilesPath", step.FilesPath));
            recipeStepElement.Add(step.Step);

            if (_appDataFolder.DirectoryExists(Path.Combine(_recipeQueueFolder, executionId))) {
                int stepIndex = GetLastStepIndex(executionId) + 1;
                _appDataFolder.CreateFile(Path.Combine(_recipeQueueFolder, executionId + Path.DirectorySeparatorChar + stepIndex),
                                          recipeStepElement.ToString());
            }
            else {
                _appDataFolder.CreateFile(
                    Path.Combine(_recipeQueueFolder, executionId + Path.DirectorySeparatorChar + "0"),
                    recipeStepElement.ToString());
            }
        }

        public RecipeStep Dequeue(string executionId) {
            Logger.Information("Dequeuing steps for recipe {0}.", executionId);
            if (!_appDataFolder.DirectoryExists(Path.Combine(_recipeQueueFolder, executionId))) {
                return null;
            }
            RecipeStep recipeStep = null;
            int stepIndex = GetFirstStepIndex(executionId);
            if (stepIndex >= 0) {
                var stepPath = Path.Combine(_recipeQueueFolder, executionId + Path.DirectorySeparatorChar + stepIndex);
                // string to xelement
                var stepElement = XElement.Parse(_appDataFolder.ReadFile(stepPath));
                var stepNameElement = stepElement.Element("Name");
                var stepName = stepNameElement != null ? stepNameElement.Value : null;
                var filesPathElement = stepElement.Element("FilesPath");
                var filesPath = filesPathElement != null ? filesPathElement.Value : null;
                Logger.Information("Dequeuing step '{0}' for recipe {1}.", stepName, executionId);
                recipeStep = new RecipeStep {
                    Name = stepName,
                    FilesPath = filesPath,
                    Step = stepElement.Element(stepName)
                };
                _appDataFolder.DeleteFile(stepPath);
            }

            if (stepIndex < 0) {
                _appDataFolder.DeleteFile(Path.Combine(_recipeQueueFolder, executionId));
            }

            return recipeStep;
        }

        private int GetFirstStepIndex(string executionId) {
            var stepFiles = new List<string>(_appDataFolder.ListFiles(Path.Combine(_recipeQueueFolder, executionId)));
            if (stepFiles.Count == 0)
                return -1;
            var currentSteps = stepFiles.Select(stepFile => Int32.Parse(stepFile.Substring(stepFile.LastIndexOf('/') + 1))).ToList();
            currentSteps.Sort();
            return currentSteps[0];
        }

        private int GetLastStepIndex(string executionId) {
            int lastIndex = -1;
            var stepFiles = _appDataFolder.ListFiles(Path.Combine(_recipeQueueFolder, executionId));
            // we always have only a handful of steps.
            foreach (var stepFile in stepFiles) {
                int stepOrder = Int32.Parse(stepFile.Substring(stepFile.LastIndexOf('/') + 1));
                if (stepOrder > lastIndex)
                    lastIndex = stepOrder;
            }

            return lastIndex;
        }
    }
}
