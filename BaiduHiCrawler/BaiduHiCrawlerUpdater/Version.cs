namespace BaiduHiCrawlerUpdater
{
    using System;

    public class Version : IComparable
    {
        public int Major { get; set; }

        public int Minor { get; set; }

        public int Revision { get; set; }

        /// <summary>
        /// Parse version string to Version object. Template of version string: v1.23.456
        /// </summary>
        /// <param name="versionString">The version string</param>
        /// <returns>The Version object</returns>
        public static Version Parse(string versionString)
        {
            // Remove leading "v"
            versionString = versionString.Substring(1);

            // Split into different parts
            var versionParts = versionString.Split('.');
            if (versionParts.Length != 3)
            {
                return null;
            }

            // Get versions
            int major;
            int minor;
            int revision;

            // Major version
            if (!int.TryParse(versionParts[0], out major))
            {
                return null;
            }

            // Minor version
            if (!int.TryParse(versionParts[1], out minor))
            {
                return null;
            }

            // Revision version
            if (!int.TryParse(versionParts[2], out revision))
            {
                return null;
            }

            // Version object
            return new Version { Major = major, Minor = minor, Revision = revision };
        }

        public int CompareTo(object obj)
        {
            if (obj == null)
            {
                return 1;
            }

            var other = obj as Version;
            if (other == null)
            {
                throw new ArgumentException("Object is not a Version");
            }

            if (this.Major != other.Major)
            {
                return this.Major.CompareTo(other.Major);
            }

            if (this.Minor != other.Minor)
            {
                return this.Minor.CompareTo(other.Minor);
            }

            return this.Revision.CompareTo(other.Revision);
        }

        public override string ToString()
        {
            return string.Format("v{0}.{1}.{2}", this.Major, this.Minor, this.Revision);
        }
    }
}
