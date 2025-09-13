using System;
#nullable enable

namespace DANCustomTools.Models.SceneExplorer
{
    public class FriseModel : ObjectWithRefModel
    {
        private string _configPath = string.Empty;

        public string ConfigPath
        {
            get => _configPath;
            set => SetProperty(ref _configPath, value);
        }

        public override void Validate()
        {
            base.Validate();

            if (string.IsNullOrWhiteSpace(ConfigPath))
                throw new ArgumentException("Config path cannot be empty");
        }
    }
}
