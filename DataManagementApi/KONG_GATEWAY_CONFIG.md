# Kong Gateway Configuration for DataManagementApi

## Problem: JWT Audience Validation

**Issue**: Keycloak JWT tokens have `audience: "account"` but backend expects `audience: "kong-gateway-client"`

**Error**: `SecurityTokenInvalidAudienceException: IDX10214: Audience validation failed`

## Solutions Implemented

### 1. Multiple Audiences Support

Backend now accepts multiple audiences:
```csharp
ValidAudiences = new[]
{
    "kong-gateway-client", // Kong Gateway client
    "account",             // Keycloak default audience  
    "realm-management"     // Keycloak realm management
}
```

### 2. Configurable Audience Validation

**Production** (`appsettings.json`):
```json
{
  "Jwt": {
    "Authority": "http://localhost:8080/realms/my-realm",
    "Audience": "kong-gateway-client",
    "ValidateAudience": true
  }
}
```

**Development** (`appsettings.Development.json`):
```json
{
  "Jwt": {
    "ValidateAudience": false
  }
}
```

## Kong Gateway Configuration

### 1. JWT Plugin Configuration

```yaml
plugins:
  - name: jwt
    config:
      key_claim_name: iss
      secret_is_base64: false
      claims_to_verify:
        - exp
        - iat
      run_on_preflight: true
```

### 2. Keycloak Integration

**Option A: Configure Keycloak Client**
1. Go to Keycloak Admin Console
2. Clients → your-client → Settings
3. Set "Valid Redirect URIs" to include Kong Gateway URLs
4. Update "Audience" in Client Scopes

**Option B: Use Kong JWT Plugin**
1. Configure Kong to validate JWT from Keycloak
2. Transform audience claim if needed
3. Proxy to DataManagementApi

### 3. Frontend Configuration

Update frontend API base URL to point to Kong Gateway:

```typescript
// .env.local
NEXT_PUBLIC_API_BASE_URL=http://localhost:8000/api

// Or in production
NEXT_PUBLIC_API_BASE_URL=https://your-kong-gateway.com/api
```

## Testing

### 1. Direct Backend Testing
```bash
# Test without Kong Gateway
curl -H "Authorization: Bearer <keycloak-token>" \
     http://localhost:5100/api/users/me
```

### 2. Through Kong Gateway
```bash
# Test through Kong Gateway
curl -H "Authorization: Bearer <keycloak-token>" \
     http://localhost:8000/api/users/me
```

### 3. Debug JWT Token

Add to Program.cs for debugging:
```csharp
// See all claims in token
Console.WriteLine("JWT Debug: All claims in token:");
foreach (var claim in claimsPrincipal.Claims)
{
    Console.WriteLine($"  {claim.Type}: {claim.Value}");
}
```

## Common Issues

### 1. Audience Mismatch
- **Problem**: Token audience doesn't match backend expectation
- **Solution**: Use multiple audiences or disable validation in dev

### 2. Issuer Mismatch  
- **Problem**: Token issuer doesn't match Keycloak realm
- **Solution**: Update `Jwt:Authority` in appsettings.json

### 3. Token Forwarding
- **Problem**: Kong Gateway not forwarding JWT token
- **Solution**: Configure JWT plugin properly

## Production Deployment

### 1. Environment Variables
```bash
# Backend
JWT__AUTHORITY=https://your-keycloak.com/realms/your-realm
JWT__AUDIENCE=kong-gateway-client
JWT__VALIDATEAUDIENCE=true

# Frontend
NEXT_PUBLIC_API_BASE_URL=https://your-kong-gateway.com/api
```

### 2. Kong Gateway Setup
```yaml
# kong.yml
services:
  - name: data-management-api
    url: http://data-management-api:5100
    
routes:
  - name: api-route
    service: data-management-api
    paths:
      - /api
      
plugins:
  - name: jwt
    service: data-management-api
    config:
      key_claim_name: iss
      secret_is_base64: false
```

### 3. Security Considerations
- Always validate audience in production
- Use HTTPS for all communications
- Rotate JWT secrets regularly
- Monitor for authentication failures 