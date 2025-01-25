using System.Collections.Immutable;
using FormCMS.Auth.DTO;
using FormCMS.Utils.IdentityExt;
using FormCMS.Cms.Services;
using FormCMS.Core.Descriptors;
using FormCMS.Utils.ResultExt;

namespace FormCMS.Auth.Services;

public class EntityPermissionService(    
    IHttpContextAccessor contextAccessor ,
    IEntityService entityService

):IEntityPermissionService
{
    public ImmutableArray<ValidFilter> List(string entityName, LoadedEntity entity, ImmutableArray<ValidFilter> filters)
    {
        if (!contextAccessor.HttpContext.GetUserId(out var userId))
        {
            throw new ResultException($"You don't have permission to read [{entityName}], not logged in");
        }
        if (contextAccessor.HttpContext.HasRole(RoleConstants.Sa))
        {
            return filters;
        }

        if (contextAccessor.HttpContext.HasClaims(AccessScope.FullAccess, entityName)
            || contextAccessor.HttpContext.HasClaims(AccessScope.FullRead, entityName))
        {
            return filters;
        }

        if (!(contextAccessor.HttpContext.HasClaims(AccessScope.RestrictedAccess, entityName)
              || contextAccessor.HttpContext.HasClaims(AccessScope.RestrictedRead, entityName)))
        {
            throw new ResultException($"You don't have permission to read [{entityName}]");
        }

        var createBy = new LoadedAttribute(TableName: entity.TableName, Constants.CreatedBy);
        return
        [
            ..filters,
            new ValidFilter(new AttributeVector("","",[],createBy),"and", 
                [new ValidConstraint(Matches.EqualsTo, [new ValidValue(userId)])])
        ];
    }

    public async Task GetOne(string entityName, string recordId)
    {
        if (!contextAccessor.HttpContext.GetUserId(out var userId))
        {
            throw new ResultException("You don't have permission to read [" + entityName + "]");
        }
        
        if (contextAccessor.HttpContext.HasRole(RoleConstants.Sa) ||
            contextAccessor.HttpContext.HasClaims(AccessScope.FullAccess, entityName) ||
            contextAccessor.HttpContext.HasClaims(AccessScope.FullRead,entityName))
        {
            return;
        }

        if (contextAccessor.HttpContext.HasClaims(AccessScope.RestrictedAccess, entityName)||
            contextAccessor.HttpContext.HasClaims(AccessScope.RestrictedAccess, entityName))
        {
            string[] attrs = [Constants.CreatedBy];
            //need to query a database to get userId in case client fake data
            var record = await entityService.SingleByIdBasic(entityName, recordId, attrs);
            if (record.TryGetValue(Constants.CreatedBy, out var createdBy) && (string)createdBy == userId)
            {
                return;
            }
            throw new ResultException(
                $"You can only access record created by you, entityName={entityName}, record id={recordId}");
        }

        throw new ResultException($"You don't have permission to save [{entityName}]");
    }

    public void Create(string entityName)
    {
        if (!(contextAccessor.HttpContext.HasRole(RoleConstants.Sa) || contextAccessor.HttpContext.HasClaims(AccessScope.FullAccess, entityName) ||
            contextAccessor.HttpContext.HasClaims(AccessScope.RestrictedAccess, entityName)))
        {
            throw new ResultException($"You don't have permission to save [{entityName}]");
        }
    }

    public async Task Change(string entityName, string recordId)
    {
        if (!contextAccessor.HttpContext.GetUserId(out var userId))
        {
            throw new ResultException("You don't have permission to read [" + entityName + "]");
        }

        if (contextAccessor.HttpContext.HasRole(RoleConstants.Sa) ||
            contextAccessor.HttpContext.HasClaims(AccessScope.FullAccess, entityName))
        {
            return;
        }

        if (contextAccessor.HttpContext.HasClaims(AccessScope.RestrictedAccess, entityName))
        {
            string[] attrs = [Constants.CreatedBy];
            //need to query database to get userId in case client fake data
            var record = await entityService.SingleByIdBasic(entityName, recordId, attrs);
            if (record.TryGetValue(Constants.CreatedBy, out var createdBy) && (string)createdBy == userId)
            {
                return;
            }
            throw new ResultException(
                    $"You can only access record created by you, entityName={entityName}, record id={recordId}");
        }

        throw new ResultException($"You don't have permission to save [{entityName}]");
    }


    public void AssignCreatedBy(Record record)
    {
        if (!contextAccessor.HttpContext.GetUserId(out var userId))
        {
            throw new ResultException("Can not assign created by, user not logged in");
        }

        record[Constants.CreatedBy] = userId;
    }
}