using System;
#nullable enable

namespace DANCustomTools.Models.SceneExplorer
{
    public class ActorModel : ObjectWithRefModel
    {
        private string _components = string.Empty;
        private string _luaPath = string.Empty;

        public string Components
        {
            get => _components;
            set => SetProperty(ref _components, value);
        }

        public string LuaPath
        {
            get => _luaPath;
            set => SetProperty(ref _luaPath, value);
        }

        public override void Validate()
        {
            base.Validate();

            if (string.IsNullOrWhiteSpace(Components))
                throw new ArgumentException("Components cannot be empty");
        }
    }
}
