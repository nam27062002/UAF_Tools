#nullable enable
using System;
using DANCustomTools.MVVM;

namespace DANCustomTools.Models.PropertiesEditor
{
    public class PropertyModel : ModelBase
    {
        private uint _objectRef = uint.MaxValue;
        private string _xmlData = string.Empty;
        private string _objectName = string.Empty;
        private bool _isConnected = false;

        public uint ObjectRef
        {
            get => _objectRef;
            set => SetProperty(ref _objectRef, value);
        }

        public string XmlData
        {
            get => _xmlData;
            set => SetProperty(ref _xmlData, value);
        }

        public string ObjectName
        {
            get => _objectName;
            set => SetProperty(ref _objectName, value);
        }

        public bool IsConnected
        {
            get => _isConnected;
            set => SetProperty(ref _isConnected, value);
        }

        public bool HasData => !string.IsNullOrEmpty(_xmlData) && _objectRef != uint.MaxValue;

        public override void Validate()
        {
            // Properties data can be empty when no object is selected
            // No validation needed
        }

        public void Clear()
        {
            ObjectRef = uint.MaxValue;
            XmlData = string.Empty;
            ObjectName = string.Empty;
        }
    }
}
