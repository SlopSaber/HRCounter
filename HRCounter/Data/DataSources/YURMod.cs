using System;
using System.Linq.Expressions;
using System.Reflection;
using HRCounter.Data.DataSources.Base;

namespace HRCounter.Data.DataSources;

internal sealed class YURMod : DataSource
{
    private EventInfo? _overlayUpdateEvent;
    private Delegate? _overlayUpdateHandler;

    protected override void Start()
    {
        var managerType = FindYURServiceManagerType();
        _overlayUpdateEvent = managerType?.GetEvent("OverlayUpdateAction", BindingFlags.Public | BindingFlags.Static);
        if (_overlayUpdateEvent?.EventHandlerType == null)
        {
            return;
        }

        var invokeMethod = _overlayUpdateEvent.EventHandlerType.GetMethod("Invoke");
        var parameters = invokeMethod?.GetParameters();
        if (parameters == null || parameters.Length != 1)
        {
            _overlayUpdateEvent = null;
            return;
        }

        var eventParameter = Expression.Parameter(parameters[0].ParameterType, "update");
        var callback = Expression.Call(
            Expression.Constant(this),
            nameof(OnOverlayStatusUpdate),
            Type.EmptyTypes,
            Expression.Convert(eventParameter, typeof(object)));
        _overlayUpdateHandler = Expression.Lambda(_overlayUpdateEvent.EventHandlerType, callback, eventParameter).Compile();
        _overlayUpdateEvent.AddEventHandler(null, _overlayUpdateHandler);
    }

    private static Type? FindYURServiceManagerType()
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var type = assembly.GetType("YUR.Fit.Unity.CoreServiceManager", false);
            if (type != null)
            {
                return type;
            }
        }

        return Type.GetType("YUR.Fit.Unity.CoreServiceManager, YUR.Fit.Unity", false);
    }

    private void OnOverlayStatusUpdate(object update)
    {
        if (update == null)
        {
            return;
        }

        var updateType = update.GetType();
        var heartRate = updateType.GetProperty("HeartRate")?.GetValue(update);
        var calculationMetrics = updateType.GetProperty("CalculationMetrics")?.GetValue(update);
        var estimatedHeartRate = calculationMetrics?.GetType().GetProperty("EstHeartRate")?.GetValue(calculationMetrics);
        var value = heartRate ?? estimatedHeartRate;
        if (value != null)
        {
            OnHeartRateDataReceived(Convert.ToInt32(value));
        }
    }

    protected override void Stop()
    {
        if (_overlayUpdateEvent != null && _overlayUpdateHandler != null)
        {
            _overlayUpdateEvent.RemoveEventHandler(null, _overlayUpdateHandler);
        }

        _overlayUpdateEvent = null;
        _overlayUpdateHandler = null;
    }
}
