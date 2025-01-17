﻿using System.IO;
using System.Text;

namespace OpenDreamRuntime.Resources {
    [Virtual]
    public class DreamResource {
        public string ResourcePath;
        public byte[]? ResourceData {
            get {
                if (_resourceData == null && Exists()) {
                    _resourceData = File.ReadAllBytes(_filePath);
                }

                return _resourceData;
            }
        }

        private string _filePath;
        private byte[]? _resourceData = null;

        public DreamResource(string filePath, string resourcePath) {
            _filePath = filePath;
            ResourcePath = resourcePath;
        }

        public bool Exists() {
            return File.Exists(_filePath);
        }

        public virtual string? ReadAsString() {
            if (ResourceData == null) return null;

            string resourceString = Encoding.ASCII.GetString(ResourceData);

            resourceString = resourceString.Replace("\r\n", "\n");
            return resourceString;
        }

        public virtual void Clear() {
            CreateDirectory();
            File.WriteAllText(_filePath, String.Empty);
        }

        public virtual void Output(DreamValue value) {
            if (value.TryGetValueAsString(out string? text)) {
                string filePath = Path.Combine(IoCManager.Resolve<DreamResourceManager>().RootPath, ResourcePath);

                CreateDirectory();
                File.AppendAllText(filePath, text + "\r\n");
                _resourceData = null;
            } else {
                throw new Exception("Invalid output operation on '" + ResourcePath + "'");
            }
        }

        private void CreateDirectory() {
            Directory.CreateDirectory(Path.GetDirectoryName(_filePath));
        }

        public override string? ToString() {
            return $"'{ResourcePath}'";
        }
    }
}
