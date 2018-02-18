﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Reflection;
using Newtonsoft.Json;
#if NET45
using Microsoft.Win32;
#endif

namespace Stripe.Infrastructure
{
    internal class Client
    {
        private HttpRequestMessage RequestMessage { get; set; }

        public Client(HttpRequestMessage requestMessage)
        {
            RequestMessage = requestMessage;
        }

        public void ApplyUserAgent()
        {
            RequestMessage.Headers.UserAgent.ParseAdd($"Stripe/v1 .NetBindings/{StripeConfiguration.StripeNetVersion}");
        }

        public void ApplyClientData()
        {
            RequestMessage.Headers.Add("X-Stripe-Client-User-Agent", GetClientUserAgentString());
        }

        private string GetClientUserAgentString()
        {
            var langVersion = "4.5";

#if NET45
            langVersion = typeof(object).GetTypeInfo().Assembly.ImageRuntimeVersion;
#endif

            var mono = testForMono();
            if (!string.IsNullOrEmpty(mono)) langVersion = mono;

            var values = new Dictionary<string, string>
            {
                { "bindings_version", StripeConfiguration.StripeNetVersion },
                { "lang", ".net" },
                { "publisher", "Jayme Davis" },
                { "lang_version", WebUtility.HtmlEncode(langVersion) },
                //{ "uname", WebUtility.HtmlEncode(getSystemInformation()) }
            };

            return JsonConvert.SerializeObject(values, Formatting.None);
        }

        private string testForMono()
        {
            var type = Type.GetType("Mono.Runtime");
            var getDisplayName = type?.GetTypeInfo().GetDeclaredMethod("GetDisplayName");

            return getDisplayName?.Invoke(null, null).ToString();
        }

        private string getSystemInformation()
        {
            var result = string.Empty;

#if NET45
            result += $"net45.platform: { Environment.OSVersion.VersionString }";
            result += $", {getOperatingSystemInfo()}"; 
            result += $", framework: {getFrameworkFromRegistry()}";
#else
            result += "portable.platform: ";

            try
            {
                result += typeof(object).GetTypeInfo().Assembly.GetCustomAttribute<AssemblyProductAttribute>().Product;
            }
            catch
            {
                result += "unknown";
            }
#endif

            return result;
        }

#if NET45
        private static string getFrameworkFromRegistry()
        {
            using (var key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey("SOFTWARE\\Microsoft\\NET Framework Setup\\NDP\\v4\\Full\\"))
            {
                var releaseKey = Convert.ToInt32(key.GetValue("Release"));
                return get45Version(releaseKey);
            }
        }

        private static string get45Version(int releaseKey)
        {
            if (releaseKey >= 393273)
                return "4.6 RC or later";

            if (releaseKey >= 379893)
                return "4.5.2";

            if (releaseKey >= 378675)
                return "4.5.1";

            if (releaseKey >= 378389)
                return "4.5";

            return "4.5 not detected! wat?";
        }

        private string getOperatingSystemInfo()
        {
            var os = Environment.OSVersion;
            var pid = os.Platform;

            switch (pid)
            {
                case PlatformID.Win32NT:
                case PlatformID.Win32S:
                case PlatformID.Win32Windows:
                case PlatformID.WinCE:
                    return "OS: Windows";
                case PlatformID.Unix:
                    return "OS: Unix";
                default:
                    return "OS: Unknown";
            }
        }
#endif

    }
}