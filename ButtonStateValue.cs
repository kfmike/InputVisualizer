using System;

namespace InputVisualizer
{
    public class ButtonStateValue
    {
        public bool IsPressed { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool Completed { get; set; }
        public int StartFrame { get; set; }
    }
}
