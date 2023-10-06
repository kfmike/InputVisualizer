/*  
    Copyright (c) RetroSpy Technologies
*/

using System;

namespace InputVisualizer.RetroSpy
{
    [Serializable]
    public class SSHMonitorDisconnectException : Exception
    {
        public SSHMonitorDisconnectException(string message) : base(message)
        {
        }

        public SSHMonitorDisconnectException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public SSHMonitorDisconnectException()
        {
        }

        protected SSHMonitorDisconnectException(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) : base(serializationInfo, streamingContext)
        {
        }
    }
}
