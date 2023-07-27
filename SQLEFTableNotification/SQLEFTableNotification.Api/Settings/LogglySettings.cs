using Loggly.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SQLEFTableNotification.Api.Settings
{
    public class LogglySettings
    {
        public string ApplicationName { get; set; }
        public string Account { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public int EndpointPort { get; set; }
        public bool IsEnabled { get; set; }
        public bool ThrowExceptions { get; set; }
        public LogTransport LogTransport { get; set; }
        public string EndpointHostname { get; set; }
        public string CustomerToken { get; set; }
    }



}
