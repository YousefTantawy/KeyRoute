using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Forms;

namespace KeyRoute
{
    public class ConfigStore
    {
        private readonly string _path;
        private static readonly JsonSerializerOptions Options = new()
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };

        public ConfigStore(string path)
        {
            _path = path;
        }

        public List<KeyMapping> Load()
        {
            if (!File.Exists(_path))
                return new List<KeyMapping>();

            try
            {
                var json = File.ReadAllText(_path);
                return JsonSerializer.Deserialize<List<KeyMapping>>(json, Options) ?? new List<KeyMapping>();
            }
            catch (JsonException)
            {
                return new List<KeyMapping>();
            }
        }

        public void Save(List<KeyMapping> mappings)
        {
            var json = JsonSerializer.Serialize(mappings, Options);
            File.WriteAllText(_path, json);
        }
    }
}
