using System;
using System.IO;
using System.Xml.Serialization;

namespace AllplanBimplusDemo.Classes
{
    public class ApplicationSettings
    {
        public ApplicationSettings()
        {
            string settingsPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\BimPlusDemo";

            if (!Directory.Exists(settingsPath))
                Directory.CreateDirectory(settingsPath);

            if (Directory.Exists(settingsPath))
            {
                _settingsFileName = Path.Combine(settingsPath, "Settings.xml");
            }
        }

        #region private member

        private string _settingsFileName;

        private TextWriter _logger;

        #endregion private member

        #region public methodes

        public void LoadSettings()
        {
            if (File.Exists(_settingsFileName))
            {
                var serializer = new XmlSerializer(typeof(ApplicationSettings));

                using (var reader = new StreamReader(_settingsFileName))
                {
                    try
                    {
                        ApplicationSettings settings = (ApplicationSettings)serializer.Deserialize(reader);
                        if (settings != null)
                        {
                            Structures_ListenToBimplus = settings.Structures_ListenToBimplus;
                        }
                    }
                    catch (Exception e)
                    {
                        Log("LoginSettings.LoadSettings: " + _settingsFileName + ".Exception: " + e.Message);
                    }
                }
            }
        }

        public void SaveSettings()
        {
            string path = Path.GetDirectoryName(_settingsFileName);
            if (path != null && !Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(ApplicationSettings));
                using (StreamWriter writer = new StreamWriter(_settingsFileName))
                {
                    serializer.Serialize(writer, this);
                    writer.Close();
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLine(string.Format("SaveSettings {0}", ex.Message));
            }
        }

        public void SetLogger(TextWriter logger)
        {
            _logger = logger;
        }

        #endregion public methodes

        #region private methods

        private void Log(string message)
        {
            if (_logger != null)
                _logger.WriteLine(message);
        }

        #endregion private methods

        #region properties

        public bool Structures_ListenToBimplus { get; set; }

        #endregion properties
    }
}
