// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Text.Json;
using System.Text.Json.Nodes;
using EdFi.DataManagementService.Backend.Postgresql.Model;
using EdFi.DataManagementService.Core.External.Backend;
using EdFi.DataManagementService.Core.External.Model;
using Npgsql;

namespace EdFi.DataManagementService.Backend.Postgresql.Operation;

/// <summary>
/// A facade of all the DB interactions. Any action requiring SQL statement execution should be here.
/// Connections and transactions are managed by the caller.
/// Exceptions are handled by the caller.
/// </summary>
public interface ISqlAction
{
    public Task<DocumentSummary?> FindDocumentEdfiDocByDocumentUuid(
        DocumentUuid documentUuid,
        string resourceName,
        PartitionKey partitionKey,
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        TraceId traceId
    );

    public Task<Document?> FindDocumentByReferentialId(
        ReferentialId referentialId,
        PartitionKey partitionKey,
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        TraceId traceId
    );

    public Task<JsonArray> GetAllDocumentsByResourceName(
        string resourceName,
        IQueryRequest queryRequest,
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        TraceId traceId
    );

    public Task<int> GetTotalDocumentsForResourceName(
        string resourceName,
        IQueryRequest queryRequest,
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        TraceId traceId
    );

    public Task<long> InsertDocumentAndAlias(
        Document document,
        int referentialPartitionKey,
        Guid referentialId,
        NpgsqlConnection connection,
        NpgsqlTransaction transaction
    );

    public Task<int> UpdateDocumentEdfiDoc(
        int documentPartitionKey,
        Guid documentUuid,
        JsonElement edfiDoc,
        JsonElement securityElements,
        JsonElement? studentSchoolAuthorizationEdOrgIds,
        JsonElement? studentEdOrgResponsibilityAuthorizationIds,
        JsonElement? contactStudentSchoolAuthorizationEdOrgIds,
        JsonElement? staffEducationOrganizationAuthorizationEdOrgIds,
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        TraceId traceId
    );

    public Task<UpdateDocumentValidationResult> UpdateDocumentValidation(
        DocumentUuid documentUuid,
        PartitionKey documentPartitionKey,
        ReferentialId referentialId,
        PartitionKey referentialPartitionKey,
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        TraceId traceId
    );

    public Task<long> InsertAlias(
        Alias alias,
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        TraceId traceId
    );

    public Task<int> UpdateAliasReferentialIdByDocumentUuid(
        short referentialPartitionKey,
        Guid referentialId,
        short documentPartitionKey,
        Guid documentUuid,
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        TraceId traceId
    );

    public Task<Guid[]> InsertReferences(
        BulkReferences bulkReferences,
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        TraceId traceId
    );

    public Task<int> DeleteDocumentByDocumentUuid(
        PartitionKey documentPartitionKey,
        DocumentUuid documentUuid,
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        TraceId traceId
    );

    public Task<string[]> FindReferencingResourceNamesByDocumentUuid(
        DocumentUuid documentUuid,
        PartitionKey documentPartitionKey,
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        TraceId traceId
    );

    public Task<Document[]> FindReferencingDocumentsByDocumentId(
        long documentId,
        short documentPartitionKey,
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        TraceId traceId
    );

    public Task<int> InsertEducationOrganizationHierarchy(
        string projectName,
        string resourceName,
        long educationOrganizationId,
        long? parentEducationOrganizationId,
        long documentId,
        short documentPartitionKey,
        NpgsqlConnection connection,
        NpgsqlTransaction transaction
    );

    public Task<int> UpdateEducationOrganizationHierarchy(
        string projectName,
        string resourceName,
        long educationOrganizationId,
        long? parentEducationOrganizationId,
        long documentId,
        short documentPartitionKey,
        NpgsqlConnection connection,
        NpgsqlTransaction transaction
    );

    public Task<int> DeleteEducationOrganizationHierarchy(
        string projectName,
        string resourceName,
        long documentId,
        short documentPartitionKey,
        NpgsqlConnection connection,
        NpgsqlTransaction transaction
    );

    public Task<long[]> GetAncestorEducationOrganizationIds(
        long[] educationOrganizationIds,
        NpgsqlConnection connection,
        NpgsqlTransaction transaction
    );

    public Task<JsonElement?> GetStudentSchoolAuthorizationEducationOrganizationIds(
        string studentUniqueId,
        NpgsqlConnection connection,
        NpgsqlTransaction transaction
    );

    public Task<JsonElement?> GetStudentEdOrgResponsibilityAuthorizationIds(
        string studentUniqueId,
        NpgsqlConnection connection,
        NpgsqlTransaction transaction
    );

    public Task<JsonElement?> GetContactStudentSchoolAuthorizationEducationOrganizationIds(
        string contactUniqueId,
        NpgsqlConnection connection,
        NpgsqlTransaction transaction
    );

    public Task<JsonElement?> GetStaffEducationOrganizationAuthorizationEdOrgIds(
        string staffUniqueId,
        NpgsqlConnection connection,
        NpgsqlTransaction transaction
    );

    public Task<int> InsertStudentSecurableDocument(
        string studentUniqueId,
        long documentId,
        short documentPartitionKey,
        NpgsqlConnection connection,
        NpgsqlTransaction transaction
    );

    public Task<int> InsertContactSecurableDocument(
        string contactUniqueId,
        long documentId,
        short documentPartitionKey,
        NpgsqlConnection connection,
        NpgsqlTransaction transaction
    );

    public Task<int> InsertStaffSecurableDocument(
        string staffUniqueId,
        long documentId,
        short documentPartitionKey,
        NpgsqlConnection connection,
        NpgsqlTransaction transaction
    );

    public Task<int> UpdateStudentSecurableDocument(
        string studentUniqueId,
        long documentId,
        short documentPartitionKey,
        NpgsqlConnection connection,
        NpgsqlTransaction transaction
    );

    public Task<int> UpdateContactSecurableDocument(
        string contactUniqueId,
        long documentId,
        short documentPartitionKey,
        NpgsqlConnection connection,
        NpgsqlTransaction transaction
    );

    public Task<int> UpdateStaffSecurableDocument(
        string staffUniqueId,
        long documentId,
        short documentPartitionKey,
        NpgsqlConnection connection,
        NpgsqlTransaction transaction
    );
}
