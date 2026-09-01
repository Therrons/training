using System;

namespace fraud_poc_project.CustomAttributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class ValidateXssAttribute : Attribute { }
}
