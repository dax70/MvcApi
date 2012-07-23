namespace MvcApi
{
    #region Using Directives
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Web.Mvc;
    using MvcApi.Properties;
    #endregion

    public class ApiControllerDescriptor : ReflectedControllerDescriptor
    {
        #region Fields
        private ActionDescriptor[] _canonicalActionsCache;
        private ConcurrentDictionary<object, object> _properties;
        private readonly ActionMethodSelector _selector; 
        #endregion

        public ApiControllerDescriptor(Type controllerType)
            : base(controllerType)
        {
            this._selector = new ActionMethodSelector(controllerType);
        }

        #region Properties

        public ConcurrentDictionary<object, object> Properties
        {
            get
            {
                if (this._properties == null)
                {
                    this._properties = new ConcurrentDictionary<object, object>();
                }
                return this._properties;
            }
        }
        #endregion

        public override ActionDescriptor FindAction(ControllerContext controllerContext, string actionName)
        {
            if (controllerContext == null)
            {
                throw new ArgumentNullException("controllerContext");
            }
            if (string.IsNullOrEmpty(actionName))
            {
                throw new ArgumentException(SRResources.Common_NullOrEmpty, "actionName");
            }
            MethodInfo methodInfo = this._selector.FindActionMethod(controllerContext, actionName);
            if (methodInfo == null)
            {
                return null;
            }
            return new ApiActionDescriptor(methodInfo, actionName, this);
        }

        public override ActionDescriptor[] GetCanonicalActions()
        {
            return (ActionDescriptor[])this.LazilyFetchCanonicalActionsCollection().Clone();
        }

        private MethodInfo[] GetAllActionMethodsFromSelector()
        {
            List<MethodInfo> list = new List<MethodInfo>();
            list.AddRange(this._selector.AliasedMethods);
            list.AddRange(this._selector.NonAliasedMethods.SelectMany(p => p));
            return list.ToArray();
        }

        private ActionDescriptor[] LazilyFetchCanonicalActionsCollection()
        {
            return LazilyFetchOrCreateDescriptors<MethodInfo, ActionDescriptor>(ref this._canonicalActionsCache, new Func<MethodInfo[]>(this.GetAllActionMethodsFromSelector), methodInfo => CreateActionDescriptor(methodInfo, this));
        }

        #region Static Fields
        private static readonly string[] deletePrefixes;
        private static readonly string[] insertPrefixes;
        private static readonly string[] updatePrefixes;
        private static readonly ConcurrentDictionary<Type, ApiControllerDescriptor> _descriptionMap;
        private static ConcurrentDictionary<Type, HashSet<Type>> _typeDescriptionProviderMap;
        #endregion

        static ApiControllerDescriptor()
        {
            deletePrefixes = new string[] { "Delete", "Remove" };
            insertPrefixes = new string[] { "Insert", "Add", "Create" };
            updatePrefixes = new string[] { "Update", "Change", "Modify" };
            _descriptionMap = new ConcurrentDictionary<Type, ApiControllerDescriptor>();
            _typeDescriptionProviderMap = new ConcurrentDictionary<Type, HashSet<Type>>();
        }

        public static TDescriptor[] LazilyFetchOrCreateDescriptors<TReflection, TDescriptor>(ref TDescriptor[] cacheLocation, Func<TReflection[]> initializer, Func<TReflection, TDescriptor> converter)
        {
            TDescriptor[] localArray = Interlocked.CompareExchange<TDescriptor[]>(ref cacheLocation, null, null);
            if (localArray != null)
            {
                return localArray;
            }
            TReflection[] localArray2 = initializer();
            List<TDescriptor> list = new List<TDescriptor>(localArray2.Length);
            for (int i = 0; i < localArray2.Length; i++)
            {
                TDescriptor item = converter(localArray2[i]);
                if (item != null)
                {
                    list.Add(item);
                }
            }
            TDescriptor[] localArray3 = list.ToArray();
            return (Interlocked.CompareExchange<TDescriptor[]>(ref cacheLocation, localArray3, null) ?? localArray3);
        }

        private static ActionDescriptor CreateActionDescriptor(MethodInfo methodInfo, ControllerDescriptor controllerDescriptor)
        {
            return new ApiActionDescriptor(methodInfo, methodInfo.Name, controllerDescriptor);
        }
    }
}
