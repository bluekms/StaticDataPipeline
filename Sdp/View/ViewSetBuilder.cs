using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Sdp.Resources;

namespace Sdp.View;

internal static class ViewSetBuilder
{
    internal static TViewSet Build<TTableSet, TViewSet>(TTableSet tableSet, ILogger logger)
        where TTableSet : class
        where TViewSet : class
    {
        var ctors = typeof(TViewSet).GetConstructors();
        if (ctors.Length != 1)
        {
            throw new InvalidOperationException(string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.ViewSetMustHaveSingleConstructor,
                typeof(TViewSet).Name));
        }

        var ctor = ctors[0];
        var parameters = ctor.GetParameters();
        var args = new object?[parameters.Length];
        var errors = new List<Exception>();
        var nullabilityContext = new NullabilityInfoContext();

        for (var i = 0; i < parameters.Length; i++)
        {
            try
            {
                args[i] = CreateView(parameters[i], tableSet, nullabilityContext, logger);
            }
            catch (Exception ex)
            {
                errors.Add(ex);
            }
        }

        if (errors.Count > 0)
        {
            throw new AggregateException(Messages.ViewsFailedToBuild, errors);
        }

        return (TViewSet)ctor.Invoke(args);
    }

    private static object CreateView<TTableSet>(
        ParameterInfo param,
        TTableSet tableSet,
        NullabilityInfoContext nullabilityContext,
        ILogger logger)
        where TTableSet : class
    {
        var viewType = param.ParameterType;

        var nullability = nullabilityContext.Create(param);
        if (nullability.WriteState != NullabilityState.NotNull)
        {
            throw new InvalidOperationException(string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.ViewSetMemberMustBeNonNullable,
                param.Name!,
                viewType.Name));
        }

        if (!typeof(IStaticDataView).IsAssignableFrom(viewType))
        {
            throw new InvalidOperationException(string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.InvalidViewParameter,
                param.Name!,
                viewType.Name));
        }

        var viewCtor = viewType.GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            [typeof(TTableSet)]);

        if (viewCtor is null)
        {
            throw new InvalidOperationException(string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.ViewConstructorNotFound,
                viewType.Name,
                typeof(TTableSet).Name));
        }

        var stopwatch = Stopwatch.StartNew();

        object view;
        try
        {
            view = viewCtor.Invoke([tableSet]);
        }
        catch (TargetInvocationException tie)
        {
            throw tie.InnerException ?? tie;
        }

        stopwatch.Stop();
        logger.LogTrace(
            Messages.BuiltView,
            param.Name,
            stopwatch.ElapsedMilliseconds);

        return view;
    }
}
