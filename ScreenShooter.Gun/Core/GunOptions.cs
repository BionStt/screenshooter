using System;
using System.Threading;

namespace ScreenShooter.Gun.Core
{
    public class GunOptions
    {
        public GunOptions(String host)
        {
            Host = host;
        }

        public String Host { get; private set; }
    }
}