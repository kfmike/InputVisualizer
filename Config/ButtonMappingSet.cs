using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using System.Linq;

namespace InputVisualizer.Config
{
    public class ButtonMappingSet
    {
        public List<ButtonMapping> ButtonMappings { get; set; } = new List<ButtonMapping>();

        public void AddButton(ButtonType buttonType, ButtonType mappedButtonType, Color color)
        {
            ButtonMappings.Add(new ButtonMapping
            {
                ButtonType = buttonType,
                MappedButtonType = mappedButtonType,
                MappingType = ButtonMappingType.Button,
                MappedKey = Keys.None,
                MappedMouseButton = MouseButtonType.None,
                Color = color
            });
        }

        public void MapToKey(ButtonType buttonType, Keys mappedKey)
        {
            var mapping = ButtonMappings.FirstOrDefault(m => m.ButtonType == buttonType);
            if (mapping == null)
            {
                return;
            }
            mapping.MappingType = ButtonMappingType.Key;
            mapping.MappedKey = mappedKey;
            mapping.MappedButtonType = ButtonType.NONE;
        }

        public void MapToMouse(ButtonType buttonType, MouseButtonType mappedMouseButton)
        {
            var mapping = ButtonMappings.FirstOrDefault(m => m.ButtonType == buttonType);
            if (mapping == null)
            {
                return;
            }
            mapping.MappingType = ButtonMappingType.Mouse;
            mapping.MappedMouseButton = mappedMouseButton;
            mapping.MappedKey = Keys.None;
            mapping.MappedButtonType = ButtonType.NONE;
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
