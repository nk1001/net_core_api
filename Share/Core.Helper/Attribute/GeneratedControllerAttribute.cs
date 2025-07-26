namespace Core.Helper.Attribute
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class GeneratedControllerAttribute : System.Attribute
    {
        public GeneratedControllerAttribute(string route, int order=99)
        {
            Route = route;
            Order = order;

        }

        public string Route { get; set; }
        public int Order { get; set; }
    }
}
