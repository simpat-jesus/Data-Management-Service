// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Text.Json.Nodes;
using EdFi.DataManagementService.Core.External.Frontend;
using EdFi.DataManagementService.Core.External.Interface;
using EdFi.DataManagementService.Core.External.Model;
using EdFi.DataManagementService.Core.Model;
using EdFi.DataManagementService.Frontend.AspNetCore.Infrastructure.Extensions;

namespace EdFi.DataManagementService.Frontend.AspNetCore;

/// <summary>
/// A thin static class that converts from ASP.NET Core to the DMS facade.
/// </summary>
public static class AspNetCoreFrontend
{
    /// <summary>
    /// Takes an HttpRequest and returns a deserialized request body
    /// </summary>
    private static async Task<string?> ExtractJsonBodyFrom(HttpRequest request)
    {
        using Stream body = request.Body;
        using StreamReader bodyReader = new(body);
        var requestBodyString = await bodyReader.ReadToEndAsync();

        if (string.IsNullOrEmpty(requestBodyString))
            return null;

        return requestBodyString;
    }

    /// <summary>
    /// Takes an HttpRequest and returns a unique trace identifier
    /// </summary>
    private static TraceId ExtractTraceIdFrom(HttpRequest request)
    {
        return new TraceId(request.HttpContext.TraceIdentifier);
    }

    /// <summary>
    /// Converts an AspNetCore HttpRequest to a DMS FrontendRequest
    /// </summary>
    private static async Task<FrontendRequest> FromRequest(HttpRequest HttpRequest, string dmsPath)
    {
        return new(
            Body: await ExtractJsonBodyFrom(HttpRequest),
            Path: $"/{dmsPath}",
            QueryParameters: HttpRequest.Query.ToDictionary(x => x.Key, x => x.Value[^1] ?? ""),
            TraceId: ExtractTraceIdFrom(HttpRequest)
        );
    }

    /// <summary>
    /// Converts a DMS FrontendResponse to an AspNetCore IResult
    /// </summary>
    private static IResult ToResult(
        IFrontendResponse frontendResponse,
        HttpContext httpContext,
        string dmsPath
    )
    {
        if (frontendResponse.LocationHeaderPath != null)
        {
            string urlBeforeDmsPath = httpContext.Request.UrlWithPathSegment()[..^(dmsPath.Length + 1)];
            httpContext.Response.Headers.Append(
                "Location",
                $"{urlBeforeDmsPath}{frontendResponse.LocationHeaderPath}"
            );
        }
        foreach (var header in frontendResponse.Headers)
        {
            httpContext.Response.Headers.Append(header.Key, header.Value);
        }

        return Results.Content(
            statusCode: frontendResponse.StatusCode,
            content: frontendResponse.Body,
            contentType: "application/json",
            contentEncoding: System.Text.Encoding.UTF8
        );
    }

    /// <summary>
    /// ASP.NET Core entry point for API POST requests to DMS
    /// </summary>
    /// <param name="httpContext">The HttpContext for the request</param>
    /// <param name="apiService">The injected DMS core facade</param>
    /// <param name="dmsPath">The portion of the request path relevant to DMS</param>
    public static async Task<IResult> Upsert(HttpContext httpContext, IApiService apiService, string dmsPath)
    {
        return ToResult(
            await apiService.Upsert(await FromRequest(httpContext.Request, dmsPath)),
            httpContext,
            dmsPath
        );
    }

    /// <summary>
    /// ASP.NET Core entry point for all API GET by id requests to DMS
    /// </summary>
    public static async Task<IResult> GetById(HttpContext httpContext, IApiService apiService, string dmsPath)
    {
        return ToResult(
            await apiService.GetById(await FromRequest(httpContext.Request, dmsPath)),
            httpContext,
            dmsPath
        );
    }

    /// <summary>
    /// ASP.NET Core entry point for all API PUT requests to DMS, which are "by id"
    /// </summary>
    public static async Task<IResult> UpdateById(
        HttpContext httpContext,
        IApiService apiService,
        string dmsPath
    )
    {
        return ToResult(
            await apiService.UpdateById(await FromRequest(httpContext.Request, dmsPath)),
            httpContext,
            dmsPath
        );
    }

    /// <summary>
    /// ASP.NET Core entry point for all API DELETE requests to DMS, which are "by id"
    /// </summary>
    public static async Task<IResult> DeleteById(
        HttpContext httpContext,
        IApiService apiService,
        string dmsPath
    )
    {
        return ToResult(
            await apiService.DeleteById(await FromRequest(httpContext.Request, dmsPath)),
            httpContext,
            dmsPath
        );
    }
}
