using System.Diagnostics.CodeAnalysis;
using eShop.Basket.API.Repositories;
using eShop.Basket.API.Extensions;
using eShop.Basket.API.Model;
using System.Security.Cryptography;
using System.Diagnostics;

namespace eShop.Basket.API.Grpc;

public class BasketService(
    IBasketRepository repository,
    ILogger<BasketService> logger) : Basket.BasketBase
{
    private static readonly ActivitySource _activitySource = new("eShop.BasketAPI");
    [AllowAnonymous]
    public override async Task<CustomerBasketResponse> GetBasket(GetBasketRequest request, ServerCallContext context)
    {
        var traceParent = context.RequestHeaders.GetValue("traceparent");
        var traceState = context.RequestHeaders.GetValue("tracestate");

        ActivityContext.TryParse(traceParent, traceState, out var activityContext);

        using var activity = _activitySource.StartActivity("AddToCart", ActivityKind.Server, activityContext);

        activity.AddEvent(new ActivityEvent("BasketService.GetBasket"));

        var userId = context.GetUserIdentity();
        if (string.IsNullOrEmpty(userId))
        {
            return new();
        }

        activity.SetTag("userId", "HASH_"+HashString(userId));

        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("Begin GetBasketById call from method {Method} for basket id HASH_{Id}", context.Method, HashString(userId));
        }

        using var databaseActivity = _activitySource.StartActivity("Redis.GetBasket", ActivityKind.Client, activity.Context);

        var data = await repository.GetBasketAsync(userId);

        if (data is not null)
        {
            return MapToCustomerBasketResponse(data);
        }

        return new();
    }

    public override async Task<CustomerBasketResponse> UpdateBasket(UpdateBasketRequest request, ServerCallContext context)
    {
        var traceParent = context.RequestHeaders.GetValue("traceparent");
        var traceState = context.RequestHeaders.GetValue("tracestate");

        ActivityContext.TryParse(traceParent, traceState, out var activityContext);

        using var activity = _activitySource.StartActivity("AddToCart", ActivityKind.Server, activityContext);

        activity.AddEvent(new ActivityEvent("BasketService.UpdateBasket"));

        var userId = context.GetUserIdentity();
        if (string.IsNullOrEmpty(userId))
        {
            ThrowNotAuthenticated();
        }

        activity.SetTag("userId", "HASH_"+HashString(userId));

        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("Begin UpdateBasket call from method {Method} for basket id {Id}", context.Method, "HASH_" + HashString(userId));
        }

        var customerBasket = MapToCustomerBasket(userId, request);
        var response = await repository.UpdateBasketAsync(customerBasket);
        if (response is null)
        {
            ThrowBasketDoesNotExist(userId); //not setting status here because I believe it is set on the other side already
        }

        return MapToCustomerBasketResponse(response);
    }

    public override async Task<DeleteBasketResponse> DeleteBasket(DeleteBasketRequest request, ServerCallContext context)
    {
        var userId = context.GetUserIdentity();
        if (string.IsNullOrEmpty(userId))
        {
            ThrowNotAuthenticated();
        }

        await repository.DeleteBasketAsync(userId);
        return new();
    }

    [DoesNotReturn]
    private static void ThrowNotAuthenticated() => throw new RpcException(new Status(StatusCode.Unauthenticated, "The caller is not authenticated."));

    [DoesNotReturn]
    private static void ThrowBasketDoesNotExist(string userId) => throw new RpcException(new Status(StatusCode.NotFound, $"Basket with buyer id HASH_{HashString(userId)} does not exist"));

    private static CustomerBasketResponse MapToCustomerBasketResponse(CustomerBasket customerBasket)
    {
        var activity= Activity.Current;
        var response = new CustomerBasketResponse();

        foreach (var item in customerBasket.Items)
        {
            response.Items.Add(new BasketItem()
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
            });
        }

        activity?.SetTag("quantity", response.Items.Count);
        activity?.SetTag("items",response.Items);

        return response;
    }

    private static CustomerBasket MapToCustomerBasket(string userId, UpdateBasketRequest customerBasketRequest)
    {
        var response = new CustomerBasket
        {
            BuyerId = userId
        };

        foreach (var item in customerBasketRequest.Items)
        {
            response.Items.Add(new()
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
            });
        }

        return response;
    }

    public static string HashString(string input)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
            return Convert.ToBase64String(hashBytes);
        }
    }
}
