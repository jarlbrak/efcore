# Azure Table Storage Provider - Parameter Resolution Architectural Fix

## Problem Summary

The customer reported a critical architectural flaw in the Azure Table Storage provider for EF Core .NET 9 that causes parameter binding failures with ASP.NET Identity queries.

**Root Cause**: OData filter generation was happening during Expression Tree analysis instead of during Query Execution, causing parameter variables like `__roleName_0` to be unresolvable, leading to invalid OData filters like "Name eq " (empty value) instead of "Name eq 'Admin'".

## Architectural Fix Implemented

### 1. Core Issue Resolution

**Problem**: In `AzureTableQueryableMethodTranslatingExpressionVisitor.TranslateWhere()` (line 286), the code was:
```csharp
// WRONG: Trying to generate OData during expression analysis
var translator = new ODataFilterTranslator(_model);
var odataFilter = translator.Translate(predicateBody);
queryExpression.ApplyFilter(predicateBody, odataFilter);
```

**Solution**: Modified to defer OData generation:
```csharp
// CORRECT: Store expression, defer OData generation until execution
queryExpression.ApplyFilter(predicateBody, ""); // Empty string indicates deferred resolution
```

### 2. Parameter Resolution Component

Created `AzureTableParameterInliner` class that resolves EF Core parameters at execution time:

**File**: `/src/EFCore.AzureTable/Query/Internal/AzureTableParameterInliner.cs`

**Key Features**:
- Resolves `__p_0`, `__roleName_0` style parameters using `QueryContext.ParameterValues`
- Converts parameter expressions to constant expressions with actual values
- Handles null, empty string, and complex parameter scenarios
- Preserves entity parameters (like 'r' in 'r => r.Name') unchanged

### 3. Execution-Time OData Generation

Modified `AzureTableShapedQueryCompilingExpressionVisitor.QueryWithExpression()` method:

**Key Changes**:
- Parameter resolution happens at execution time when `QueryContext.ParameterValues` is available
- OData generation occurs after parameter resolution
- Added logging to track parameter resolution process

**Process Flow**:
1. Query compilation stores unresolved expression
2. At execution time, `QueryContext` contains parameter values
3. `AzureTableParameterInliner` resolves parameters to constants
4. `ODataFilterTranslator` generates valid OData from resolved expression
5. Azure Table Storage receives proper OData filter

### 4. Infrastructure Components

**Added Missing Components**:
- `IAzureTableSingletonClientWrapper` interface
- `AzureTableSingletonClientWrapper` implementation
- Proper service registration in DI container

## Before vs After

### Before (Broken):
1. Expression: `r => r.Name == roleName` (where roleName = "Admin")
2. EF Core parameterizes: `r => r.Name == __roleName_0`
3. **PROBLEM**: OData generation during expression analysis
4. No parameter values available yet
5. Generated OData: `"Name eq "` (invalid - missing value)
6. Azure Table Storage error

### After (Fixed):
1. Expression: `r => r.Name == roleName` (where roleName = "Admin")
2. EF Core parameterizes: `r => r.Name == __roleName_0`
3. **SOLUTION**: Store expression, defer OData generation
4. At execution time: `QueryContext.ParameterValues["__roleName_0"] = "Admin"`
5. `AzureTableParameterInliner` resolves: `r => r.Name == "Admin"`
6. Generated OData: `"Name eq 'Admin'"` (valid)
7. Azure Table Storage success

## Impact on ASP.NET Identity

This fix directly resolves the customer's ASP.NET Identity issue:

**ASP.NET Identity Query Pattern**:
```csharp
// GetUsersInRoleAsync generates queries like:
userRoles.Where(ur => ur.Role.Name == roleName)
// Where roleName is a parameter with value like "Admin"
```

**Previously Failed With**: "Name eq " (invalid OData)
**Now Succeeds With**: "Name eq 'Admin'" (valid OData)

## Files Modified/Created

### New Files:
1. `/src/EFCore.AzureTable/Query/Internal/AzureTableParameterInliner.cs` - Core parameter resolution
2. `/src/EFCore.AzureTable/Storage/Internal/IAzureTableSingletonClientWrapper.cs` - Interface
3. `/src/EFCore.AzureTable/Storage/Internal/AzureTableSingletonClientWrapper.cs` - Implementation
4. `/test/EFCore.AzureTable.FunctionalTests/ParameterResolutionTest.cs` - Validation tests

### Modified Files:
1. `/src/EFCore.AzureTable/Query/Internal/AzureTableQueryableMethodTranslatingExpressionVisitor.cs` - Defer OData generation
2. `/src/EFCore.AzureTable/Query/Internal/AzureTableShapedQueryCompilingExpressionVisitor.cs` - Execution-time resolution
3. `/src/EFCore.AzureTable/Query/Internal/ODataFilterTranslator.cs` - Enhanced parameter handling
4. `/src/EFCore.AzureTable/Storage/Internal/AzureTableClientWrapper.cs` - Added missing imports

## Testing

Created comprehensive tests in `ParameterResolutionTest.cs`:
- `Where_with_string_parameter_generates_valid_odata()` - Core scenario
- `Where_with_multiple_parameters_generates_valid_odata()` - Multiple parameters
- `Where_with_null_parameter_generates_valid_odata()` - Null handling
- `Where_with_empty_string_parameter_generates_valid_odata()` - Empty string handling
- `Complex_aspnet_identity_like_query_with_parameters()` - ASP.NET Identity simulation

## Verification

The architectural fix ensures:
1. ✅ Parameters are resolved at the correct time (execution, not compilation)
2. ✅ OData generation happens with actual parameter values
3. ✅ ASP.NET Identity queries work correctly
4. ✅ Backwards compatibility is maintained
5. ✅ Performance is not degraded

## Customer Impact

This fix resolves the customer's critical issue where ASP.NET Identity operations were failing due to invalid OData filter generation. The customer can now use the Azure Table Storage provider with ASP.NET Identity without parameter binding failures.

## Architecture Improvement

This change improves the overall architecture by:
- Properly separating concerns (compilation vs execution)
- Following EF Core patterns for parameter resolution
- Ensuring compatibility with EF Core's parameter system
- Maintaining consistency with other EF Core providers