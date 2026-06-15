using System.Diagnostics;
using System.Globalization;
using Microsoft.Extensions.Logging;
using Sdp.Resources;

namespace Sdp.View;

public static class ViewSetBuildHelper
{
    public static TView Build<TView, TTableSet>(
        TTableSet tableSet,
        string memberName,
        Func<TTableSet, TView> buildView,
        ILogger logger)
    {
        var stopwatch = Stopwatch.StartNew();

        var view = buildView(tableSet);

        stopwatch.Stop();

        logger.LogTrace(Messages.BuiltView, memberName, stopwatch.ElapsedMilliseconds);

        return view;
    }

    public static Exception InvalidViewParameterError(string parameterName, string typeName)
    {
        return new InvalidOperationException(string.Format(
            CultureInfo.CurrentCulture,
            Messages.Composite.InvalidViewParameter,
            parameterName,
            typeName));
    }

    public static Exception ViewConstructorNotFoundError(string viewTypeName, string tableSetTypeName)
    {
        return new InvalidOperationException(string.Format(
            CultureInfo.CurrentCulture,
            Messages.Composite.ViewConstructorNotFound,
            viewTypeName,
            tableSetTypeName));
    }

    public static Exception NullableViewMemberError(string parameterName, string typeName)
    {
        return new InvalidOperationException(string.Format(
            CultureInfo.CurrentCulture,
            Messages.Composite.ViewSetMemberMustBeNonNullable,
            parameterName,
            typeName));
    }

    public static Exception ViewTargetsDifferentTableSetError(
        string viewTypeName,
        string viewTableSetName,
        string managerTableSetName)
    {
        return new InvalidOperationException(string.Format(
            CultureInfo.CurrentCulture,
            Messages.Composite.ViewTargetsDifferentTableSet,
            viewTypeName,
            viewTableSetName,
            managerTableSetName));
    }

    public static Exception ViewNotPartialError(string viewTypeName)
    {
        return new InvalidOperationException(string.Format(
            CultureInfo.CurrentCulture,
            Messages.Composite.ViewMustBePartial,
            viewTypeName));
    }

    public static Exception ViewContainingTypeNotPartialError(string viewTypeName)
    {
        return new InvalidOperationException(string.Format(
            CultureInfo.CurrentCulture,
            Messages.Composite.ViewContainingTypeMustBePartial,
            viewTypeName));
    }

    public static string ViewsFailedToBuildMessage
        => Messages.ViewsFailedToBuild;
}
