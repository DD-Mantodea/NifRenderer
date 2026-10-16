using System.Globalization;

namespace NifRenderer.Extensions
{
    public static class StringExt
    {
        extension(int value)
        {
            public string Format()
            {
                if (value >= 1000000)
                    return (value / 1000000f).ToString("F2", CultureInfo.InvariantCulture) + "M";

                if (value >= 1000)
                    return (value / 1000f).ToString("F1", CultureInfo.InvariantCulture) + "k";

                return value.ToString(CultureInfo.InvariantCulture);
            }
        }
    }
}
