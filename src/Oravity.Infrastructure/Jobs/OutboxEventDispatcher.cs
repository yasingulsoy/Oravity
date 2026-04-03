using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using Oravity.SharedKernel.Events;

namespace Oravity.Infrastructure.Jobs;

/// <summary>
/// Outbox event tipini JSON payload'dan deserialize edip MediatR'a iletir.
/// Dispatcher mapping tablosu:
///   "AppointmentCreated"     → AppointmentCreatedEvent
///   "AppointmentCompleted"   → AppointmentCompletedEvent
///   "PaymentReceived"        → PaymentReceivedEvent
///   "TreatmentItemCompleted" → TreatmentItemCompletedEvent
/// </summary>
public class OutboxEventDispatcher
{
    private readonly IPublisher _publisher;
    private readonly ILogger<OutboxEventDispatcher> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy        = JsonNamingPolicy.CamelCase
    };

    public OutboxEventDispatcher(IPublisher publisher, ILogger<OutboxEventDispatcher> logger)
    {
        _publisher = publisher;
        _logger    = logger;
    }

    /// <summary>
    /// JSON payload'ı event tipine göre deserialize edip MediatR üzerinden yayınlar.
    /// Bilinmeyen event tipi için <see cref="InvalidOperationException"/> fırlatır.
    /// </summary>
    public async Task Dispatch(string eventType, string jsonPayload, CancellationToken ct = default)
    {
        _logger.LogDebug("OutboxEventDispatcher: işleniyor → {EventType}", eventType);

        INotification notification = eventType switch
        {
            "AppointmentCreated" => Deserialize<AppointmentCreatedEvent>(jsonPayload),
            "AppointmentCompleted" => Deserialize<AppointmentCompletedEvent>(jsonPayload),
            "PaymentReceived" => Deserialize<PaymentReceivedEvent>(jsonPayload),
            "TreatmentItemCompleted" => Deserialize<TreatmentItemCompletedEvent>(jsonPayload),

            // İleride eklenecek
            // "StockDeductionRequired" => Deserialize<StockDeductionEvent>(jsonPayload),
            // "UtsReportRequired"      => Deserialize<UtsReportEvent>(jsonPayload),

            _ => throw new InvalidOperationException(
                $"Bilinmeyen outbox event tipi: '{eventType}'. Dispatcher mapping'e eklenmelidir.")
        };

        await _publisher.Publish(notification, ct);
        _logger.LogDebug("OutboxEventDispatcher: yayınlandı → {EventType}", eventType);
    }

    private static T Deserialize<T>(string json) =>
        JsonSerializer.Deserialize<T>(json, JsonOpts)
        ?? throw new InvalidOperationException($"Payload deserialize edilemedi: {typeof(T).Name}");
}
