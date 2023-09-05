
namespace InputVisualizer.UI
{
    public class ConfigViewModel
    {
        public ConfigViewModel(string name, object content)
        {
            Name = name;
            Content = content;
        }

        public string Name { get; }
        public object Content { get; }

        public override string ToString()
        {
            return Name;
        }
    }
}
