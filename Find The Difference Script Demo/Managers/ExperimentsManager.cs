using CrimsonLibrary.SupportLibrary.Extensions;
using CrimsonLibrary.SupportLibrary.Utils.Generics;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CrimsonGames.CBN.Managers
{
    [Serializable]
    public enum ExperimentNames
    {
    }

    [Serializable]
    public class ExperimentData
    {
        public string name;
        public float appendToVersion;
    }

    public class ExperimentsManager : GenericManager<ExperimentsManager>
    {
        ExperimentDataSettings settings;

        public List<ExperimentNames> activeExperiments = new List<ExperimentNames>();
        public Action onExperimentsLoaded;

        public bool HasActiveExperiments
        {
            get
            {
                return activeExperiments.Count > 0;
            }
        }

        private void Awake()
        {
            base.Awake();
            SettingsManager.Instance.onConfigDownloaded += OnConfigDownloaded;
        }
       
        private void OnConfigDownloaded()
        {
            settings = SettingsManager.Instance.ExperimentData;

            foreach (var experiment in settings.experimentData)
            {
                SetExperimentActive(experiment);
            }
        }

        private void SetExperimentActive(ExperimentData experimentData)
        {
            if (experimentData.name.TryToEnum(out ExperimentNames experimentName))
            {
                activeExperiments.Add(experimentName);
            }

            onExperimentsLoaded.SafeInvoke();
        }
    }
}
