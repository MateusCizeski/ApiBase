﻿using ApiBase.Core.Common.Resolvers;
using System.Linq.Expressions;
using System.Reflection;

namespace ApiBase.Core.Common.Bindings
{
    public interface IBindingResolver
    {
        MemberAssignment Resolve(MemberInitResolver resolver, int depth, Expression source, PropertyInfo srcProp, PropertyInfo destProp);
    }
}
