namespace EchoPatcher
{
    internal class MissingResourceException : Exception
    {
        public string ResourceName { get; set; }

        public MissingResourceException(string resourceName)
            : base($"Could not find resource: {resourceName}. Please place the correct file at this path.")
        {
            ResourceName = resourceName;
        }
    }
}
