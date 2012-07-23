namespace System.Web.TestUtil
{
    using System;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Globalization;

    public static class UnitTestHelper
    {

        public static NameValueCollection ToNameValue(object opaque)
        {
            if (opaque != null)
            {
                NameValueCollection collection = new NameValueCollection();
                foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(opaque))
                {
                    collection.Add(property.Name.Replace('_', '-'), property.GetValue(opaque).ToString());
                }
                return collection;
            }
            return null;
        }

        public static bool EnglishBuildAndOS
        {
            get
            {
                bool englishBuild = String.Equals(CultureInfo.CurrentCulture.TwoLetterISOLanguageName, "en",
                    StringComparison.OrdinalIgnoreCase);
                bool englishOS = String.Equals(CultureInfo.CurrentCulture.TwoLetterISOLanguageName, "en",
                    StringComparison.OrdinalIgnoreCase);
                return englishBuild && englishOS;
            }
        }
    }
}
