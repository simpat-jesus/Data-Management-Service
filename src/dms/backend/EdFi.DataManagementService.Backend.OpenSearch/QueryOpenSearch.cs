// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using EdFi.DataManagementService.Core.External.Backend;
using EdFi.DataManagementService.Core.External.Model;
using Microsoft.Extensions.Logging;
using OpenSearch.Client;
using OpenSearch.Net;

namespace EdFi.DataManagementService.Backend.OpenSearch;

/// <summary>
/// Queries OpenSearch for documents. Example query DSL usage:
///
///  {
///    "from": 100
///    "size": 20,
///    "query": {
///      "bool": {
///        "must": [
///          {
///            "match_phrase": {
///              "edfidoc.schoolYearDescription": "Year 2025"
///            }
///          },
///          {
///            "match_phrase": {
///              "edfidoc.currentSchoolYear": false
///            }
///          },
///          {
///            "bool": {
///              "should": [
///                {
///                  "terms": {
///                    "securityelements.EducationOrganization": {
///                      "index": "edfi.dms.educationorganizationhierarchytermslookup",
///                      "id": "6001010",
///                      "path": "hierarchy.array"
///                    }
///                  }
///                }
///              ]
///            }
///          }
///        ]
///      }
///    }
///  }
///
/// </summary>
public static partial class QueryOpenSearch
{
    [GeneratedRegex(@"^\$\.")]
    public static partial Regex JsonPathPrefixRegex();

    // Imposes a consistent sort order across queries without specifying a sort field
    private static JsonArray SortDirective()
    {
        return new(new JsonObject { ["_doc"] = new JsonObject { ["order"] = "asc" } });
    }

    /// <summary>
    /// Returns OpenSearch index name from the given ResourceInfo.
    /// OpenSearch indexes are required to be lowercase only, with no periods.
    /// </summary>
    private static string IndexFromResourceInfo(ResourceInfo resourceInfo)
    {
        return $@"{resourceInfo.ProjectName.Value}${resourceInfo.ResourceName.Value}"
            .ToLower()
            .Replace(".", "-");
    }

    private static string QueryFieldFrom(JsonPath documentPath)
    {
        return JsonPathPrefixRegex().Replace(documentPath.Value, "");
    }

    public static async Task<QueryResult> QueryDocuments(
        IOpenSearchClient client,
        IQueryRequest queryRequest,
        ILogger logger
    )
    {
        logger.LogDebug("Entering QueryOpenSearch.Query - {TraceId}", queryRequest.TraceId.Value);

        try
        {
            string indexName = IndexFromResourceInfo(queryRequest.ResourceInfo);

            // Build API client requested filters
            JsonArray terms = [];
            foreach (QueryElement queryElement in queryRequest.QueryElements)
            {
                // If just one document path, it's a pure AND
                if (queryElement.DocumentPaths.Length == 1)
                {
                    terms.Add(
                        new JsonObject
                        {
                            ["match_phrase"] = new JsonObject
                            {
                                [$@"edfidoc.{QueryFieldFrom(queryElement.DocumentPaths[0])}"] =
                                    queryElement.Value,
                            },
                        }
                    );
                }
                else
                {
                    // If more than one document path, it's an OR
                    JsonObject[] possibleTerms = queryElement
                        .DocumentPaths.Select(documentPath => new JsonObject
                        {
                            ["match_phrase"] = new JsonObject
                            {
                                [$@"edfidoc.{QueryFieldFrom(documentPath)}"] = queryElement.Value,
                            },
                        })
                        .ToArray();

                    terms.Add(
                        new JsonObject
                        {
                            ["bool"] = new JsonObject { ["should"] = new JsonArray(possibleTerms) },
                        }
                    );
                }
            }

            IEnumerable<JsonObject?> authorizationFilters = queryRequest
                .AuthorizationStrategyEvaluators.Select(strategyEvaluator =>
                {
                    IEnumerable<JsonObject> namespaceFilters = strategyEvaluator
                        .Filters.Where(f => f.FilterPath == "Namespace")
                        .Select(filter => new JsonObject
                        {
                            ["match_phrase"] = new JsonObject
                            {
                                [$"securityelements.{filter.FilterPath}"] = filter.Value,
                            },
                        });

                    IEnumerable<JsonObject> edOrgFilters = strategyEvaluator
                        .Filters.Where(f => f.FilterPath == "EducationOrganization")
                        .Select(filter => new JsonObject
                        {
                            ["terms"] = new JsonObject
                            {
                                [$"securityelements.{filter.FilterPath}"] = new JsonObject
                                {
                                    ["index"] = "edfi.dms.educationorganizationhierarchytermslookup",
                                    ["id"] = filter.Value,
                                    ["path"] = "hierarchy.array",
                                },
                            },
                        });

                    JsonObject[] strategyFilters = namespaceFilters.Union(edOrgFilters).ToArray();

                    if (strategyFilters.Any())
                    {
                        // Use the appropriate boolean operator based on the strategy
                        string boolOperator =
                            strategyEvaluator.Operator == FilterOperator.Or ? "should" : "must";

                        return new JsonObject
                        {
                            ["bool"] = new JsonObject { [boolOperator] = new JsonArray(strategyFilters) },
                        };
                    }

                    return null;
                })
                .Where(filter => filter != null);

            foreach (JsonObject? filter in authorizationFilters)
            {
                terms.Add(filter);
            }

            JsonObject query = new()
            {
                ["query"] = new JsonObject { ["bool"] = new JsonObject { ["must"] = terms } },
                ["sort"] = SortDirective(),
            };

            // Add in PaginationParameters if any
            if (queryRequest.PaginationParameters.Limit != null)
            {
                query.Add(new("size", queryRequest.PaginationParameters.Limit));
            }

            if (queryRequest.PaginationParameters.Offset != null)
            {
                query.Add(new("from", queryRequest.PaginationParameters.Offset));
            }

            logger.LogDebug("Query - {TraceId} - {Query}", queryRequest.TraceId.Value, query.ToJsonString());

            BytesResponse response = await client.Http.PostAsync<BytesResponse>(
                $"/{indexName}/_search",
                d => d.Body(query.ToJsonString())
            );

            if (response.Success)
            {
                JsonNode hits = JsonSerializer.Deserialize<JsonNode>(response.Body)!["hits"]!;

                if (hits is null)
                {
                    return new QueryResult.QuerySuccess(new JsonArray(), 0);
                }

                int totalCount = hits!["total"]!["value"]!.GetValue<int>();

                JsonNode[] documents = hits!["hits"]!
                    .AsArray()
                    // DeepClone() so they can be placed in a new JsonArray
                    .Select(node => node!["_source"]!["edfidoc"]!.DeepClone())!
                    .ToArray()!;

                return new QueryResult.QuerySuccess(new JsonArray(documents), totalCount);
            }

            logger.LogCritical(
                "Unsuccessful OpenSearch Response - {TraceId} - {DebugInformation}",
                queryRequest.TraceId.Value,
                response.DebugInformation
            );
            return new QueryResult.UnknownFailure("Unknown Failure");
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Uncaught Query failure - {TraceId}", queryRequest.TraceId.Value);
            return new QueryResult.UnknownFailure("Unknown Failure");
        }
    }
}
