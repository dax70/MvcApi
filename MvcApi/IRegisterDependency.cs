using System;

namespace MvcApi
{
    public interface IRegisterDependency
    {
        void SetSingle<T>(T instance) where T : class;

        void SetMultiple<T>(params T[] instances) where T : class;
    }
}
