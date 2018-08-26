using System;

namespace ScreenShooter.Gun.Core
{
    public class ShotResult
    {
        public Byte[] Bytes { get; protected set; }
        public String FileName { get; protected set; }
        public String MimeType { get; protected set; }
    }
}