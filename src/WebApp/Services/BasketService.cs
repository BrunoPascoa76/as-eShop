using eShop.Basket.API.Grpc;
using GrpcBasketItem = eShop.Basket.API.Grpc.BasketItem;
using GrpcBasketClient = eShop.Basket.API.Grpc.Basket.BasketClient;
using System.Diagnostics;
using Grpc.Core;
using OpenTelemetry.Instrumentation.GrpcNetClient;

namespace eShop.WebApp.Services;

public class BasketService(GrpcBasketClient basketClient)
{
    public async Task<IReadOnlyCollection<BasketQuantity>> GetBasketAsync()
    {
        var currentActivity = Activity.Current;
        currentActivity?.AddEvent(new ActivityEvent("GetBasketAsyncCall"));

        var metadata = new Metadata();
        if (currentActivity != null)
        {
            metadata.Add("traceparent", currentActivity.TraceId.ToHexString());
            if (!string.IsNullOrEmpty(currentActivity.TraceStateString))
            {
                metadata.Add("tracestate", currentActivity.TraceStateString);
            }
        }

        var result = await basketClient.GetBasketAsync(new());
        currentActivity?.AddEvent(new ActivityEvent("GetBasketAsyncCallResponse"));

        return MapToBasket(result);
    }

    public async Task DeleteBasketAsync()
    {
        await basketClient.DeleteBasketAsync(new DeleteBasketRequest());
    }

    public async Task UpdateBasketAsync(IReadOnlyCollection<BasketQuantity> basket)
    {
        var currentActivity = Activity.Current;
        var updatePayload = new UpdateBasketRequest();

        foreach (var item in basket)
        {
            var updateItem = new GrpcBasketItem
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
            };
            updatePayload.Items.Add(updateItem);
        }
        var metadata = new Metadata();
        if (currentActivity != null)
        {
            metadata.Add("traceparent", currentActivity.TraceId.ToHexString());
            if (!string.IsNullOrEmpty(currentActivity.TraceStateString))
            {
                metadata.Add("tracestate", currentActivity.TraceStateString);
            }
        }
        currentActivity?.AddEvent(new ActivityEvent("UpdateBasketAsyncCall"));
        await basketClient.UpdateBasketAsync(updatePayload);
    }

    private static List<BasketQuantity> MapToBasket(CustomerBasketResponse response)
    {
        var result = new List<BasketQuantity>();
        foreach (var item in response.Items)
        {
            result.Add(new BasketQuantity(item.ProductId, item.Quantity));
        }

        return result;
    }
}

public record BasketQuantity(int ProductId, int Quantity);
