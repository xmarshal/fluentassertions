﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions.Common;
using FluentAssertions.Execution;

namespace FluentAssertions.Specialized;

/// <summary>
/// Contains a number of methods to assert that an asynchronous method yields the expected result.
/// </summary>
[DebuggerNonUserCode]
public class AsyncFunctionAssertions<TTask, TAssertions> : DelegateAssertionsBase<Func<TTask>, TAssertions>
    where TTask : Task
    where TAssertions : AsyncFunctionAssertions<TTask, TAssertions>
{
    [Obsolete("This class is intended as base class. This ctor is accidentally public and will be removed in Version 7.")]
    public AsyncFunctionAssertions(Func<TTask> subject, IExtractExceptions extractor)
        : this(subject, extractor, new Clock())
    {
    }

    [Obsolete("This class is intended as base class. This ctor is accidentally public and will be made protected in Version 7.")]
    public AsyncFunctionAssertions(Func<TTask> subject, IExtractExceptions extractor, IClock clock)
        : base(subject, extractor, clock)
    {
    }

    protected override string Identifier => "async function";

    /// <summary>
    /// Asserts that the current <typeparamref name="TTask"/> will complete within the specified time.
    /// </summary>
    /// <param name="timeSpan">The allowed time span for the operation.</param>
    /// <param name="because">
    /// A formatted phrase as is supported by <see cref="string.Format(string,object[])" /> explaining why the assertion
    /// is needed. If the phrase does not start with the word <i>because</i>, it is prepended automatically.
    /// </param>
    /// <param name="becauseArgs">
    /// Zero or more objects to format using the placeholders in <paramref name="because" />.
    /// </param>
    public async Task<AndConstraint<TAssertions>> CompleteWithinAsync(
        TimeSpan timeSpan, string because = "", params object[] becauseArgs)
    {
        bool success = Execute.Assertion
            .ForCondition(Subject is not null)
            .BecauseOf(because, becauseArgs)
            .FailWith("Expected {context:task} to complete within {0}{reason}, but found <null>.", timeSpan);

        if (success)
        {
            (TTask task, TimeSpan remainingTime) = InvokeWithTimer(timeSpan);

            success = Execute.Assertion
                .ForCondition(remainingTime >= TimeSpan.Zero)
                .BecauseOf(because, becauseArgs)
                .FailWith("Expected {context:task} to complete within {0}{reason}.", timeSpan);

            if (success)
            {
                bool completesWithinTimeout = await CompletesWithinTimeoutAsync(task, remainingTime);

                Execute.Assertion
                    .ForCondition(completesWithinTimeout)
                    .BecauseOf(because, becauseArgs)
                    .FailWith("Expected {context:task} to complete within {0}{reason}.", timeSpan);
            }
        }

        return new AndConstraint<TAssertions>((TAssertions)this);
    }

    /// <summary>
    /// Asserts that the current <typeparamref name="TTask"/> will not complete within the specified time.
    /// </summary>
    /// <param name="timeSpan">The allowed time span for the operation.</param>
    /// <param name="because">
    /// A formatted phrase as is supported by <see cref="string.Format(string,object[])" /> explaining why the assertion
    /// is needed. If the phrase does not start with the word <i>because</i>, it is prepended automatically.
    /// </param>
    /// <param name="becauseArgs">
    /// Zero or more objects to format using the placeholders in <paramref name="because" />.
    /// </param>
    public async Task<AndConstraint<TAssertions>> NotCompleteWithinAsync(
        TimeSpan timeSpan, string because = "", params object[] becauseArgs)
    {
        bool success = Execute.Assertion
            .ForCondition(Subject is not null)
            .BecauseOf(because, becauseArgs)
            .FailWith("Did not expect {context:task} to complete within {0}{reason}, but found <null>.", timeSpan);

        if (success)
        {
            (Task task, TimeSpan remainingTime) = InvokeWithTimer(timeSpan);

            if (remainingTime >= TimeSpan.Zero)
            {
                bool completesWithinTimeout = await CompletesWithinTimeoutAsync(task, remainingTime);

                Execute.Assertion
                    .ForCondition(!completesWithinTimeout)
                    .BecauseOf(because, becauseArgs)
                    .FailWith("Did not expect {context:task} to complete within {0}{reason}.", timeSpan);
            }
        }

        return new AndConstraint<TAssertions>((TAssertions)this);
    }

    /// <summary>
    /// Asserts that the current <see cref="Func{Task}"/> throws an exception of the exact type <typeparamref name="TException"/> (and not a derived exception type).
    /// </summary>
    /// <typeparam name="TException">The type of exception expected to be thrown.</typeparam>
    /// <param name="because">
    /// A formatted phrase as is supported by <see cref="string.Format(string,object[])" /> explaining why the assertion
    /// is needed. If the phrase does not start with the word <i>because</i>, it is prepended automatically.
    /// </param>
    /// <param name="becauseArgs">
    /// Zero or more objects to format using the placeholders in <paramref name="because" />.
    /// </param>
    /// <returns>
    /// Returns an object that allows asserting additional members of the thrown exception.
    /// </returns>
    public async Task<ExceptionAssertions<TException>> ThrowExactlyAsync<TException>(string because = "",
        params object[] becauseArgs)
        where TException : Exception
    {
        Type expectedType = typeof(TException);

        bool success = Execute.Assertion
            .ForCondition(Subject is not null)
            .BecauseOf(because, becauseArgs)
            .FailWith("Expected {context} to throw exactly {0}{reason}, but found <null>.", expectedType);

        if (success)
        {
            Exception exception = await InvokeWithInterceptionAsync(Subject);

            Execute.Assertion
                .ForCondition(exception is not null)
                .BecauseOf(because, becauseArgs)
                .FailWith("Expected {0}{reason}, but no exception was thrown.", expectedType);

            exception.Should().BeOfType(expectedType, because, becauseArgs);

            return new ExceptionAssertions<TException>(new[] { exception as TException });
        }

        return new ExceptionAssertions<TException>(Array.Empty<TException>());
    }

    /// <summary>
    /// Asserts that the current <see cref="Func{Task}"/> throws an exception of type <typeparamref name="TException"/>.
    /// </summary>
    /// <typeparam name="TException">The type of exception expected to be thrown.</typeparam>
    /// <param name="because">
    /// A formatted phrase as is supported by <see cref="string.Format(string,object[])" /> explaining why the assertion
    /// is needed. If the phrase does not start with the word <i>because</i>, it is prepended automatically.
    /// </param>
    /// <param name="becauseArgs">
    /// Zero or more objects to format using the placeholders in <paramref name="because" />.
    /// </param>
    public async Task<ExceptionAssertions<TException>> ThrowAsync<TException>(string because = "",
        params object[] becauseArgs)
        where TException : Exception
    {
        bool success = Execute.Assertion
            .ForCondition(Subject is not null)
            .BecauseOf(because, becauseArgs)
            .FailWith("Expected {context} to throw {0}{reason}, but found <null>.", typeof(TException));

        if (success)
        {
            Exception exception = await InvokeWithInterceptionAsync(Subject);
            return ThrowInternal<TException>(exception, because, becauseArgs);
        }

        return new ExceptionAssertions<TException>(Array.Empty<TException>());
    }

    /// <summary>
    /// Asserts that the current <see cref="Func{Task}"/> throws an exception of type <typeparamref name="TException"/>
    /// within a specific timeout.
    /// </summary>
    /// <typeparam name="TException">The type of exception expected to be thrown.</typeparam>
    /// <param name="timeSpan">The allowed time span for the operation.</param>
    /// <param name="because">
    /// A formatted phrase as is supported by <see cref="string.Format(string,object[])" /> explaining why the assertion
    /// is needed. If the phrase does not start with the word <i>because</i>, it is prepended automatically.
    /// </param>
    /// <param name="becauseArgs">
    /// Zero or more objects to format using the placeholders in <paramref name="because" />.
    /// </param>
    public async Task<ExceptionAssertions<TException>> ThrowWithinAsync<TException>(
        TimeSpan timeSpan, string because = "", params object[] becauseArgs)
        where TException : Exception
    {
        bool success = Execute.Assertion
            .ForCondition(Subject is not null)
            .BecauseOf(because, becauseArgs)
            .FailWith("Expected {context} to throw {0} within {1}{reason}, but found <null>.",
                typeof(TException), timeSpan);

        if (success)
        {
            Exception caughtException = await InvokeWithInterceptionAsync(timeSpan);
            return AssertThrows<TException>(caughtException, timeSpan, because, becauseArgs);
        }

        return new ExceptionAssertions<TException>(Array.Empty<TException>());
    }

    private ExceptionAssertions<TException> AssertThrows<TException>(
        Exception exception, TimeSpan timeSpan, string because, object[] becauseArgs)
        where TException : Exception
    {
        TException[] expectedExceptions = Extractor.OfType<TException>(exception).ToArray();

        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .WithExpectation("Expected a <{0}> to be thrown within {1}{reason}, ",
                typeof(TException), timeSpan)
            .ForCondition(exception is not null)
            .FailWith("but no exception was thrown.")
            .Then
            .ForCondition(expectedExceptions.Length > 0)
            .FailWith("but found <{0}>:" + Environment.NewLine + "{1}.",
                exception?.GetType(),
                exception)
            .Then
            .ClearExpectation();

        return new ExceptionAssertions<TException>(expectedExceptions);
    }

    private async Task<Exception> InvokeWithInterceptionAsync(TimeSpan timeout)
    {
        try
        {
            // For the duration of this nested invocation, configure CallerIdentifier
            // to match the contents of the subject rather than our own call site.
            //
            //   Func<Task> action = async () => await subject.Should().BeSomething();
            //   await action.Should().ThrowAsync<Exception>();
            //
            // If an assertion failure occurs, we want the message to talk about "subject"
            // not "await action".
            using (CallerIdentifier.OnlyOneFluentAssertionScopeOnCallStack()
                       ? CallerIdentifier.OverrideStackSearchUsingCurrentScope()
                       : default)
            {
                (TTask task, TimeSpan remainingTime) = InvokeWithTimer(timeout);
                if (remainingTime < TimeSpan.Zero)
                {
                    // timeout reached without exception
                    return null;
                }

                if (task.IsFaulted)
                {
                    // exception in synchronous portion
                    return task.Exception!.GetBaseException();
                }

                // Start monitoring the task regarding timeout.
                // Here we do not need to know whether the task completes (successfully) in timeout
                // or does not complete. We are only interested in the exception which is thrown, not returned.
                // So, we can ignore the result.
                _ = await CompletesWithinTimeoutAsync(task, remainingTime);
            }

            return null;
        }
        catch (Exception exception)
        {
            return exception;
        }
    }

    /// <summary>
    /// Asserts that the current <see cref="Func{Task}"/> does not throw any exception.
    /// </summary>
    /// <param name="because">
    /// A formatted phrase as is supported by <see cref="string.Format(string,object[])" /> explaining why the assertion
    /// is needed. If the phrase does not start with the word <i>because</i>, it is prepended automatically.
    /// </param>
    /// <param name="becauseArgs">
    /// Zero or more objects to format using the placeholders in <paramref name="because" />.
    /// </param>
    public async Task<AndConstraint<TAssertions>> NotThrowAsync(string because = "", params object[] becauseArgs)
    {
        bool success = Execute.Assertion
            .ForCondition(Subject is not null)
            .BecauseOf(because, becauseArgs)
            .FailWith("Expected {context} not to throw{reason}, but found <null>.");

        if (success)
        {
            try
            {
                await Subject.Invoke();
            }
            catch (Exception exception)
            {
                return NotThrowInternal(exception, because, becauseArgs);
            }
        }

        return new AndConstraint<TAssertions>((TAssertions)this);
    }

    /// <summary>
    /// Asserts that the current <see cref="Func{Task}"/> does not throw an exception of type <typeparamref name="TException"/>.
    /// </summary>
    /// <typeparam name="TException">The type of exception expected to not be thrown.</typeparam>
    /// <param name="because">
    /// A formatted phrase as is supported by <see cref="string.Format(string,object[])" /> explaining why the assertion
    /// is needed. If the phrase does not start with the word <i>because</i>, it is prepended automatically.
    /// </param>
    /// <param name="becauseArgs">
    /// Zero or more objects to format using the placeholders in <paramref name="because" />.
    /// </param>
    public async Task<AndConstraint<TAssertions>> NotThrowAsync<TException>(string because = "", params object[] becauseArgs)
        where TException : Exception
    {
        bool success = Execute.Assertion
            .ForCondition(Subject is not null)
            .BecauseOf(because, becauseArgs)
            .FailWith("Expected {context} not to throw{reason}, but found <null>.");

        if (success)
        {
            try
            {
                await Subject.Invoke();
            }
            catch (Exception exception)
            {
                return NotThrowInternal<TException>(exception, because, becauseArgs);
            }
        }

        return new AndConstraint<TAssertions>((TAssertions)this);
    }

    /// <summary>
    /// Asserts that the current <see cref="Func{T}"/> stops throwing any exception
    /// after a specified amount of time.
    /// </summary>
    /// <remarks>
    /// The <see cref="Func{T}"/> is invoked. If it raises an exception,
    /// the invocation is repeated until it either stops raising any exceptions
    /// or the specified wait time is exceeded.
    /// </remarks>
    /// <param name="waitTime">
    /// The time after which the <see cref="Func{T}"/> should have stopped throwing any exception.
    /// </param>
    /// <param name="pollInterval">
    /// The time between subsequent invocations of the <see cref="Func{T}"/>.
    /// </param>
    /// <param name="because">
    /// A formatted phrase as is supported by <see cref="string.Format(string,object[])" /> explaining why the assertion
    /// is needed. If the phrase does not start with the word <i>because</i>, it is prepended automatically.
    /// </param>
    /// <param name="becauseArgs">
    /// Zero or more objects to format using the placeholders in <paramref name="because" />.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="waitTime"/> or <paramref name="pollInterval"/> are negative.</exception>
    public Task<AndConstraint<TAssertions>> NotThrowAfterAsync(TimeSpan waitTime, TimeSpan pollInterval, string because = "",
        params object[] becauseArgs)
    {
        Guard.ThrowIfArgumentIsNegative(waitTime);
        Guard.ThrowIfArgumentIsNegative(pollInterval);

        bool success = Execute.Assertion
            .ForCondition(Subject is not null)
            .BecauseOf(because, becauseArgs)
            .FailWith("Expected {context} not to throw any exceptions after {0}{reason}, but found <null>.", waitTime);

        if (success)
        {
            return AssertionTaskAsync();

            async Task<AndConstraint<TAssertions>> AssertionTaskAsync()
            {
                TimeSpan? invocationEndTime = null;
                Exception exception = null;
                ITimer timer = Clock.StartTimer();

                while (invocationEndTime is null || invocationEndTime < waitTime)
                {
                    exception = await InvokeWithInterceptionAsync(Subject);

                    if (exception is null)
                    {
                        return new AndConstraint<TAssertions>((TAssertions)this);
                    }

                    await Clock.DelayAsync(pollInterval, CancellationToken.None);
                    invocationEndTime = timer.Elapsed;
                }

                Execute.Assertion
                    .BecauseOf(because, becauseArgs)
                    .FailWith("Did not expect any exceptions after {0}{reason}, but found {1}.", waitTime, exception);

                return new AndConstraint<TAssertions>((TAssertions)this);
            }
        }

        return Task.FromResult(new AndConstraint<TAssertions>((TAssertions)this));
    }

    /// <summary>
    ///     Invokes the subject and measures the sync execution time.
    /// </summary>
    private protected (TTask result, TimeSpan remainingTime) InvokeWithTimer(TimeSpan timeSpan)
    {
        ITimer timer = Clock.StartTimer();
        TTask result = Subject.Invoke();
        TimeSpan remainingTime = timeSpan - timer.Elapsed;

        return (result, remainingTime);
    }

    /// <summary>
    ///     Monitors the specified task whether it completes withing the remaining time span.
    /// </summary>
    private protected async Task<bool> CompletesWithinTimeoutAsync(Task target, TimeSpan remainingTime)
    {
        using var delayCancellationTokenSource = new CancellationTokenSource();

        Task completedTask =
            await Task.WhenAny(target, Clock.DelayAsync(remainingTime, delayCancellationTokenSource.Token));

        if (completedTask.IsFaulted)
        {
            // Throw the inner exception.
            await completedTask;
        }

        if (completedTask != target)
        {
            // The monitored task did not complete.
            return false;
        }

        // The monitored task is completed, we shall cancel the clock.
        delayCancellationTokenSource.Cancel();
        return true;
    }

    private static async Task<Exception> InvokeWithInterceptionAsync(Func<Task> action)
    {
        try
        {
            // For the duration of this nested invocation, configure CallerIdentifier
            // to match the contents of the subject rather than our own call site.
            //
            //   Func<Task> action = async () => await subject.Should().BeSomething();
            //   await action.Should().ThrowAsync<Exception>();
            //
            // If an assertion failure occurs, we want the message to talk about "subject"
            // not "await action".
            using (CallerIdentifier.OnlyOneFluentAssertionScopeOnCallStack()
                       ? CallerIdentifier.OverrideStackSearchUsingCurrentScope()
                       : default)
            {
                await action();
            }

            return null;
        }
        catch (Exception exception)
        {
            return exception;
        }
    }
}
