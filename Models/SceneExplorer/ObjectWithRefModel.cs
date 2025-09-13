using System;
#nullable enable
using DANCustomTools.MVVM;

namespace DANCustomTools.Models.SceneExplorer
{
    public class ObjectWithRefModel : ModelBase
    {
        private uint _objectRef = uint.MaxValue;
        private string _friendlyName = string.Empty;
        private bool _isOnline = false;
        private string _offlineId = string.Empty;

        public uint ObjectRef
        {
            get => _objectRef;
            set => SetProperty(ref _objectRef, value);
        }

        public string FriendlyName
        {
            get => _friendlyName;
            set => SetProperty(ref _friendlyName, value);
        }

        public bool IsOnline
        {
            get => _isOnline;
            set => SetProperty(ref _isOnline, value);
        }

        public string OfflineId
        {
            get => _offlineId;
            set => SetProperty(ref _offlineId, value);
        }

        public override void Validate()
        {
            if (string.IsNullOrWhiteSpace(FriendlyName))
                throw new ArgumentException("Friendly name cannot be empty");
        }
    }
}
