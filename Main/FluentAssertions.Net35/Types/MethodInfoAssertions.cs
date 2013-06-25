using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;

namespace FluentAssertions.Types
{
    /// <summary>
    /// Contains assertions for the <see cref="MethodInfo"/> objects returned by the parent <see cref="MethodInfoSelector"/>.
    /// </summary>
    [DebuggerNonUserCode]
    public class MethodInfoAssertions : ReferenceTypeAssertions<MethodInfo, MethodInfoAssertions>
    {
        private readonly bool isAssertingSingleMethod;

        /// <summary>
        /// Initializes a new instance of the <see cref="MethodInfoAssertions"/> class.
        /// </summary>
        /// <param name="methodInfo">The method to assert.</param>
        public MethodInfoAssertions(MethodInfo methodInfo)
        {
            isAssertingSingleMethod = true;
            SubjectMethods = new[] { methodInfo };
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MethodInfoAssertions"/> class.
        /// </summary>
        /// <param name="methodInfo">The methods to assert.</param>
        public MethodInfoAssertions(IEnumerable<MethodInfo> methods)
        {
            SubjectMethods = methods;
        }

        /// <summary>
        /// Gets the object which value is being asserted.
        /// </summary>
        public IEnumerable<MethodInfo> SubjectMethods { get; private set; }

        /// <summary>
        /// Asserts that the selected methods are virtual.
        /// </summary>
        /// <param name="reason">
        /// A formatted phrase as is supported by <see cref="string.Format(string,object[])" /> explaining why the assertion 
        /// is needed. If the phrase does not start with the word <i>because</i>, it is prepended automatically.
        /// </param>
        /// <param name="reasonArgs">
        /// Zero or more objects to format using the placeholders in <see cref="reason" />.
        /// </param>
        public AndConstraint<MethodInfoAssertions> BeVirtual(string reason = "", params object[] reasonArgs)
        {
            IEnumerable<MethodInfo> nonVirtualMethods = GetAllNonVirtualMethodsFromSelection();

            string failureMessage = isAssertingSingleMethod
                ? "Expected method " + GetDescriptionsFor(new[] { SubjectMethods.Single() }) +
                    " to be virtual{reason}, but it is not virtual."
                : "Expected all selected methods to be virtual{reason}, but the following methods are" + " not virtual:\r\n" +
                    GetDescriptionsFor(nonVirtualMethods);

            Execute.Assertion
                .ForCondition(!nonVirtualMethods.Any())
                .BecauseOf(reason, reasonArgs)
                .FailWith(failureMessage);

            return new AndConstraint<MethodInfoAssertions>(this);
        }

        private MethodInfo[] GetAllNonVirtualMethodsFromSelection()
        {
            var query =
                    from method in SubjectMethods
                    where !method.IsVirtual || method.IsFinal
                    select method;

            return query.ToArray();
        }

        /// <summary>
        /// Asserts that the selected methods are decorated with the specified <typeparamref name="TAttribute"/>.
        /// </summary>
        /// <param name="reason">
        /// A formatted phrase as is supported by <see cref="string.Format(string,object[])" /> explaining why the assertion 
        /// is needed. If the phrase does not start with the word <i>because</i>, it is prepended automatically.
        /// </param>
        /// <param name="reasonArgs">
        /// Zero or more objects to format using the placeholders in <see cref="reason" />.
        /// </param>
        public AndConstraint<MethodInfoAssertions> BeDecoratedWith<TAttribute>(string reason = "", params object[] reasonArgs)
        {
            IEnumerable<MethodInfo> methodsWithoutAttribute = GetMethodsWithout<TAttribute>(attr => true);

            string failureMessage = isAssertingSingleMethod
                ? "Expected method " + GetDescriptionsFor(new[] { SubjectMethods.Single() }) +
                    " to be decorated with {0}{reason}, but that attribute was not found."
                : "Expected all selected methods to be decorated with {0}{reason}, but the" + " following methods are not:\r\n" +
                    GetDescriptionsFor(methodsWithoutAttribute);

            Execute.Assertion
                .ForCondition(!methodsWithoutAttribute.Any())
                .BecauseOf(reason, reasonArgs)
                .FailWith(failureMessage, typeof(TAttribute));

            return new AndConstraint<MethodInfoAssertions>(this);
        }

        /// <summary>
        /// Asserts that the selected methods are decorated with an attribute of type <typeparamref name="TAttribute"/>
        /// that matches the specified <paramref name="isMatchingAttributePredicate"/>.
        /// </summary>
        /// <param name="isMatchingAttributePredicate">
        /// The predicate that the attribute must match.
        /// </param>
        /// <param name="reason">
        /// A formatted phrase as is supported by <see cref="string.Format(string,object[])" /> explaining why the assertion 
        /// is needed. If the phrase does not start with the word <i>because</i>, it is prepended automatically.
        /// </param>
        /// <param name="reasonArgs">
        /// Zero or more objects to format using the placeholders in <see cref="reason" />.
        /// </param>
        public AndConstraint<MethodInfoAssertions> BeDecoratedWith<TAttribute>(
            Func<TAttribute, bool> isMatchingAttributePredicate,  string reason = "", params object[] reasonArgs)
        {
            IEnumerable<MethodInfo> methodsWithoutAttribute = GetMethodsWithout<TAttribute>(isMatchingAttributePredicate);

            string failureMessage = isAssertingSingleMethod
                ? "Expected method " + GetDescriptionsFor(new[] { SubjectMethods.Single() }) +
                    " to be decorated with {0}{reason}, but that attribute was not found."
                : "Expected all selected methods to be decorated with {0}{reason}, but the" + " following methods are not:\r\n" +
                    GetDescriptionsFor(methodsWithoutAttribute);

            Execute.Assertion
                .ForCondition(!methodsWithoutAttribute.Any())
                .BecauseOf(reason, reasonArgs)
                .FailWith(failureMessage, typeof(TAttribute));

            return new AndConstraint<MethodInfoAssertions>(this);
        }

        private MethodInfo[] GetMethodsWithout<TAttribute>(Func<TAttribute, bool> isMatchingPredicate)
        {
            return SubjectMethods.Where(method => !IsDecoratedWith(method, isMatchingPredicate)).ToArray();
        }

        private static bool IsDecoratedWith<TAttribute>(MethodInfo method, Func<TAttribute, bool> isMatchingPredicate)
        {
            return method.GetCustomAttributes(false).OfType<TAttribute>().Any(isMatchingPredicate);
        }

        private static string GetDescriptionsFor(IEnumerable<MethodInfo> methods)
        {
            return string.Join(Environment.NewLine, methods.Select(GetDescriptionFor).ToArray());
        }

        private static string GetDescriptionFor(MethodInfo method)
        {
            string returnTypeName;
#if !WINRT
            returnTypeName = method.ReturnType.Name;
#else
            returnTypeName = method.ReturnType.GetTypeInfo().Name;
#endif

            return string.Format("{0} {1}.{2}", returnTypeName, method.DeclaringType, method.Name);
        }

        /// <summary>
        /// Returns the type of the subject the assertion applies on.
        /// </summary>
        protected override string Context
        {
            get { return "method"; }
        }
    }
}
