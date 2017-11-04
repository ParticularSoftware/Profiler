﻿namespace ServiceInsight.Framework.Settings
{
    using System;
    using System.Configuration;

    public static class ApplicationConfiguration
    {
        public static bool SkipCertificateValidation { get; private set; }

        public static void Initialize()
        {
            SkipCertificateValidation = GetSkipCertificateValidation();
        }

        private static bool GetSkipCertificateValidation()
        {
            try
            {
                return Convert.ToBoolean(ConfigurationManager.AppSettings["SkipCertificateValidation"] ?? "false");
            }
            catch (Exception ex)
            {
                throw new ConfigurationErrorsException("Application setting 'SkipCertificateValidation' cannot be converted to type Boolean.", ex);
            }
        }
    }
}
