﻿using System.Reflection;

namespace MvcApi.Formatting
{
    /// <summary>
    /// Interface to determine which data members on a particular type are required.
    /// </summary>
    public interface IRequiredMemberSelector
    {
        /// <summary>
        /// Determines whether a given member is required on deserialization.
        /// </summary>
        /// <param name="member">The <see cref="T:System.Reflection.MemberInfo" /> that will be deserialized.</param>
        /// <returns><c>true</c> if <paramref name="member" /> should be treated as a required member, otherwise <c>false</c>.</returns>
        bool IsRequiredMember(MemberInfo member);
    }
}
