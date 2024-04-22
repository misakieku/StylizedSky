using System;

namespace Misaki.StylizedSky
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    class CopyFilterAttribute : Attribute
    {
        public enum Filter
        {
            Exclude = 1,        // field or backing field will not be checked by CopyTo test (white listing)
            CheckContent = 2    // check the content of object value instead of doing a simple reference check
        }
#if UNITY_EDITOR
        public readonly Filter filter;
#endif

        protected CopyFilterAttribute(Filter test)
        {
#if UNITY_EDITOR
            this.filter = test;
#endif
        }
    }

    sealed class ExcludeCopyAttribute : CopyFilterAttribute
    {
        public ExcludeCopyAttribute()
            : base(Filter.Exclude)
        { }
    }

    sealed class ValueCopyAttribute : CopyFilterAttribute
    {
        public ValueCopyAttribute()
            : base(Filter.CheckContent)
        { }
    }
}
