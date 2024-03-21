using InputVisualizer.Usb2Snes;

namespace InputVisualizer.Config
{
    public class GeneralSettings
    {
        public string Usb2SnesServer { get; set; } = Usb2SnesClient.DEFAULT_SERVER;
        public string Usb2SnesPort { get; set; } = Usb2SnesClient.DEFAULT_PORT;
    }
}
