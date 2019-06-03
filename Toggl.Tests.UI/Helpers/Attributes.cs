using System;

[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class IgnoreOnAndroidAttribute
    #if __DROID__
    : NUnit.Framework.IgnoreAttribute { }
    #else
    : Attribute { }
    #endif

[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class IgnoreOnIosAttribute
    #if __IOS__
        : NUnit.Framework.IgnoreAttribute
    {
    public IgnoreOnIosAttribute() : base("") { }

    public IgnoreOnIosAttribute(string reason) : base(reason) { }

}
#else
    : Attribute { }
#endif

