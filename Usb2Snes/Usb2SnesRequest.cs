
namespace InputVisualizer.Usb2Snes
{
    public class Usb2SnesRequest
    {
        public string Opcode { get; set; }
        public string Space { get; set; }
        public string[] Flags { get; set; } = new string[0];
        public string[] Operands { get; set; } = new string[0];
    }
}
