#nullable enable
using DANCustomTools.Core.Services;
using DANCustomTools.Models.PropertiesEditor;
using PluginCommon;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DANCustomTools.Services
{
    public class PropertiesEditorService : EnginePluginServiceBase, IPropertiesEditorService
    {
        private string _dataPath = string.Empty;
        private const string MSG_PROPERTIES = "Properties";
        private const string MSG_DUMP_TO_FILE = "DumpToFile";
        private const string MSG_CLEAR = "Clear";
        private const string MSG_GET_SESSION_INFO = "getSessionInfo";
        private const uint INVALID_OBJREF = 0;

        public override string PluginName => "PropertiesEditor_Plugin";

        public event EventHandler<PropertyModel>? PropertiesUpdated;
        public event EventHandler<string>? DataPathUpdated;

        public string DataPath => _dataPath;

        public PropertiesEditorService(ILogService logService, IEngineHostService engineHost)
            : base(logService, engineHost)
        {
        }



        public void RequestObjectProperties(uint objectRef)
        {
            LogService.Info($"Requesting properties for object ref: {objectRef}");
        }

        public void SendPropertiesUpdate(uint objectRef, string xmlData)
        {
            if (!IsConnected || objectRef == INVALID_OBJREF)
                return;

            SendMessage(blob =>
            {
                blob.push(MSG_PROPERTIES);
                blob.push(objectRef);
                blob.push(xmlData);
            });

            LogService.Info($"Sent property update for object ref: {objectRef}");
        }

        public void ClearProperties()
        {
            var propertyModel = new PropertyModel();
            propertyModel.Clear();
            PropertiesUpdated?.Invoke(this, propertyModel);
        }

        public void DumpToFile(string fileName)
        {
            if (!IsConnected)
                return;

            SendMessage(blob =>
            {
                blob.push(MSG_DUMP_TO_FILE);
                blob.push(fileName);
            });

            LogService.Info($"Requested dump to file: {fileName}");
        }

        protected override int GetNetworkLoopSleepInterval() => 1000;

        protected override void OnPluginRegistered()
        {
            SendGetSessionInfo();
        }

        protected override void ProcessMessage(blobWrapper blob)
        {
            string message = "";
            blob.extract(ref message);

            switch (message)
            {
                case MSG_PROPERTIES:
                    ProcessPropertiesMessage(blob);
                    break;
                case MSG_CLEAR:
                    ProcessClearMessage();
                    break;
                case MSG_GET_SESSION_INFO:
                    ProcessSessionInfoMessage(blob);
                    break;
            }
        }

        private void ProcessPropertiesMessage(blobWrapper blob)
        {
            uint objectRef = INVALID_OBJREF;
            string xmlData = "";

            blob.extract(ref objectRef);
            blob.extractString8(ref xmlData);

            var propertyModel = new PropertyModel
            {
                ObjectRef = objectRef,
                XmlData = xmlData,
                IsConnected = IsConnected
            };

            PropertiesUpdated?.Invoke(this, propertyModel);
            LogService.Info($"Received properties for object ref: {objectRef}");
        }

        private void ProcessClearMessage()
        {
            ClearProperties();
            LogService.Info("Cleared properties");
        }

        private void ProcessSessionInfoMessage(blobWrapper blob)
        {
            string dataPath = "";
            blob.extract(ref dataPath);

            _dataPath = dataPath;
            DataPathUpdated?.Invoke(this, dataPath);
            LogService.Info($"Received data path: {dataPath}");
        }


        private void SendGetSessionInfo()
        {
            SendMessage(blob => blob.push(MSG_GET_SESSION_INFO));
        }

    }
}
