using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace InputVisualizer.Config
{
    public class ButtonMappingSet
    {
        public List<ButtonMapping> ButtonMappings { get; set; } = new List<ButtonMapping>();

        public void AddButton( ButtonType buttonType, ButtonType mappedButtonType, Color color )
        {
            ButtonMappings.Add(new ButtonMapping
            {
                ButtonType = buttonType,
                MappedButtonType = mappedButtonType,
                Color = color
            });
        }

        public void InitOrder()
        {
            for (var i = 0; i < ButtonMappings.Count; i++)
            {
                ButtonMappings[i].Order = i;
            }
        }
    }
}
