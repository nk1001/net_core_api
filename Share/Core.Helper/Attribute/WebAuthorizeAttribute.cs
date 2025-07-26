namespace Core.Helper.Attribute
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class WebAuthorizeAttribute:System.Attribute
    {

        public readonly string _key;
        public readonly string _action;
        public WebAuthorizeAttribute(string key = "*", string action = "*") : base()
        {
            _key = key;
            _action = action;
        }
    }
}
